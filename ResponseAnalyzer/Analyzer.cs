using Framework.Luis;
using Framework.Misc;
using FuzzyString;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.ResponseAnalyzer
{
    /// <summary>
    /// Examine input with regard to user responses.
    /// </summary>
    public sealed class Analyzer
    {
        private readonly LuisRecognizer _classifier;
        private readonly double _threshold;
        private Task<RecognizerResult> _classification;
        private string _response;

        private readonly List<string> _blackList;
        private static readonly string BlackListPath = RootPath.GetRootPath("Framework", "ResponseAnalyzer", "Blacklist.json");

        private static readonly Dictionary<Language, string> Configs = new Dictionary<Language, string> {
            { Language.English, "en" },
            { Language.Deutsch, "de" }
        };


        /// <summary>
        /// Creates the analyzer.
        /// </summary>
        /// <param name="config">The configuration of the bot</param>
        /// <param name="lang">Defines the language.</param>
        /// <param name="threshold">The default threshold for Luis classification.</param>
        /// <exception cref="ArgumentException">unsupported language.</exception>
        public Analyzer(IConfiguration config, Language lang, double threshold = 0.7)
        {
            if (!Configs.ContainsKey(lang))
                throw new ArgumentException("Your Language is not supported.");

            _blackList = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(BlackListPath))[Configs[lang]];

            var map = config.GetSection("ResponseAnalyzer").GetSection(Configs[lang]).Get<Dictionary<string, object>>();
            LuisServiceDefinition lsd = JsonConvert.DeserializeObject<LuisServiceDefinition>(JsonConvert.SerializeObject(map));
            _classifier = new LuisRecognizer(lsd.GetLuisService(), lsd.GetPredictOpts());
            _threshold = threshold;
        }


        /// <summary>
        /// Recognize a new input.
        /// </summary>
        /// <param name="turnContext">The bot context with input.</param>
        public void Recognize(ITurnContext turnContext)
        {
            _response = turnContext.Activity.Text;
            _classification = _classifier.RecognizeAsync(turnContext, default(CancellationToken));
        }

        /// <summary>
        /// Classifies wheter a user entered yes or no.
        /// </summary>
        /// <returns>Null, true or false.</returns>
        public async Task<bool?> IsYesOrNo()
        {
            var classification = await _classification;
            double? score = classification.Intents["YesOrNo"].Score;
            if (score == null || score < _threshold)
            {
                return null;
            }

            if (!classification.Entities.GetEntities(false).TryGetValue("Indicator", out List<JToken> indicators))
            {
                // No indicators
                return null;
            }
            if (indicators.Count != 1)
            {
                // Ambigous indicators
                return null;
            }
            return indicators[0].ToObject<string[]>()[0] == "yes";
        }

        /// <summary>
        /// Classifies wheter a user requested a cancellation.
        /// </summary>
        /// <returns>The indicator.</returns>
        public async Task<bool> IsCancel()
        {
            var classification = await _classification;
            return classification.Intents["Cancel"].Score >= _threshold;
        }



        #region Options
        /// <summary>
        /// Classifies wheter objects have been mentioned in the response.
        /// </summary>
        /// <typeparam name="T">The type of objects.</typeparam>
        /// <param name="objects">The objects.</param>
        /// <param name="objToValues">A function which maps an object to a list of string values to be searched for.</param>
        /// <param name="matchAllValues">Indicates wheter all of the string values of an object have to be matched to generate a hit or not.</param>
        /// <returns>A list of the mentioned objects.</returns>
        /// <see cref="BlackLists"/>
        public List<T> Mentioned<T>(List<T> objects, Func<T, List<string>> objToValues, bool matchAllValues = false)
        {
            List<Tuple<T, float>> result = new List<Tuple<T, float>>();

            var split = Regex.Matches(_response.Replace('-', ' '), "[A-Za-z0-9äöüÄÖÜß_\\-\\.]+");
            var tokens = split.Select(t => t.Value).Where(t => !_blackList.Any(b => b.ToLower() == t.ToLower())).Distinct().ToList();

            float max = 0;

            foreach (var obj in objects)
            {
                var vals = objToValues(obj).SelectMany(s => s.Split('-')).Distinct().ToList();
                float val;

                if ((val = StringMentioned(tokens, vals, matchAllValues)) > 0)
                {
                    result.Add(new Tuple<T, float>(obj, val));
                    max = Math.Max(max, val);
                }
            }

            return result.FindAll(t => t.Item2 == max).ConvertAll(t => t.Item1);
        }

        private float StringMentioned(List<string> tokens, List<string> stringsToMatch, bool matchAllValues)
        {
            float max = 0;
            foreach (string str in stringsToMatch)
            {
                if (string.IsNullOrWhiteSpace(str) || _blackList.Any(b => b.ToLower() == str.ToLower()))
                    continue;

                float match = tokens.ConvertAll(s => AreSimilar(str, s)).Sum();


                if (match <= 0 && matchAllValues)
                    return 0;
                max += match;
            }

            return max;

        }

        /// <summary>
        /// Examines wheter a user referenced an option.
        /// </summary>
        /// <param name="possibleOptions">All possible options.</param>
        /// <returns>The mentioned options.</returns>
        public List<string> HasOptions(IEnumerable<string> possibleOptions)
        {
            List<string> options = new List<string>();
            foreach (string option in possibleOptions)
                if (HasOption(option))
                    options.Add(option);
            return options;
        }

        private bool HasOption(string option)
        {
            foreach (string part in Regex.Split(_response, "\\s+"))
            {
                if (AreSimilar(option, part) > 0)
                    return true;
            }
            return false;
        }

        private float AreSimilar(string option, string part)
        {
            if (option == null || part == null)
                return 0;

            option = option.ToLower();
            part = part.ToLower();

            if (option.Length > 3 && part.Length > 3 && option.Contains(part))
                return 1;


            bool result = option.Length > 3 && part.Length > 3 && ContainsContained(option, part);
            result = result || option.ApproximatelyEquals(part, FuzzyStringComparisonTolerance.Normal, FuzzyStringComparisonOptions.UseSorensenDiceDistance) && option.ApproximatelyEquals(part, FuzzyStringComparisonTolerance.Strong, FuzzyStringComparisonOptions.UseHammingDistance);

            if (result)
            {
                // Levensthein:
                int distance = Levensthein(part, option);
                float normalized = distance / (float)Math.Max(option.Length, part.Length);
                result = normalized < 0.4;
            }


#if DEBUG
            if (result)
            {
                Console.WriteLine($"DEBUG: Matched {option}, {part}");
            }
#endif
            return result ? 0.5f : 0;
        }



        private static int Levensthein(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
                return m;
            if (m == 0)
                return n;
            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }


            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        private bool ContainsContained(string option, string part) => option.Contains(part) || part.Contains(option);
        #endregion
    }
}

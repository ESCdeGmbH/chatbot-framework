using Framework.Misc;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.DialogAnalyzer
{
    public class RegexQuestionAnalyzer : QuestionAnalyzer
    {
        private List<Tuple<QuestionType, double>> _result;

        private static readonly Dictionary<Language, string> Maps = new Dictionary<Language, string> {
            { Language.English, "EN" },
            { Language.Deutsch, "DE" }
        };

        private static readonly List<Language> Configs = new List<Language> {
            Language.Deutsch
        };

        private Dictionary<string, List<string>> _map;

        /// <summary>
        /// Creates the analyzer.
        /// </summary>
        /// <param name="lang">Defines the language.</param>
        /// <exception cref="ArgumentException">unsupported language.</exception>
        public RegexQuestionAnalyzer(Language lang)
        {
            if (!Configs.Contains(lang))
                throw new ArgumentException("Your Language is not supported.");
            _map = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(MiscExtensions.LoadEmbeddedResource($"Framework.DialogAnalyzer.QuestionTypes_{Maps[lang]}.json"));
        }
        public override Task<List<Tuple<QuestionType, double>>> GetQuestionTypes()
        {
            return _result == null ? Task.FromResult(new List<Tuple<QuestionType, double>>()) : Task.FromResult(new List<Tuple<QuestionType, double>>(_result));
        }

        public override void Recognize(ITurnContext turnContext)
        {
            _result = new List<Tuple<QuestionType, double>>();
            string text = turnContext.Activity.Text?.ToLower();
            if (text == null)
            {
                _result = null;
                return;
            }

            CheckStart(_result, text, QuestionType.When);
            CheckContains(_result, text, QuestionType.When);

            CheckStart(_result, text, QuestionType.Where);
            CheckContains(_result, text, QuestionType.Where);

            CheckStart(_result, text, QuestionType.Who);
            CheckContains(_result, text, QuestionType.Who);

            CheckStart(_result, text, QuestionType.HowLong);

            CheckStart(_result, text, QuestionType.HowMany);

            CheckStart(_result, text, QuestionType.How);
            CheckContains(_result, text, QuestionType.How);

            CheckStart(_result, text, QuestionType.What);
        }

        private void CheckStart(List<Tuple<QuestionType, double>> result, string text, QuestionType question)
        {
            if (result.Any())
                return;
            string key = Enum.GetName(typeof(QuestionType), question).ToLower();
            if (!_map.ContainsKey(key))
                return;
            var vals = _map[key];
            if (vals.Any(h => text.StartsWith($"{h }")))
                result.Add(new Tuple<QuestionType, double>(question, 1));
        }


        private void CheckContains(List<Tuple<QuestionType, double>> result, string text, QuestionType question)
        {
            if (result.Any())
                return;
            string key = Enum.GetName(typeof(QuestionType), question).ToLower() + "_contains";
            if (!_map.ContainsKey(key))
                return;
            var vals = _map[key];
            if (vals.Any(h => text.Contains(h)))
                result.Add(new Tuple<QuestionType, double>(question, 1));
        }
    }
}

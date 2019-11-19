using Framework.Luis;
using Framework.Misc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.QuestionAnalyzer
{
    /// <summary>
    /// Examine input with regard to questions.
    /// </summary>
    public sealed class Analyzer : AnalyzerBase
    {
        private readonly LuisRecognizer _classifier;
        private readonly double _threshold;
        private Task<RecognizerResult> _classification;
        private string _response;

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
        public Analyzer(IConfiguration config, Language lang, double threshold)
        {
            if (!Configs.ContainsKey(lang))
                throw new ArgumentException("Your Language is not supported.");
            var map = config.GetSection("QuestionAnalyzer").GetSection(Configs[lang]).Get<Dictionary<string, object>>();
            LuisServiceDefinition lsd = JsonConvert.DeserializeObject<LuisServiceDefinition>(JsonConvert.SerializeObject(map));
            _classifier = new LuisRecognizer(lsd.GetLuisService(), lsd.GetPredictOpts());
            _threshold = threshold;
        }


        public override void Recognize(ITurnContext turnContext)
        {
            _response = turnContext.Activity.Text;
            _classification = _classifier.RecognizeAsync(turnContext, default(CancellationToken));
        }

        /// <summary>
        /// Retrieve the found question types.
        /// </summary>
        /// <returns>The found question types with their probability.</returns>
        public override async Task<List<Tuple<QuestionType, double>>> GetQuestionTypes()
        {
            var classification = await _classification;
            List<Tuple<QuestionType, double>> questionTypesOverThreshold = new List<Tuple<QuestionType, double>>();

            foreach (string qt in classification.Intents.Keys)
            {
                double? score = classification.Intents[qt].Score;
                if ((score != null) && (score >= _threshold))
                    questionTypesOverThreshold.Add(Enum.Parse<QuestionType>(qt), (double)score);
            }
            return questionTypesOverThreshold;
        }




    }
}

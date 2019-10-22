using Framework.Misc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Luis
{
    public class CombinedLuisRecognizer
    {
        private LuisRecognizer _withCorrection;
        private LuisRecognizer _withoutCorrection;

        private RecognizerResult _resultWithCorrection;
        private RecognizerResult _resultWithoutCorrection;

        public CombinedLuisRecognizer(LuisServiceDefinition lsd, bool trySpellcheck = true)
        {
            _withoutCorrection = new LuisRecognizer(lsd.GetLuisService(), new LuisPredictionOptions() { IncludeAllIntents = true });
            if (trySpellcheck && lsd.SpellCheckerKey != null)
                _withCorrection = new LuisRecognizer(lsd.GetLuisService(), lsd.GetPredictOpts());
        }

        public async Task Recognize(ITurnContext context, CancellationToken cancellationToken)
        {
            _resultWithCorrection = await _withCorrection?.RecognizeAsync(context, cancellationToken);
            _resultWithoutCorrection = await _withoutCorrection.RecognizeAsync(context, cancellationToken);
        }

        /// <summary>
        /// The result of classification with highest score included.
        /// </summary>
        public RecognizerResult GetResult()
        {
            if (_resultWithoutCorrection == null)
                return null;

            var topIntentWithoutCorrection = _resultWithoutCorrection.GetTopScoringIntent();
            if (_resultWithCorrection != null)
            {
                var topIntentWithCorrections = _resultWithCorrection?.GetTopScoringIntent();
                if (topIntentWithoutCorrection.score < (topIntentWithCorrections?.score ?? double.MinValue))
                    return _resultWithCorrection;
            }
            return _resultWithoutCorrection;
        }
        /// <summary>
        /// The detected entities.
        /// </summary>
        /// <param name="cleanup">Indicator for cleanup (e.g. remove match to group if you are asking for resource group).</param>
        /// <returns>All detected entities by entity-definitions.</returns>
        public Dictionary<string, List<JToken>> GetEntities(bool cleanup = true)
        {
            if (_resultWithCorrection == null)
                return _resultWithoutCorrection?.Entities?.GetEntities(cleanup) ?? new Dictionary<string, List<JToken>>();

            var corrected = _resultWithCorrection?.Entities?.GetEntities(cleanup) ?? new Dictionary<string, List<JToken>>();
            var passed = _resultWithoutCorrection?.Entities?.GetEntities(cleanup) ?? new Dictionary<string, List<JToken>>();
            Dictionary<string, List<JToken>> result = new Dictionary<string, List<JToken>>(passed);

            foreach (var kv in corrected)
            {
                var val1 = result.GetValueOrDefault(kv.Key, new List<JToken>());
                var val2 = kv.Value;

                var cleaned = val1.With(val2).Distinct(new Compare()).ToList();

                result[kv.Key] = cleaned;
            }

            return result;
        }

        private class Compare : IEqualityComparer<JToken>
        {
            public bool Equals(JToken x, JToken y) => x?.ToString() == y?.ToString();

            public int GetHashCode(JToken obj) => obj?.ToString()?.GetHashCode() ?? 0;
        }
    }
}

using Framework.Dialogs;
using Framework.Luis;
using Framework.Misc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Framework
{
    public abstract partial class Bot<S, B, D> : ActivityHandler, IBot, IBot4Dialog where B : IBot4Dialog where S : BotServices where D : BaseDialog<B, S>
    {
        /// <summary>
        /// Luis instance with spell checker.
        /// </summary>
        protected LuisRecognizer _withCorrections;
        /// <summary>
        /// Luis instance without spell checker.
        /// </summary>
        protected LuisRecognizer _withoutCorrections;

        /// <summary>
        /// Luis result with spell checker.
        /// </summary>
        protected RecognizerResult ResultsWithCorrection { private get; set; }
        /// <summary>
        /// Luis result without spell checker.
        /// </summary>
        protected RecognizerResult ResultsWithoutCorrection { private get; set; }

        /// <summary>
        /// The aggregated result.
        /// </summary>
        public RecognizerResult Result => GetResult();

        private RecognizerResult GetResult()
        {
            if (ResultsWithCorrection == null)
                return null;

            var topIntent = ResultsWithCorrection?.GetTopScoringIntent();
            if (ResultsWithoutCorrection != null && topIntent != null)
            {
                var topIntentWithoutCorrections = ResultsWithoutCorrection?.GetTopScoringIntent();
                if (topIntent.Value.score < (topIntentWithoutCorrections?.score ?? double.MinValue))
                    return ResultsWithoutCorrection;
            }
            return ResultsWithCorrection;
        }

        /// <summary>
        /// The detected entities.
        /// </summary>
        /// <param name="cleanup">Indicator for cleanup (e.g. remove match to group if you are asking for resource group).</param>
        /// <returns>All detected entities by entity-definitions.</returns>
        public Dictionary<string, List<JToken>> GetEntities(bool cleanup = true)
        {
            if (ResultsWithoutCorrection == null)
                return ResultsWithCorrection?.Entities?.GetEntities(cleanup) ?? new Dictionary<string, List<JToken>>();

            var corrected = ResultsWithCorrection?.Entities?.GetEntities(cleanup) ?? new Dictionary<string, List<JToken>>();
            var passed = ResultsWithoutCorrection?.Entities?.GetEntities(cleanup) ?? new Dictionary<string, List<JToken>>();
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

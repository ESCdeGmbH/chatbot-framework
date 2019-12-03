using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Framework.Classifier.LuisExtensions;

namespace Framework.Classifier
{
    /// <summary>
    /// Defines an implementation of <see cref="IClassifier"/> using Luis.
    /// </summary>
    public sealed class LuisClassifier : IClassifier
    {
        private LuisRecognizer _withCorrection;
        private LuisRecognizer _withoutCorrection;

        private RecognizerResult _resultWithCorrection;
        private RecognizerResult _resultWithoutCorrection;

        /// <summary>
        /// Creates a classifier with Luis.
        /// </summary>
        /// <param name="lsd">Definition of a Luis service.</param>
        /// <param name="trySpellcheck">Indicates if spellchecker is on or off.</param>
        public LuisClassifier(LuisServiceDefinition lsd, bool trySpellcheck = true)
        {
            _withoutCorrection = new LuisRecognizer(lsd.GetLuisService(), new LuisPredictionOptions() { IncludeAllIntents = true });
            if (trySpellcheck && lsd.SpellCheckerKey != null)
                _withCorrection = new LuisRecognizer(lsd.GetLuisService(), lsd.GetPredictOpts());
        }
        
        public async Task Recognize(ITurnContext context, CancellationToken cancellationToken)
        {
            _resultWithCorrection = await (_withCorrection?.RecognizeAsync(context, cancellationToken) ?? Task.FromResult<RecognizerResult>(null));
            _resultWithoutCorrection = await _withoutCorrection.RecognizeAsync(context, cancellationToken);
        }

        public ClassifierResult GetResult(bool cleanup = true)
        {
            if (_resultWithoutCorrection == null)
                return null;

            var topIntentWithoutCorrection = _resultWithoutCorrection.GetTopScoringIntent();
            if (_resultWithCorrection != null)
            {
                var topIntentWithCorrections = _resultWithCorrection?.GetTopScoringIntent();
                if (topIntentWithoutCorrection.score < (topIntentWithCorrections?.score ?? double.MinValue))
                    return ToResult(_resultWithCorrection, cleanup);
            }
            return ToResult(_resultWithoutCorrection, cleanup);
        }

        private ClassifierResult ToResult(RecognizerResult result, bool cleanup)
        {
            string text = result.Text;
            Dictionary<string, double> intents = new Dictionary<string, double>();
            foreach (var intent in result.Intents)
                intents.Add(intent.Key, intent.Value.Score ?? double.NaN);

            Dictionary<string, List<IEntity>> entities = ToEntities(result, cleanup);

            return new ClassifierResult(text, intents, entities);
        }

        private Dictionary<string, List<IEntity>> ToEntities(RecognizerResult result, bool cleanup)
        {
            Dictionary<string, List<IEntity>> entities = new Dictionary<string, List<IEntity>>();

            foreach (var entity in result.Entities.GetRawEntities(cleanup))
            {
                // Check for special TimeDate Entitiy from LUIS
                if (IsTimeX(entity, out List<DateTime> times))
                {
                    AddEntity(entities, "datetime", new TimeEntity(entity.Value[0].Text, entity.Value[0].StartIndex, entity.Value[0].EndIndex, times));
                }
                else
                {
                    // ListEntity
                    foreach (var instance in entity.Value)
                    {
                        AddEntity(entities, entity.Key, new GroupEntity(instance.Text, instance.StartIndex, instance.EndIndex, entity.Key, instance.Resolution.ToObject<string[]>()[0]));
                    }
                }
            }
            return entities;
        }

        private void AddEntity(Dictionary<string, List<IEntity>> entities, string key, IEntity entity)
        {
            if (entities.ContainsKey(key))
                entities[key].Add(entity);
            else
                entities.Add(key, new List<IEntity> { entity });
        }

        private bool IsTimeX(KeyValuePair<string, List<LuisRawEntity>> entity, out List<DateTime> times)
        {
            times = null;
            if (entity.Key != "datetime" || entity.Value.Count != 1 || entity.Value[0].Type != "builtin.datetimeV2.date")
                return false;

            try
            {
                times = new List<DateTime>();
                foreach (var v in entity.Value)
                {
                    var expr = v.Resolution["timex"];
                    string[] dates = expr.ToObject<string[]>();
                    var dts = dates.ToList().ConvertAll(d => DateTime.Parse(d));
                    times.AddRange(dts);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

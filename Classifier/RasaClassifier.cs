using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Classifier
{
    /// <summary>
    /// Implements an <see cref="IClassifier"/> with Rasa.
    /// Currently no entities will be used.
    /// </summary>
    public class RasaClassifier : IClassifier
    {
        private readonly string _serverUrl;
        private readonly HttpClient _client;
        private readonly double _minEntityConfidence;
        private readonly ILogger _logger;

        private RasaClassificationResult _classification;

        /// <summary>
        /// Creates a new classifier with Rasa.
        /// </summary>
        /// <param name="serverUrl">The url to the rasa server e.g. 'http://localhost'.</param>
        /// <param name="port">The port to access to the rasa server.</param>
        /// <param name="logger">The logger (optional)</param>
        /// <param name="minEntityConfidence">the minimum confidence of an entity</param>
        public RasaClassifier(string serverUrl, int port = 5005, ILogger logger = null, double minEntityConfidence = 0.9)
        {
            _logger = logger;
            _serverUrl = serverUrl + ":" + port;
            _client = new HttpClient { BaseAddress = new Uri(_serverUrl), Timeout = new TimeSpan(0, 0, 20) };
            _minEntityConfidence = minEntityConfidence;
            var status = _client.GetAsync("/status").GetAwaiter().GetResult();
            if (!status.IsSuccessStatusCode)
                throw new ArgumentException("RASA server does not respond. Did you used the --enable-api flag when starting the rasa server?");
        }

        public ClassifierResult GetResult(bool cleanup = true)
        {
            if (_classification == null)
                return null;

            Dictionary<string, double> scoring = new Dictionary<string, double>();
            foreach (var score in _classification.IntentRanking)
                scoring.Add(score.Name, score.Score);

            Dictionary<string, List<IEntity>> entities = new Dictionary<string, List<IEntity>>();
            foreach (var entity in _classification.Entities)
                if (entity.Score >= _minEntityConfidence)
                    AddEntity(entities, entity.Group, new GroupEntity(_classification.Text.Substring(entity.Start, entity.End - entity.Start), entity.Start, entity.End, entity.Group, entity.Value));

            ClassifierResult result = new ClassifierResult(_classification.Text, scoring, entities);
            return result;
        }
        private void AddEntity(Dictionary<string, List<IEntity>> entities, string key, IEntity entity)
        {
            if (entities.ContainsKey(key))
                entities[key].Add(entity);
            else
                entities.Add(key, new List<IEntity> { entity });
        }

        public async Task Recognize(ITurnContext context, CancellationToken cancellationToken)
        {
            string text = context.Activity.Text;

            string requestBody = JsonConvert.SerializeObject(new Dictionary<string, string>() { { "text", text } });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            HttpResponseMessage rasaResponse = await _client.PostAsync("/model/parse", content);

            if (!rasaResponse.IsSuccessStatusCode)
            {
                string error = await rasaResponse.Content.ReadAsStringAsync();
                _logger?.LogError(error);
                _classification = null;
                return;
            }
            string classification = await rasaResponse.Content.ReadAsStringAsync();
            _classification = JsonConvert.DeserializeObject<RasaClassificationResult>(classification);
        }


        private class RasaClassificationResult
        {
            [JsonProperty("intent")]
            public RasaIntent Intent { get; set; }
            [JsonProperty("intent_ranking")]
            public List<RasaIntent> IntentRanking { get; set; }
            [JsonProperty("text")]
            public string Text { get; set; }
            [JsonProperty("entities")]
            public List<RasaEntity> Entities { get; set; }
        }

        private class RasaEntity
        {
            [JsonProperty("entity")]
            public string Group { get; set; }
            [JsonProperty("value")]
            public string Value { get; set; }
            [JsonProperty("confidence")]
            public double Score { get; set; }

            [JsonProperty("start")]
            public int Start { get; set; }
            [JsonProperty("end")]
            public int End { get; set; }
        }

        private class RasaIntent
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("confidence")]
            public double Score { get; set; }
        }
    }
}

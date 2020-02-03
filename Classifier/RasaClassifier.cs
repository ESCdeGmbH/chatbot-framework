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
        private readonly ILogger _logger;

        private RasaClassificationResult _classification;

        /// <summary>
        /// Creates a new classifier with Rasa.
        /// </summary>
        /// <param name="serverUrl">The url to the rasa server e.g. 'http://localhost'.</param>
        /// <param name="port">The port to access to the rasa server.</param>
        /// <param name="logger">The logger (optional)</param>
        public RasaClassifier(string serverUrl, int port = 5005, ILogger logger = null)
        {
            _logger = logger;
            _serverUrl = serverUrl + ":" + port;
            _client = new HttpClient { BaseAddress = new Uri(_serverUrl), Timeout = new TimeSpan(0, 0, 20) };
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

            ClassifierResult result = new ClassifierResult(_classification.Text, scoring, new Dictionary<string, List<IEntity>>());
            return result;
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


        [Serializable]
        private class RasaClassificationResult
        {
            [JsonProperty("intent")]
            public RasaIntent Intent { get; set; }
            [JsonProperty("intent_ranking")]
            public List<RasaIntent> IntentRanking { get; set; }
            [JsonProperty("text")]
            public string Text { get; set; }
        }

        [Serializable]
        private class RasaIntent
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("confidence")]
            public double Score { get; set; }
        }
    }
}

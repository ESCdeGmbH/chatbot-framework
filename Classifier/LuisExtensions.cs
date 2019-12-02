using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Framework.Classifier
{
    /// <summary>
    /// The extensions to work with Luis.
    /// </summary>
    public static class LuisExtensions
    {
        /// <summary>
        /// Extracts the intents of a Luis instance.
        /// </summary>
        /// <param name="_this">The definition of the Luis service.</param>
        /// <returns>A list of intents.</returns>
        public static List<LuisIntent> GetIntents(this LuisServiceDefinition _this)
        {
            if (_this == null)
                return new List<LuisIntent>();

            List<LuisIntent> intents = new List<LuisIntent>();
            string request = $"https://{_this.Region}.api.cognitive.microsoft.com/luis/api/v2.0/apps/{_this.AppId}/versions/{_this.Version}/intents?skip=0&take=100";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _this.AuthoringKey);
                var response = client.GetAsync(request).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                    return new List<LuisIntent>();
                var data = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                intents = JsonConvert.DeserializeObject<List<LuisIntent>>(data);
            }
            return intents;
        }

        /// <summary>
        /// Adds a ListEntity to an existing Luis instance.
        /// The name of the ListEntity has to be unused in the instance.
        /// Elsewhere this method has no effect.
        /// </summary>
        /// <param name="_this">The Luis service definition.</param>
        /// <param name="name">The name of the ListEntity.</param>
        /// <param name="sublistsWithCanonicalAndList">The entries of the ListEntity. The first value of tuple defines the canonical string. The second one the synonyms.</param>
        /// <returns>The Id of the newly created ListEntity.</returns>
        public static string AddListEntity(this LuisServiceDefinition _this, string name, List<Tuple<string, List<string>>> sublistsWithCanonicalAndList)
        {

            string request = $"https://{_this.Region}.api.cognitive.microsoft.com/luis/api/v2.0/apps/{_this.AppId}/versions/{_this.Version}/closedlists";

            ListEntity le = new ListEntity
            {
                Name = name,
                Sublists = new List<Dictionary<string, object>>()
            };
            foreach (var sl in sublistsWithCanonicalAndList)
            {
                Dictionary<string, object> element = new Dictionary<string, object>
                {
                    { "canonicalForm", sl.Item1 },
                    { "list", sl.Item2 }
                };
                le.Sublists.Add(element);
            }

            string json = JsonConvert.SerializeObject(le);
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _this.AuthoringKey);
                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                var response = client.PostAsync(request, content).GetAwaiter().GetResult();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }


        }

        [Serializable]
        private sealed class ListEntity
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("sublists")]
            public List<Dictionary<string, object>> Sublists { get; set; }
        }

        /// <summary>
        /// Represents an intent of Luis.
        /// </summary>
        [Serializable]
        public sealed class LuisIntent
        {
            /// <summary>
            /// The Id of the intent.
            /// </summary>
            [JsonProperty("id")]
            public string ID { get; private set; }
            /// <summary>
            /// The name of the intent.
            /// </summary>
            [JsonProperty("name")]
            public string Name { get; private set; }
        }


        public static double GetSentiment(this RecognizerResult _result)
        {
            if (!_result.Properties.ContainsKey("sentiment"))
                return 0.5;

            JToken token = (JToken)_result.Properties["sentiment"];
            double score = token.Value<double>("score");

            return score;
        }

        public static Sentiment InterpreteSentiment(this double score)
        {
            return score < 0.35 ? Sentiment.Negative : (score > 0.7 ? Sentiment.Positive : Sentiment.Neutral);
        }


        /// <summary>
        /// Retrieve the entities of the entity property in a Luis result.
        /// </summary>
        /// <param name="entities">The entity property in a Luis result.</param>
        /// <param name="cleanup">Indicator for cleanup (e.g. remove match to group if you are asking for resource group).</param>
        /// <returns>Extracted entities with their values.</returns>
        public static Dictionary<string, List<LuisRawEntity>> GetRawEntities(this JObject entities, bool cleanup)
        {
            if (entities == null)
                return new Dictionary<string, List<LuisRawEntity>>();


            var mapType2Data = entities.GetValue("$instance").ToObject<Dictionary<string, List<LuisRawEntity>>>();

            foreach (var entityType in mapType2Data.Keys)
            {
                entities.TryGetValue(entityType, out JToken resolution);

                JToken[] reses = resolution.Children().ToArray() ?? new JToken[0];
                List<LuisRawEntity> entitiesFromLuis = mapType2Data[entityType];
                if (reses.Length != entitiesFromLuis.Count)
                {
                    // Shall not happen ..
                    continue;
                }

                for (int i = 0; i < reses.Length; i++)
                    entitiesFromLuis[i].Resolution = reses[i];
            }

            return cleanup ? CleanupRaw(mapType2Data) : mapType2Data;
        }

        private static Dictionary<string, List<LuisRawEntity>> CleanupRaw(Dictionary<string, List<LuisRawEntity>> rawData)
        {
            Dictionary<string, List<LuisRawEntity>> result = new Dictionary<string, List<LuisRawEntity>>();
            foreach (string key in rawData.Keys)
            {
                List<LuisRawEntity> value = rawData[key].ToList();
                if (value.Count == 0 || value[0].Resolution == null)
                    continue;
                var example = value[0];
                if (example.Resolution.Type != JTokenType.Array)
                {
                    result.Add(key, value);
                    continue;
                }

                if (value.Any(a => a.Resolution.ToObject<object[]>().Length != 1))
                {
                    // Not each one element ..
                    continue;
                }

                value.Sort((e1, e2) => e2.Text.Length - e1.Text.Length);
                List<LuisRawEntity> newValue = new List<LuisRawEntity>();
                List<string> texts = new List<string>();
                foreach (LuisRawEntity v in value)
                {
                    if (texts.Any(nt => nt.Contains(v.Text)))
                        continue;
                    newValue.Add(v);
                    texts.Add(v.Text);
                }

                result.Add(key, newValue);
            }

            return result;
        }

        /// <summary>
        /// Retrieve the entities of the entity property in a Luis result.
        /// </summary>
        /// <param name="entities">The entity property in a Luis result.</param>
        /// <param name="cleanup">Indicator for cleanup (e.g. remove match to group if you are asking for resource group).</param>
        /// <returns>Extracted entities with their values.</returns>
        public static Dictionary<string, List<JToken>> GetEntities(this JObject entities, bool cleanup)
        {
            if (entities == null)
                return new Dictionary<string, List<JToken>>();


            var mapType2Data = entities.GetRawEntities(cleanup);
            Dictionary<string, List<JToken>> result = new Dictionary<string, List<JToken>>();
            foreach (var key in mapType2Data.Keys)
                result.Add(key, mapType2Data[key].ConvertAll(d => d.Resolution));
            return result;
        }

        [Serializable]
        public sealed class LuisRawEntity
        {
            [JsonProperty("startIndex")]
            public int StartIndex { get; set; }
            [JsonProperty("endIndex")]
            public int EndIndex { get; set; }
            [JsonProperty("text")]
            public string Text { get; set; }
            [JsonProperty("type")]
            public string Type { get; set; }
            public JToken Resolution { get; set; }
        }
    }
}

using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Configuration;
using Newtonsoft.Json;
using System;

namespace Framework.Classifier
{
    /// <summary>
    /// Defines a Luis service.
    /// </summary>
    [Serializable]
    public sealed class LuisServiceDefinition
    {
        /// <summary>
        /// The name of the Luis service.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; private set; }
        /// <summary>
        /// The version of the Luis service.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; private set; }
        /// <summary>
        /// The application Id of the Luis service.
        /// </summary>
        [JsonProperty("appId")]
        public string AppId { get; private set; }
        /// <summary>
        /// The authoring key of the Luis service.
        /// </summary>
        [JsonProperty("authoringKey")]
        public string AuthoringKey { get; private set; }
        /// <summary>
        /// The subscription key of the Luis service in Azure.
        /// </summary>
        [JsonProperty("subscriptionKey")]
        public string SubscriptionKey { get; private set; }
        /// <summary>
        /// The region of the Luis service.
        /// </summary>
        [JsonProperty("region")]
        public string Region { get; private set; }
        /// <summary>
        ///  The spell checker key of the spell checker subscription in Azure.
        /// </summary>
        [JsonProperty("spellCheckerKey")]
        public string SpellCheckerKey { get; private set; }

        /// <summary>
        /// Converts a Luis service definition into a Luis service.
        /// </summary>
        /// <returns>The Luis service.</returns>
        public LuisService GetLuisService()
        {
            LuisService ls = new LuisService
            {
                AppId = AppId,
                AuthoringKey = AuthoringKey,
                SubscriptionKey = SubscriptionKey,
                Version = Version,
                Region = Region
            };
            return ls;
        }

        /// <summary>
        /// Generate the prediction options.
        /// </summary>
        /// <returns>The prediction options.</returns>
        public LuisPredictionOptions GetPredictOpts()
        {
            if (string.IsNullOrWhiteSpace(SpellCheckerKey))
                return new LuisPredictionOptions() { IncludeAllIntents = true };
            LuisPredictionOptions lo = new LuisPredictionOptions
            {

                SpellCheck = true,
                BingSpellCheckSubscriptionKey = SpellCheckerKey,
                IncludeAllIntents = true
            };
            return lo;

        }

    }
}


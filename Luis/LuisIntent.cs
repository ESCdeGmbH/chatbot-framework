using Newtonsoft.Json;
using System;

namespace Framework.Luis
{
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
}


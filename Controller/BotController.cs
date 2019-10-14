using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Controller
{
    /// <summary>
    /// Base class for handling user requests.
    /// </summary>
    /// <typeparam name="TBot"> is class type of the bot.</typeparam>
    [Route("api/messages")]
    [ApiController]
    public abstract class BotController<TBot> : ControllerBase where TBot : IBot
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        /// <summary>
        /// The configuration derived from the settings.
        /// </summary>
        protected readonly IConfiguration _config;
        /// <summary>
        /// The conversational state for the dialog framework.
        /// </summary>
        protected readonly ConversationState _state;
        /// <summary>
        /// The logger factory to create the loggers.
        /// </summary>
        protected readonly ILoggerFactory _loggerFactory;
        private static readonly Dictionary<string, TBot> bots = new Dictionary<string, TBot>();
        private static readonly Dictionary<string, DateTime> lastAccess = new Dictionary<string, DateTime>();
        /// <summary>
        /// Time to store the users session in seconds.
        /// </summary>
        private const float TTLinS = 60 * 60;

        /// <summary>
        /// Create a controller.
        /// </summary>
        /// <param name="adapter">The connector between BotFramework and bot.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="state">The conversational state for the dialog framework.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public BotController(IBotFrameworkHttpAdapter adapter, IConfiguration config, ConversationState state, ILoggerFactory loggerFactory)
        {
            _adapter = adapter;
            _config = config;
            _state = state;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Handle the user request.
        /// </summary>
        /// <returns>the response of the bot.</returns>
        [HttpPost]
        public async Task PostAsync()
        {
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            JObject json = JsonConvert.DeserializeObject<JObject>(GetDocumentContents(Request));
            var id = json["conversation"]["id"].ToString();
            TBot bot;
            lock (bots)
            {
                if (!bots.TryGetValue(id, out bot))
                {
                    TBot newBot = CreateBot();
                    bots.Add(id, newBot);
                    bot = newBot;
                    lastAccess.Add(id, DateTime.UtcNow);
                }
                else
                {
                    lastAccess[id] = DateTime.UtcNow;
                }
            }

            await _adapter.ProcessAsync(Request, Response, bot);

            Cleanup();
        }

        /// <summary>
        /// Instantiate a new bot.
        /// </summary>
        /// <returns>The created bot.</returns>
        protected abstract TBot CreateBot();

        private void Cleanup()
        {
            lock (bots)
            {
                foreach (var conversation in bots.Keys.ToList())
                {
                    if ((DateTime.UtcNow - lastAccess[conversation]).TotalSeconds > TTLinS)
                    {
                        lastAccess.Remove(conversation);
                        bots.Remove(conversation);
                    }
                }
            }
        }
        private string GetDocumentContents(HttpRequest Request)
        {
            string body = new StreamReader(Request.Body).ReadToEnd();
            byte[] requestData = Encoding.UTF8.GetBytes(body);
            Request.Body = new MemoryStream(requestData);
            return body;
        }
    }
}

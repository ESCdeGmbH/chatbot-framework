using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.Dialogs
{
    /// <summary>
    /// Defines the interface of the bot for the dialogs.
    /// </summary>
    public interface IBot4Dialog
    {  
        /// <summary>
        /// The latest results of Luis.
        /// </summary>
        RecognizerResult Result { get; }

        /// <summary>
        /// The detected entities.
        /// </summary>
        /// <param name="cleanup">Indicator for cleanup (e.g. remove match to group if you are asking for resource group).</param>
        /// <returns>All detected entities by entity-definitions.</returns>
        Dictionary<string, List<JToken>> GetEntities(bool cleanup = true);

        /// <summary>
        /// Sends a text.
        /// </summary>
        /// <param name="msg">The text.</param>
        /// <param name="context">The bot context.</param>
        /// <returns>The response of the bot framework.</returns>
        Task<ResourceResponse> SendMessage(string msg, ITurnContext context);
       
        /// <summary>
        /// Sends an activity as an adaptive card.
        /// </summary>
        /// <param name="msg">The activity.</param>
        /// <param name="context">The bot context.</param>
        /// <returns>The response of the bot framework.</returns>
        Task<ResourceResponse> SendMessage(Activity msg, ITurnContext context);

    }
}

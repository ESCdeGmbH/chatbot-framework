using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Framework
{
    /// <summary>
    /// Defines the default bot framework HttpAdapter as already defined in the bot framework.
    /// </summary>
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        private readonly ILogger<BotFrameworkHttpAdapter> logger;
        private readonly ConversationState conversationState;

        /// <summary>
        /// Creates an adapter with an error handler.
        /// </summary>
        /// <param name="configuration">The global configuration.</param>
        /// <param name="logger">The logger for the bot framework adapter.</param>
        /// <param name="conversationState">The conversational state of the bot.</param>
        public AdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger, ConversationState conversationState = null)
            : base(configuration, logger)
        {
            this.logger = logger;
            this.conversationState = conversationState;
            OnTurnError = OnError;
        }

        /// <summary>
        /// Handle an error.
        /// </summary>
        /// <param name="turnContext">The context of the bot.</param>
        /// <param name="exception">The occured exception.</param>
        /// <returns>The execution task.</returns>
        public virtual async Task OnError(ITurnContext turnContext, Exception exception)
        {
            // Log any leaked exception from the application.
            logger.LogError($"Exception caught : {exception.Message}");

            // Send a catch-all apology to the user.
            await turnContext.SendActivityAsync("Sorry, it looks like something went wrong.");

            if (conversationState != null)
            {
                try
                {
                    // Delete the conversationState for the current conversation to prevent the
                    // bot from getting stuck in a error-loop caused by being in a bad state.
                    // ConversationState should be thought of as similar to "cookie-state" in a Web pages.
                    await conversationState.DeleteAsync(turnContext);
                }
                catch (Exception e)
                {
                    logger.LogError($"Exception caught on attempting to Delete ConversationState : {e.Message}");
                }
            }

        }
    }
}

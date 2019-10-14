using Framework.Misc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Dialogs
{
    /// <summary>
    /// Defines the login dialog for OAuth. Adopted from the original sources of the bot framework.
    /// </summary>
    public class LoginDialog : ComponentDialog
    {
        /// <summary>
        /// The id of the dialog.
        /// </summary>
        public const string ID = "Login";
        private readonly Action<string> _callbackToken;
        private readonly string _welcomeText;

        /// <summary>
        /// Creates the login dialog.
        /// </summary>
        /// <param name="configuration">The configuration where the "ConnectionName" defines the connection name of the OAuth.</param>
        /// <param name="callbackToken">The callback to store the OAuth token.</param>
        /// <param name="welcomeText">Defines the welcome text that will be shown by the dialog.</param>
        public LoginDialog(IConfiguration configuration, Action<string> callbackToken, string welcomeText) : base(ID)
        {
            _callbackToken = callbackToken;
            _welcomeText = welcomeText;
            AddDialog(new OAuthPrompt(nameof(OAuthPrompt), new OAuthPromptSettings
            {
                ConnectionName = configuration["ConnectionName"],
                Text = "You need a token to login",
                Title = "Get Token",
                Timeout = 300000, // User has 5 minutes to login
            }));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
                LoginStepAsync,
                TryAddAuth
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var msg = CardBuilder.BuildOptionPrompt(_welcomeText, new CardBuilder.PromptOption("Login", "Login")).ToActivity(stepContext.Context);
            await stepContext.Context.SendActivityAsync(msg);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> TryAddAuth(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tokenResponse = (TokenResponse)stepContext?.Result;
            if (tokenResponse != null)
            {
                var sgc = new SimpleGraphClient(tokenResponse.Token);
                var user = await sgc.GetMeAsync();
                if (user != null)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You are now logged in as {user.DisplayName}"), cancellationToken);
                    _callbackToken(tokenResponse.Token);
                    return await stepContext.NextAsync();
                }
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Token Invalid. Please try again."), cancellationToken);
            return await stepContext.EndDialogAsync();
        }
    }
}

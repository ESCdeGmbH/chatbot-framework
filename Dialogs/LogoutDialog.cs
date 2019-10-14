using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzureDashbot.Dialogs
{

    /// <summary>
    /// Defines the logout dialog for OAuth. Adopted from the original sources of the bot framework.
    /// </summary>
    public class LogoutDialog : WaterfallDialog
    {
        /// <summary>
        /// The id of the dialog.
        /// </summary>
        public const string ID = "Logout";

        private readonly string _connectionName;
        private readonly Action _resetToken;

        /// <summary>
        /// Creates the logout dialog.
        /// </summary>
        /// <param name="configuration">The configuration where the "ConnectionName" defines the connection name of the OAuth.</param>
        /// <param name="resetToken">The callback action to reset the token.</param>
        public LogoutDialog(IConfiguration configuration, Action resetToken) : base(ID)
        {
            _connectionName = configuration["ConnectionName"];
            _resetToken = resetToken;
            AddStep(Logout);
        }
        private async Task<DialogTurnResult> Logout(DialogContext innerDc, CancellationToken cancellationToken)
        {
            // The bot adapter encapsulates the authentication processes.
            var botAdapter = (BotFrameworkAdapter)innerDc.Context.Adapter;
            await botAdapter.SignOutUserAsync(innerDc.Context, _connectionName, null, cancellationToken);
            _resetToken();
            await innerDc.Context.SendActivityAsync(MessageFactory.Text("You have been signed out."), cancellationToken);
            return await innerDc.CancelAllDialogsAsync();
        }
    }
}

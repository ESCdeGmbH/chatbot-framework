using Framework.Classifier;
using Framework.Dialogs;
using Framework.Misc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Framework
{
    /// <summary>
    /// The base class of all bots.
    /// </summary>
    /// <typeparam name="S">The specific bot service classes of this bot.</typeparam>
    /// <typeparam name="B">The specific interface to the dialogs of this bot.</typeparam>
    /// <typeparam name="D">The specific base class of the dialogs of this bot.</typeparam>
    public abstract partial class Bot<S, B, D> : ActivityHandler, IBot, IBot4Dialog where B : IBot4Dialog where S : BotServices where D : BaseDialog<B, S>
    {
        private readonly string _ttsLanguage;
        /// <summary>
        /// The handler for Luis intent classification.
        /// </summary>
        /// <param name="context">The context of the bot.</param>
        /// <param name="entities">The entities found by Luis.</param>
        /// <returns>The execution.</returns>
        public delegate Task Handler(ITurnContext context);
        /// <summary>
        /// The logger of this bot.
        /// </summary>
        protected readonly ILogger _logger;
        /// <summary>
        /// The bot services.
        /// </summary>
        protected readonly S BotServices;
        /// <summary>
        /// The conversational state of the dialogs.
        /// </summary>
        protected readonly ConversationState State;
        /// <summary>
        /// The Intent handlers (maps intents(lower case) to handler).
        /// </summary>
        protected readonly Dictionary<string, Handler> IntentHandler;
        /// <summary>
        /// The main classifier Luis instance.
        /// </summary>
        protected IClassifier _recognizer;

        /// <summary>
        /// The result of classification with highest score included.
        /// </summary>
        public ClassifierResult Result => _recognizer?.GetResult();

        /// <summary>
        /// Creates a new bot.
        /// </summary>
        /// <param name="state">The conversational state of the dialogs.</param>
        /// <param name="services">The services of the bot.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="ttsLanguage">The language for text to speech (null disables the tts service).</param>
        protected Bot(ConversationState state, S services, ILoggerFactory loggerFactory, string ttsLanguage = null)
        {
            // Null => Disable ..
            _ttsLanguage = ttsLanguage;
            BotServices = services ?? throw new ArgumentNullException(nameof(services));

            _logger = loggerFactory?.CreateLogger<Bot<S, B, D>>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger.LogTrace("Bot turn start.");

            State = state;
            IntentHandler = new Dictionary<string, Handler>();

            LoadDialogs(out Dialogs, out DialogInstances);
        }

        /// <summary>
        /// A dialog set of all possible dialogs.
        /// </summary>
        protected readonly DialogSet Dialogs;
        /// <summary>
        /// The instances of stateful dialogs.
        /// </summary>
        protected readonly List<BaseDialog<B, S>> DialogInstances;

        /// <summary>
        /// Load all dialogs.
        /// </summary>
        /// <param name="dialogs">The dialog set.</param>
        /// <param name="instances">The stateful dialogs.</param>
        protected abstract void LoadDialogs(out DialogSet dialogs, out List<BaseDialog<B, S>> instances);

        /// <summary>
        /// Returns a Luis handler which starts a dialog.
        /// </summary>
        /// <param name="dialogID">The dialog id.</param>
        /// <returns>The Luis handler.</returns>
        protected Handler StartDialog(string dialogID)
        {
            return (ctx) => HandleStartDialog(dialogID, ctx);
        }

        private async Task HandleStartDialog(string dialogID, ITurnContext context)
        {
            var dCtx = await Dialogs.CreateContextAsync(context);
            var result = await dCtx.BeginDialogAsync(dialogID);
            await HandleDialogResult(context, default(CancellationToken), dCtx, result);
        }

        /// <summary>
        /// Postprocessing the result of a dialog.
        /// </summary>
        /// <param name="ctx">The context of the bot.</param>
        /// <param name="cancellationToken">The cancellation token of the current context.</param>
        /// <param name="dialogContext">The dialog context.</param>
        /// <param name="dialogResult">The dialog result.</param>
        /// <returns>An execution.</returns>
        protected virtual async Task HandleDialogResult(ITurnContext ctx, CancellationToken cancellationToken, DialogContext dialogContext, DialogTurnResult dialogResult)
        {
            switch (dialogResult.Status)
            {
                case DialogTurnStatus.Empty:
                    DialogInstances.ForEach(d => d.Reset());
                    await SelectTopic(ctx, cancellationToken);
                    break;
                case DialogTurnStatus.Complete:
                    await dialogContext.EndDialogAsync();
                    DialogInstances.ForEach(d => d.Reset());
                    break;
                case DialogTurnStatus.Cancelled:
                    await dialogContext.CancelAllDialogsAsync();
                    DialogInstances.ForEach(d => d.Reset());
                    break;
                case DialogTurnStatus.Waiting:
                    break;
            }
            await State.SaveChangesAsync(ctx, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Select topic and run dialog.
        /// </summary>
        /// <param name="ctx">the context</param>
        /// <param name="cancellationToken">the cancellation token</param>
        /// <returns>indicator of success</returns>
        protected abstract Task<bool> SelectTopic(ITurnContext ctx, CancellationToken cancellationToken);

        protected abstract override Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken);

        protected virtual async Task HandleDialog(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var dialogContext = await Dialogs.CreateContextAsync(turnContext);
            var dialogResult = await dialogContext.ContinueDialogAsync();
            await HandleDialogResult(turnContext, cancellationToken, dialogContext, dialogResult);
        }

        #region SendMessage

        /// <summary>
        /// Sends a text.
        /// </summary>
        /// <param name="msg">The text.</param>
        /// <param name="context">The bot context.</param>
        /// <returns>The response of the bot framework.</returns>
        public virtual async Task<ResourceResponse> SendMessage(string msg, ITurnContext ctx)
        {
            if (_ttsLanguage == null)
                return await ctx.SendActivityAsync(msg);
            return await ctx.SendActivityAsync(msg, TextToSpeechService.GenerateSsml(CleanupTextForSpeech(msg), _ttsLanguage));
        }

        private static string CleanupTextForSpeech(string text)
        {
            string res = text;
            // Replace "[XYZ](www.microsoft.com)" with XYZ ..
            Regex links = new Regex("\\[[A-Za-z0-9 _]+\\]\\([A-Za-z0-9 _/\\?#%:\\.]+\\)");
            var matches = links.Matches(res);
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                string match = matches[i].Value;
                match = match.Split("](")[0].Substring(1);
                res = res.Substring(0, matches[i].Index) + match + res.Substring(matches[i].Index + matches[i].Length);
            }

            return res;
        }

        /// <summary>
        /// Sends an activity as an adaptive card.
        /// </summary>
        /// <param name="msg">The activity.</param>
        /// <param name="context">The bot context.</param>
        /// <returns>The response of the bot framework.</returns>
        public virtual async Task<ResourceResponse> SendMessage(Activity activity, ITurnContext ctx)
        {
            return await ctx.SendActivityAsync(activity);
        }

        #endregion
    }
}

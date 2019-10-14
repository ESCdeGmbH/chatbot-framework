using System;

namespace Framework.Dialogs
{
    /// <summary>
    /// Defines a dialog schema.
    /// </summary>
    /// <typeparam name="B">The bot which shall implement <see cref="IBot4Dialog"/>.</typeparam>
    /// <typeparam name="S">The bot services.</typeparam>
    public abstract class BaseDialog<B, S> : StatefulWaterfallDialog.StatefulWaterfallDialog where B : IBot4Dialog where S : BotServices
    {
        /// <summary>
        /// The bot services.
        /// </summary>
        protected readonly S BotServices;
        /// <summary>
        /// The actual bot.
        /// </summary>
        protected readonly B TheBot;
        /// <summary>
        /// The response analyzer for this dialog.
        /// </summary>
        protected readonly ResponseAnalyzer.Analyzer ResponseAnalyzer;
        /// <summary>
        /// The question analyzer for this dialog.
        /// </summary>
        protected readonly QuestionAnalyzer.Analyzer QuestionAnalyzer;

        /// <summary>
        /// Configure the BaseDialog.
        /// </summary>
        /// <param name="services">The bot services.</param>
        /// <param name="bot">The bot.</param>
        /// <param name="ID">The id of the dialog.</param>
        protected BaseDialog(S services, B bot, string ID) : base(ID)
        {
            BotServices = services ?? throw new ArgumentNullException(nameof(services));
            TheBot = bot == null ? throw new ArgumentNullException(nameof(bot)) : bot;
            ResponseAnalyzer = services.ResponseAnalyzer;
            QuestionAnalyzer = services.QuestionAnalyzer;
        }
    }
}

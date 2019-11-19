using Microsoft.Extensions.Configuration;

namespace Framework
{
    /// <summary>
    /// The base class for bot services.
    /// </summary>
    public abstract class BotServices
    {
        /// <summary>
        /// Creates a new bot service.
        /// </summary>
        /// <param name="configuration">The configuration of the bot.</param>
        protected BotServices(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// The configuration of the bot.
        /// </summary>
        public IConfiguration Configuration { get; private set; }
        /// <summary>
        /// The response analyzer for all dialogs.
        /// </summary>
        public ResponseAnalyzer.Analyzer ResponseAnalyzer { get; protected set; }
        /// <summary>
        /// The question analyzer for all dialogs.
        /// </summary>
        public QuestionAnalyzer.Analyzer QuestionAnalyzer { get; protected set; }
    }
}

using Framework.Misc;
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
        /// <param name="language">The language to be used.</param>
        protected BotServices(IConfiguration configuration, Language language)
        {
            Configuration = configuration;
            ResponseAnalyzer = configuration.GetSection("ResponseAnalyzer").Exists() ? new ResponseAnalyzer.Analyzer(configuration, language) : null;
            QuestionAnalyzer = configuration.GetSection("QuestionAnalyzer").Exists() ? new QuestionAnalyzer.Analyzer(configuration, language) : null;
        }

        /// <summary>
        /// The configuration of the bot.
        /// </summary>
        public IConfiguration Configuration { get; private set; }
        /// <summary>
        /// The response analyzer for all dialogs.
        /// </summary>
        public ResponseAnalyzer.Analyzer ResponseAnalyzer { get; private set; }
        /// <summary>
        /// The question analyzer for all dialogs.
        /// </summary>
        public QuestionAnalyzer.Analyzer QuestionAnalyzer { get; private set; }
    }
}

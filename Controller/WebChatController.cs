using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Framework.Controller
{
    /// <summary>
    /// Provides the tokens for the website.
    /// </summary>
    [Route("/chat")]
    [ApiController]
    public class WebChatController : ControllerBase
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Create a controller by a given configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public WebChatController(IConfiguration config) { _config = config; }

        /// <summary>
        /// Provides the DirectLineToken.
        /// </summary>
        /// <returns>The token.</returns>
        [HttpPost]
        [Route("token")]
        public Task<string> Token()
        {
            return Task.FromResult(_config.GetValue("DirectLineToken", ""));
        }

        /// <summary>
        /// Provides the speech subscription key.
        /// </summary>
        /// <returns>The key.</returns>
        [HttpPost]
        [Route("speech")]
        public Task<string> Speech()
        {
            return Task.FromResult(_config.GetValue("SpeechSubscriptionKey", ""));
        }
    }
}

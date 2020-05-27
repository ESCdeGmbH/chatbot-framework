using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Controller
{
    /// <summary>
    /// Controller that handles messages for offline frontend. Will be enabled if SignalR is enabled in Startup.cs.
    /// </summary>
    [Route("v3/conversations")]
    [ApiController]
    public class OfflineController : ControllerBase
    {
        protected readonly IHubContext<OfflineHub> _offlineHubContext;
        private static Dictionary<string, List<JObject>> _callbackQueue = new Dictionary<string, List<JObject>>();
        
        public OfflineController(IHubContext<OfflineHub> hub = null)
        {
            _offlineHubContext = hub;
        }

        /// <summary>
        /// The endpoint for the bot to send messages to the offline client with the given conversation ID.
        /// </summary>
        /// <param name="conversationId">The conversation ID.</param>
        /// <param name="activityId">The activity ID.</param>
        /// <returns>Status Code</returns>
        [HttpPost]
        [Route("{conversationId}/activities/{activityId}")]
        public async Task<IActionResult> Callback(string conversationId, string activityId)
        {
            if (_offlineHubContext == null) return NotFound();
            JObject json = JsonConvert.DeserializeObject<JObject>(await GetDocumentContents(Request));
            AddDataToCallbackQueue(conversationId, json);
            await _offlineHubContext.Clients.All.SendAsync(conversationId);
            return Ok();
        }

        /// <summary>
        /// Get a list of the latest activities as json.
        /// </summary>
        /// <param name="conversationId">The conversation ID.</param>
        /// <returns>A list of the latest activities.</returns>
        [HttpPost]
        [Route("{conversationId}/poll")]
        public Task<IActionResult> Poll(string conversationId)
        {
            if (_offlineHubContext == null) return Task.FromResult<IActionResult>(NotFound());
            return Task.FromResult<IActionResult>(Ok(JsonConvert.SerializeObject(GetQueuedContent(conversationId))));
        }

        private async Task<string> GetDocumentContents(HttpRequest Request)
        {
            string body;
            using (var reader = new StreamReader(Request.Body))
                body = await reader.ReadToEndAsync();

            byte[] requestData = Encoding.UTF8.GetBytes(body);
            Request.Body = new MemoryStream(requestData);
            return body;
        }

        private void AddDataToCallbackQueue(string conversationId, JObject content)
        {
            List<JObject> queue = new List<JObject>();
            lock(_callbackQueue)
            {
                if (_callbackQueue.ContainsKey(conversationId))
                {
                    queue = _callbackQueue[conversationId];
                } else
                {
                    _callbackQueue.Add(conversationId, queue);
                }
            }
            lock(queue)
            {
                queue.Add(content);
            }
        }

        private List<JObject> GetQueuedContent(string conversationId)
        {
            List<JObject> queue = new List<JObject>();
            lock (_callbackQueue)
            {
                if (_callbackQueue.ContainsKey(conversationId))
                {
                    queue = _callbackQueue[conversationId];
                    _callbackQueue.Remove(conversationId);
                }
            }
            return queue;
        }
    }
}

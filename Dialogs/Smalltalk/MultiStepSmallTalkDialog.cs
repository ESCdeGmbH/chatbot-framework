using Framework.Misc;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Dialogs.Smalltalk
{
    /// <summary>
    /// This class represents the base of dialogs which perform a user intration with multiple steps.
    /// The Name of the Subclass must match the schema "{TopicName}Dialog".
    /// </summary>
    /// <typeparam name="B">the bot interface</typeparam>
    /// <typeparam name="S">the specific bot services</typeparam>
    public abstract class MultiStepSmallTalkDialog<B, S> : BaseDialog<B, S> where B : IBot4Dialog where S : BotServices
    {
        /// <summary>
        /// The path to the folder of smalltalk template jsons. The name of the templates must match {TopicName}.json
        /// </summary>
        protected readonly string _smallTalkPath;
        private readonly string _top;

        /// <summary>
        /// Create a new MultiStepDialog.
        /// </summary>
        /// <param name="services">the bot services</param>
        /// <param name="bot">the bot itself</param>
        /// <param name="ID">the id of the dialog</param>
        /// <param name="smallTalkPath">the path to the smalltalk templates. The path to the folder of smalltalk template jsons. The name of the templates must match {TopicName}.json</param>
        /// <param name="resetOnEnd">Indicator whether EndDialogAsync() shall automatically invoke Reset() </param>
        protected MultiStepSmallTalkDialog(S services, B bot, string ID, string smallTalkPath, bool resetOnEnd = true) : base(services, bot, ID, resetOnEnd)
        {
            _smallTalkPath = smallTalkPath;
            if (!GetType().Name.EndsWith("Dialog"))
                throw new System.Exception("MultiStepSmallTalkDialogs have to match the schema: {Intent_Name}Dialog.cs");
            _top = GetType().Name.Substring(0, GetType().Name.Length - "Dialog".Length);
        }

        /// <summary>
        /// Generate a simple answer from the json template of this dialog.
        /// </summary>
        /// <param name="stepContext">the waterfall step context</param>
        /// <param name="cancellationToken">the cancellation token</param>
        /// <returns></returns>
        protected async Task<DialogTurnResult> SimpleAnswer(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string json = MiscExtensions.LoadEmbeddedResource(_smallTalkPath + "." + $"{_top}.json");
            List<string> answers = json == null ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(json);
            await TheBot.SendMessage(GetSimpleAnswer(answers), stepContext.Context);
            return await stepContext.NextAsync();
        }
        /// <summary>
        /// Extract answer from answers templates.
        /// </summary>
        /// <param name="answersTemplate">all templates</param>
        /// <returns>one answer</returns>
        protected virtual string GetSimpleAnswer(List<string> answersTemplate) => answersTemplate.Random();
    }
}

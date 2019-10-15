using Framework.Misc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Dialogs.Smalltalk
{
    /// <summary>
    /// This class represents a dialog which performs a user intration with one single step.
    /// </summary>
    /// <typeparam name="B">the bot interface</typeparam>
    /// <typeparam name="S">the specific bot services</typeparam>
    public class SingleStepSmalltalk<B, S> : BaseDialog<B, S> where B : IBot4Dialog where S : BotServices
    {
        private readonly string _smallTalkPath;

        /// <summary>
        /// Create a new SingleStepDialog.
        /// </summary>
        /// <param name="services">the bot services</param>
        /// <param name="bot">the bot itself</param>
        /// <param name="ID">the id of the dialog</param>
        /// <param name="smallTalkPath">the path to the smalltalk templates. The path to the folder of smalltalk template jsons. The name of the templates must match {TopicName}.json</param>
        public SingleStepSmalltalk(S services, B bot, string ID, string smallTalkPath) : base(services, bot, ID)
        {
            _smallTalkPath = smallTalkPath;
        }


        protected override void AddInitialSteps()
        {
            AddStep(ClassifySmallTalk);
        }

        private async Task<DialogTurnResult> ClassifySmallTalk(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var top = TheBot.Result.GetTopScoringIntent().intent.Substring("st_".Length);
            if (!File.Exists(Path.Combine(_smallTalkPath, $"{top}.json")))
            {
                await TheBot.SendMessage($"Ich habe noch nicht gelernt auf {top} zu antworten.", stepContext.Context);
            }
            else
            {
                string json = File.ReadAllText(Path.Combine(_smallTalkPath, $"{top}.json"));
                List<string> answers = JsonConvert.DeserializeObject<List<string>>(json);
                await TheBot.SendMessage(GetAnswer(answers), stepContext.Context);
            }
            return await stepContext.NextAsync();
        }

        /// <summary>
        /// Extract answer from answers templates.
        /// </summary>
        /// <param name="answersTemplate">all templates</param>
        /// <returns>one answer</returns>
        protected virtual string GetAnswer(List<string> answersTemplate) => answersTemplate.Random();
    }
}

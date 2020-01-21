using Framework.Classifier;
using Framework.Misc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        /// <summary>
        /// The path to the smalltalk jsons.
        /// </summary>
        protected readonly string SmallTalkPath;

        /// <summary>
        /// Create a new SingleStepDialog.
        /// </summary>
        /// <param name="services">the bot services</param>
        /// <param name="bot">the bot itself</param>
        /// <param name="ID">the id of the dialog</param>
        /// <param name="smallTalkPath">the path to the smalltalk templates. The path to the folder of smalltalk template jsons. The name of the templates must match {TopicName}.json</param>
        public SingleStepSmalltalk(S services, B bot, string ID, string smallTalkPath) : base(services, bot, ID)
        {
            SmallTalkPath = smallTalkPath;
        }


        protected override void AddInitialSteps()
        {
            AddStep(ClassifySmallTalk);
        }

        private async Task<DialogTurnResult> ClassifySmallTalk(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var top = TheBot.Result.GetTopScoringIntent().Item1.Substring("st_".Length);
            if (MiscExtensions.LoadEmbeddedResource(SmallTalkPath + "." + $"{top}.json") == null)
            {
                await TheBot.SendMessage($"Ich habe noch nicht gelernt auf {top} zu antworten.", stepContext.Context);
            }
            else
            {
                List<string> answers = FindSpecificAnswers(top);
                await TheBot.SendMessage(GetAnswer(answers), stepContext.Context);
            }
            return await stepContext.NextAsync();
        }

        /// <summary>
        /// Find answers based on the topics.
        /// </summary>
        /// <param name="top">the topic</param>
        /// <returns>a list of answer templates</returns>
        protected virtual List<string> FindSpecificAnswers(string top)
        {
            List<string> files = Assembly.GetEntryAssembly().GetManifestResourceNames().Where(s => s.StartsWith(SmallTalkPath))
                .Select(s => s.Substring(SmallTalkPath.Length + 1))
                .Select(f => Path.GetFileNameWithoutExtension(f)).Where(f => f.StartsWith(top)).ToList();
            if (!files.Any())
                throw new FileNotFoundException($"No file found for {top}");

            if (files.Count == 1)
            {
                string data = MiscExtensions.LoadEmbeddedResource(SmallTalkPath + "." + $"{files[0]}.json");
                return JsonConvert.DeserializeObject<List<string>>(data);
            }

            List<string> entities = files.Where(f => f.StartsWith($"{top}_")).Select(f => f.Split('_', 2)[1]).ToList();

            // Assume List Entity Name to be "E_{topic}"
            if (!TheBot.Result.Entities.TryGetValue($"E_{top}", out List<IEntity> foundEntities))
            {
                string data = MiscExtensions.LoadEmbeddedResource(SmallTalkPath + "." + $"{top}.json");
                return JsonConvert.DeserializeObject<List<string>>(data);
            }

            // Assume List Entity
            var roots = foundEntities.Where(e => e.EType == EntityType.Group).Cast<GroupEntity>().Select(e => e.Shape);

            List<string> paths = entities.Where(e => roots.Contains(e)).Select(e => SmallTalkPath + "." + $"{top}_{e}.json").ToList();
            if (!paths.Any())
            {
                string data = MiscExtensions.LoadEmbeddedResource(SmallTalkPath + "." + $"{top}.json");
                return JsonConvert.DeserializeObject<List<string>>(data);
            }

            return paths.SelectMany(p => JsonConvert.DeserializeObject<List<string>>(MiscExtensions.LoadEmbeddedResource(p))).ToList();
        }

        /// <summary>
        /// Extract answer from answers templates.
        /// </summary>
        /// <param name="answersTemplate">all templates</param>
        /// <returns>one answer</returns>
        protected virtual string GetAnswer(List<string> answersTemplate) => answersTemplate.Random();
    }
}

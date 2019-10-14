using AdaptiveCards;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Misc
{
    /// <summary>
    /// Helper class to build AdaptiveCards.
    /// </summary>
    public static class CardBuilder
    {
        #region Itemize
        /// <summary>
        /// Create a enumeration.
        /// </summary>
        /// <param name="title">the title</param>
        /// <param name="items">the items</param>
        /// <returns>the enumeration</returns>
        public static Attachment BuildItemize(string title, IEnumerable<string> items)
        {
            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion());
            if (!items.Any())
                items = new string[] { "Nothing" };

            AdaptiveFactSet facts = new AdaptiveFactSet();
            foreach (var item in items)
                facts.Facts.Add(new AdaptiveFact("*", item));

            card.Body.Add(new AdaptiveTextBlock($"**{title}**") { MaxLines = 3, Wrap = true });
            card.Body.Add(facts);

            var attachement = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
            return attachement;
        }
        #endregion

        #region Prompt

        /// <summary>
        /// Defines the prompt options for an adaptive card prompt.
        /// </summary>
        public struct PromptOption
        {
            /// <summary>
            /// The title of the option.
            /// </summary>
            public string Title;
            /// <summary>
            /// The actual text what will be entered by clicking on this option.
            /// </summary>
            public string Text;

            /// <summary>
            /// Creates a prompt option.
            /// </summary>
            /// <param name="title">The title of the option.</param>
            /// <param name="text">The text that will be shown by using this option.</param>
            public PromptOption(string title, string text)
            {
                Title = title;
                Text = text;
            }
        }

        /// <summary>
        /// Build a yes/no option prompt.
        /// </summary>
        /// <param name="question">The question text.</param>
        /// <returns>The prompt.</returns>
        public static Attachment BuildYesNo(string question) => BuildOptionPrompt(question, new PromptOption("Yes", "Yes"), new PromptOption("No", "No"));

        /// <summary>
        /// Build a prompt with multiple options.
        /// </summary>
        /// <param name="intro">The intro text or question.</param>
        /// <param name="options">All possible options.</param>
        /// <returns>The prompt.</returns>
        public static Attachment BuildOptionPrompt(string intro, params PromptOption[] options)
        {

            List<AdaptiveAction> buttons = new List<AdaptiveAction>();
            foreach (PromptOption o in options)
            {
                var button = new AdaptiveSubmitAction()
                {
                    Title = o.Title,
                    Data = o.Text,

                };
                buttons.Add(button);
            }
            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion());
            AdaptiveTextBlock header = new AdaptiveTextBlock(intro) { Wrap = true };
            card.Body.Add(header);
            card.Actions = buttons;

            var attachement = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
            return attachement;
        }

        #endregion
    }
}

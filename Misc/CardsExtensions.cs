using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;

namespace Framework.Misc
{
    /// <summary>
    /// Extensions for CardBuilder.
    /// <see cref="CardBuilder"/>
    /// </summary>
    public static class CardsExtensions
    {
        /// <summary>
        /// Converts an adaptive card to an attachement.
        /// </summary>
        /// <param name="_this">The adaptive card.</param>
        /// <returns>The attachement.</returns>
        public static Attachment ToAttachment(this AdaptiveCard _this) => new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = _this
        };

        /// <summary>
        /// Converts an attachement to an activity.
        /// </summary>
        /// <param name="attachment">The attachement.</param>
        /// <param name="context">The context of the bot.</param>
        /// <returns>The activity.</returns>
        public static Activity ToActivity(this Attachment attachment, ITurnContext context)
        {
            var response = context.Activity.CreateReply();
            response.Attachments = new List<Attachment>() { attachment };
            return response;
        }

        /// <summary>
        /// Converts an adaptive card to an activity.
        /// </summary>
        /// <param name="card">The adaptive card.</param>
        /// <param name="context">The context of the bot.</param>
        /// <returns>The activity.</returns>
        public static Activity ToActivity(this AdaptiveCard card, ITurnContext context) => card.ToAttachment().ToActivity(context);

    }
}

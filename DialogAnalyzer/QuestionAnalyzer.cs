using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.DialogAnalyzer
{
    public abstract class QuestionAnalyzer
    {   /// <summary>
        /// Recognize a new input.
        /// </summary>
        /// <param name="turnContext">The bot context with input.</param>
        public abstract void Recognize(ITurnContext turnContext);
        /// <summary>
        /// Retrieve the found question types.
        /// </summary>
        /// <returns>The found question types with their probability.</returns>
        public abstract Task<List<Tuple<QuestionType, double>>> GetQuestionTypes();

        /// <summary>
        /// Builds a wrapper to execute actions according to the question type.
        /// </summary>
        /// <param name="none">Mandatory parameter, which will be executed if no question had an probility over threshold or no handler is definied for the classified question.</param>
        /// <param name="how">The handler for "how" questions.</param>
        /// <param name="howMany">The handler for "how many" questions.</param>
        /// <param name="what">The handler for "what" questions.</param>
        /// <param name="where">The handler for "where" questions.</param>
        /// <param name="who">The handler for "who" questions.</param>
        /// <param name="why">The handler for "why" questions.</param>
        /// <param name="when">The handler for "when" questions.</param>
        /// <param name="howLong">The handler for "how long" questions.</param>
        /// <returns></returns>
        public async Task HandleQuestion(Action none, Action how = null, Action howMany = null, Action what = null, Action where = null, Action who = null, Action why = null, Action when = null, Action howLong = null)
        {
            if (none == null)
                throw new ArgumentNullException(nameof(none));

            var questionTypes = await GetQuestionTypes();

            QuestionType types = GetMaximialIntents(questionTypes);
            Dictionary<QuestionType, Action> handlers = new Dictionary<QuestionType, Action> {
                { QuestionType.How, how },
                { QuestionType.HowMany, howMany },
                { QuestionType.What, what },
                { QuestionType.Where, where },
                { QuestionType.Who, who },
                { QuestionType.Why, why },
                { QuestionType.When, when },
                { QuestionType.HowLong, howLong },
            };

            if (!handlers.TryGetValue(types, out Action handler))
                handler = none;
            if (handler == null)
                handler = none;

            handler();
        }

        /// <summary>
        /// Determines the top scoring of all questiontypes.
        /// </summary>
        /// <param name="foundTypes">The types from the classifier.</param>
        /// <returns>The most likely type.</returns>
        public static QuestionType GetMaximialIntents(List<Tuple<QuestionType, double>> foundTypes)
        {
            if (foundTypes.Count == 0)
                return QuestionType.None;


            QuestionType maxKey = foundTypes[0].Item1;
            double maxValue = foundTypes[0].Item2;


            for (int i = 0; i < foundTypes.Count; i++)
            {
                if (foundTypes[i].Item2 > maxValue)
                {
                    maxKey = foundTypes[i].Item1;
                    maxValue = foundTypes[i].Item2;
                }
            }

            return maxKey;

        }
    }
}

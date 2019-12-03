using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Classifier
{
    /// <summary>
    /// Defines the interface for classifier connections.
    /// </summary>
    public interface IClassifier
    {
        /// <summary>
        /// Interprets the current user input.
        /// </summary>
        /// <param name="context">Current context of the dialog.</param>
        /// <param name="cancellationToken">Cancellation token of the current context.</param>
        /// <returns></returns>
        Task Recognize(ITurnContext context, CancellationToken cancellationToken);
        /// <summary>
        /// Retrieves the last classification result.
        /// </summary>
        /// <param name="cleanup">Indicates whether the entities shell be preprocessed.</param>
        /// <returns></returns>
        ClassifierResult GetResult(bool cleanup = true);
    }
    /// <summary>
    /// Defines a classification result.
    /// </summary>
    public sealed class ClassifierResult
    {
        /// <summary>
        /// Create a new classification result.
        /// </summary>
        /// <param name="text">The given input.</param>
        /// <param name="intents">The found intents with its scorings.</param>
        /// <param name="entities">The found entities.</param>
        public ClassifierResult(string text, Dictionary<string, double> intents, Dictionary<string, List<IEntity>> entities)
        {
            Text = text;
            Intents = intents;
            Entities = entities;
        }

        /// <summary>
        /// The given input.
        /// </summary>
        public string Text { get; }
        /// <summary>
        /// The found intents with its scorings.
        /// </summary>
        public Dictionary<string, double> Intents { get; }
        /// <summary>
        /// The found entities.
        /// </summary>
        public Dictionary<string, List<IEntity>> Entities { get; }

        /// <summary>
        /// Retrieves the top scoring intent and its score.
        /// </summary>
        /// <returns>Tuple of top intent and score.</returns>
        public (string, double) GetTopScoringIntent()
        {

            (string, double) max = (null, double.NaN);

            double maxScore = double.NegativeInfinity;
            foreach (string key in Intents.Keys)
            {
                if (Intents[key] > maxScore)
                {
                    max = (key, Intents[key]);
                    maxScore = Intents[key];
                }
            }
            return max;
        }
    }

    /// <summary>
    /// Defines an entity.
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// The text snippet matched to the entity.
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Start index of the snippet.
        /// </summary>
        int StartIndex { get; }
        /// <summary>
        /// End index of the snippet.
        /// </summary>
        int EndIndex { get; }
        /// <summary>
        /// Type of the found entity.
        /// </summary>
        EntityType EType { get; }
    }

    /// <summary>
    /// Defines the type of an entity. May be extended in later versions.
    /// </summary>
    public enum EntityType
    {
        /// <summary>
        /// Defines the type of a grouped Entity. See Luis List Entity.
        /// </summary>
        /// <see cref="GroupEntity"/>
        Group,
        /// <summary>
        /// Defines the type of a time specification.
        /// </summary>
        /// <see cref="TimeEntity"/>
        Time
    }

    /// <summary>
    /// Defines the result of an entity which defines a group of shapes (and their associated text snippets).
    /// </summary>
    public sealed class GroupEntity : IEntity
    {
        /// <summary>
        /// Create a new result.
        /// </summary>
        /// <param name="text">The text snippet.</param>
        /// <param name="startIndex">The start index of the snippet.</param>
        /// <param name="endIndex">The end index of the snippet.</param>
        /// <param name="group">The associated group.</param>
        /// <param name="shape">Defines the shape.</param>
        public GroupEntity(string text, int startIndex, int endIndex, string group, string shape)
        {
            Text = text;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Group = group;
            Shape = shape;
        }
        
        public string Text { get; }
        public int StartIndex { get; }
        public int EndIndex { get; }

        /// <summary>
        /// The name of the group.
        /// </summary>
        public string Group { get; }
        /// <summary>
        /// The name of the shape.
        /// </summary>
        public string Shape { get; }
        
        public EntityType EType => EntityType.Group;
    }

    /// <summary>
    /// Defines the result of an entity which defines times.
    /// </summary>
    public sealed class TimeEntity : IEntity
    {
        /// <summary>
        /// Creates a time entity.
        /// </summary>
        /// <param name="text">The text snippet.</param>
        /// <param name="startIndex">The start index of the snippet.</param>
        /// <param name="endIndex">The end index of the snippet.</param>
        /// <param name="times">Datetimes associated with the datetime results.</param>
        public TimeEntity(string text, int startIndex, int endIndex, List<DateTime> times)
        {
            Text = text;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Times = times;
        }

        public string Text { get; }
        public int StartIndex { get; }
        public int EndIndex { get; }
        /// <summary>
        /// The found datetimes.
        /// </summary>
        public List<DateTime> Times { get; }
        public EntityType EType => EntityType.Time;
    }
}

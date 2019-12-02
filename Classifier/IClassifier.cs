using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Classifier
{
    public interface IClassifier
    {
        Task Recognize(ITurnContext context, CancellationToken cancellationToken);
        ClassifierResult GetResult(bool cleanup = true);
    }
    public sealed class ClassifierResult
    {
        public ClassifierResult(string text, Dictionary<string, double> intents, Dictionary<string, List<IEntity>> entities)
        {
            Text = text;
            Intents = intents;
            Entities = entities;
        }

        public string Text { get; }
        public Dictionary<string, double> Intents { get; }
        public Dictionary<string, List<IEntity>> Entities { get; }

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

    public interface IEntity
    {
        string Text { get; }
        int StartIndex { get; }
        int EndIndex { get; }
        EntityType EType { get; }
    }

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

    public sealed class GroupEntity : IEntity
    {
        public GroupEntity(string text, int startIndex, int endIndex, string group, string normalizedValue)
        {
            Text = text;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Group = group;
            NormalizedValue = normalizedValue;
        }

        public string Text { get; }
        public int StartIndex { get; }
        public int EndIndex { get; }

        // NameOfEntity
        public string Group { get; }
        public string NormalizedValue { get; }

        public EntityType EType => EntityType.Group;
    }

    public sealed class TimeEntity : IEntity
    {
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
        public List<DateTime> Times { get; }
        public EntityType EType => EntityType.Time;
    }
}

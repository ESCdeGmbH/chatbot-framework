using Framework.Misc;
using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.DialogAnalyzer
{
    public class RegexQuestionAnalyzer : QuestionAnalyzer
    {
        private List<Tuple<QuestionType, double>> _result;

        private static readonly List<Language> Configs = new List<Language> {
            Language.Deutsch
        };

        /// <summary>
        /// Creates the analyzer.
        /// </summary>
        /// <param name="lang">Defines the language.</param>
        /// <exception cref="ArgumentException">unsupported language.</exception>
        public RegexQuestionAnalyzer(Language lang)
        {
            if (!Configs.Contains(lang))
                throw new ArgumentException("Your Language is not supported.");
        }
        public override Task<List<Tuple<QuestionType, double>>> GetQuestionTypes()
        {
            return _result == null ? Task.FromResult(new List<Tuple<QuestionType, double>>()) : Task.FromResult(new List<Tuple<QuestionType, double>>(_result));
        }

        public override void Recognize(ITurnContext turnContext)
        {
            _result = new List<Tuple<QuestionType, double>>();
            string text = turnContext.Activity.Text.ToLower();

            List<string> how = new List<string> { "wie" };
            List<string> howLong = new List<string> { "wie lange", "wie lang", "wie viele stunden", "wie viele tage", "wie viele minuten", "wieviele stunden", "wieviele tage", "wieviele minuten" };
            List<string> howMany = new List<string> { "wie viele", "wie viel", "wieviele", "wieviel" };
            List<string> what = new List<string> { "was", "welche", "welcher", "welches", "welchen", "was für ein", "was für eine", "welchem", "wofür", "als was" };
            List<string> when = new List<string> { "wann", "welche zeit", "welcher zeit", "um welche zeit", "zu welcher zeit", "um wieviel uhr", "wieviel uhr", "zu welcher uhrzeit" };
            List<string> where = new List<string> { "wo", "wohin", "woher", "von wo", "an welchem ort" };
            List<string> who = new List<string> { "wer", "wem", "wessen", "wen" };
            List<string> why = new List<string> { "warum", "weshalb", "wieso", "weswegen", "wozu" };

            
            if (when.Any(h => text.StartsWith($"{h} ")) || text.Contains(" uhr "))
                _result.Add(new Tuple<QuestionType, double>(QuestionType.When, 1));
            else if (where.Any(h => text.StartsWith($"{h} ")) || text.Contains(" ort "))
                _result.Add(new Tuple<QuestionType, double>(QuestionType.Where, 1));
            else if (who.Any(h => text.StartsWith($"{h} ")) || text.Contains(" person "))
                _result.Add(new Tuple<QuestionType, double>(QuestionType.Who, 1));
            else if (howLong.Any(h => text.StartsWith($"{h} ")))
                _result.Add(new Tuple<QuestionType, double>(QuestionType.HowLong, 1));
            else if (howMany.Any(h => text.StartsWith($"{h} ")))
                _result.Add(new Tuple<QuestionType, double>(QuestionType.HowMany, 1));
            else if (how.Any(h => text.StartsWith($"{h} ")))
                _result.Add(new Tuple<QuestionType, double>(QuestionType.How, 1));
            else if (why.Any(h => text.StartsWith($"{h} ")) || text.Contains(" grund ") || text.Contains(" zweck "))
                _result.Add(new Tuple<QuestionType, double>(QuestionType.Why, 1));
            else if (what.Any(h => text.StartsWith($"{h} ")))
                _result.Add(new Tuple<QuestionType, double>(QuestionType.What, 1));
        }
    }
}

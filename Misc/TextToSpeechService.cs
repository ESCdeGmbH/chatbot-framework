using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Framework.Misc
{
    /// <summary>
    /// This class is used to understand the spoken language  by the user
    /// </summary>
    public static class TextToSpeechService
    {
        private const string GenderMale = "male";
        private const string GenderFemale = "female";

        /// <summary>
        /// Generates the SSML.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="language">The language.</param>
        /// <returns>System.String.</returns>
        public static string GenerateSsml(string message, string language)
        {
            try
            {
                // Voice Fonts don't support SSML right now so let's strip out tags
                var messageDoc = XDocument.Parse($"<root>{message}</root>");
                message = string.Join(" ", messageDoc.Descendants().Where(x => !x.HasElements && !string.IsNullOrEmpty(x.Value)).Select(x => x.Value?.Trim()));
            }
            catch (Exception) { }


            var voice = GetLocaleVoiceName(language, GenderFemale);
            var voiceName = voice.Value;
            var voiceLanguage = voice.Key;

            XNamespace ns = "http://www.w3.org/2001/10/synthesis";
            var ssmlDoc = new XDocument(
                new XElement(
                    ns + "speak",
                    new XAttribute("version", "1.0"),
                    new XAttribute(XNamespace.Xmlns + "mstts", "http://www.w3.org/2001/mstts"),
                    new XAttribute(XNamespace.Xmlns + "emo", "http://www.w3.org/2009/10/emotionml"),
                    new XAttribute(XNamespace.Xml + "lang", voiceLanguage),
                    new XElement(
                        ns + "voice",
                        new XAttribute("name", voiceName),
                        new XRaw(message))));

            return ssmlDoc.ToString();

        }
        private static KeyValuePair<string, string> GetLocaleVoiceName(string language, string gender)
        {
            var dictionary = new Dictionary<string, KeyValuePair<string, string>>();

            // List here: https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/supported-languages
            const string prefix = "Microsoft Server Speech Text to Speech Voice";
            if (GenderMale.Equals(gender))
            {
                dictionary["en"] = new KeyValuePair<string, string>("en-US", $"{prefix} (en-US, BenjaminRUS)");
                dictionary["es"] = new KeyValuePair<string, string>("es-ES", $"{prefix} (es-ES, Pablo, Apollo)");
                dictionary["fr"] = new KeyValuePair<string, string>("fr-FR", $"{prefix} (fr-FR, Paul, Apollo)");
                dictionary["de"] = new KeyValuePair<string, string>("de-DE", $"{prefix} (de-DE, Stefan, Apollo)");
                dictionary["ja"] = new KeyValuePair<string, string>("ja-JP", $"{prefix} (ja-JP, Ichiro, Apollo)");
                dictionary["ru"] = new KeyValuePair<string, string>("ru-RU", $"{prefix} (ru-RU, Pavel, Apollo)");
                dictionary["zh"] = new KeyValuePair<string, string>("zh-CN", $"{prefix} (zh-CN, Kangkang, Apollo)");
            }
            else
            {
                dictionary["en"] = new KeyValuePair<string, string>("en-US", $"{prefix} (en-US, JessaRUS)");
                dictionary["es"] = new KeyValuePair<string, string>("es-ES", $"{prefix} (es-ES, Laura, Apollo)");
                dictionary["fr"] = new KeyValuePair<string, string>("fr-FR", $"{prefix} (fr-FR, Julie, Apollo)");
                dictionary["de"] = new KeyValuePair<string, string>("de-DE", $"{prefix} (de-DE, Hedda)");
                dictionary["ja"] = new KeyValuePair<string, string>("ja-JP", $"{prefix} (ja-JP, Ayumi, Apollo)");
                dictionary["ru"] = new KeyValuePair<string, string>("ru-RU", $"{prefix} (ru-RU, Irina, Apollo)");
                dictionary["zh"] = new KeyValuePair<string, string>("zh-CN", $"{prefix} (zh-CN, Yaoyao, Apollo)");
            }

            dictionary["it"] = new KeyValuePair<string, string>("it-IT", $"{prefix} (it-IT, Cosimo, Apollo)");
            dictionary["ar"] = new KeyValuePair<string, string>("ar-EG", $"{prefix} (ar-EG, Hoda)");
            dictionary["hi"] = new KeyValuePair<string, string>("hi-IN", $"{prefix} (hi-IN, Kalpana, Apollo)");
            dictionary["ko"] = new KeyValuePair<string, string>("ko-KR", $"{prefix} (ko-KR,HeamiRUS)");
            dictionary["pt"] = new KeyValuePair<string, string>("pt-BR", $"{prefix} (pt-BR, Daniel, Apollo)");

            var key = language.Split("-")[0];
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }

            return dictionary["en"];
        }

        private class XRaw : XText
        {
            public XRaw(string text) : base(text) { }
            /// <summary>
            /// Writes this node to an <see cref="T:System.Xml.XmlWriter"></see>.
            /// </summary>
            public override void WriteTo(System.Xml.XmlWriter writer) => writer.WriteRaw(Value);
        }
    }
}

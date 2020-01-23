using DDictionary.Domain.Entities;

namespace DDictionary.Presentation.Converters
{
    public static class PartOfSpeechTranslator
    {
        public static string ToShortString(this PartOfSpeech pos)
        {
            //TODO: Put short strings for the PartOfSpeech into resources.

            switch(pos)
            {
                case PartOfSpeech.Unknown: return "-";
                case PartOfSpeech.Noun: return "n";
                case PartOfSpeech.Pronoun: return "pron";
                case PartOfSpeech.Verb: return "v";
                case PartOfSpeech.Adjective: return "a";
                case PartOfSpeech.Adverb: return "adv";
                case PartOfSpeech.Preposition: return "prep";
                case PartOfSpeech.Conjunction: return "cj";
                case PartOfSpeech.Interjection: return "int";

                default: 
                    return pos.ToString();
            }
        }

        public static string ToFullString(this PartOfSpeech pos)
        {
            //TODO: Put full strings for the PartOfSpeech into resources.

            switch(pos)
            {
                case PartOfSpeech.Unknown: return "unknown";
                case PartOfSpeech.Noun: return "noun";
                case PartOfSpeech.Pronoun: return "pronoun";
                case PartOfSpeech.Verb: return "verb";
                case PartOfSpeech.Adjective: return "adjective";
                case PartOfSpeech.Adverb: return "adverb";
                case PartOfSpeech.Preposition: return "preposition";
                case PartOfSpeech.Conjunction: return "conjunction";
                case PartOfSpeech.Interjection: return "interjection";

                default:
                    return pos.ToString();
            }
        }
    }
}

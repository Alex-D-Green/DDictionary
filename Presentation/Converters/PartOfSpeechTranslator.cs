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
    }
}

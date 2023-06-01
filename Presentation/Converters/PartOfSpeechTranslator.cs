using DDictionary.Domain.Entities;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation.Converters
{
    public static class PartOfSpeechTranslator
    {
        public static string ToShortString(this PartOfSpeech pos)
        {
            switch(pos)
            {
                case PartOfSpeech.Unknown:      return PrgResources.POFShortUnknown;
                case PartOfSpeech.Noun:         return PrgResources.POFShortNoun;
                case PartOfSpeech.Pronoun:      return PrgResources.POFShortPronoun;
                case PartOfSpeech.Verb:         return PrgResources.POFShortVerb;
                case PartOfSpeech.Adjective:    return PrgResources.POFShortAdjective;
                case PartOfSpeech.Adverb:       return PrgResources.POFShortAdverb;
                case PartOfSpeech.Preposition:  return PrgResources.POFShortPreposition;
                case PartOfSpeech.Conjunction:  return PrgResources.POFShortConjunction;
                case PartOfSpeech.Interjection: return PrgResources.POFShortInterjection;
                case PartOfSpeech.Numeral:      return PrgResources.POFShortNumeral;

                default: 
                    return pos.ToString();
            }
        }

        public static string ToFullString(this PartOfSpeech pos)
        {
            switch(pos)
            {
                case PartOfSpeech.Unknown:      return PrgResources.POFFullUnknown;
                case PartOfSpeech.Noun:         return PrgResources.POFFullNoun;
                case PartOfSpeech.Pronoun:      return PrgResources.POFFullPronoun;
                case PartOfSpeech.Verb:         return PrgResources.POFFullVerb;
                case PartOfSpeech.Adjective:    return PrgResources.POFFullAdjective;
                case PartOfSpeech.Adverb:       return PrgResources.POFFullAdverb;
                case PartOfSpeech.Preposition:  return PrgResources.POFFullPreposition;
                case PartOfSpeech.Conjunction:  return PrgResources.POFFullConjunction;
                case PartOfSpeech.Interjection: return PrgResources.POFFullInterjection;
                case PartOfSpeech.Numeral:      return PrgResources.POFFullNumeral;

                default:
                    return pos.ToString();
            }
        }
    }
}

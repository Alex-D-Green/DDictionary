using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDictionary.Domain
{
    //https://www.thoughtco.com/part-of-speech-english-grammar-1691590

    public enum PartOfSpeech: byte
    {
        Unknown,
        Noun,
        Pronoun,
        Verb,
        Adjective,
        Adverb,
        Preposition,
        Conjunction, //and, but, or, so, yet, with
        Interjection //ah, whoops, ouch, yabba dabba do!
    }


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

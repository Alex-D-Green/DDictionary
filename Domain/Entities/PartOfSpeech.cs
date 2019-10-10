
namespace DDictionary.Domain.Entities
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
}

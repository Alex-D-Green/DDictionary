using System;
using System.Collections.Generic;

namespace DDictionary.Domain
{
    public class Clause
    {
        public int Id { get; set; }
        public string Sound { get; set; }
        public string Word { get; set; }
        public string Transcription { get; set; }
        public IReadOnlyCollection<Translation> Translations { get; set; }
        public string Context { get; set; }
        public IReadOnlyCollection<Relation> Relations { get; set; }
        public DateTime Added { get; set; }
        public DateTime Updated { get; set; }
        public WordGroup Group { get; set; }
    }
}

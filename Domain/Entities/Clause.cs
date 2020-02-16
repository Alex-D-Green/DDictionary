using System;
using System.Collections.Generic;

namespace DDictionary.Domain.Entities
{
    public class Clause
    {
        public int Id { get; set; }
        public string Sound { get; set; }
        public string Word { get; set; }
        public string Transcription { get; set; }
        public List<Translation> Translations { get; set; } = new List<Translation>();
        public string Context { get; set; }
        public List<Relation> Relations { get; set; } = new List<Relation>();
        public DateTime Added { get; set; }
        public DateTime Updated { get; set; }
        public WordGroup Group { get; set; }
    }
}

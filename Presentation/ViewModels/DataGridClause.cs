using System;

using DDictionary.Domain.Entities;


namespace DDictionary.Presentation.ViewModels
{
    public sealed class DataGridClause
    {
        public int Id { get; set; }
        public string Sound { get; set; }
        public string Word { get; set; }
        public string Transcription { get; set; }
        public string Translations { get; set; }
        public string Context { get; set; }
        public string Relations { get; set; }
        public bool HasRelations { get; set; }
        public DateTime Added { get; set; }
        public DateTime Updated { get; set; }
        public DateTime Watched { get; set; }
        public int WatchedCount { get; set; }
        public WordGroup Group { get; set; }
    }
}

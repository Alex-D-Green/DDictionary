using System;

using DDictionary.Domain.Entities;


namespace DDictionary.Presentation.ViewModels
{
    public sealed class DataGridClause
    {
        public int Id { get; set; }
        public string Sound { get; set; }
        public string Word { get; set; }
        public AsteriskType AsteriskType { get; set; }
        public string Transcription { get; set; }

        /// <summary>Translations according to selected part of speech.</summary>
        public string Translations { get; set; }

        /// <summary>
        /// All possible translations (all parts of speech).
        /// <para><see langword="null"/> if there is no difference between 
        /// <see cref="AllTranslations"/> and <see cref="Translations"/>.
        /// </para>
        /// </summary>
        public string AllTranslations { get; set; }
        
        public string Context { get; set; }
        public string Relations { get; set; }
        public bool HasRelations { get; set; }
        public DateTime Added { get; set; }
        public DateTime Updated { get; set; }
        public DateTime Watched { get; set; }
        public int WatchedCount { get; set; }
        public WordGroup Group { get; set; }
        
        public double UnderstandingScore { get; set; }
        public double SpellingScore { get; set; }
        public double ListeningScore { get; set; }
    }
}

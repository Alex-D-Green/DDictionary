using System;

namespace DDictionary.DAL.ViewModels
{
    public sealed class ClauseDataGridDTO
    {
        public int Id { get; set; }
        public string Sound { get; set; }
        public string Word { get; set; }
        public string Transcription { get; set; }
        public string Translations { get; set; }
        public string Context { get; set; }
        public string Relations { get; set; }
        public DateTime Added { get; set; }
        public DateTime Updated { get; set; }
        public string Group { get; set; }
    }
}

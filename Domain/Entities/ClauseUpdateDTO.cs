
namespace DDictionary.Domain.Entities
{
    public class ClauseUpdateDTO
    {
        public int Id { get; set; }
        public string Sound { get; set; }
        public string Word { get; set; }
        public string Transcription { get; set; }
        public string Context { get; set; }
        public WordGroup Group { get; set; }
    }
}

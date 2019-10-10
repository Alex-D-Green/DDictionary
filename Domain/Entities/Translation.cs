
namespace DDictionary.Domain.Entities
{
    public class Translation
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public PartOfSpeech Part { get; set; }
    }
}

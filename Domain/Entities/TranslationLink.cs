namespace DDictionary.Domain.Entities
{
    public class TranslationLink
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public Translation Translation { get; set; }
    }
}

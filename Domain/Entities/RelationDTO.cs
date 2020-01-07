
namespace DDictionary.Domain.Entities
{
    public sealed class RelationDTO
    {
        public int Id { get; set; }
        public int ToWordId { get; set; }
        public string ToWord { get; set; }
        public string Description { get; set; }
        public bool DescriptionWasChanged { get; set; }
        public bool MakeInterconnected { get; set; }
    }
}

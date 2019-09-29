
namespace DDictionary.Domain
{
    public class Relation
    {
        public int Id { get; set; }
        public Clause From { get; set; }
        public Clause To { get; set; }
        public string Description { get; set; }
    }
}

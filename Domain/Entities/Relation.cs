
namespace DDictionary.Domain.Entities
{
    public class Relation
    {
        public int Id { get; set; }
        public Clause ToClause { get; set; }
        public string Description { get; set; }
    }
}

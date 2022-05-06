using System;

namespace DDictionary.Domain.Entities
{
    public class AsteriskDTO
    {
        public int ClauseId { get; set; }
        public AsteriskType Type { get; set; }
        public DateTime? MeaningLastTrain { get; set; }
        public DateTime? SpellingLastTrain { get; set; }
        public DateTime? ListeningLastTrain { get; set; }
    }
}

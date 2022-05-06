using System;

using DDictionary.Domain.Entities;


namespace DDictionary.Domain.DTO
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

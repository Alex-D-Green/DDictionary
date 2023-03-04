using System;
using DDictionary.Domain.Entities;

namespace DDictionary.Domain.DTO
{
    public class TrainingHistoryEntryDTO
    {
        public int Id { get; set; }

        public int ClauseId { get; set; }

        public TestType TrainingType { get; set; }

        public DateTime TrainingDate { get; set; }

        public bool Success { get; set; }

        public string Word { get; set; }

        public WordGroup Group { get; set; }

        public AsteriskType? Type { get; set; }
    }
}

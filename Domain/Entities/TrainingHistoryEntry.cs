using System;

namespace DDictionary.Domain.Entities
{
    public class TrainingHistoryEntry
    {
        public int Id { get; set; }

        public int ClauseId { get; set; }

        public TestType TrainingType { get; set; }

        public DateTime TrainingDate { get; set; }

        public bool Success { get; set; }
    }
}

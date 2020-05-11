using System;


namespace DDictionary.Domain.Entities
{
    public class TrainingStatisticDTO
    {
        public TestType TestType { get; set; }
        public int ClauseId { get; set; }
        public int Success { get; set; }
        public int Fail { get; set; }
        public DateTime LastTraining { get; set; }
    }
}

using System;


namespace DDictionary.Domain.Entities
{
    public class ShortTrainingStatistic
    {
        public TestType TestType { get; set; }

        public int Count { get; set; }

        public DateTime LastTraining { get; set; }
    }
}

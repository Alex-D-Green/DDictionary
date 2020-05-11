using System.Collections.Generic;


namespace DDictionary.Domain.Entities
{
    public class WordTrainingStatisticDTO
    {
        public int Id { get; set; }
        public string Word { get; set; }

        public IReadOnlyCollection<TrainingStatisticDTO> Statistics { get; set; }
    }
}

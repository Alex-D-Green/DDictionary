namespace DDictionary.Domain.Entities
{
    /// <summary>
    /// Type of special mark.
    /// This mark shows what kind of trainings should be forced for the word.
    /// </summary>
    public enum AsteriskType
    {
        /// <summary>There is no special mark.</summary>
        None,

        /// <summary>All kinds of trainings should be forced.</summary>
        AllTypes,

        /// <summary>Meaning trainings should be forced.</summary>
        Meaning,

        /// <summary>Spelling trainings should be forced.</summary>
        Spelling,

        /// <summary>Listening trainings should be forced.</summary>
        Listening
    }
}

using System;
using System.Collections.Generic;

using DDictionary.Domain.Entities;


namespace DDictionary.Presentation.Testing
{
    /// <summary>
    /// Info about the user's answer for a round.
    /// </summary>
    public class TestAnswer
    {
        /// <summary>Asked word.</summary>
        public Clause Word { get; set; }

        /// <summary>A word that's given as the answer.</summary>
        public Clause GivenAnswer { get; set; }

        /// <summary>The answer considered as correct.</summary>
        public bool Correct { get; set; }

        /// <summary>Time was spent on answer.</summary>
        public TimeSpan Time { get; set; }

        /// <summary>Tries were used to give the answer.</summary>
        public int Tries { get; set; }

        /// <summary>Shows that this word was deleted from the dictionary.</summary>
        public bool Deleted { get; set; }
    }


    /// <summary>
    /// Compares two answers by their words' ids, nothing more.
    /// </summary>
    public class TestAnswerSameWordComparer: IEqualityComparer<TestAnswer>
    {
        public bool Equals(TestAnswer x, TestAnswer y)
        {
            return Int32.Equals(x?.Word.Id, y?.Word.Id);
        }

        public int GetHashCode(TestAnswer obj)
        {
            return obj.Word.Id.GetHashCode();
        }
    }
}

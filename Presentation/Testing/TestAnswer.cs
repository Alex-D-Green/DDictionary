using System;
using System.Collections.Generic;

using DDictionary.Domain.Entities;


namespace DDictionary.Presentation.Testing
{
    public class TestAnswer
    {
        public Clause Word { get; set; }

        public Clause GivenAnswer { get; set; }

        public bool Correct { get; set; }

        public TimeSpan Time { get; set; }

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

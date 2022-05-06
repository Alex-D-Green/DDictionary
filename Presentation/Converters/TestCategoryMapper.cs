using System.Collections.Generic;
using System.Linq;
using DDictionary.Domain.Entities;

namespace DDictionary.Presentation.Converters
{
    /// <summary>
    /// Mapper that groups trainings into categories.
    /// </summary>
    /// <remarks>
    /// <code>
    ///              meaning  spelling  listening
    /// tr-word         *
    /// word-tr         *
    /// constr          *        *
    /// listening                *         *
    /// sprint          *
    /// </code>
    /// </remarks>
    public static class TestCategoryMapper
    {
        /// <summary>
        /// List of test types which included into "Meaning" category.
        /// </summary>
        public readonly static IReadOnlyList<TestType> MeaningTestTypes = new[] { 
            TestType.TranslationWord,
            TestType.WordTranslation,
            TestType.WordConstructor,
            TestType.Sprint
        };

        /// <summary>
        /// List of test types which included into "Spelling" category.
        /// </summary>
        public readonly static IReadOnlyList<TestType> SpellingTestTypes = new[] {
            TestType.WordConstructor,
            TestType.Listening
        };

        /// <summary>
        /// List of test types which included into "Listening" category.
        /// </summary>
        public readonly static IReadOnlyList<TestType> ListeningTestTypes = new[] {
            TestType.Listening
        };


        /// <summary>
        /// Is test type <paramref name="testType"/> in the "Meaning" category.
        /// </summary>
        /// <param name="testType">Test type.</param>
        /// <returns><c>true</c> if this test type in the "Meaning" category, otherwise <c>false</c>.</returns>
        public static bool IsItMeaningCategory(TestType testType)
        {
            return MeaningTestTypes.Contains(testType);
        }

        /// <summary>
        /// Is test type <paramref name="testType"/> in the "Spelling" category.
        /// </summary>
        /// <param name="testType">Test type.</param>
        /// <returns><c>true</c> if this test type in the "Spelling" category, otherwise <c>false</c>.</returns>
        public static bool IsItSpellingCategory(TestType testType)
        {
            return SpellingTestTypes.Contains(testType);
        }

        /// <summary>
        /// Is test type <paramref name="testType"/> in the "Listening" category.
        /// </summary>
        /// <param name="testType">Test type.</param>
        /// <returns><c>true</c> if this test type in the "Listening" category, otherwise <c>false</c>.</returns>
        public static bool IsItListeningCategory(TestType testType)
        {
            return ListeningTestTypes.Contains(testType);
        }
    }
}

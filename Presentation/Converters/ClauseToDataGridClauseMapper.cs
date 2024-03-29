﻿using System.Collections.Generic;
using System.Linq;

using DDictionary.Domain;
using DDictionary.Domain.Entities;
using DDictionary.Presentation.ViewModels;


namespace DDictionary.Presentation.Converters
{
    public static class ClauseToDataGridClauseMapper
    {
        /// <summary>
        /// Make ClauseDataGrid from Clause.
        /// </summary>
        /// <param name="filter">Current filter.</param>
        public static DataGridClause MapToDataGridClause(this Clause cl, FiltrationCriteria filter = null)
        {
            if(cl is null)
                throw new System.ArgumentNullException(nameof(cl));


            string translations = MakeTranslationsString(cl.Translations, filter);
            string allTranslations = MakeTranslationsString(cl.Translations, null);

            if(translations == allTranslations)
                allTranslations = null;

            return new DataGridClause {
                Id = cl.Id,
                Sound = cl.Sound,
                Word = cl.Word,
                AsteriskType = cl.Asterisk?.Type ?? AsteriskType.None,
                Transcription = cl.Transcription,
                Translations = translations,
                AllTranslations = allTranslations,
                Context = cl.Context,
                Relations = MakeRelationsString(cl.Relations.Select(o => o.ToClause.Word)),
                HasRelations = (cl.Relations.Count > 0),
                Added = cl.Added,
                Updated = cl.Updated,
                Watched = cl.Watched,
                WatchedCount = cl.WatchedCount,
                Group = cl.Group,

                UnderstandingScore = CalculateUnderstandingScore(cl.TrainingStatistics),
                SpellingScore = CalculateSpellingScore(cl.TrainingStatistics),
                ListeningScore = CalculateListeningScore(cl.TrainingStatistics)
            };
        }

        /// <summary>
        /// Get a string representation of the translations.
        /// </summary>
        /// <param name="filter">Current filter.</param>
        public static string MakeTranslationsString(IEnumerable<Translation> translations, 
            FiltrationCriteria filter = null)
        {
            if(filter?.PartOfSpeech != null && translations != null)
                translations = translations.Where(o => o.Part == filter.PartOfSpeech);

            if(translations?.Any() != true)
                return "";

            return translations.OrderBy(o => o.Index)
                               .Aggregate("", (s, o) => s += $"{TranslationConverter.ConvertToString(o)}; ")
                               .TrimEnd(' ', ';');
        }

        /// <summary>
        /// Get a string representation of the relations.
        /// </summary>
        public static string MakeRelationsString(IEnumerable<string> relations)
        {
            if(relations?.Any() != true) //There are no relations let's add the placeholder to allow user to add some
                return $"[{Properties.Resources.AddRelationPlaceholder}]";

            return relations.Distinct()
                            .OrderBy(o => o)
                            .Aggregate("", (s, o) => s += $"{o}; ")
                            .TrimEnd(' ', ';');
        }

        /// <summary>
        /// Get the score for understanding that accumulates results from all related tests.
        /// </summary>
        /// <returns>From 0 to 100 percent, where 100 is the best score.</returns>
        public static double CalculateUnderstandingScore(IEnumerable<TrainingStatistic> statistics)
        {
            return CalculateScore(statistics, TestCategoryMapper.MeaningTestTypes);
        }

        /// <summary>
        /// Get the score for spelling that accumulates results from all related tests.
        /// </summary>
        /// <returns>From 0 to 100 percent, where 100 is the best score.</returns>
        public static double CalculateSpellingScore(IEnumerable<TrainingStatistic> statistics)
        {
            return CalculateScore(statistics, TestCategoryMapper.SpellingTestTypes);
        }

        /// <summary>
        /// Get the score for listening that accumulates results from all related tests.
        /// </summary>
        /// <returns>From 0 to 100 percent, where 100 is the best score.</returns>
        public static double CalculateListeningScore(IEnumerable<TrainingStatistic> statistics)
        {
            return CalculateScore(statistics, TestCategoryMapper.ListeningTestTypes);
        }

        private static double CalculateScore(IEnumerable<TrainingStatistic> statistics, IReadOnlyList<TestType> types)
        {
            if(statistics is null)
                return 0;

            double success = 0;
            double fail = 0;

            foreach(TrainingStatistic st in statistics.Where(o => types.Contains(o.TestType)))
            {
                success += st.Success;
                fail += st.Fail;
            }

            double total = success + fail;

            return total > 0 ? (success / total * 100) : 0;
        }
    }
}

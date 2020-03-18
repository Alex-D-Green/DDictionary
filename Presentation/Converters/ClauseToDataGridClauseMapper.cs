using System.Collections.Generic;
using System.Linq;

using DDictionary.Domain.Entities;
using DDictionary.Presentation.ViewModels;


namespace DDictionary.Presentation.Converters
{
    public static class ClauseToDataGridClauseMapper
    {
        /// <summary>
        /// Make ClauseDataGrid from Clause.
        /// </summary>
        public static DataGridClause MapToDataGridClause(this Clause cl)
        {
            if(cl is null)
                throw new System.ArgumentNullException(nameof(cl));


            return new DataGridClause {
                Id = cl.Id,
                Sound = cl.Sound,
                Word = cl.Word,
                Transcription = cl.Transcription,
                Translations = MakeTranslationsString(cl.Translations),
                Context = cl.Context,
                Relations = MakeRelationsString(cl.Relations.Select(o => o.ToClause.Word)),
                HasRelations = (cl.Relations.Count > 0),
                Added = cl.Added,
                Updated = cl.Updated,
                Watched = cl.Watched,
                WatchedCount = cl.WatchedCount,
                Group = cl.Group
            };
        }

        /// <summary>
        /// Get a string representation of the translations.
        /// </summary>
        public static string MakeTranslationsString(IEnumerable<Translation> translations)
        {
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
    }
}

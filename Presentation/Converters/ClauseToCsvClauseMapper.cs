using System.Collections.Generic;
using System.Linq;

using DDictionary.Domain.Entities;
using DDictionary.Presentation.ViewModels;


namespace DDictionary.Presentation.Converters
{
    public static class ClauseToCsvClauseMapper
    {
        /// <summary>
        /// Make CsvClause from Clause.
        /// </summary>
        public static CsvClause MapToCsvClause(this Clause cl)
        {
            if(cl is null)
                throw new System.ArgumentNullException(nameof(cl));


            return new CsvClause
            {
                Id = cl.Id,
                Sound = cl.Sound,
                Word = cl.Word,
                Transcription = cl.Transcription,
                Translations = cl.Translations.OrderBy(o => o.Index)
                                              .Aggregate("", (s, o) => s += $"{TranslationConverter.ConvertToString(o)}; ")
                                              .TrimEnd(' ', ';'),
                Context = cl.Context,
                Relations = MakeRelationsString(cl.Relations),
                Added = cl.Added,
                Updated = cl.Updated,
                Group = cl.Group.ToGradeStr()
            };
        }

        /// <summary>
        /// Get a string representation of the relations.
        /// </summary>
        public static string MakeRelationsString(IEnumerable<Relation> relations)
        {
            if(relations?.Any() != true)
                return "";

            return relations.Select(o => $"{o.ToClause.Word} -- {o.Description}")
                            .OrderBy(o => o)
                            .Aggregate("", (s, o) => s += $"{o}; ")
                            .TrimEnd(' ', ';');
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

using DDictionary.Domain.Entities;
using DDictionary.Presentation.ViewModels;


namespace DDictionary.Presentation.Converters
{
    public static class ClauseToCsvClauseMapper
    {
        /// <summary>The separator symbol that is used to aggregate clause's translations into a string.</summary>
        public const char TranslationsSeparator = ';';

        /// <summary>The separator symbol that is used to aggregate clause's relations into a string.</summary>
        public const char RelationsSeparator = ';';

        /// <summary>The separator string that is used to merge the parts of a relation into a string.</summary>
        public const string RelationDescriptionSeparator = "--";


        /// <summary>
        /// Make CsvClause from Clause.
        /// </summary>
        public static CsvClause MapToCsvClause(this Clause cl)
        {
            if(cl is null)
                throw new ArgumentNullException(nameof(cl));


            return new CsvClause
            {
                Id = cl.Id,
                Sound = cl.Sound,
                Word = cl.Word,
                Transcription = cl.Transcription,
                Translations = cl.Translations
                    .OrderBy(o => o.Index)
                    .Aggregate("", (s, o) => s += $"{TranslationConverter.ConvertToString(o)}{TranslationsSeparator} ")
                    .TrimEnd(' ', TranslationsSeparator),
                Context = cl.Context,
                Relations = MakeRelationsString(cl.Relations),
                Added = cl.Added,
                Updated = cl.Updated,
                Watched = cl.Watched,
                WatchedCount = cl.WatchedCount,
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

            return relations.Select(o => $"{o.ToClause.Word} {RelationDescriptionSeparator} {o.Description}")
                            .OrderBy(o => o)
                            .Aggregate("", (s, o) => s += $"{o}{RelationsSeparator} ")
                            .TrimEnd(' ', RelationsSeparator);
        }

        /// <summary>
        /// Make Clause from CsvClause.
        /// </summary>
        /// <remarks>Obviously not all data will be present in the result clause (no Ids, not full clause for relations, etc), 
        /// only that could be obtained from CSV clause.
        /// </remarks>
        public static Clause MapFromCsvClause(this CsvClause csvClause)
        {
            if(csvClause is null)
                throw new ArgumentNullException(nameof(csvClause));


            DateTime now = DateTime.Now;

            var clause = new Clause {
                Sound = csvClause.Sound,
                Word = csvClause.Word,
                Transcription = csvClause.Transcription,
                Context = csvClause.Context,
                Added = now,
                Updated = now,
                Watched = now,
                Group = WordGroupTranslator.FromGradeStr(csvClause.Group)
            };

            if(!String.IsNullOrEmpty(csvClause.Translations))
            {
                clause.Translations = csvClause.Translations
                                               .Split(TranslationsSeparator)
                                               .Select(o => o.Trim())
                                               .Where(o => !String.IsNullOrEmpty(o))
                                               .Select((o, i) =>
                                               {
                                                   var tr = TranslationConverter.Parse(o);
                                                   tr.Index = i;

                                                   return tr;
                                               })
                                               .ToList();
            }
            else
                clause.Translations = new List<Translation>(0);

            if(!String.IsNullOrEmpty(csvClause.Relations))
            {
                clause.Relations = csvClause.Relations
                                            .Split(RelationsSeparator)
                                            .Select(o => o.Trim())
                                            .Where(o => !String.IsNullOrEmpty(o))
                                            .Select(o =>
                                            {
                                                string[] parts = o.Split(new[] { RelationDescriptionSeparator },
                                                                         StringSplitOptions.None)
                                                                  .Select(x => x.Trim())
                                                                  .ToArray();

                                                if(parts.Length != 2)
                                                    throw new ArgumentException($"Wrong relations' description ({o}).");

                                                return new Relation {
                                                    ToClause = new Clause { Word = parts[0] },
                                                    Description = parts[1]
                                                };
                                            })
                                            .ToList();
            }
            else
                clause.Relations = new List<Relation>(0);

            return clause;
        }
    }
}

﻿using System.Collections.Generic;
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
        public static DataGridClause MakeClauseDataGrid(this Clause cl)
        {
            if(cl is null)
                throw new System.ArgumentNullException(nameof(cl));


            return new DataGridClause {
                Id = cl.Id,
                Sound = cl.Sound,
                Word = cl.Word,
                Transcription = cl.Transcription,
                Translations = cl.Translations.OrderBy(o => o.Index)
                                              .Aggregate("", (s, o) => s += $"{TranslationConverter.ConvertToString(o)}; ")
                                              .TrimEnd(' ', ';'),
                Context = cl.Context,
                Relations = MakeRelationsString(cl.Relations),
                HasRelations = (cl.Relations.Count > 0),
                Added = cl.Added,
                Updated = cl.Updated,
                Group = cl.Group
            };
        }

        /// <summary>
        /// Get a string representation of the relations.
        /// </summary>
        public static string MakeRelationsString(IEnumerable<Relation> relations)
        {
            if(relations?.Any() != true) //There are no relations let's add the placeholder to allow user to add some
                return $"[{Properties.Resources.AddRelationPlaceholder}]";

            return relations.Select(o => o.To.Word)
                            .Distinct()
                            .OrderBy(o => o)
                            .Aggregate("", (s, o) => s += $"{o}; ")
                            .TrimEnd(' ', ';');
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using DDictionary.Domain.Entities;

namespace DDictionary.Domain
{
    /// <summary>
    /// The filtration criteria for fetching clauses.
    /// </summary>
    public sealed class FiltrationCriteria
    {
        /// <summary>
        /// A substring that should be present in Word, Context, any Relations' Word or any Translations.
        /// Empty string or <c>null</c> means there is no filtration by substring.
        /// </summary>
        public string TextFilter { get; set; }

        /// <summary>
        /// The list of shown groups.
        /// Empty list or <c>null</c> means there is no filtration by groups.
        /// </summary>
        public IEnumerable<WordGroup> ShownGroups { get; set; }

        /// <summary>
        /// Clauses have to be related to this one.
        /// <c>null</c> means there is no filtration by relations.
        /// </summary>
        public Clause RelatedFrom { get; set; }

        /// <summary>
        /// The filter is empty (there is no filtration).
        /// </summary>
        public bool Empty 
        { 
            get => (RelatedFrom is null && String.IsNullOrEmpty(TextFilter) && ShownGroups?.Any() != true);
        }


        /// <summary>
        /// Clear all properties (there will not be any filtration).
        /// </summary>
        public void Clear()
        {
            TextFilter = "";
            ShownGroups = Enumerable.Empty<WordGroup>();
            RelatedFrom = null;

            Debug.Assert(Empty);
        }
    }
}

using System.Collections.Generic;

using DDictionary.Domain;

namespace DDictionary.DAL
{
    /// <summary>
    /// The object to work with data storage.
    /// </summary>
    public interface IDBFacade
    {
        /// <summary>
        /// Get dictionary clause by id.
        /// </summary>
        Clause GetClauseById(int id);

        /// <summary>
        /// Get all clauses that match to the filtration criteria.
        /// </summary>
        /// <param name="filter">The filtration criteria, <c>null</c> - without filtration.</param>
        IEnumerable<Clause> GetClauses(FiltrationCriteria filter = null);

        /// <summary>
        /// Get the total amount of clauses.
        /// </summary>
        int GetTotalClauses();
    }
}
using System.Collections.Generic;

using DDictionary.Domain.Entities;

namespace DDictionary.Domain
{
    //TODO: Make sure that in the all places where IDBFacade is used there is a data exception handling!

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

        /// <summary>
        /// Get all words with their ids.
        /// </summary>
        IEnumerable<JustWordDTO> GetJustWords();

        /// <summary>
        /// Add or update relation data. If <paramref name="relationId"/> equals 0 then a new relation will be created 
        /// otherwise the existing one will be updated.
        /// </summary>
        /// <param name="relationId">Relation id or 0 (for new ones).</param>
        /// <param name="fromClauseId">From clause id.</param>
        /// <param name="toClauseId">To clause id.</param>
        /// <param name="relDescription">Relation's description.</param>
        void AddOrUpdateRelation(int relationId, int fromClauseId, int toClauseId, string relDescription);

        /// <summary>
        /// Remove given relation.
        /// </summary>
        /// <param name="relationId">Relation id.</param>
        void RemoveRelation(int relationId);

        /// <summary>
        /// Remove these clauses.
        /// </summary>
        /// <param name="clauseIds">Ids of the clauses that should be deleted.</param>
        void RemoveClauses(params int[] clauseIds);
    }
}
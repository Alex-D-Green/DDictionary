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
        /// <returns>The clause with the given id or <c>null</c>, if there is no such one.</returns>
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
        /// Add or update clause data. If the id of the <paramref name="clause"/> equals 0 then a new 
        /// clause will be created otherwise the existing one will be updated.
        /// </summary>
        /// <param name="clause">Clause data.</param>
        /// <returns>The id of the created/updated clause.</returns>
        int AddOrUpdateClause(ClauseUpdateDTO clause);

        /// <summary>
        /// Remove these clauses.
        /// </summary>
        /// <param name="clauseIds">Ids of the clauses that should be deleted.</param>
        void RemoveClauses(params int[] clauseIds);

        /// <summary>
        /// "Move" these clauses to the destination group.
        /// </summary>
        /// <param name="toGroup">The destination group.</param>
        /// <param name="clauseIds">Ids of the clauses that should be moved.</param>
        void MoveClausesToGroup(WordGroup toGroup, params int[] clauseIds);

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
        /// <returns>The id of the created/updated relation.</returns>
        int AddOrUpdateRelation(int relationId, int fromClauseId, int toClauseId, string relDescription);

        /// <summary>
        /// Remove given relations.
        /// </summary>
        /// <param name="relationIds">Relations ids.</param>
        void RemoveRelations(params int[] relationIds);

        /// <summary>
        /// Add or update translation data. If the id of the <paramref name="translation"/> equals 0 then a new 
        /// translation will be created otherwise the existing one will be updated.
        /// </summary>
        /// <param name="translation">Translation data.</param>
        /// <param name="toClauseId">Id of the clause that the translation belongs to.</param>
        /// <returns>The id of the created/updated translation.</returns>
        int AddOrUpdateTranslation(Translation translation, int toClauseId);

        /// <summary>
        /// Remove given translations.
        /// </summary>
        /// <param name="translationIds">Translations ids.</param>
        void RemoveTranslations(params int[] translationIds);
    }
}
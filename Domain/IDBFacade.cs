using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DDictionary.Domain.Entities;


namespace DDictionary.Domain
{
    /// <summary>
    /// Error handler delegate.
    /// </summary>
    /// <param name="e">Exception.</param>
    /// <param name="handled">Handler should return <c>true</c> if the error is handled or leave it otherwise.</param>
    public delegate void ErrorHandler(Exception e, ref bool handled);


    /// <summary>
    /// The object to work with data storage.
    /// </summary>
    public interface IDBFacade
    {
        /// <summary>
        /// The event fires when a Data Access Level error occurs (e.g. IO error, Sql error etc.).
        /// </summary>
        event ErrorHandler OnErrorOccurs;


        /// <summary>
        /// Get dictionary clause by id.
        /// </summary>
        /// <returns>The clause with the given id or <c>null</c>, if there is no such one.</returns>
        Task<Clause> GetClauseByIdAsync(int id);

        /// <summary>
        /// Get all clauses that match to the filtration criteria.
        /// </summary>
        /// <param name="filter">The filtration criteria, <c>null</c> - without filtration.</param>
        Task<IEnumerable<Clause>> GetClausesAsync(FiltrationCriteria filter = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the total amount of clauses.
        /// </summary>
        Task<int> GetTotalClausesAsync();

        /// <summary>
        /// Add or update clause data. If the id of the <paramref name="clause"/> equals 0 then a new 
        /// clause will be created otherwise the existing one will be updated.
        /// </summary>
        /// <param name="clause">Clause data.</param>
        /// <param name="updateWatched">Update the clause's watching data.</param>
        /// <returns>The id of the created/updated clause.</returns>
        Task<int> AddOrUpdateClauseAsync(ClauseUpdateDTO clause, bool updateWatched);

        /// <summary>
        /// Update the data when the clause was watched for the last time.
        /// </summary>
        /// <returns>The new value of the watch counter.</returns>
        Task<int> UpdateClauseWatchAsync(int id);

        /// <summary>
        /// Remove these clauses.
        /// </summary>
        /// <param name="clauseIds">Ids of the clauses that should be deleted.</param>
        Task RemoveClausesAsync(params int[] clauseIds);

        /// <summary>
        /// "Move" these clauses to the destination group.
        /// </summary>
        /// <param name="toGroup">The destination group.</param>
        /// <param name="clauseIds">Ids of the clauses that should be moved.</param>
        Task MoveClausesToGroupAsync(WordGroup toGroup, params int[] clauseIds);

        /// <summary>
        /// Get all words with their ids.
        /// </summary>
        Task<IEnumerable<JustWordDTO>> GetJustWordsAsync();

        /// <summary>
        /// Add or update relation data. If <paramref name="relationId"/> equals 0 then a new relation will be created 
        /// otherwise the existing one will be updated.
        /// </summary>
        /// <param name="relationId">Relation id or 0 (for new ones).</param>
        /// <param name="fromClauseId">From clause id.</param>
        /// <param name="toClauseId">To clause id.</param>
        /// <param name="relDescription">Relation's description.</param>
        /// <returns>The id of the created/updated relation.</returns>
        Task<int> AddOrUpdateRelationAsync(int relationId, int fromClauseId, int toClauseId, string relDescription);

        /// <summary>
        /// Remove given relations.
        /// </summary>
        /// <param name="relationIds">Relations ids.</param>
        Task RemoveRelationsAsync(params int[] relationIds);

        /// <summary>
        /// Add or update translation data. If the id of the <paramref name="translation"/> equals 0 then a new 
        /// translation will be created otherwise the existing one will be updated.
        /// </summary>
        /// <param name="translation">Translation data.</param>
        /// <param name="toClauseId">Id of the clause that the translation belongs to.</param>
        /// <returns>The id of the created/updated translation.</returns>
        Task<int> AddOrUpdateTranslationAsync(Translation translation, int toClauseId);

        /// <summary>
        /// Remove given translations.
        /// </summary>
        /// <param name="translationIds">Translations ids.</param>
        Task RemoveTranslationsAsync(params int[] translationIds);
    }
}
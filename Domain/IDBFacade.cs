using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DDictionary.Domain.DTO;
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
        /// Currently used data source.
        /// </summary>
        /// <seealso cref="DDictionary.Domain.IDBFacade.SetUpDataSource"/>
        string DataSource { get; }


        /// <summary>
        /// Set up data source (e.g. data base's file name). <b>For this kind of provider.</b>
        /// </summary>
        /// <seealso cref="DDictionary.Domain.IDBFacade.DataSource"/>
        void SetUpDataSource(string DataSource);

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
        /// Check if the word exists in the dictionary, if so returns its Id otherwise returns <c>0</c>.
        /// </summary>
        Task<int> GetClauseIdByWordAsync(string word);

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
        /// Update clause's group.
        /// </summary>
        /// <remarks>Also update and watch timestamps will be updated.</remarks>
        /// <param name="id">Clause identifier.</param>
        /// <param name="group">Needed group for clause.</param>
        Task UpdateClauseGroupAsync(int id, WordGroup group);

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

        /// <summary>
        /// Set (or remove if <paramref name="asterisk"/> equals <see cref="AsteriskType.None"/>) 
        /// certain type of asterisk for the clause.
        /// </summary>
        /// <param name="clauseId">Id of the clause that the asterisk belongs to.</param>
        /// <param name="asterisk">Asterisk type.</param>
        Task SetAsteriskAsync(int clauseId, AsteriskType asterisk);

        /// <summary>
        /// Remove asterisks for given clauses.
        /// </summary>
        /// <param name="clausesIds">Clauses ids.</param>
        Task RemoveAsteriskAsync(params int[] clausesIds);

        /// <summary>
        /// Update needed timestamps for the clause's asterisk.
        /// </summary>
        /// <param name="clauseId">Id of the clause that the asterisk belongs to.</param>
        /// <param name="meaning">Timestamp for meaning training (<see cref="AsteriskType.Meaning"/>).</param>
        /// <param name="spelling">Timestamp for spelling training (<see cref="AsteriskType.Spelling"/>).</param>
        /// <param name="listening">Timestamp for listening training (<see cref="AsteriskType.Listening"/>).</param>
        Task UpdateTimestampsForAsteriskAsync(int clauseId, DateTime? meaning, DateTime? spelling, DateTime? listening);

        /// <summary>
        /// Add a set of clauses in one sitting.
        /// </summary>
        /// <remarks>This method should work by principle all or nothing.</remarks>
        /// <returns>All founded errors in data. If the list is empty then the operation is successful.</returns>
        /// <seealso cref="DDictionary.Presentation.Converters.ClauseToCsvClauseMapper.MapFromCsvClause"/>
        Task<List<string>> BulkAddClausesAsync(IEnumerable<Clause> clauses);

        /// <summary>
        /// Add or update training statistic. 
        /// Creates a new record for the pair Test type + Word, or update the exists one.
        /// </summary>
        /// <param name="test">Test type.</param>
        /// <param name="clauseId">Clause's id.</param>
        /// <param name="success">Word's training was success.</param>
        Task AddOrUpdateTrainingStatisticAsync(TestType test, int clauseId, bool success);

        /// <summary>
        /// Get the clause training statistic.
        /// </summary>
        /// <param name="test">Test type.</param>
        /// <param name="clauseId">Clause's id.</param>
        Task<TrainingStatisticDTO> GetClauseTrainingStatisticAsync(TestType test, int clauseId);

        /// <summary>
        /// Get <b>all words</b> with their training statistics for the needed kinds of tests.
        /// </summary>
        /// <param name="types">Needed tests' types, <b>empty array - return statistic of all tests' types</b>.</param>
        Task<IEnumerable<WordTrainingStatisticDTO>> GetWordTrainingStatisticsAsync(params TestType[] types);

        /// <summary>
        /// Get general statistics for all training types (if any).
        /// </summary>
        Task<IEnumerable<TrainingStatistic>> GetGeneralTrainingStatisticsAsync();

        /// <summary>
        /// Get statistics for a certain period for all training types.
        /// </summary>
        /// <param name="since">Consider data that isn't older than this date.</param>
        Task<IEnumerable<ShortTrainingStatistic>> GetGeneralTrainingStatisticsAsync(DateTime since);

        /// <summary>
        /// Get training history.
        /// </summary>
        /// <param name="limit">Count of records to retrieve.</param>
        Task<IEnumerable<TrainingHistoryEntryDTO>> GetTrainingHistoryAsync(int limit);

        /// <summary>
        /// Add training history. 
        /// Creates a new record for the pair Test type + Word with current date.
        /// </summary>
        /// <param name="clauseId">Clause's id.</param>
        /// <param name="success">Word's training was success.</param>
        /// <param name="test">Test type.</param>
        Task AddTrainingHistoryAsync(int clauseId, bool success, TestType test);

        /// <summary>
        /// Remove older records from training history (except <paramref name="leaveRecords"/>).
        /// </summary>
        /// <param name="leaveRecords">The amount of records should be leaved in the table.</param>
        Task RemoveOldTrainingHistoryAsync(int leaveRecords);
    }
}
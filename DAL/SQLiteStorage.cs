using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapper;
using DDictionary.DAL.Migrations;
using DDictionary.Domain;
using DDictionary.Domain.DTO;
using DDictionary.Domain.Entities;


namespace DDictionary.DAL
{
    //https://www.youtube.com/watch?v=ayp3tHEkRc0

    public sealed class SQLiteStorage: IDBFacade
    {
        private static string dataSource = ".\\DictionaryDB.db";
        private static bool databaseWasChecked = false;


        public event ErrorHandler OnErrorOccurs;

        /// <summary>
        /// Currently used data source.
        /// </summary>
        /// <value><seealso cref="DDictionary.DAL.SQLiteStorage.dataSource"/></value>
        public string DataSource { get => dataSource; }


        public void SetUpDataSource(string dataSource)
        {
            SQLiteStorage.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            databaseWasChecked = false;
        }

        public async Task<Clause> GetClauseByIdAsync(int id)
        {
            if(id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "The identifier has to be greater than 0.");


            const string sql = 
                "SELECT * FROM [Clauses] [cl]\n" +
                 "    LEFT JOIN [Translations] [tr] ON [tr].[ClauseId] = [cl].[Id]\n" +
                 "    LEFT JOIN [Relations] [rl] ON [rl].[FromClauseId] = [cl].[Id]\n" +
                 "    LEFT JOIN [Clauses] [cl2] ON [cl2].[Id] = [rl].[ToClauseId]\n" +
                 "    LEFT JOIN [TrainingStatistics] [trst] ON [trst].[ClauseId] = [cl].[Id]\n" +
                 "    LEFT JOIN [Asterisks] [ast] ON [ast].[ClauseId] = [cl].[Id]\n" +
                 "    WHERE [cl].[Id] = @ClauseId; ";

            try
            {
                return (await GetClauses(sql, new { ClauseId = id })).SingleOrDefault();
            }
            catch(Exception e) when(HandleError(e))
            { return null; }
        }

        public async Task<IEnumerable<Clause>> GetClausesAsync(FiltrationCriteria filter = null, 
            CancellationToken cancellationToken = default)
        {
            if(filter is null)
                filter = new FiltrationCriteria(); //Empty filter - without filtration

            var sql = new StringBuilder();

            sql.AppendLine("SELECT * FROM [Clauses] [cl]");
            sql.AppendLine("    LEFT JOIN [Translations] [tr] ON [tr].[ClauseId] = [cl].[Id]");
            sql.AppendLine("    LEFT JOIN [Relations] [rl] ON [rl].[FromClauseId] = [cl].[Id]");
            sql.AppendLine("    LEFT JOIN [Clauses] [cl2] ON [cl2].[Id] = [rl].[ToClauseId]");
            sql.AppendLine("    LEFT JOIN [TrainingStatistics] [trst] ON [trst].[ClauseId] = [cl].[Id]");
            sql.AppendLine("    LEFT JOIN [Asterisks] [ast] ON [ast].[ClauseId] = [cl].[Id]");

            string nextJoin = "WHERE";

            if(filter.RelatedFrom != null)
            {
                sql.AppendFormat("    {0} [cl].[Id] = {1} OR ", nextJoin, filter.RelatedFrom.Id);
                sql.AppendFormat(         "EXISTS( SELECT 1 FROM [Relations] [srl] WHERE ");
                sql.AppendFormat(                 "[srl].[ToClauseId] = [cl].[Id] AND [srl].[FromClauseId] = {0})\n", 
                    filter.RelatedFrom.Id);

                nextJoin = "AND";
            }

            if(filter.HasSound != null)
            {
                if(filter.HasSound == true)
                    sql.AppendFormat("    {0} [cl].[Sound] IS NOT NULL AND [cl].[Sound] <> ''\n", nextJoin);
                else
                    sql.AppendFormat("    {0} [cl].[Sound] IS NULL OR [cl].[Sound] = ''\n", nextJoin);

                nextJoin = "AND";
            }

            if(filter.AddedAfter != null)
            {
                sql.AppendFormat("    {0} [cl].[Added] >= '{1}'\n", nextJoin, 
                    filter.AddedAfter.Value.ToString(DateTimeFormatInfo.CurrentInfo.UniversalSortableDateTimePattern));

                nextJoin = "AND";
            }

            if(filter.AddedBefore != null)
            {
                sql.AppendFormat("    {0} [cl].[Added] <= '{1}'\n", nextJoin, 
                    filter.AddedBefore.Value.ToString(DateTimeFormatInfo.CurrentInfo.UniversalSortableDateTimePattern));

                nextJoin = "AND";
            }

            if(filter.ShownGroups?.Any() == true)
            {
                string gr = filter.ShownGroups.Distinct()
                                              .Aggregate(new StringBuilder(), (s, g) => s.AppendFormat("{0}, ", (byte)g))
                                              .ToString().Trim(' ', ',');

                sql.AppendFormat("    {0} [cl].[Group] IN ({1})\n", nextJoin, gr);
                
                nextJoin = "AND";
            }

            if(filter.PartOfSpeech.HasValue)
            {
                sql.AppendFormat("    {0} EXISTS( SELECT 1 FROM [Translations] [str] WHERE ", nextJoin);
                sql.AppendFormat(                "[str].[ClauseId] = [cl].[Id] AND [str].[Part] = {0})\n", 
                    (byte)filter.PartOfSpeech);

                nextJoin = "AND";
            }

            if(String.IsNullOrEmpty(filter.TextFilter))
                return await GetClauses(sql.ToString(), cancellationToken: cancellationToken);

            //Adding the escape symbol to the percent symbol (single backslash is used as an escape symbol)
            string escaped = filter.TextFilter.Replace("%", "\\%");

            var parameters = new {
                TextP  = $"{escaped}%", //Text Percent
                PTextP = $"%{escaped}%" //Percent Text Percent
            };

            //Primary search target (the word itself - the beginning is matched), in alphabet order
            var tmpSql = $"{sql}    {nextJoin} [cl].[Word] LIKE @TextP ESCAPE '\\'\nORDER BY [cl].[Word]\n";
            
            IEnumerable<Clause> ret = await GetClauses(tmpSql, parameters, cancellationToken);

            //Secondary target (the word itself except primary target - matched but not the beginning)
            tmpSql = $"{sql}    {nextJoin} [cl].[Word] LIKE @PTextP ESCAPE '\\' AND [cl].[Word] NOT LIKE @TextP ESCAPE '\\'\n";
            
            ret = ret.Concat(await GetClauses(tmpSql, parameters, cancellationToken));

            //Tertiary target (relations, excluding the word itself)
            var sqlCopy = new StringBuilder(sql.ToString());
            sql.AppendFormat("    {0} [cl].[Word] NOT LIKE @PTextP ESCAPE '\\'\n", nextJoin);
            sql.AppendFormat("    AND EXISTS( SELECT 1 FROM [Relations] [rltf] WHERE [rltf].[FromClauseId] = [cl].[Id]\n");
            sql.Append(      "                AND EXISTS( SELECT 1 FROM [Clauses] [cltf] ");
            sql.Append(                      "WHERE [rltf].[ToClauseId] = [cltf].[Id] ");
            sql.Append(                      "AND [cltf].[Word] LIKE @PTextP ESCAPE '\\' )\n");
            sql.Append(      "    )\n");

            ret = ret.Concat(await GetClauses(sql.ToString(), parameters, cancellationToken));

            //Quaternary targets (excluding all previous targets)
            sql = sqlCopy;
            sql.AppendFormat("    {0} [cl].[Word] NOT LIKE @PTextP ESCAPE '\\'\n", nextJoin);
            sql.Append(      "    AND NOT EXISTS( SELECT 1 FROM [Relations] [rltf] WHERE [rltf].[FromClauseId] = [cl].[Id]\n");
            sql.Append(      "                    AND EXISTS( SELECT 1 FROM [Clauses] [cltf] ");
            sql.Append(                                       "WHERE [rltf].[ToClauseId] = [cltf].[Id] ");
            sql.Append(                                       "AND [cltf].[Word] LIKE @PTextP ESCAPE '\\' )\n");
            sql.Append(      "    )\n");
            sql.Append(      "    AND ([cl].[Context] LIKE @PTextP ESCAPE '\\'\n");
            sql.Append(      "        OR EXISTS( SELECT 1 FROM [Translations] [trtf] ");
            sql.Append(                         "WHERE [trtf].[ClauseId] = [cl].[Id] ");
            sql.Append(                         "AND [trtf].[Text] LIKE @PTextP ESCAPE '\\' )\n");
            sql.Append(      "    )\n");

            //To get the words' matches in the beginning
            return ret.Concat(await GetClauses(sql.ToString(), parameters, cancellationToken));
        }

        private async Task<IEnumerable<Clause>> GetClauses(string sql, object parameters = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                {
                    //https://dapper-tutorial.net/result-multi-mapping#example-query-multi-mapping-one-to-many

                    var clauseDictionary = new Dictionary<int, Clause>();

                    //https://dapper-tutorial.net/knowledge-base/25540793/cancellation-token-on-dapper
                    var cmd = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);

                    try
                    {
                        IEnumerable<Clause> clauses =
                            await cnn.QueryAsync<Clause, Translation, Relation, Clause, TrainingStatistic, Asterisk, Clause>(cmd,
                                (clause, translation, relation, clauseTo, trainingStatistics, asterisk) =>
                                {
                                    if(!clauseDictionary.TryGetValue(clause.Id, out Clause clauseEntry))
                                    {
                                        clauseEntry = clause;
                                        clauseDictionary.Add(clauseEntry.Id, clauseEntry);
                                    }

                                    if(translation != null && !clauseEntry.Translations.Any(o => o.Id == translation.Id))
                                        clauseEntry.Translations.Add(translation);

                                    if(relation != null && !clauseEntry.Relations.Any(o => o.Id == relation.Id))
                                    {
                                        relation.ToClause = clauseTo;
                                        clauseEntry.Relations.Add(relation);
                                    }

                                    if(trainingStatistics != null && 
                                       !clauseEntry.TrainingStatistics.Any(o => o.TestType == trainingStatistics.TestType))
                                    {
                                        clauseEntry.TrainingStatistics.Add(trainingStatistics);
                                    }

                                    clauseEntry.Asterisk = asterisk;

                                    return clauseEntry;
                                }, "Id,TestType,ClauseId");

                        return clauses.Distinct();
                    }
                    catch(DataException ex) when(cancellationToken.IsCancellationRequested)
                    { throw new TaskCanceledException(ex.Message, ex); } //Sometime occurs during cancellation
                    catch(SQLiteException ex) when(cancellationToken.IsCancellationRequested)
                    { throw new TaskCanceledException(ex.Message, ex); } //Sometime occurs during cancellation
                }
            }
            catch(Exception e) when(!(e is TaskCanceledException) && HandleError(e)) //Skip TaskCanceledException
            { return Enumerable.Empty<Clause>(); } //If the exception was handled
        }

        public async Task<int> GetTotalClausesAsync()
        {
            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                    return await cnn.ExecuteScalarAsync<int>("SELECT Count(*) FROM [Clauses]");
            }
            catch(Exception e) when(HandleError(e))
            { return 0; }
        }

        public async Task<int> GetClauseIdByWordAsync(string word)
        {
            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                {
                    IEnumerable<int> ids = 
                        await cnn.QueryAsync<int>("SELECT [Id] FROM [Clauses] WHERE [Word] = @Word COLLATE NOCASE", 
                            new { Word = word });

                    if(ids.Count() > 1)
                        throw new DataException($"There are more than one clause \"{word}\" in the DB.");

                    return ids.SingleOrDefault();
                }
            }
            catch(Exception e) when(HandleError(e))
            { return -1; }
        }

        public async Task<IEnumerable<JustWordDTO>> GetJustWordsAsync()
        {
            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                    return await cnn.QueryAsync<JustWordDTO>("SELECT [Id], [Word] FROM [Clauses]");
            }
            catch(Exception e) when(HandleError(e))
            { return Enumerable.Empty<JustWordDTO>(); }
        }

        public async Task<int> AddOrUpdateClauseAsync(ClauseUpdateDTO clause, bool updateWatched)
        {
            if(clause is null)
                throw new ArgumentNullException(nameof(clause));


            DateTime now = DateTime.Now;

            try
            {
                if(clause.Id == 0)
                { //Insert a new record
                    const string sql =
                        "INSERT INTO [Clauses] ([Sound], [Word], [Transcription], [Context], [Added], [Updated]," +
                        " [Watched], [WatchedCount], [Group])\n" +
                        "    VALUES (@Sound, @Word, @Transcription, @Context, @Added, @Updated, @Watched, 0, @Group);\n" +
                        "SELECT last_insert_rowid(); ";

                    using(IDbConnection cnn = await GetConnectionAsync())
                        return await cnn.QuerySingleAsync<int>(sql, new
                        {
                            Sound = clause.Sound,
                            Word = clause.Word,
                            Transcription = clause.Transcription,
                            Context = clause.Context,
                            Added = now,
                            Updated = now,
                            Watched = now,
                            Group = clause.Group
                        });
                }
                else
                { //Update an existing record
                    string sql = updateWatched
                        ? 
                            "UPDATE [Clauses] SET [Sound] = @Sound, [Word] = @Word, [Transcription] = @Transcription," +
                            " [Context] = @Context, [Updated] = @Updated, [Watched] = @Watched," +
                            " [WatchedCount] = [WatchedCount] + 1, [Group] = @Group\n" +
                            "    WHERE [Id] = @Id; "
                        : 
                            "UPDATE [Clauses] SET [Sound] = @Sound, [Word] = @Word, [Transcription] = @Transcription," +
                            " [Context] = @Context, [Updated] = @Updated, [Group] = @Group\n" +
                            "    WHERE [Id] = @Id; ";

                    using(IDbConnection cnn = await GetConnectionAsync())
                        await cnn.ExecuteAsync(sql, new
                        {
                            Id = clause.Id,
                            Sound = clause.Sound,
                            Word = clause.Word,
                            Transcription = clause.Transcription,
                            Context = clause.Context,
                            Updated = now,
                            Watched = now,
                            Group = clause.Group
                        });

                    return clause.Id;
                }
            }
            catch(Exception e) when(HandleError(e))
            { return 0; }
        }

        public async Task<int> UpdateClauseWatchAsync(int id)
        {
            if(id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "The identifier has to be greater than 0.");


            const string sql =
                "UPDATE [Clauses] SET [Watched] = @Watched WHERE [Id] = @Id;\n" +
                "UPDATE [Clauses] SET [WatchedCount] = [WatchedCount] + 1 WHERE [Id] = @Id;\n" +
                "SELECT [WatchedCount] FROM [Clauses] WHERE [Id] = @Id; ";

            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                    return await cnn.ExecuteScalarAsync<int>(sql, new { Id = id, Watched = DateTime.Now });
            }
            catch(Exception e) when(HandleError(e))
            { return -1; }
        }

        public async Task UpdateClauseGroupAsync(int id, WordGroup group)
        {
            if(id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "The identifier has to be greater than 0.");


            const string sql =
                "UPDATE [Clauses] SET [Updated] = @Updated, [Watched] = @Watched, [Group] = @Group WHERE [Id] = @Id; ";

            DateTime now = DateTime.Now;

            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                    await cnn.ExecuteAsync(sql, new { 
                        Id = id,
                        Updated = now,
                        Watched = now,
                        Group = group 
                    });
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        public async Task RemoveClausesAsync(params int[] clauseIds)
        {
            if(clauseIds?.Any(o => o <= 0) == true)
                throw new ArgumentException("All identifiers have to be greater than 0.", nameof(clauseIds));


            if(clauseIds is null || clauseIds.Length == 0)
                return;

            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                    await cnn.ExecuteAsync("DELETE FROM [Clauses] WHERE [Id] IN @Nums", new { Nums = clauseIds });
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        public async Task MoveClausesToGroupAsync(WordGroup toGroup, params int[] clauseIds)
        {
            if(clauseIds?.Any(o => o <= 0) == true)
                throw new ArgumentException("All identifiers have to be greater than 0.", nameof(clauseIds));


            if(clauseIds is null || clauseIds.Length == 0)
                return;

            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                    await cnn.ExecuteAsync("UPDATE [Clauses] SET [Group] = @ToGroup WHERE [Id] IN @Nums", 
                        new { ToGroup = (byte)toGroup, Nums = clauseIds });

                //The group changing isn't counted as clause's modification so the last update date shouldn't be changed
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        public async Task<int> AddOrUpdateRelationAsync(int relationId, int fromClauseId, int toClauseId, 
            string relDescription)
        {
            if(relationId < 0)
                throw new ArgumentOutOfRangeException(nameof(relationId), relationId, 
                    "The identifier has to be equal or greater than 0.");

            if(fromClauseId <= 0)
                throw new ArgumentOutOfRangeException(nameof(fromClauseId), fromClauseId, 
                    "The identifier has to be greater than 0.");

            if(toClauseId <= 0)
                throw new ArgumentOutOfRangeException(nameof(toClauseId), toClauseId,
                    "The identifier has to be greater than 0.");


            try
            {
                if(relationId == 0)
                { //Insert a new record
                    const string sql =
                        "INSERT INTO [Relations] ([FromClauseId], [ToClauseId], [Description]) VALUES (@From, @To, @Descr);\n" +
                        "UPDATE [Clauses] SET [Updated] = @Now\n" +
                        "    WHERE [Id] = @From;\n" +
                        "SELECT last_insert_rowid(); ";

                    using(IDbConnection cnn = await GetConnectionAsync())
                        return await cnn.QuerySingleAsync<int>(sql, new { 
                            From = fromClauseId, 
                            To = toClauseId, 
                            Descr = relDescription, 
                            Now = DateTime.Now 
                        });
                }
                else
                { //Update an existing record
                    const string sql =
                        "UPDATE [Relations] SET [FromClauseId] = @From, [ToClauseId] = @To, [Description] = @Descr\n" +
                        "    WHERE [Id] = @Id;\n" +
                        "UPDATE [Clauses] SET [Updated] = @Now\n" +
                        "    WHERE [Id] = @From; ";

                    using(IDbConnection cnn = await GetConnectionAsync())
                        await cnn.ExecuteAsync(sql, new { 
                            Id = relationId, 
                            From = fromClauseId, 
                            To = toClauseId, 
                            Descr = relDescription, 
                            Now = DateTime.Now 
                        });

                    return relationId;
                }
            }
            catch(Exception e) when(HandleError(e))
            { return 0; }
        }

        public async Task RemoveRelationsAsync(params int[] relationIds)
        {
            if(relationIds?.Any(o => o <= 0) == true)
                throw new ArgumentException("All identifiers have to be greater than 0.", nameof(relationIds));


            if(relationIds is null || relationIds.Length == 0)
                return;

            const string sql =
                "UPDATE [Clauses] SET [Updated] = @Now\n" +
                "    WHERE [Id] IN (SELECT [FromClauseId] FROM [Relations] WHERE [Id] IN @Nums);\n" + 
                "DELETE FROM [Relations] WHERE [Id] IN @Nums; ";

            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                    await cnn.ExecuteAsync(sql, new { Now = DateTime.Now, Nums = relationIds });
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        public async Task<int> AddOrUpdateTranslationAsync(Translation translation, int toClauseId)
        {
            if(translation is null)
                throw new ArgumentNullException(nameof(translation));

            if(toClauseId <= 0)
                throw new ArgumentOutOfRangeException(nameof(toClauseId), toClauseId,
                    "The identifier has to be greater than 0.");


            try
            {
                if(translation.Id == 0)
                { //Insert a new record
                    const string sql =
                        "INSERT INTO [Translations] ([Index], [Text], [Part], [ClauseId])\n" +
                        "    VALUES (@Index, @Text, @Part, @ClauseId);\n" +
                        "UPDATE [Clauses] SET [Updated] = @Now\n" +
                        "    WHERE [Id] = @ClauseId;\n" +
                        "SELECT last_insert_rowid(); ";

                    using(IDbConnection cnn = await GetConnectionAsync())
                        return await cnn.QuerySingleAsync<int>(sql, new { 
                            translation.Index, 
                            translation.Text, 
                            translation.Part, 
                            ClauseId = toClauseId,
                            Now = DateTime.Now
                        });
                }
                else
                { //Update an existing record
                    const string sql =
                        "UPDATE [Translations] SET [Index] = @Index, [Text] = @Text, [Part] = @Part, [ClauseId] = @ClauseId\n" +
                        "    WHERE [Id] = @Id;\n" +
                        "UPDATE [Clauses] SET [Updated] = @Now\n" +
                        "    WHERE [Id] = @ClauseId; ";

                    using(IDbConnection cnn = await GetConnectionAsync())
                        await cnn.ExecuteAsync(sql, new { 
                            translation.Id, 
                            translation.Index, 
                            translation.Text, 
                            translation.Part, 
                            ClauseId = toClauseId,
                            Now = DateTime.Now
                        });

                    return translation.Id;
                }
            }
            catch(Exception e) when(HandleError(e))
            { return 0; }
        }

        public async Task RemoveTranslationsAsync(params int[] translationIds)
        {
            if(translationIds?.Any(o => o <= 0) == true)
                throw new ArgumentException("All identifiers have to be greater than 0.", nameof(translationIds));


            if(translationIds is null || translationIds.Length == 0)
                return;

            const string sql =
                "UPDATE [Clauses] SET [Updated] = @Now\n" +
                "    WHERE [Id] IN (SELECT [ClauseId] FROM [Translations] WHERE [Id] IN @Nums);\n" +
                "DELETE FROM [Translations] WHERE [Id] IN @Nums; ";

            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                    await cnn.ExecuteAsync(sql, new { Now = DateTime.Now, Nums = translationIds });
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        public async Task SetAsteriskAsync(int clauseId, AsteriskType asteriskType)
        {
            if(clauseId <= 0)
                throw new ArgumentOutOfRangeException(nameof(clauseId), clauseId,
                    "The identifier has to be greater than 0.");


            if(asteriskType == AsteriskType.None)
            {
                await RemoveAsteriskAsync(clauseId);
                
                return;
            }


            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                {
                    string sql = "SELECT * FROM [Asterisks] WHERE [ClauseId] = @ClauseId; ";

                    Asterisk asterisk = 
                        await cnn.QuerySingleOrDefaultAsync<Asterisk>(sql, new { ClauseId = clauseId });

                    if(asterisk is null)
                    {
                        sql =
                            "INSERT INTO [Asterisks] ([ClauseId], [Type], [MeaningLastTrain], [SpellingLastTrain], [ListeningLastTrain])\n" +
                            "    VALUES (@ClauseId, @Type, @MeaningLastTrain, @SpellingLastTrain, @ListeningLastTrain); ";

                        await cnn.ExecuteAsync(sql, new {
                            ClauseId = clauseId,
                            Type = asteriskType,
                            MeaningLastTrain = (DateTime?)null,
                            SpellingLastTrain = (DateTime?)null,
                            ListeningLastTrain = (DateTime?)null
                        });
                    }
                    else if(asterisk.Type != asteriskType)
                    {
                        sql =
                            "UPDATE [Asterisks] SET\n" +
                            "    [Type] = @Type,\n" +
                            "    [MeaningLastTrain] = @MeaningLastTrain,\n" +
                            "    [SpellingLastTrain] = @SpellingLastTrain,\n" +
                            "    [ListeningLastTrain] = @ListeningLastTrain\n" +
                            "    WHERE [ClauseId] = @ClauseId; ";

                        await cnn.ExecuteAsync(sql, new {
                            ClauseId = clauseId,
                            Type = asteriskType,
                            MeaningLastTrain = (DateTime?)null,
                            SpellingLastTrain = (DateTime?)null,
                            ListeningLastTrain = (DateTime?)null
                        });
                    }
                }
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        public async Task RemoveAsteriskAsync(params int[] clausesIds)
        {
            if(clausesIds?.Any(o => o <= 0) == true)
                throw new ArgumentException("All identifiers have to be greater than 0.", nameof(clausesIds));


            if(clausesIds is null || clausesIds.Length == 0)
                return;

            const string sql = "DELETE FROM [Asterisks] WHERE [ClauseId] IN @Nums; ";

            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                    await cnn.ExecuteAsync(sql, new { Nums = clausesIds });
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        public async Task UpdateTimestampsForAsteriskAsync(int clauseId, DateTime? meaning, DateTime? spelling, DateTime? listening)
        {
            if(clauseId <= 0)
                throw new ArgumentOutOfRangeException(nameof(clauseId), clauseId,
                    "The identifier has to be greater than 0.");


            const string sql =
                "UPDATE [Asterisks] SET\n" +
                "    [MeaningLastTrain] = IFNULL(@MeaningLastTrain, [MeaningLastTrain]),\n" +
                "    [SpellingLastTrain] = IFNULL(@SpellingLastTrain, [SpellingLastTrain]),\n" +
                "    [ListeningLastTrain] = IFNULL(@ListeningLastTrain, [ListeningLastTrain])\n" +
                "    WHERE [ClauseId] = @ClauseId; ";

            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                {
                    await cnn.ExecuteAsync(sql, new { 
                        ClauseId = clauseId,
                        MeaningLastTrain = meaning,
                        SpellingLastTrain = spelling,
                        ListeningLastTrain = listening
                    });
                }
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", 
            Justification = "<Pending>")]
        public async Task<List<string>> BulkAddClausesAsync(IEnumerable<Clause> clauses)
        {
            if(clauses is null)
                throw new ArgumentNullException(nameof(clauses));


            const string insertClauseSql =
                "INSERT INTO [Clauses] ([Sound], [Word], [Transcription], [Context], [Added], [Updated]," +
                " [Watched], [WatchedCount], [Group])\n" +
                "    VALUES (@Sound, @Word, @Transcription, @Context, @Added, @Updated, @Watched, 0, @Group);\n" +
                "SELECT last_insert_rowid(); ";

            const string insertTranslationSql =
                "INSERT INTO [Translations] ([Index], [Text], [Part], [ClauseId]) VALUES (@Index, @Text, @Part, @ClauseId); ";

            const string insertRelationSql =
                "INSERT INTO [Relations] ([FromClauseId], [ToClauseId], [Description])\n" +
                "    VALUES (@From, (SELECT [Id] FROM [Clauses] WHERE [Word] = @Word COLLATE NOCASE), @Descr); ";

            const string isWordExistSql = "SELECT 1 FROM [Clauses] WHERE [Word] = @Word COLLATE NOCASE; ";

            DateTime now = DateTime.Now;

            var errors = new List<string>();

            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                {
                    //Inner function to quick check if the word already exists
                    async Task<bool> isWordExist(string word) =>
                        (await cnn.QueryAsync(isWordExistSql, new { Word = word })).Any();


                    cnn.Open();
                    using(IDbTransaction transaction = cnn.BeginTransaction())
                    {
                        var tmpLst = new List<Clause>();
                        int idx = 0;

                        //This formation is used instead of foreach cycle to have ability to catch an exception 
                        //and still keep moving forward.
                        using(IEnumerator<Clause> enumerator = clauses.GetEnumerator())
                        {
                            while(true)
                            { //The "first" run
                                idx++;

                                try 
                                { 
                                    if(!enumerator.MoveNext()) 
                                        break; //The sequence is finished
                                }
                                catch(Exception e)
                                { //An error occurs during the fetching this clause
                                    errors.Add($"The clause #{idx}: {e.Message};");
                                    
                                    continue; //Keep fetching, move to the next clause
                                }

                                Clause clause = enumerator.Current; //Get next clause

                                if(String.IsNullOrWhiteSpace(clause.Word))
                                {
                                    errors.Add($"The clause #{idx}: an empty word;");
                                    continue;
                                }

                                if(clause.Translations.Count < 1)
                                {
                                    errors.Add($"The clause #{idx} ({clause.Word}): contains no translations;");
                                    continue;
                                }

                                if(await isWordExist(clause.Word))
                                {
                                    errors.Add(
                                        $"The clause #{idx} ({clause.Word}): this word already exists in the dictionary;");
                                    continue;
                                }

                                try
                                {
                                    //Adding the clause itself
                                    clause.Id = await cnn.QuerySingleAsync<int>(insertClauseSql, new {
                                        Sound = clause.Sound,
                                        Word = clause.Word,
                                        Transcription = clause.Transcription,
                                        Context = clause.Context,
                                        Added = now,
                                        Updated = now,
                                        Watched = now,
                                        Group = clause.Group
                                    });

                                    //Adding all clause's translations
                                    await cnn.ExecuteAsync(insertTranslationSql, clause.Translations.Select(tr =>
                                        new { ClauseId = clause.Id, Index = tr.Index, Text = tr.Text, Part = tr.Part }));

                                    //Preparations for the "second" run below
                                    tmpLst.Add(
                                        new Clause { Id = clause.Id, Word = clause.Word, Relations = clause.Relations });
                                }
                                catch(Exception e)
                                { errors.Add($"The clause #{idx} ({clause.Word}): {e.Message};"); }
                            }
                        }

                        foreach(Clause clause in tmpLst)
                        { //The "second" run
                            try
                            {
                                //Adding all correct clause's relations
                                await cnn.ExecuteAsync(insertRelationSql, clause.Relations.Select(o =>
                                {
                                    if(!isWordExist(o.ToClause.Word).Result)
                                    {
                                        errors.Add($"The relations of the clause ({clause.Word}): there is no word \"{o.ToClause.Word}\" in the dictionary;");
                                        return null; //To skip this relation and move forward
                                    }
                                    else
                                        return new { From = clause.Id, Word = o.ToClause.Word, Descr = o.Description };
                                })
                                .Where(o => o != null));
                            }
                            catch(Exception e)
                            { errors.Add($"The relations of the clause ({clause.Word}): {e.Message};"); }
                        }

                        if(errors.Count == 0)
                            transaction.Commit();
                        else
                            transaction.Rollback(); //All or nothing
                    }
                }
            }
            catch(Exception e) when(HandleError(e))
            { errors.Add(e.Message); }

            return errors;
        }

        public async Task AddOrUpdateTrainingStatisticAsync(TestType test, int clauseId, bool success)
        {
            if(clauseId <= 0)
                throw new ArgumentException("Identifier has to be greater than 0.", nameof(clauseId));

            try
            {
                TrainingStatisticDTO tmp = await GetClauseTrainingStatisticAsync(test, clauseId);

                if(tmp is null)
                { //Insert a new record
                    tmp = new TrainingStatisticDTO {
                        TestType = test,
                        ClauseId = clauseId,
                        Success = success ? 1 : 0,
                        Fail = success ? 0 : 1,
                        LastTraining = DateTime.Now
                    };

                    const string sql =
                        "INSERT INTO [TrainingStatistics] ([TestType], [ClauseId], [Success], [Fail], [LastTraining])\n" +
                        "VALUES (@TestType, @ClauseId, @Success, @Fail, @LastTraining);";

                    using(IDbConnection cnn = await GetConnectionAsync())
                        cnn.Execute(sql, tmp);
                }
                else
                { //Update an existing record
                    if(success)
                        tmp.Success++;
                    else
                        tmp.Fail++;

                    tmp.LastTraining = DateTime.Now;

                    const string sql =
                        "UPDATE [TrainingStatistics] SET [Success] = @Success, [Fail] = @Fail, [LastTraining] = @LastTraining\n" +
                        "    WHERE [TestType] = @TestType AND [ClauseId] = @ClauseId;";

                    using(IDbConnection cnn = await GetConnectionAsync())
                        cnn.Execute(sql, tmp);
                }
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        public async Task<TrainingStatisticDTO> GetClauseTrainingStatisticAsync(TestType test, int clauseId)
        {
            if(clauseId <= 0)
                throw new ArgumentException("Identifier has to be greater than 0.", nameof(clauseId));

            try
            {
                using(IDbConnection cnn = await GetConnectionAsync())
                {
                    return await cnn.QuerySingleOrDefaultAsync<TrainingStatisticDTO>(
                        "SELECT * FROM [TrainingStatistics] WHERE [TestType] = @Type AND [ClauseId] = @Id",
                        new { Type = test, Id = clauseId });
                }
            }
            catch(Exception e) when(HandleError(e))
            { return null; }
        }

        public async Task<IEnumerable<WordTrainingStatisticDTO>> GetWordTrainingStatisticsAsync(params TestType[] types)
        {
            if(types is null)
                throw new ArgumentNullException(nameof(types));


            try
            {
                const string sql = "SELECT [CL].[Id], [Cl].[Word], [TS].*, [AST].* FROM [Clauses] [Cl]\n" +
                                   "    LEFT JOIN [TrainingStatistics] [TS] ON [Cl].[Id] = [TS].[ClauseId]\n" +
                                   "    LEFT JOIN [Asterisks] [AST] ON [Cl].[Id] = [AST].[ClauseId]; ";

                //https://dapper-tutorial.net/result-multi-mapping#example-query-multi-mapping-one-to-many

                var wtsDictionary = new Dictionary<int, WordTrainingStatisticDTO>();
                var tsDictionary = new Dictionary<(int ClauseId, TestType), TrainingStatisticDTO>();

                using(IDbConnection cnn = await GetConnectionAsync())
                {
                    IEnumerable<WordTrainingStatisticDTO> wts =
                        await cnn.QueryAsync<Clause, TrainingStatistic, Asterisk, WordTrainingStatisticDTO>(sql,
                            (clause, trainingStatistic, asterisk) =>
                            {
                                if(!wtsDictionary.TryGetValue(clause.Id, out WordTrainingStatisticDTO wtsEntry))
                                {
                                    wtsEntry = new WordTrainingStatisticDTO {
                                        Id = clause.Id,
                                        Word = clause.Word
                                    };

                                    wtsDictionary.Add(wtsEntry.Id, wtsEntry);
                                }

                                if(trainingStatistic != null &&
                                    !tsDictionary.ContainsKey((wtsEntry.Id, trainingStatistic.TestType)))
                                {
                                    tsDictionary.Add((wtsEntry.Id, trainingStatistic.TestType), new TrainingStatisticDTO {
                                        ClauseId = wtsEntry.Id,
                                        TestType = trainingStatistic.TestType,
                                        Success = trainingStatistic.Success,
                                        Fail = trainingStatistic.Fail,
                                        LastTraining = trainingStatistic.LastTraining
                                    });
                                }

                                if(wtsEntry.Asterisk is null && asterisk != null)
                                {
                                    wtsEntry.Asterisk = new AsteriskDTO {
                                        ClauseId = wtsEntry.Id,
                                        Type = asterisk.Type,
                                        MeaningLastTrain = asterisk.MeaningLastTrain,
                                        SpellingLastTrain = asterisk.SpellingLastTrain,
                                        ListeningLastTrain = asterisk.ListeningLastTrain
                                    };
                                }

                                return wtsEntry;
                            }, splitOn: "TestType,ClauseId");

                    return wts.Distinct().Select(o =>
                    {
                        o.Statistics = tsDictionary.Where(x => x.Key.ClauseId == o.Id)
                                                   .Select(x => x.Value)
                                                   .ToList()
                                                   .AsReadOnly();

                        return o;
                    });
                }
            }
            catch(Exception e) when(HandleError(e))
            { return Enumerable.Empty<WordTrainingStatisticDTO>(); }
        }

        public async Task<IEnumerable<TrainingStatistic>> GetGeneralTrainingStatisticsAsync()
        {
            try
            {
                const string sql = "SELECT [TS].[TestType],\n" +
                                   "       SUM ([TS].[Success]) AS [Success],\n" +
                                   "       SUM ([TS].[Fail]) AS [Fail],\n" +
                                   "       MAX ([TS].[LastTraining]) AS [LastTraining]\n" +
                                   "    FROM [TrainingStatistics] [TS]\n" +
                                   "    GROUP BY [TS].[TestType]";

                using(IDbConnection cnn = await GetConnectionAsync())
                    return await cnn.QueryAsync<TrainingStatistic>(sql);
            }
            catch(Exception e) when(HandleError(e))
            { return Enumerable.Empty<TrainingStatistic>(); }
        }

        public async Task<IEnumerable<ShortTrainingStatistic>> GetGeneralTrainingStatisticsAsync(DateTime since)
        {
            try
            {
                const string sql = "SELECT [TS].[TestType],\n" +
                                   "       COUNT (*) AS [Count],\n" +
                                   "       MAX ([TS].[LastTraining]) AS [LastTraining]\n" +
                                   "    FROM [TrainingStatistics] [TS]\n" +
                                   "    WHERE @Since IS NULL OR [TS].[LastTraining] > @Since\n" +
                                   "    GROUP BY [TS].[TestType]";

                using(IDbConnection cnn = await GetConnectionAsync())
                    return await cnn.QueryAsync<ShortTrainingStatistic>(sql, new { Since = since });
            }
            catch(Exception e) when(HandleError(e))
            { return Enumerable.Empty<ShortTrainingStatistic>(); }
        }

        public async Task<IEnumerable<TrainingHistoryEntryDTO>> GetTrainingHistoryAsync(int limit)
        {
            if (limit <= 0)
                throw new ArgumentException("Limit has to be greater than 0.", nameof(limit));

            try
            {
                const string sql = "SELECT * FROM [TrainingHistory] [TH]\n" +
                                   "    LEFT JOIN [Clauses] [Cl] ON [Cl].[Id] = [TH].[ClauseId]\n" +
                                   "    LEFT JOIN [Asterisks] [At] ON [CL].[Id] = [At].[ClauseId]\n" +
                                   "  ORDER BY [TH].[TrainingDate] DESC, [TH].[Id] DESC\n" +
                                   "  LIMIT @Limit; ";

                using (IDbConnection cnn = await GetConnectionAsync())
                    return await cnn.QueryAsync<TrainingHistoryEntryDTO>(sql, new { Limit = limit });
            }
            catch (Exception e) when (HandleError(e))
            { return Enumerable.Empty<TrainingHistoryEntryDTO>(); }
        }

        public async Task AddTrainingHistoryAsync(int clauseId, bool success, TestType test)
        {
            if (clauseId <= 0)
                throw new ArgumentException("Identifier has to be greater than 0.", nameof(clauseId));

            try
            {
                var tmp = new TrainingHistoryEntry {
                    ClauseId = clauseId,
                    Success = success,
                    TrainingType = test,
                    TrainingDate = DateTime.Now
                };

                const string sql =
                    "INSERT INTO [TrainingHistory] ([ClauseId], [Success], [TrainingType], [TrainingDate])\n" +
                    "  VALUES (@ClauseId, @Success, @TrainingType, @TrainingDate); ";

                using (IDbConnection cnn = await GetConnectionAsync())
                    cnn.Execute(sql, tmp);
            }
            catch (Exception e) when (HandleError(e))
            { }
        }

        public async Task RemoveOldTrainingHistoryAsync(int leaveRecords)
        {
            if (leaveRecords <= 0)
                throw new ArgumentException("Value has to be greater than 0.", nameof(leaveRecords));


            const string sql = "DELETE FROM [TrainingHistory] WHERE [Id] NOT IN (\n" +
                               "    SELECT [Id] FROM [TrainingHistory]\n" +
                               "        ORDER BY [TrainingDate] DESC, [Id] DESC\n" +
                               "        LIMIT @Limit\n" +
                               "); ";

            try
            {
                using (IDbConnection cnn = await GetConnectionAsync())
                    await cnn.ExecuteAsync(sql, new { Limit = leaveRecords });
            }
            catch (Exception e) when (HandleError(e))
            { }
        }

        /// <summary>
        /// Get connection to SQLite database with default connection string.
        /// </summary>
        private async Task<IDbConnection> GetConnectionAsync()
        {
            try
            {
                var cnn = new SQLiteConnection(
                    String.Format(ConfigurationManager.ConnectionStrings["Default"].ConnectionString, DataSource));

                if(!databaseWasChecked)
                {
                    await CreateDBStructureIfNeededAsync(cnn);
                    databaseWasChecked = true;
                }

                return cnn;
            }
            catch(Exception e) when(HandleError(e))
            { return null; }
        }

        /// <summary>
        /// Create the correct structure in a new database if needed.
        /// </summary>
        private static async Task CreateDBStructureIfNeededAsync(IDbConnection cnn)
        {
            IOrderedEnumerable<Type> migrations = 
                Assembly.GetAssembly(typeof(SQLiteStorage)).GetTypes()
                                                           .Where(o => o.IsSubclassOf(typeof(SQLiteMigrationBase)))
                                                           .OrderBy(o => o.GetType().Name);

            var mm = new MigrationManager(migrations.Select(o => Activator.CreateInstance(o, cnn))
                                                    .Cast<IMigration>());

            await mm.UpdateDatabaseAsync();
        }

        /// <summary>
        /// Rises <see cref="OnErrorOccurs"/> event.
        /// </summary>
        /// <param name="e">An exception to handle.</param>
        /// <returns><c>true</c> if somebody handled the exception, otherwise <c>false</c>.</returns>
        private bool HandleError(Exception e)
        {
            bool handled = false;

            OnErrorOccurs?.Invoke(e, ref handled);

            return handled;
        }
    }
}

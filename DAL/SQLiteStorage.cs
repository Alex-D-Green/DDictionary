using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using DDictionary.Domain;
using DDictionary.Domain.Entities;

namespace DDictionary.DAL
{
    //https://www.youtube.com/watch?v=ayp3tHEkRc0

    public sealed class SQLiteStorage: IDBFacade
    {
        static SQLiteStorage()
        {
            //using(IDbConnection cnn = new SQLiteStorage().GetConnection())
            //{
            //    const string sql1 =
            //        "INSERT INTO [Clauses] ([Sound], [Word], [Transcription], [Context], [Added], [Updated], [Watched]," + 
            //        " [WatchedCount], [Group])\n" +
            //        "    VALUES (@Sound, @Word, @Transcription, @Context, @Added, @Updated, @Watched, 0, @Group); ";

            //    const string sql2 =
            //        "INSERT INTO [Translations] ([Index], [Text], [Part], [ClauseId]) " +
            //        "VALUES (@Index, @Text, @Part, @ClauseId); ";

            //    var clause = new Clause
            //    {
            //        Id = 0,
            //        Sound = "https://audiocdn.lingualeo.com/v2/2/29775-631152008.mp3",
            //        Word = "pear",
            //        Transcription = "pɛə",
            //        Context = "I hate pears!",
            //        Added = new DateTime(2019, 9, 2, 12, 0, 0, DateTimeKind.Local),
            //        Updated = new DateTime(2019, 9, 3, 12, 0, 0, DateTimeKind.Local),
            //        Watched = new DateTime(2019, 9, 4, 12, 0, 0, DateTimeKind.Local),
            //        Group = WordGroup.C_KindaKnown
            //    };

            //    var transl = (
            //        Index: 0,
            //        Text: "груша",
            //        Part: PartOfSpeech.Noun,
            //        ClauseId: 0
            //    );

            //    for(int i = 0; i < 5000; i++) //It takes a while
            //    {
            //        clause.Word = $"pear{i}";
            //        cnn.Execute(sql1, clause);

            //        transl.ClauseId = 4 + i;
            //        cnn.Execute(sql2, transl);
            //    }
            //}
        }

        public event ErrorHandler OnErrorOccurs;


        public async Task<Clause> GetClauseByIdAsync(int id)
        {
            if(id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "The identifier has to be greater than 0.");


            const string sql = 
                "SELECT * FROM [Clauses] [cl]\n" +
                 "    LEFT JOIN [Translations] [tr] ON [tr].[ClauseId] = [cl].[Id]\n" +
                 "    LEFT JOIN [Relations] [rl] ON [rl].[FromClauseId] = [cl].[Id]\n" +
                 "    LEFT JOIN [Clauses] [cl2] ON [cl2].[Id] = [rl].[ToClauseId]\n" +
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

            string nextJoin = "WHERE";

            if(filter.RelatedFrom != null)
            {
                sql.AppendFormat("    {0} [cl].[Id] = {1} OR ", nextJoin, filter.RelatedFrom.Id);
                sql.AppendFormat(         "EXISTS( SELECT * FROM [Relations] [srl] WHERE ");
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

            if(filter.ShownGroups?.Any() == true)
            {
                string gr = filter.ShownGroups.Distinct()
                                              .Aggregate(new StringBuilder(), (s, g) => s.AppendFormat("{0}, ", (byte)g))
                                              .ToString().Trim(' ', ',');

                sql.AppendFormat("    {0} [cl].[Group] IN ({1})\n", nextJoin, gr);
                
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
            sql.AppendFormat("    AND EXISTS( SELECT * FROM [Relations] [rltf] WHERE [rltf].[FromClauseId] = [cl].[Id]\n");
            sql.Append(      "                AND EXISTS( SELECT * FROM [Clauses] [cltf] ");
            sql.Append(                      "WHERE [rltf].[ToClauseId] = [cltf].[Id] ");
            sql.Append(                      "AND [cltf].[Word] LIKE @PTextP ESCAPE '\\' )\n");
            sql.Append(      "    )\n");

            ret = ret.Concat(await GetClauses(sql.ToString(), parameters, cancellationToken));

            //Quaternary targets (excluding all previous targets)
            sql = sqlCopy;
            sql.AppendFormat("    {0} [cl].[Word] NOT LIKE @PTextP ESCAPE '\\'\n", nextJoin);
            sql.Append(      "    AND NOT EXISTS( SELECT * FROM [Relations] [rltf] WHERE [rltf].[FromClauseId] = [cl].[Id]\n");
            sql.Append(      "                    AND EXISTS( SELECT * FROM [Clauses] [cltf] ");
            sql.Append(                                       "WHERE [rltf].[ToClauseId] = [cltf].[Id] ");
            sql.Append(                                       "AND [cltf].[Word] LIKE @PTextP ESCAPE '\\' )\n");
            sql.Append(      "    )\n");
            sql.Append(      "    AND ([cl].[Context] LIKE @PTextP ESCAPE '\\'\n");
            sql.Append(      "        OR EXISTS( SELECT * FROM [Translations] [trtf] ");
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
                using(IDbConnection cnn = GetConnection())
                {
                    //https://dapper-tutorial.net/result-multi-mapping#example-query-multi-mapping-one-to-many

                    var clauseDictionary = new Dictionary<int, Clause>();

                    //https://dapper-tutorial.net/knowledge-base/25540793/cancellation-token-on-dapper
                    var cmd = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);

                    try
                    {
                        IEnumerable<Clause> clauses =
                            await cnn.QueryAsync<Clause, Translation, Relation, Clause, Clause>(cmd,
                                (clause, translation, relation, clauseTo) =>
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

                                    return clauseEntry;
                                });

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
                using(IDbConnection cnn = GetConnection())
                    return await cnn.ExecuteScalarAsync<int>("SELECT Count(*) FROM [Clauses]");
            }
            catch(Exception e) when(HandleError(e))
            { return 0; }
        }

        public async Task<int> GetClauseIdByWordAsync(string word)
        {
            try
            {
                using(IDbConnection cnn = GetConnection())
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
                using(IDbConnection cnn = GetConnection())
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

                    using(IDbConnection cnn = GetConnection())
                        return (await cnn.QueryAsync<int>(sql, new
                        {
                            Sound = clause.Sound,
                            Word = clause.Word,
                            Transcription = clause.Transcription,
                            Context = clause.Context,
                            Added = now,
                            Updated = now,
                            Watched = now,
                            Group = clause.Group
                        }))
                        .Single();
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

                    using(IDbConnection cnn = GetConnection())
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
                using(IDbConnection cnn = GetConnection())
                    return await cnn.ExecuteScalarAsync<int>(sql, new { Id = id, Watched = DateTime.Now });
            }
            catch(Exception e) when(HandleError(e))
            { return -1; }
        }

        public async Task RemoveClausesAsync(params int[] clauseIds)
        {
            if(clauseIds?.Any(o => o <= 0) == true)
                throw new ArgumentException("All identifiers have to be greater than 0.", nameof(clauseIds));


            if(clauseIds is null || clauseIds.Length == 0)
                return;

            try
            {
                using(IDbConnection cnn = GetConnection())
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
                using(IDbConnection cnn = GetConnection())
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

                    using(IDbConnection cnn = GetConnection())
                        return (await cnn.QueryAsync<int>(sql, new { 
                            From = fromClauseId, 
                            To = toClauseId, 
                            Descr = relDescription, 
                            Now = DateTime.Now 
                        }))
                        .Single();
                }
                else
                { //Update an existing record
                    const string sql =
                        "UPDATE [Relations] SET [FromClauseId] = @From, [ToClauseId] = @To, [Description] = @Descr\n" +
                        "    WHERE [Id] = @Id;\n" +
                        "UPDATE [Clauses] SET [Updated] = @Now\n" +
                        "    WHERE [Id] = @From; ";

                    using(IDbConnection cnn = GetConnection())
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
                using(IDbConnection cnn = GetConnection())
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

                    using(IDbConnection cnn = GetConnection())
                        return (await cnn.QueryAsync<int>(sql, new { 
                            translation.Index, 
                            translation.Text, 
                            translation.Part, 
                            ClauseId = toClauseId,
                            Now = DateTime.Now
                        }))
                        .Single();
                }
                else
                { //Update an existing record
                    const string sql =
                        "UPDATE [Translations] SET [Index] = @Index, [Text] = @Text, [Part] = @Part, [ClauseId] = @ClauseId\n" +
                        "    WHERE [Id] = @Id;\n" +
                        "UPDATE [Clauses] SET [Updated] = @Now\n" +
                        "    WHERE [Id] = @ClauseId; ";

                    using(IDbConnection cnn = GetConnection())
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
                using(IDbConnection cnn = GetConnection())
                    await cnn.ExecuteAsync(sql, new { Now = DateTime.Now, Nums = translationIds });
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
                using(IDbConnection cnn = GetConnection())
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
                                    clause.Id = (await cnn.QueryAsync<int>(insertClauseSql, new {
                                        Sound = clause.Sound,
                                        Word = clause.Word,
                                        Transcription = clause.Transcription,
                                        Context = clause.Context,
                                        Added = now,
                                        Updated = now,
                                        Watched = now,
                                        Group = clause.Group
                                    }))
                                    .Single();

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

        /// <summary>
        /// Get connection to SQLite database with default connection string.
        /// </summary>
        private IDbConnection GetConnection()
        {
            try
            {
                return new SQLiteConnection(ConfigurationManager.ConnectionStrings["Default"].ConnectionString);
            }
            catch(Exception e) when(HandleError(e))
            { return null; }
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

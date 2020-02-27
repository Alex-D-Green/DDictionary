using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

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
            //using(IDbConnection cnn = GetConnection())
            //{
            //    const string sql1 =
            //        "INSERT INTO [Clauses] ([Sound], [Word], [Transcription], [Context], [Added], [Updated], [Group]) " +
            //        "VALUES (@Sound, @Word, @Transcription, @Context, @Added, @Updated, @Group); ";

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

        public Clause GetClauseById(int id)
        {
            const string sql = 
                "SELECT * FROM [Clauses] [cl]\n" +
                 "    LEFT JOIN [Translations] [tr] ON [tr].[ClauseId] = [cl].[Id]\n" +
                 "    LEFT JOIN [Relations] [rl] ON [rl].[FromClauseId] = [cl].[Id]\n" +
                 "    LEFT JOIN [Clauses] [cl2] ON [cl2].[Id] = [rl].[ToClauseId]\n" +
                 "    WHERE [cl].[Id] = @ClauseId; ";

            try
            {
                return GetClauses(sql, new { ClauseId = id }).SingleOrDefault();
            }
            catch(Exception e) when(HandleError(e))
            { return null; }
        }

        public IEnumerable<Clause> GetClauses(FiltrationCriteria filter = null)
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
                sql.AppendFormat("    {0} EXISTS(SELECT * FROM [Relations] [srl] ", nextJoin);
                sql.AppendFormat(                "WHERE [srl].[ToClauseId] = [cl].[Id] AND [srl].[FromClauseId] = {0})\n", 
                    filter.RelatedFrom.Id);

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
                return GetClauses(sql.ToString());


            //Primary search target (the word itself - the beginning is matched), in alphabet order
            var tmpSql = $"{sql}    {nextJoin} [cl].[Word] LIKE '{filter.TextFilter}%'\nORDER BY [cl].[Word]\n";
            
            IEnumerable<Clause> ret = GetClauses(tmpSql);

            //Secondary target (the word itself except primary target - matched but not the beginning)
            tmpSql = $"{sql}    {nextJoin} [cl].[Word] LIKE '%{filter.TextFilter}%' AND [cl].[Word] NOT LIKE '{filter.TextFilter}%'\n";
            
            ret = ret.Concat(GetClauses(tmpSql));

            //Tertiary target (relations, excluding the word itself)
            var sqlCopy = new StringBuilder(sql.ToString());
            sql.AppendFormat("    {0} [cl].[Word] NOT LIKE '%{1}%'\n", nextJoin, filter.TextFilter);
            sql.AppendFormat("    AND EXISTS( SELECT * FROM [Relations] [rltf] WHERE [rltf].[FromClauseId] = [cl].[Id]\n");
            sql.Append(      "                AND EXISTS( SELECT * FROM [Clauses] [cltf] ");
            sql.Append(                      "WHERE [rltf].[ToClauseId] = [cltf].[Id] ");
            sql.AppendFormat(                "AND [cltf].[Word] LIKE '%{0}%' )\n", filter.TextFilter);
            sql.Append(      "    )\n");

            ret = ret.Concat(GetClauses(sql.ToString()));

            //Quaternary targets (excluding all previous targets)
            sql = sqlCopy;
            sql.AppendFormat("    {0} [cl].[Word] NOT LIKE '%{1}%'\n", nextJoin, filter.TextFilter);
            sql.Append(      "    AND NOT EXISTS( SELECT * FROM [Relations] [rltf] WHERE [rltf].[FromClauseId] = [cl].[Id]\n");
            sql.Append(      "                    AND EXISTS( SELECT * FROM [Clauses] [cltf] ");
            sql.Append(                                       "WHERE [rltf].[ToClauseId] = [cltf].[Id] ");
            sql.AppendFormat(                                 "AND [cltf].[Word] LIKE '%{0}%' )\n", filter.TextFilter);
            sql.Append(      "    )\n");
            sql.AppendFormat("    AND ([cl].[Context] LIKE '%{0}%'\n", filter.TextFilter);
            sql.Append(      "        OR EXISTS( SELECT * FROM [Translations] [trtf] ");
            sql.Append(                         "WHERE [trtf].[ClauseId] = [cl].[Id] ");
            sql.AppendFormat(                   "AND [trtf].[Text] LIKE '%{0}%' )\n", filter.TextFilter);
            sql.Append(      "    )\n");

            return ret.Concat(GetClauses(sql.ToString())); //To get the words' matches in the beginning
        }

        private IEnumerable<Clause> GetClauses(string sql, object parameters = null)
        {
            try
            {
                using(IDbConnection cnn = GetConnection())
                {
                    var clauseDictionary = new Dictionary<int, Clause>();

                    IEnumerable<Clause> clauses =
                        cnn.Query<Clause, Translation, Relation, Clause, Clause>(sql,
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
                            },
                            parameters)
                        .Distinct();

                    return clauses;
                }
            }
            catch(Exception e) when(HandleError(e))
            { return Enumerable.Empty<Clause>(); }
        }

        public int GetTotalClauses()
        {
            try
            {
                using(IDbConnection cnn = GetConnection())
                    return cnn.ExecuteScalar<int>("SELECT Count(*) FROM [Clauses]");
            }
            catch(Exception e) when(HandleError(e))
            { return 0; }
        }

        public IEnumerable<JustWordDTO> GetJustWords()
        {
            try
            {
                using(IDbConnection cnn = GetConnection())
                    return cnn.Query<JustWordDTO>("SELECT [Id], [Word] FROM [Clauses]");
            }
            catch(Exception e) when(HandleError(e))
            { return Enumerable.Empty<JustWordDTO>(); }
    }

        public int AddOrUpdateClause(ClauseUpdateDTO clause)
        {
            if(clause is null)
                throw new ArgumentNullException(nameof(clause));

            DateTime now = DateTime.Now;

            try
            {
                if(clause.Id == 0)
                { //Insert a new record
                    const string sql =
                        "INSERT INTO [Clauses] ([Sound], [Word], [Transcription], [Context], [Added], [Updated], [Group]) " +
                        "VALUES (@Sound, @Word, @Transcription, @Context, @Added, @Updated, @Group);\n" +
                        "SELECT last_insert_rowid(); ";

                    using(IDbConnection cnn = GetConnection())
                        return cnn.Query<int>(sql, new
                        {
                            Sound = clause.Sound,
                            Word = clause.Word,
                            Transcription = clause.Transcription,
                            Context = clause.Context,
                            Added = now,
                            Updated = now,
                            Group = clause.Group
                        })
                        .Single();
                }
                else
                { //Update an existing record
                    const string sql =
                        "UPDATE [Clauses] SET [Sound] = @Sound, [Word] = @Word, [Transcription] = @Transcription," +
                        " [Context] = @Context, [Updated] = @Updated, [Group] = @Group " +
                        "WHERE Id = @Id; ";

                    using(IDbConnection cnn = GetConnection())
                        cnn.Execute(sql, new
                        {
                            Id = clause.Id,
                            Sound = clause.Sound,
                            Word = clause.Word,
                            Transcription = clause.Transcription,
                            Context = clause.Context,
                            Updated = now,
                            Group = clause.Group
                        });

                    return clause.Id;
                }
            }
            catch(Exception e) when(HandleError(e))
            { return 0; }
        }

        public void RemoveClauses(params int[] clauseIds)
        {
            if(clauseIds is null || clauseIds.Length == 0)
                return;

            string nums = clauseIds.Aggregate(new StringBuilder(), (s, n) => s.AppendFormat("{0}, ", n))
                                   .ToString().Trim(' ', ',');

            try
            {
                using(IDbConnection cnn = GetConnection())
                    cnn.Execute($"DELETE FROM [Clauses] WHERE [Id] IN ({nums}); ");
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        public void MoveClausesToGroup(WordGroup toGroup, params int[] clauseIds)
        {
            if(clauseIds is null || clauseIds.Length == 0)
                return;

            string nums = clauseIds.Aggregate(new StringBuilder(), (s, n) => s.AppendFormat("{0}, ", n))
                                   .ToString().Trim(' ', ',');

            try
            {
                using(IDbConnection cnn = GetConnection())
                    cnn.Execute($"UPDATE [Clauses] SET [Group] = {(byte)toGroup} WHERE [Id] IN ({nums}); ");

                //The group changing isn't counted as clause's modification so the last update date shouldn't be changed
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        public int AddOrUpdateRelation(int relationId, int fromClauseId, int toClauseId, string relDescription)
        {
            try
            {
                if(relationId == 0)
                { //Insert a new record
                    const string sql =
                        "INSERT INTO [Relations] ([FromClauseId], [ToClauseId], [Description]) VALUES (@From, @To, @Descr);\n" +
                        "SELECT last_insert_rowid(); ";

                    using(IDbConnection cnn = GetConnection())
                        return cnn.Query<int>(sql, new { From = fromClauseId, To = toClauseId, Descr = relDescription })
                                  .Single();
                }
                else
                { //Update an existing record
                    const string sql =
                        "UPDATE [Relations] SET [FromClauseId] = @From, [ToClauseId] = @To, [Description] = @Descr " +
                        "WHERE [Id] = @Id; ";

                    using(IDbConnection cnn = GetConnection())
                        cnn.Execute(sql,
                            new { Id = relationId, From = fromClauseId, To = toClauseId, Descr = relDescription });

                    return relationId;
                }
            }
            catch(Exception e) when(HandleError(e))
            { return 0; }
        }

        public void RemoveRelations(params int[] relationIds)
        {
            if(relationIds is null || relationIds.Length == 0)
                return;

            string nums = relationIds.Aggregate(new StringBuilder(), (s, n) => s.AppendFormat("{0}, ", n))
                                     .ToString().Trim(' ', ',');

            try
            {
                using(IDbConnection cnn = GetConnection())
                    cnn.Execute($"DELETE FROM [Relations] WHERE [Id] IN ({nums}); ");
            }
            catch(Exception e) when(HandleError(e))
            { }
        }

        public int AddOrUpdateTranslation(Translation translation, int toClauseId)
        {
            if(translation is null)
                throw new ArgumentNullException(nameof(translation));

            try
            {
                if(translation.Id == 0)
                { //Insert a new record
                    const string sql =
                        "INSERT INTO [Translations] ([Index], [Text], [Part], [ClauseId]) " +
                        "VALUES (@Index, @Text, @Part, @ClauseId);\n" +
                        "SELECT last_insert_rowid(); ";

                    using(IDbConnection cnn = GetConnection())
                        return cnn.Query<int>(sql,
                                              new { translation.Index, translation.Text, translation.Part, ClauseId = toClauseId })
                                  .Single();
                }
                else
                { //Update an existing record
                    const string sql =
                        "UPDATE [Translations] SET [Index] = @Index, [Text] = @Text, [Part] = @Part, [ClauseId] = @ClauseId " +
                        "WHERE [Id] = @Id; ";

                    using(IDbConnection cnn = GetConnection())
                        cnn.Execute(sql,
                            new { translation.Id, translation.Index, translation.Text, translation.Part, ClauseId = toClauseId });

                    return translation.Id;
                }
            }
            catch(Exception e) when(HandleError(e))
            { return 0; }
        }

        public void RemoveTranslations(params int[] translationIds)
        {
            if(translationIds is null || translationIds.Length == 0)
                return;

            string nums = translationIds.Aggregate(new StringBuilder(), (s, n) => s.AppendFormat("{0}, ", n))
                                        .ToString().Trim(' ', ',');

            try
            {
                using(IDbConnection cnn = GetConnection())
                    cnn.Execute($"DELETE FROM [Translations] WHERE [Id] IN ({nums}); ");
            }
            catch(Exception e) when(HandleError(e))
            { }
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

using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace DDictionary.DAL.Migrations
{
    /// <summary>
    /// Initial creation of the database structure.
    /// </summary>
    public sealed class SQLiteMigration_0001: SQLiteMigrationBase
    {
        public SQLiteMigration_0001(IDbConnection cnn)
            : base(cnn)
        {
            if(cnn is null) 
                throw new ArgumentNullException(nameof(cnn));
        }


        public override async Task<bool> NeedToApplyAsync()
        {
            const string sql = 
                "SELECT EXISTS( SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Clauses' );";

            return (await cnn.ExecuteScalarAsync<int>(sql)) != 1;
        }

        public override async Task ApplyMigrationAsync()
        {
            //Create correct structure
            await cnn.ExecuteAsync(Properties.Resources.DictionaryDB);
        }
    }
}

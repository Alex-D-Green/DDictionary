using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace DDictionary.DAL.Migrations
{
    /// <summary>
    /// Creation of the migration table.
    /// </summary>
    public sealed class SQLiteMigration_0002: SQLiteMigrationBase
    {
        public SQLiteMigration_0002(IDbConnection cnn)
            : base(cnn)
        {
            if(cnn is null) 
                throw new ArgumentNullException(nameof(cnn));
        }


        public override async Task<bool> NeedToApplyAsync()
        {
            const string sql = 
                "SELECT EXISTS( SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Migrations' );";

            return (await cnn.ExecuteScalarAsync<int>(sql)) != 1;
        }

        public override async Task ApplyMigrationAsync()
        {
            await cnn.ExecuteAsync("DROP TABLE IF EXISTS 'Migrations';");

            await cnn.ExecuteAsync("CREATE TABLE IF NOT EXISTS 'Migrations' (\n" +
                                   "    'Id' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,\n" +
                                   "    'Migration' TEXT NOT NULL,\n" +
                                   "    'Applied' TEXT NOT NULL\n" +
                                   ");");

            await cnn.ExecuteAsync("DROP INDEX IF EXISTS 'IDX_Migration';");

            await cnn.ExecuteAsync("CREATE INDEX IF NOT EXISTS 'IDX_Migration' ON 'Migrations' (\n" +
                                   "    'Migration'\n" +
                                   ");");

            await ListInMigrationAsync(nameof(SQLiteMigration_0001));
            await ListInMigrationAsync(nameof(SQLiteMigration_0002));
        }
    }
}

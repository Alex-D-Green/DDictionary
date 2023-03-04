using System;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace DDictionary.DAL.Migrations
{
    /// <summary>
    /// Addition of Training History table.
    /// </summary>
    public sealed class SQLiteMigration_0004: SQLiteMigrationBase
    {
        public SQLiteMigration_0004(IDbConnection cnn)
            : base(cnn)
        {
            if(cnn is null)
                throw new ArgumentNullException(nameof(cnn));
        }


        public override async Task ApplyMigrationAsync()
        {
            await cnn.ExecuteAsync("DROP TABLE IF EXISTS 'TrainingHistory'; ");

            await cnn.ExecuteAsync("CREATE TABLE 'TrainingHistory' (\n" +
                                   "    'Id'    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,\n" +
                                   "    'ClauseId'  INTEGER NOT NULL,\n" +
                                   "    'TrainingType'  INTEGER NOT NULL,\n" +
                                   "    'TrainingDate'  TEXT NOT NULL,\n" +
                                   "    'Success'   INTEGER NOT NULL,\n" +
                                   "    FOREIGN KEY('ClauseId') REFERENCES 'Clauses'('Id') ON DELETE CASCADE ON UPDATE CASCADE\n" +
                                   "); ");

            await cnn.ExecuteAsync("DROP INDEX IF EXISTS 'IDX_TrainingDate'; ");

            await cnn.ExecuteAsync("CREATE INDEX IF NOT EXISTS 'IDX_TrainingDate' ON 'TrainingHistory' (\n" +
                                   "    'TrainingDate'\n" +
                                   "); ");

            await ListInMigrationAsync(nameof(SQLiteMigration_0004));
        }
    }
}

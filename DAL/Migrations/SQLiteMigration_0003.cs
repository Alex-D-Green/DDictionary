using System;
using System.Data;
using System.Threading.Tasks;

using Dapper;

namespace DDictionary.DAL.Migrations
{
    /// <summary>
    /// Addition of Asterisks table.
    /// </summary>
    public sealed class SQLiteMigration_0003: SQLiteMigrationBase
    {
        public SQLiteMigration_0003(IDbConnection cnn)
            : base(cnn)
        {
            if(cnn is null)
                throw new ArgumentNullException(nameof(cnn));
        }


        public override async Task ApplyMigrationAsync()
        {
            await cnn.ExecuteAsync("DROP TABLE IF EXISTS 'Asterisks'; ");

            await cnn.ExecuteAsync("CREATE TABLE IF NOT EXISTS 'Asterisks' (\n" +
                                   "    'ClauseId' INTEGER NOT NULL PRIMARY KEY UNIQUE,\n" +
                                   "    'Type' INTEGER NOT NULL,\n" +
                                   "    'MeaningLastTrain' TEXT,\n" +
                                   "    'SpellingLastTrain' TEXT,\n" +
                                   "    'ListeningLastTrain' TEXT,\n" +
                                   "    FOREIGN KEY('ClauseId') REFERENCES 'Clauses'('Id') ON DELETE CASCADE ON UPDATE CASCADE\n" +
                                   "); ");

            await ListInMigrationAsync(nameof(SQLiteMigration_0003));
        }
    }
}

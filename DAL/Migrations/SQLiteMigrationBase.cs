using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DDictionary.Domain;

namespace DDictionary.DAL.Migrations
{
    /// <summary>
    /// A migration for SQLite data source.
    /// </summary>
    public abstract class SQLiteMigrationBase: IMigration
    {
        protected readonly IDbConnection cnn;


        protected SQLiteMigrationBase(IDbConnection cnn)
        {
            this.cnn = cnn ?? throw new ArgumentNullException(nameof(cnn));
        }


        public virtual async Task<bool> NeedToApplyAsync()
        {
            const string sql = 
                "SELECT EXISTS( SELECT 1 FROM [Migrations] WHERE [Migration] = @Migration );";

            return (await cnn.ExecuteScalarAsync<int>(sql, new { Migration = GetType().Name })) != 1;
        }

        public abstract Task ApplyMigrationAsync();


        protected async Task ListInMigrationAsync(string name)
        {
            const string sql = "INSERT INTO [Migrations] ([Migration], [Applied])\n" +
                               "    VALUES (@Migration, @Applied);";

            await cnn.ExecuteAsync(sql, new {
                Migration = name,
                Applied = DateTime.Now
            });
        }
    }
}

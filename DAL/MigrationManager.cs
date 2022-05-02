using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DDictionary.Domain;

namespace DDictionary.DAL
{
    /// <summary>
    /// The manager of migrations. Helps to manage migrations for a certain data source.
    /// </summary>
    public sealed class MigrationManager
    {
        /// <summary>
        /// The list of all available migrations for the data source.
        /// </summary>
        public IEnumerable<IMigration> Migrations { get; }


        public MigrationManager(IEnumerable<IMigration> migrations)
        {
            Migrations = migrations ?? throw new ArgumentNullException(nameof(migrations));
        }


        /// <summary>
        /// Apply all needed migrations to the related data source.
        /// </summary>
        public async Task UpdateDatabaseAsync()
        {
            foreach(var migration in Migrations)
            {
                if(await migration.NeedToApplyAsync())
                    await migration.ApplyMigrationAsync();
            }
        }
    }
}

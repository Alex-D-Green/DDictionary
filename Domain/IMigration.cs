using System.Threading.Tasks;

namespace DDictionary.Domain
{
    /// <summary>
    /// A data source migration - an update to move structure from one version to the next  one.
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        /// Checks whether the migration needed for the source.
        /// </summary>
        /// <returns><c>true</c> if this migration should be applied, <c>false</c> otherwise.</returns>
        Task<bool> NeedToApplyAsync();

        /// <summary>
        /// Apply this migration.
        /// </summary>
        Task ApplyMigrationAsync();
    }
}

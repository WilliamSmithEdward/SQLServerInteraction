namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// When the current database's latest backup finished, from <c>msdb.dbo.backupset</c>, or null when it has never been backed up.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the finish time of the latest backup, or null.</returns>
        public async Task<DateTime?> GetLastBackupDateTimeAsync(CancellationToken cancellationToken = default) =>
            (DateTime?)await CatalogScalarAsync("SELECT MAX(backup_finish_date) FROM msdb.dbo.backupset WHERE database_name = DB_NAME()", null, cancellationToken);
    }
}

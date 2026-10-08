namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// The total size of the current database's data and log files in bytes: <c>SUM(size) * 8192</c> from <c>sys.master_files</c>, where <c>size</c> counts 8 KB pages. Needs permission to read <c>sys.master_files</c>.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the size in bytes.</returns>
        public async Task<long> GetDatabaseSizeInBytesAsync(CancellationToken cancellationToken = default) =>
            Convert.ToInt64(await CatalogScalarAsync("SELECT SUM(CAST(size AS bigint)) * 8192 FROM sys.master_files WHERE database_id = DB_ID()", null, cancellationToken));
    }
}

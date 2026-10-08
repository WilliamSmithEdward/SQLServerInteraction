namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a SQL script from a file in the SQL Server database, as one batch. GO separators are not supported.
        /// </summary>
        /// <param name="filePath">The path of the file containing the SQL script.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteScriptFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            string script = await File.ReadAllTextAsync(filePath, cancellationToken);
            await ExecuteSQLAsync(script, cancellationToken);
        }
    }
}

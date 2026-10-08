namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Whether a database of the given name exists on the server, read from SqlClient's Databases schema collection without regard to case.
        /// </summary>
        /// <param name="databaseName">The name of the database, compared as a value.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is true if the database exists.</returns>
        public async Task<bool> DoesDatabaseExistAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            var databases = await SchemaColumnAsync("Databases", null, "database_name", cancellationToken);
            return databases.Contains(databaseName, StringComparer.OrdinalIgnoreCase);
        }
    }
}

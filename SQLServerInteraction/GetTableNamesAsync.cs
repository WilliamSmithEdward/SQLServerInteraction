namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// The table and view names of the database, from SqlClient's Tables schema collection, without schema names.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the list of names.</returns>
        public Task<List<string>> GetTableNamesAsync(CancellationToken cancellationToken = default) =>
            SchemaColumnAsync("Tables", null, "TABLE_NAME", cancellationToken);
    }
}

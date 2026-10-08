namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// The column names of a table, from SqlClient's Columns schema collection.
        /// </summary>
        /// <param name="tableName">The name of the table, passed to the schema lookup as restrictions. A bare name (<c>Sales</c>) matches in any schema; <c>dbo.Sales</c> or <c>[dbo].[Sales]</c> matches that schema only.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the column names of every table or view with that name.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public Task<List<string>> GetColumnNamesAsync(string tableName, CancellationToken cancellationToken = default)
        {
            var (database, schema, table) = SqlIdentifier.ParseTable(tableName);
            return SchemaColumnAsync("Columns", [database, schema, table], "COLUMN_NAME", cancellationToken);
        }
    }
}

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Whether a table or view of the given name exists, from INFORMATION_SCHEMA.TABLES.
        /// </summary>
        /// <param name="tableName">The name of the table, sent as parameters. A bare name (<c>Sales</c>) matches in any schema; <c>dbo.Sales</c> or <c>[dbo].[Sales]</c> matches that schema only.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is true if the table exists.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public async Task<bool> DoesTableExistAsync(string tableName, CancellationToken cancellationToken = default)
        {
            object? count = await CatalogScalarAsync(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @table AND (@schema IS NULL OR TABLE_SCHEMA = @schema) AND (@database IS NULL OR TABLE_CATALOG = @database)",
                TableNameParameters(tableName), cancellationToken);
            return Convert.ToInt32(count) > 0;
        }
    }
}

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Every primary key column of a table, in key order, from INFORMATION_SCHEMA.
        /// </summary>
        /// <param name="tableName">The name of the table, sent as parameters. A bare name (<c>Sales</c>) matches in any schema; <c>dbo.Sales</c> or <c>[dbo].[Sales]</c> matches that schema only.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the column names, empty when the table has no primary key.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public Task<List<string>> GetTablePrimaryKeyColumnsAsync(string tableName, CancellationToken cancellationToken = default) =>
            CatalogStringsAsync(
                "SELECT kcu.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu ON kcu.CONSTRAINT_CATALOG = tc.CONSTRAINT_CATALOG AND kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND tc.TABLE_NAME = @table AND (@schema IS NULL OR tc.TABLE_SCHEMA = @schema) AND (@database IS NULL OR tc.TABLE_CATALOG = @database) ORDER BY tc.TABLE_SCHEMA, kcu.ORDINAL_POSITION",
                TableNameParameters(tableName), cancellationToken);
    }
}

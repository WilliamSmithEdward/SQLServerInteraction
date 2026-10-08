namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// The columns of a table with their data types, from INFORMATION_SCHEMA.COLUMNS in ordinal order.
        /// </summary>
        /// <param name="tableName">The name of the table, sent as parameters. A bare name (<c>Sales</c>) matches in any schema; <c>dbo.Sales</c> or <c>[dbo].[Sales]</c> matches that schema only.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is a dictionary of column name to data type name.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public async Task<Dictionary<string, string>> GetTableColumnsAsync(string tableName, CancellationToken cancellationToken = default)
        {
            var rows = await CatalogRowsAsync(
                "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table AND (@schema IS NULL OR TABLE_SCHEMA = @schema) AND (@database IS NULL OR TABLE_CATALOG = @database) ORDER BY ORDINAL_POSITION",
                TableNameParameters(tableName),
                reader => (Column: reader.GetString(0), Type: reader.GetString(1)),
                cancellationToken);

            var columns = new Dictionary<string, string>();
            foreach (var (column, type) in rows)
            {
                if (column.Length > 0) columns[column] = type;
            }
            return columns;
        }
    }
}

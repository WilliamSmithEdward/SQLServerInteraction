namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// The number of rows in a table, from <c>SELECT COUNT(*)</c>.
        /// </summary>
        /// <param name="tableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the row count.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public async Task<int> GetTableRowCountAsync(string tableName, CancellationToken cancellationToken = default)
        {
            string sql = $"SELECT COUNT(*) FROM {SqlIdentifier.Quote(tableName)}";
            return Convert.ToInt32(await CatalogScalarAsync(sql, null, cancellationToken));
        }
    }
}

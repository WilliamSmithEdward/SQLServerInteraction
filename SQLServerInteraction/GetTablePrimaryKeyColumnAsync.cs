namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// The first primary key column of a table in key order, or null when it has no primary key.
        /// </summary>
        /// <param name="tableName">The name of the table, sent as parameters. A bare name (<c>Sales</c>) matches in any schema; <c>dbo.Sales</c> or <c>[dbo].[Sales]</c> matches that schema only.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the column name, or null.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public async Task<string?> GetTablePrimaryKeyColumnAsync(string tableName, CancellationToken cancellationToken = default) =>
            (await GetTablePrimaryKeyColumnsAsync(tableName, cancellationToken)).FirstOrDefault();
    }
}

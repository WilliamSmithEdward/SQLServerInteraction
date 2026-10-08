namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Drops an index on a specified table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="indexName">The name of the index, plain or bracketed, quoted as an identifier.</param>
        /// <exception cref="ArgumentException">The table or index name is not a valid name.</exception>
        public void IndexDrop(string tableName, string indexName)
        {
            ExecuteSQL(IndexDropSql(tableName, indexName));
        }

        /// <summary><c>DROP INDEX [index] ON [table]</c>, with both names quoted.</summary>
        internal static string IndexDropSql(string tableName, string indexName) =>
            $"DROP INDEX {SqlIdentifier.Quote(indexName, maxParts: 1)} ON {SqlIdentifier.Quote(tableName)}";
    }

    public partial class SQLServerTransaction
    {
        /// <inheritdoc cref="SQLServerInstance.IndexDrop(string, string)"/>
        public void IndexDrop(string tableName, string indexName)
        {
            ExecuteSQL(SQLServerInstance.IndexDropSql(tableName, indexName));
        }

        /// <summary>
        /// Asynchronously drops an index on a specified table, in this transaction.
        /// </summary>
        /// <param name="tableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="indexName">The name of the index, plain or bracketed, quoted as an identifier.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The table or index name is not a valid name.</exception>
        public Task IndexDropAsync(string tableName, string indexName, CancellationToken cancellationToken = default)
        {
            return ExecuteSQLAsync(SQLServerInstance.IndexDropSql(tableName, indexName), cancellationToken);
        }
    }
}

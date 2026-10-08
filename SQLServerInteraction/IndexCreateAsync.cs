namespace SQLServerInteraction
{
    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Creates an index on a specified column of a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="columnName">The name of the column, plain or bracketed, quoted as an identifier. The index is named IX_ followed by this name.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The table or column name is not a valid name.</exception>
        public Task IndexCreateAsync(string tableName, string columnName, CancellationToken cancellationToken = default) =>
            ExecuteSQLAsync(SQLServerInstance.IndexCreateSql(tableName, columnName), cancellationToken);
    }

    public partial class SQLServerInstance
    {
        /// <inheritdoc cref="SQLServerTransaction.IndexCreateAsync(string, string, CancellationToken)"/>
        public Task IndexCreateAsync(string tableName, string columnName, CancellationToken cancellationToken = default) =>
            ExecuteSQLAsync(IndexCreateSql(tableName, columnName), cancellationToken);

        /// <summary><c>CREATE INDEX [IX_column] ON [table] ([column])</c>, with every name quoted.</summary>
        internal static string IndexCreateSql(string tableName, string columnName)
        {
            string column = SqlIdentifier.Parse(columnName, maxParts: 1)[0];
            return $"CREATE INDEX {SqlIdentifier.QuotePart("IX_" + column)} ON {SqlIdentifier.Quote(tableName)} ({SqlIdentifier.QuotePart(column)})";
        }
    }
}

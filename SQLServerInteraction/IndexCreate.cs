namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Creates an index on a specified column of a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="columnName">The name of the column, plain or bracketed, quoted as an identifier. The index is named IX_ followed by this name.</param>
        /// <exception cref="ArgumentException">The table or column name is not a valid name.</exception>
        public void IndexCreate(string tableName, string columnName)
        {
            string column = SqlIdentifier.Parse(columnName, maxParts: 1)[0];
            string sql = $"CREATE INDEX {SqlIdentifier.QuotePart("IX_" + column)} ON {SqlIdentifier.Quote(tableName)} ({SqlIdentifier.QuotePart(column)})";
            ExecuteSQL(sql);
        }
    }
}

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
            string sql = $"DROP INDEX {SqlIdentifier.Quote(indexName, maxParts: 1)} ON {SqlIdentifier.Quote(tableName)}";
            ExecuteSQL(sql);
        }
    }
}

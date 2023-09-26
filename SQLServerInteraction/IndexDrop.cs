namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Drops an index on a specified table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table containing the index.</param>
        /// <param name="indexName">The name of the index to drop.</param>
        public void IndexDrop(string tableName, string indexName)
        {
            string sql = $"DROP INDEX {indexName} ON {tableName}";
            ExecuteSQL(sql);
        }
    }
}

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Creates an index on a specified column of a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table for which the index is created.</param>
        /// <param name="columnName">The name of the column on which to create the index.</param>
        public void IndexCreate(string tableName, string columnName)
        {
            string sql = $"CREATE INDEX IX_{columnName} ON {tableName} ({columnName})";
            ExecuteSQL(sql);
        }
    }
}

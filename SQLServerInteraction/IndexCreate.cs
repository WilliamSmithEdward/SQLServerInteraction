namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Creates an index on a specified column of a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table for which the index is created, inserted into the SQL as written, without quoting or escaping.</param>
        /// <param name="columnName">The name of the column on which to create the index, inserted into the SQL as written, without quoting or escaping. The index is named IX_ followed by this name.</param>
        public void IndexCreate(string tableName, string columnName)
        {
            string sql = $"CREATE INDEX IX_{columnName} ON {tableName} ({columnName})";
            ExecuteSQL(sql);
        }
    }
}

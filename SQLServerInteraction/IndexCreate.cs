namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public void IndexCreate(string tableName, string columnName)
        {
            string sql = $"CREATE INDEX IX_{columnName} ON {tableName} ({columnName})";
            ExecuteSQL(sql);
        }
    }
}

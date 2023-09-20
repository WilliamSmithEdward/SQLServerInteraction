namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public void IndexDrop(string tableName, string indexName)
        {
            string sql = $"DROP INDEX {indexName} ON {tableName}";
            ExecuteSQL(sql);
        }
    }
}

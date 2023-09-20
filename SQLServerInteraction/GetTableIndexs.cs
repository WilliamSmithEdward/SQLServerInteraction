using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public List<string> GetTableIndexs(string tableName)
        {
            var indexes = new List<string>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string sql = $"SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('{tableName}') AND is_primary_key = 0 AND type_desc = 'NONCLUSTERED'";

            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                string indexName = reader["name"].ToString() ?? "";
                if(!string.IsNullOrEmpty(indexName)) indexes.Add(indexName);
            }

            return indexes;
        }

    }
}

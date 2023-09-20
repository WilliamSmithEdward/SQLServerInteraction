using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public void InsertData(string sqlServerTableName, Dictionary<string, object> values)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string columns = string.Join(", ", values.Keys);
            string parameters = string.Join(", ", values.Keys.Select(key => "@" + key));
            string sql = $"INSERT INTO {sqlServerTableName} ({columns}) VALUES ({parameters})";

            using var command = new SqlCommand(sql, connection);

            foreach (var kvp in values)
            {
                command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
            }

            command.ExecuteNonQuery();
        }
    }
}

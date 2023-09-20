using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public async Task InsertDataAsync(string sqlServerTableName, Dictionary<string, object> values)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Create SQL query to insert data
            string columns = string.Join(", ", values.Keys);
            string parameters = string.Join(", ", values.Keys.Select(key => "@" + key));
            string sql = $"INSERT INTO {sqlServerTableName} ({columns}) VALUES ({parameters})";

            using var command = new SqlCommand(sql, connection);

            foreach (var kvp in values)
            {
                command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
            }

            await command.ExecuteNonQueryAsync();
        }
    }
}

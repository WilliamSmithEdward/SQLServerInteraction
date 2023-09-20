using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public async Task UpdateDataAsync(string sqlServerTableName, Dictionary<string, object> valuesToUpdate, string condition)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string updates = string.Join(", ", valuesToUpdate.Select(kvp => $"{kvp.Key} = @{kvp.Key}"));
            string sql = $"UPDATE {sqlServerTableName} SET {updates} WHERE {condition}";

            using var command = new SqlCommand(sql, connection);

            foreach (var kvp in valuesToUpdate)
            {
                command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
            }

            await command.ExecuteNonQueryAsync();
        }
    }
}

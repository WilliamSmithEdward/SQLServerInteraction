using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public async Task ExecuteParameterizedQueryAsync(string sql, Dictionary<string, object> parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            foreach (var kvp in parameters)
            {
                command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
            }

            await command.ExecuteNonQueryAsync();
        }
    }
}

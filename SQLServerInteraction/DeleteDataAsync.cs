using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public async Task DeleteDataAsync(string sqlServerTableName, string condition)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = $"DELETE FROM {sqlServerTableName} WHERE {condition}";

            using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

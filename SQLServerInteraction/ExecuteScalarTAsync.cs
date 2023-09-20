using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public async Task<T?> ExecuteScalarAsync<T>(string sql)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            var result = await command.ExecuteScalarAsync();

            return result != null && result != DBNull.Value ? (T)Convert.ChangeType(result, typeof(T)) : default;
        }
    }
}

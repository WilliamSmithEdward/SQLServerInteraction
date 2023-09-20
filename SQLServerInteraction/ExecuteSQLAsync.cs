using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public async Task ExecuteSQLAsync(string sql)
        {
            using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            await command.ExecuteNonQueryAsync();
        }
    }
}

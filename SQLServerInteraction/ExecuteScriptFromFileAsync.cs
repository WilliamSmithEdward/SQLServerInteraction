using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public async Task ExecuteScriptFromFileAsync(string filePath)
        {
            string script = await File.ReadAllTextAsync(filePath);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(script, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

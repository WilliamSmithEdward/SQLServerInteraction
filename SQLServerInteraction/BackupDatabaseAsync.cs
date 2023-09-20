using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public async Task BackupDatabaseAsync(string backupFilePath)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = $"BACKUP DATABASE [{connection.Database}] TO DISK = '{backupFilePath}'";

            using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

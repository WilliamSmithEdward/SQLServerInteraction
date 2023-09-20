using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public async Task RestoreDatabaseAsync(string backupFilePath)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = $"USE master; RESTORE DATABASE [{connection.Database}] FROM DISK = '{backupFilePath}'";

            using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

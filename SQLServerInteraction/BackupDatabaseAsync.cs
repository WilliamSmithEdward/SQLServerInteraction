using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Creates a backup of the current SQL Server database asynchronously.
        /// </summary>
        /// <param name="backupFilePath">The file path where the backup will be saved.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

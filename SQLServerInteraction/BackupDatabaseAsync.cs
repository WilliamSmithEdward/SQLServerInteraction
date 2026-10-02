using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Creates a backup of the current SQL Server database asynchronously.
        /// </summary>
        /// <param name="backupFilePath">The path on the SQL Server machine where the backup will be saved. It is sent as a parameter, as is the database name.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task BackupDatabaseAsync(string backupFilePath)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("BACKUP DATABASE @database TO DISK = @path", connection);
            command.Parameters.AddWithValue("@database", connection.Database);
            command.Parameters.AddWithValue("@path", backupFilePath);
            await command.ExecuteNonQueryAsync();
        }
    }
}

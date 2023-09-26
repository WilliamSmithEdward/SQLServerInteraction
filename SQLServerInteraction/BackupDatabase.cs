using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Creates a backup of the current SQL Server database synchronously.
        /// </summary>
        /// <param name="backupFilePath">The file path where the backup will be saved.</param>
        public void BackupDatabase(string backupFilePath)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string sql = $"BACKUP DATABASE [{connection.Database}] TO DISK = '{backupFilePath}'";

            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}

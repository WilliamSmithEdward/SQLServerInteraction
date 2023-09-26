using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Restores a database from a specified backup file.
        /// </summary>
        /// <param name="backupFilePath">The file path to the database backup.</param>
        public void RestoreDatabase(string backupFilePath)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string sql = $"USE master; RESTORE DATABASE [{connection.Database}] FROM DISK = '{backupFilePath}'";

            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}

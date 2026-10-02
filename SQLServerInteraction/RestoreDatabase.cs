using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Restores the database named in the connection string from a specified backup file, with no RESTORE options.
        /// </summary>
        /// <param name="backupFilePath">The path of the backup on the SQL Server machine, inserted into the SQL as written, without quoting or escaping.</param>
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

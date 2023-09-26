using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the date and time of the last database backup for the current SQL Server database.
        /// </summary>
        /// <returns>The date and time of the last database backup, or null if no backup is found.</returns>
        public DateTime? GetLastBackupDateTime()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string sql = "SELECT MAX(backup_finish_date) FROM msdb.dbo.backupset WHERE database_name = DB_NAME()";

            using var command = new SqlCommand(sql, connection);
            var lastBackupDateTime = command.ExecuteScalar();

            return lastBackupDateTime != DBNull.Value ? (DateTime?)lastBackupDateTime : null;
        }
    }
}

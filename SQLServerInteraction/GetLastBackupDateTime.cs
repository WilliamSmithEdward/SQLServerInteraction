using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
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

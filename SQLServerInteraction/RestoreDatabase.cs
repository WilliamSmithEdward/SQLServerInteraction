using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
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

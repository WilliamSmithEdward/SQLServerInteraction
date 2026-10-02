using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Restores the database named in the connection string from a specified backup file, with no RESTORE options.
        /// The connection switches to master first, so that it does not hold the database open itself.
        /// </summary>
        /// <param name="backupFilePath">The path of the backup on the SQL Server machine. It is sent as a parameter, as is the database name.</param>
        public void RestoreDatabase(string backupFilePath)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string database = connection.Database;
            connection.ChangeDatabase("master");

            using var command = new SqlCommand("RESTORE DATABASE @database FROM DISK = @path", connection);
            command.Parameters.AddWithValue("@database", database);
            command.Parameters.AddWithValue("@path", backupFilePath);
            command.ExecuteNonQuery();
        }
    }
}

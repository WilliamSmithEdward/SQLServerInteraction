using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Backs up the database named in the connection string to a file, with <c>BACKUP DATABASE ... TO DISK</c>. The path must be reachable by the SQL Server service account; the name and the path are sent as parameters.
        /// </summary>
        /// <param name="backupFilePath">The path of the backup file, as the server sees it.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task BackupDatabaseAsync(string backupFilePath, CancellationToken cancellationToken = default) =>
            WithConnectionAsync(async connection =>
            {
                using var command = new SqlCommand("BACKUP DATABASE @database TO DISK = @path", connection);
                command.Parameters.AddWithValue("@database", connection.Database);
                command.Parameters.AddWithValue("@path", backupFilePath);
                return await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);
    }
}

using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Restores the database named in the connection string from a backup file, with <c>RESTORE DATABASE ... FROM DISK</c> and no options. The connection switches to <c>master</c> first, so that it does not hold the database open itself; the name and the path are sent as parameters.
        /// </summary>
        /// <param name="backupFilePath">The path of the backup file, as the server sees it.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task RestoreDatabaseAsync(string backupFilePath, CancellationToken cancellationToken = default) =>
            WithConnectionAsync(async connection =>
            {
                string database = connection.Database;
                await connection.ChangeDatabaseAsync("master", cancellationToken);

                using var command = new SqlCommand("RESTORE DATABASE @database FROM DISK = @path", connection);
                command.Parameters.AddWithValue("@database", database);
                command.Parameters.AddWithValue("@path", backupFilePath);
                return await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);
    }
}

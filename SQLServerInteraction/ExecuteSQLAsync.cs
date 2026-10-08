using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Executes a SQL command in the SQL Server database.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteSQLAsync(string sql, CancellationToken cancellationToken = default)
        {
            using var command = new SqlCommand(sql, _connection, Transaction);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public partial class SQLServerInstance
    {
        /// <inheritdoc cref="SQLServerTransaction.ExecuteSQLAsync(string, CancellationToken)"/>
        public Task ExecuteSQLAsync(string sql, CancellationToken cancellationToken = default) =>
            RunAsync(work => work.ExecuteSQLAsync(sql, cancellationToken), cancellationToken);
    }
}

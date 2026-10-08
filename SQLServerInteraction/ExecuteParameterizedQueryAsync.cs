using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Executes a parameterized SQL command with SqlParameter objects. No result set or row count is returned.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">An array of SqlParameter objects to be added to the SQL command. A SqlParameter can belong to only one command, so build new ones for each call.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteParameterizedQueryAsync(string sql, SqlParameter[] parameters, CancellationToken cancellationToken = default)
        {
            using var command = new SqlCommand(sql, _connection, Transaction);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public partial class SQLServerInstance
    {
        /// <inheritdoc cref="SQLServerTransaction.ExecuteParameterizedQueryAsync(string, SqlParameter[], CancellationToken)"/>
        public Task ExecuteParameterizedQueryAsync(string sql, SqlParameter[] parameters, CancellationToken cancellationToken = default) =>
            RunAsync(work => work.ExecuteParameterizedQueryAsync(sql, parameters, cancellationToken), cancellationToken);
    }
}

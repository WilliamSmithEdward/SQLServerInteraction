using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Executes a non-query SQL command with parameters.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">A dictionary of parameters to be added to the SQL command. Names work with or without the @, and null is sent as NULL.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteNonQueryWithParametersAsync(string sql, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            using var command = new SqlCommand(sql, _connection, Transaction);
            CommandParameters.Add(command, parameters);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public partial class SQLServerInstance
    {
        /// <inheritdoc cref="SQLServerTransaction.ExecuteNonQueryWithParametersAsync(string, Dictionary{string, object}, CancellationToken)"/>
        public Task ExecuteNonQueryWithParametersAsync(string sql, Dictionary<string, object> parameters, CancellationToken cancellationToken = default) =>
            RunAsync(work => work.ExecuteNonQueryWithParametersAsync(sql, parameters, cancellationToken), cancellationToken);
    }
}

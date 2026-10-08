using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Executes a stored procedure in the SQL Server database.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure to execute. It is sent to SqlClient as a procedure name, not parsed as a batch.</param>
        /// <param name="parameters">An optional array of SQL parameters to pass to the stored procedure.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteStoredProcedureAsync(string storedProcedureName, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default)
        {
            using var command = new SqlCommand(storedProcedureName, _connection, Transaction);
            command.CommandType = CommandType.StoredProcedure;
            if (parameters != null) command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public partial class SQLServerInstance
    {
        /// <inheritdoc cref="SQLServerTransaction.ExecuteStoredProcedureAsync(string, SqlParameter[], CancellationToken)"/>
        public Task ExecuteStoredProcedureAsync(string storedProcedureName, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default) =>
            RunAsync(work => work.ExecuteStoredProcedureAsync(storedProcedureName, parameters, cancellationToken), cancellationToken);
    }
}

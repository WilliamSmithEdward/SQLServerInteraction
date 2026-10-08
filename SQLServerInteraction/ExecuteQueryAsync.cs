using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Executes a SQL query and returns the results as a DataTable.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is a DataTable containing the results of the query.</returns>
        public async Task<DataTable> ExecuteQueryAsync(string sql, CancellationToken cancellationToken = default)
        {
            var dataTable = new DataTable();

            using var command = new SqlCommand(sql, _connection, Transaction);
            using var adapter = new SqlDataAdapter(command);
            using var registration = cancellationToken.Register(command.Cancel);
            await Task.Run(() => adapter.Fill(dataTable), cancellationToken);

            return dataTable;
        }
    }

    public partial class SQLServerInstance
    {
        /// <inheritdoc cref="SQLServerTransaction.ExecuteQueryAsync(string, CancellationToken)"/>
        public Task<DataTable> ExecuteQueryAsync(string sql, CancellationToken cancellationToken = default) =>
            RunAsync(work => work.ExecuteQueryAsync(sql, cancellationToken), cancellationToken);
    }
}

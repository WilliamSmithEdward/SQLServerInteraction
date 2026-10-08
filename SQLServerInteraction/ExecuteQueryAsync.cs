using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously executes a SQL query and returns the results as a DataTable.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <returns>A DataTable containing the results of the query.</returns>
        public async Task<DataTable> ExecuteQueryAsync(string sql)
        {
            using var work = await ConnectAsync();
            return await work.ExecuteQueryAsync(sql);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Asynchronously executes a SQL query and returns the results as a DataTable.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A DataTable containing the results of the query.</returns>
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
}

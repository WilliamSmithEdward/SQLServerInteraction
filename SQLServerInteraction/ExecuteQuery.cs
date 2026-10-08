using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a SQL query and returns the results as a DataTable.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <returns>A DataTable containing the results of the query.</returns>
        public DataTable ExecuteQuery(string sql)
        {
            using var work = Connect();
            return work.ExecuteQuery(sql);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <inheritdoc cref="SQLServerInstance.ExecuteQuery(string)"/>
        public DataTable ExecuteQuery(string sql)
        {
            var dataTable = new DataTable();

            using var command = new SqlCommand(sql, _connection, Transaction);
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);

            return dataTable;
        }
    }
}

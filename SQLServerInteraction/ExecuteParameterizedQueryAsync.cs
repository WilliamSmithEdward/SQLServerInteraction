using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously executes a parameterized SQL command with SqlParameter objects. No result set or row count is returned.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">An array of SqlParameter objects to be added to the SQL command.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteParameterizedQueryAsync(string sql, SqlParameter[] parameters)
        {
            using var work = await ConnectAsync();
            await work.ExecuteParameterizedQueryAsync(sql, parameters);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Asynchronously executes a parameterized SQL command with SqlParameter objects. No result set or row count is returned.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">An array of SqlParameter objects to be added to the SQL command.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteParameterizedQueryAsync(string sql, SqlParameter[] parameters, CancellationToken cancellationToken = default)
        {
            using var command = new SqlCommand(sql, _connection, Transaction);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}

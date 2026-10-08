using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously executes a SQL query and returns the result as a single value of type T.
        /// </summary>
        /// <typeparam name="T">The type of the expected result. A nullable type such as <c>int?</c> works: the value is converted to its underlying type.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <returns>A task representing the asynchronous operation that returns the result of the query as a single value of type T, or the default value of T if the result is null or DBNull.Value.</returns>
        public async Task<T?> ExecuteScalarAsync<T>(string sql)
        {
            using var work = await ConnectAsync();
            return await work.ExecuteScalarAsync<T>(sql);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Asynchronously executes a SQL query and returns the result as a single value of type T.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation that returns the result of the query as a single value of type T, or the default value of T if the result is null or DBNull.Value.</returns>
        public async Task<T?> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken = default)
        {
            using var command = new SqlCommand(sql, _connection, Transaction);
            return ValueConversion.ChangeType<T>(await command.ExecuteScalarAsync(cancellationToken));
        }
    }
}

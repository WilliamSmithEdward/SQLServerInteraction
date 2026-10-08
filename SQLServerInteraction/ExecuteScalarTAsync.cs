using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Executes a SQL query and returns the result as a single value of type T.
        /// </summary>
        /// <typeparam name="T">The type of the expected result. A nullable type such as <c>int?</c> works: the value is converted to its underlying type.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the first column of the first row as a value of type T, or the default value of T if the result is null or DBNull.Value.</returns>
        public async Task<T?> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken = default)
        {
            using var command = new SqlCommand(sql, _connection, Transaction);
            return ValueConversion.ChangeType<T>(await command.ExecuteScalarAsync(cancellationToken));
        }
    }

    public partial class SQLServerInstance
    {
        /// <inheritdoc cref="SQLServerTransaction.ExecuteScalarAsync{T}(string, CancellationToken)"/>
        public Task<T?> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken = default) =>
            RunAsync(work => work.ExecuteScalarAsync<T>(sql, cancellationToken), cancellationToken);
    }
}

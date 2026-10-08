using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously executes a SQL query and returns the first column of every row, converted to <typeparamref name="T"/> with Convert.ChangeType. A nullable type such as <c>int?</c> converts to its underlying type, and NULL gives null.
        /// </summary>
        /// <typeparam name="T">The type of objects to retrieve from the query results.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of objects of type <typeparamref name="T"/> containing the results of the query.</returns>
        public async Task<List<T>> ExecuteQueryAsync<T>(string sql)
        {
            using var work = await ConnectAsync();
            return await work.ExecuteQueryAsync<T>(sql);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Asynchronously executes a SQL query and returns the first column of every row, converted to <typeparamref name="T"/> with Convert.ChangeType. A nullable type such as <c>int?</c> converts to its underlying type, and NULL gives null.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of objects of type <typeparamref name="T"/> containing the results of the query.</returns>
        public async Task<List<T>> ExecuteQueryAsync<T>(string sql, CancellationToken cancellationToken = default)
        {
            var results = new List<T>();

            using var command = new SqlCommand(sql, _connection, Transaction);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(ValueConversion.ChangeQueryValue<T>(reader[0]));
            }

            return results;
        }
    }
}

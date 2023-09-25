using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously executes a SQL query and returns the results as a list of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects to retrieve from the query results.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of objects of type <typeparamref name="T"/> containing the results of the query.</returns>
        public async Task<List<T>> ExecuteQueryAsync<T>(string sql)
        {
            var results = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var result = reader[0];
                results.Add((T)Convert.ChangeType(result, typeof(T)));
            }

            return results;
        }
    }
}

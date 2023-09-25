using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously executes a SQL query and returns the result as a single value of type T.
        /// </summary>
        /// <typeparam name="T">The type of the expected result.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <returns>A task representing the asynchronous operation that returns the result of the query as a single value of type T, or the default value of T if the result is null or DBNull.Value.</returns>
        public async Task<T?> ExecuteScalarAsync<T>(string sql)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            var result = await command.ExecuteScalarAsync();

            return result != null && result != DBNull.Value ? (T)Convert.ChangeType(result, typeof(T)) : default;
        }
    }
}

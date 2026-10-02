using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a SQL query and returns the result as a single value of type T.
        /// </summary>
        /// <typeparam name="T">The type of the expected result. A nullable type such as <c>int?</c> works: the value is converted to its underlying type.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <returns>The result of the query as a single value of type T, or the default value of T if the result is null or DBNull.Value.</returns>
        public T? ExecuteScalar<T>(string sql)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);

            var result = command.ExecuteScalar();

            return ValueConversion.ChangeType<T>(result);
        }
    }
}

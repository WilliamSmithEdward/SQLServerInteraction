using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a SQL query and returns the results as a list of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects to retrieve from the query results.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <returns>A list of objects of type <typeparamref name="T"/> containing the results of the query.</returns>
        public List<T> ExecuteQuery<T>(string sql)
        {
            var results = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var result = reader[0];
                results.Add((T)Convert.ChangeType(result, typeof(T)));
            }

            return results;
        }
    }
}

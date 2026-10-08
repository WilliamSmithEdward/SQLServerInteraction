using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a SQL query and returns the first column of every row, converted to <typeparamref name="T"/> with Convert.ChangeType. A nullable type such as <c>int?</c> converts to its underlying type, and NULL gives null.
        /// </summary>
        /// <typeparam name="T">The type of objects to retrieve from the query results.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <returns>A list of objects of type <typeparamref name="T"/> containing the results of the query.</returns>
        public List<T> ExecuteQuery<T>(string sql)
        {
            using var work = Connect();
            return work.ExecuteQuery<T>(sql);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <inheritdoc cref="SQLServerInstance.ExecuteQuery{T}(string)"/>
        public List<T> ExecuteQuery<T>(string sql)
        {
            var results = new List<T>();

            using var command = new SqlCommand(sql, _connection, Transaction);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(ValueConversion.ChangeQueryValue<T>(reader[0]));
            }

            return results;
        }
    }
}

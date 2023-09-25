using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a parameterized SQL query with SqlParameter objects.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">An array of SqlParameter objects to be added to the SQL command.</param>
        public void ExecuteParameterizedQuery(string sql, SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);

            command.ExecuteNonQuery();
        }
    }
}

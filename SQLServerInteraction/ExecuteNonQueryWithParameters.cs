using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a non-query SQL command with parameters.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">A dictionary of parameters to be added to the SQL command.</param>
        public void ExecuteNonQueryWithParameters(string sql, Dictionary<string, object> parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);

            foreach (var kvp in parameters)
            {
                command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
            }

            command.ExecuteNonQuery();
        }
    }
}

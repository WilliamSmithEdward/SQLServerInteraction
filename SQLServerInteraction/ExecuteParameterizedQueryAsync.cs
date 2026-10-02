using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously executes a parameterized SQL command with SqlParameter objects. No result set or row count is returned.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">An array of SqlParameter objects to be added to the SQL command.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteParameterizedQueryAsync(string sql, SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);

            await command.ExecuteNonQueryAsync();
        }
    }
}

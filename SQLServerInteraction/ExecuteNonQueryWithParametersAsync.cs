using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously executes a non-query SQL command with parameters.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">A dictionary of parameters to be added to the SQL command. Names work with or without the @, and null is sent as NULL.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteNonQueryWithParametersAsync(string sql, Dictionary<string, object> parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            CommandParameters.Add(command, parameters);

            await command.ExecuteNonQueryAsync();
        }
    }
}

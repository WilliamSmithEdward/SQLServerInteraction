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
            using var work = await ConnectAsync();
            await work.ExecuteNonQueryWithParametersAsync(sql, parameters);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <inheritdoc cref="SQLServerInstance.ExecuteNonQueryWithParametersAsync(string, Dictionary{string, object})"/>
        public async Task ExecuteNonQueryWithParametersAsync(string sql, Dictionary<string, object> parameters)
        {
            using var command = new SqlCommand(sql, _connection, Transaction);
            CommandParameters.Add(command, parameters);
            await command.ExecuteNonQueryAsync();
        }
    }
}

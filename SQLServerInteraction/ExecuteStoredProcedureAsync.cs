using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a stored procedure asynchronously in the SQL Server database.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure to execute.</param>
        /// <param name="parameters">An optional array of SQL parameters to pass to the stored procedure.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteStoredProcedureAsync(string storedProcedureName, SqlParameter[]? parameters = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(storedProcedureName, connection);
            command.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await command.ExecuteNonQueryAsync();
        }
    }
}

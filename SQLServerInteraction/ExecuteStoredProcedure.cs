using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a stored procedure synchronously in the SQL Server database.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure to execute.</param>
        /// <param name="parameters">An optional array of SQL parameters to pass to the stored procedure.</param>
        public void ExecuteStoredProcedure(string storedProcedureName, SqlParameter[]? parameters = null)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(storedProcedureName, connection);
            command.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            command.ExecuteNonQuery();
        }
    }
}

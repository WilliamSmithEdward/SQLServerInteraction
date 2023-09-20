using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public List<string> GetStoredProcedureParameters(string storedProcedureName)
        {
            var parameters = new List<string>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(storedProcedureName, connection);
            command.CommandType = CommandType.StoredProcedure;

            SqlCommandBuilder.DeriveParameters(command);

            for (int i = 1; i < command.Parameters.Count; i++)
            {
                parameters.Add(command.Parameters[i].ParameterName);
            }

            return parameters;
        }
    }
}

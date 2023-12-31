﻿using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the names of parameters for a stored procedure in the SQL Server database.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure for which to retrieve parameters.</param>
        /// <returns>A list of parameter names for the specified stored procedure.</returns>
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

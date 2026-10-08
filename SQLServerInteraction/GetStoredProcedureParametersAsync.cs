using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// The parameter names of a stored procedure, with their <c>@</c>, from <c>SqlCommandBuilder.DeriveParameters</c>. The return value is left out, and nothing runs the procedure.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure. It is sent to SqlClient as a procedure name, not parsed as a batch.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the parameter names in order.</returns>
        public Task<List<string>> GetStoredProcedureParametersAsync(string storedProcedureName, CancellationToken cancellationToken = default) =>
            WithConnectionAsync(async connection =>
            {
                using var command = new SqlCommand(storedProcedureName, connection);
                command.CommandType = CommandType.StoredProcedure;
                await Task.Run(() => SqlCommandBuilder.DeriveParameters(command), cancellationToken);

                return command.Parameters.Cast<SqlParameter>().Skip(1).Select(parameter => parameter.ParameterName).ToList();
            }, cancellationToken);
    }
}

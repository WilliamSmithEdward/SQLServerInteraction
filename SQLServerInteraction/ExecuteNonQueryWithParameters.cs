using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a non-query SQL command with parameters.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">A dictionary of parameters to be added to the SQL command. Names work with or without the @, and null is sent as NULL.</param>
        public void ExecuteNonQueryWithParameters(string sql, Dictionary<string, object> parameters)
        {
            using var work = Connect();
            work.ExecuteNonQueryWithParameters(sql, parameters);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <inheritdoc cref="SQLServerInstance.ExecuteNonQueryWithParameters(string, Dictionary{string, object})"/>
        public void ExecuteNonQueryWithParameters(string sql, Dictionary<string, object> parameters)
        {
            using var command = new SqlCommand(sql, _connection, Transaction);
            CommandParameters.Add(command, parameters);
            command.ExecuteNonQuery();
        }
    }
}

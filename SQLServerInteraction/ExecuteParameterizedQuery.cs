using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public void ExecuteParameterizedQuery(string sql, SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);

            command.ExecuteNonQuery();
        }
    }
}

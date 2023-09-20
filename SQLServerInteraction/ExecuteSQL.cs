using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public void ExecuteSQL(string sql)
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            using var command = new SqlCommand(sql, connection);

            command.ExecuteNonQuery();
        }
    }
}

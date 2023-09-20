using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public void DeleteData(string sqlServerTableName, string condition)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string sql = $"DELETE FROM {sqlServerTableName} WHERE {condition}";

            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}

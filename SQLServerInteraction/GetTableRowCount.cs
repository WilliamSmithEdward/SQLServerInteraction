using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public int GetTableRowCount(string tableName)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string sql = $"SELECT COUNT(*) FROM {tableName}";

            using var command = new SqlCommand(sql, connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}

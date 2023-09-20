using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public T? ExecuteScalar<T>(string sql)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);

            var result = command.ExecuteScalar();

            return result != null && result != DBNull.Value ? (T)Convert.ChangeType(result, typeof(T)) : default;
        }
    }
}

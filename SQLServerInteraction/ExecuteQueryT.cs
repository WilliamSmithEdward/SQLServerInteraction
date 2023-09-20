using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public List<T> ExecuteQuery<T>(string sql)
        {
            var results = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var result = reader[0];
                results.Add((T)Convert.ChangeType(result, typeof(T)));
            }

            return results;
        }
    }
}

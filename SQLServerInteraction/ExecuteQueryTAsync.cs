using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public async Task<List<T>> ExecuteQueryAsync<T>(string sql)
        {
            var results = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var result = reader[0];
                results.Add((T)Convert.ChangeType(result, typeof(T)));
            }

            return results;
        }
    }
}

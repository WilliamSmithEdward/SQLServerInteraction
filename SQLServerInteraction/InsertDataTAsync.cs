using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public async Task InsertDataAsync<T>(T data, string sqlServerTableName) where T : class
        {
            var properties = typeof(T).GetProperties();
            var columns = string.Join(", ", properties.Select(p => p.Name));
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            string sql = $"INSERT INTO {sqlServerTableName} ({columns}) VALUES ({values})";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            foreach (var property in properties)
            {
                command.Parameters.AddWithValue("@" + property.Name, property.GetValue(data) ?? DBNull.Value);
            }

            await command.ExecuteNonQueryAsync();
        }
    }
}

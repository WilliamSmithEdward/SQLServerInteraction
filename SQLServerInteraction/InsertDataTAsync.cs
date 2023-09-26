using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Inserts data into a SQL Server table asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of data to insert.</typeparam>
        /// <param name="data">The data to be inserted.</param>
        /// <param name="sqlServerTableName">The name of the SQL Server table to insert data into.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

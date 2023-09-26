using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Inserts data into a SQL Server table asynchronously using a dictionary of column names and corresponding values.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the SQL Server table to insert data into.</param>
        /// <param name="values">A dictionary containing column names and corresponding values for insertion.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InsertDataAsync(string sqlServerTableName, Dictionary<string, object> values)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Create SQL query to insert data
            string columns = string.Join(", ", values.Keys);
            string parameters = string.Join(", ", values.Keys.Select(key => "@" + key));
            string sql = $"INSERT INTO {sqlServerTableName} ({columns}) VALUES ({parameters})";

            using var command = new SqlCommand(sql, connection);

            foreach (var kvp in values)
            {
                command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
            }

            await command.ExecuteNonQueryAsync();
        }
    }
}

using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a SQL command asynchronously in the SQL Server database.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteSQLAsync(string sql)
        {
            using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            await command.ExecuteNonQueryAsync();
        }
    }
}

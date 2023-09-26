using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a SQL script from a file asynchronously in the SQL Server database.
        /// </summary>
        /// <param name="filePath">The path to the file containing the SQL script.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteScriptFromFileAsync(string filePath)
        {
            string script = await File.ReadAllTextAsync(filePath);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(script, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

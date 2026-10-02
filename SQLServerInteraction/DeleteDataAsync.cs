using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously deletes the records of a SQL Server table that match a condition.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="condition">The rows to delete: SQL text without the WHERE keyword, inserted as written. Put values in <paramref name="parameters"/> rather than in the text. To delete every row, pass <c>1 = 1</c>.</param>
        /// <param name="parameters">Optional parameters for <paramref name="condition"/>. Names work with or without the @, and null is sent as NULL.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The table name is not a valid name, or <paramref name="condition"/> is empty or whitespace.</exception>
        public async Task DeleteDataAsync(string sqlServerTableName, string condition, Dictionary<string, object>? parameters = null)
        {
            string sql = $"DELETE FROM {SqlIdentifier.Quote(sqlServerTableName)} WHERE {RequireCondition(condition, nameof(condition))}";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            CommandParameters.Add(command, parameters);
            await command.ExecuteNonQueryAsync();
        }
    }
}

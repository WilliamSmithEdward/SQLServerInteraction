using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously updates records in a SQL Server table.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the SQL Server table to update, inserted into the SQL as written, without quoting or escaping.</param>
        /// <param name="valuesToUpdate">A dictionary containing column names and their corresponding values to update. The values are sent as parameters; the keys are inserted into the SQL as written, without quoting or escaping, so bracket a name that needs it ("[My Field]"). Pass DBNull.Value, not null, for NULL.</param>
        /// <param name="condition">An optional condition to filter which records to update. SQL text without the WHERE keyword, inserted into the SQL as written, without quoting or escaping. Defaults to "1 = 1", updating every row, if not provided.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateDataAsync(string sqlServerTableName, Dictionary<string, object> valuesToUpdate, string condition = "")
        {
            string setClause = string.Join(", ", valuesToUpdate.Select(kvp => $"{kvp.Key} = @{kvp.Key.Replace(" ", "_").Replace("[", "").Replace("]", "")}"));
            if (string.IsNullOrEmpty(condition)) condition = "1 = 1";
            string sql = $"UPDATE {sqlServerTableName} SET {setClause} WHERE {condition}";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);

            foreach (var kvp in valuesToUpdate)
            {
                command.Parameters.AddWithValue("@" + kvp.Key.Replace(" ", "_").Replace("[", "").Replace("]", ""), kvp.Value);
            }

            await command.ExecuteNonQueryAsync();
        }
    }
}

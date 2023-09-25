using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously updates records in a SQL Server table.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the SQL Server table to update.</param>
        /// <param name="valuesToUpdate">A dictionary containing column names and their corresponding values to update. "[My Field]" is OK.</param>
        /// <param name="condition">An optional condition to filter which records to update. Defaults to "1 = 1" if not provided.</param>
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

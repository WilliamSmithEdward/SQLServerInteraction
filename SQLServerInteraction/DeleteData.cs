using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Deletes the records of a SQL Server table that match a condition.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="condition">The rows to delete: SQL text without the WHERE keyword, inserted as written. To delete every row, pass <c>1 = 1</c>. To send values as parameters, use the overload that takes them.</param>
        /// <exception cref="ArgumentException">The table name is not a valid name, or <paramref name="condition"/> is empty or whitespace.</exception>
        public void DeleteData(string sqlServerTableName, string condition = "")
        {
            DeleteData(sqlServerTableName, condition, null);
        }

        /// <summary>
        /// Deletes the records of a SQL Server table that match a condition, with parameters for the values in it.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="condition">The rows to delete: SQL text without the WHERE keyword, inserted as written. Put values in <paramref name="parameters"/> rather than in the text. To delete every row, pass <c>1 = 1</c>.</param>
        /// <param name="parameters">Parameters for <paramref name="condition"/>, or null for none. Names work with or without the @, and null is sent as NULL.</param>
        /// <exception cref="ArgumentException">The table name is not a valid name, or <paramref name="condition"/> is empty or whitespace.</exception>
        public void DeleteData(string sqlServerTableName, string condition, Dictionary<string, object>? parameters)
        {
            string sql = $"DELETE FROM {SqlIdentifier.Quote(sqlServerTableName)} WHERE {RequireCondition(condition, nameof(condition))}";

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            CommandParameters.Add(command, parameters);
            command.ExecuteNonQuery();
        }
    }
}

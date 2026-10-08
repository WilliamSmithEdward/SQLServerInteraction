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
            using var work = Connect();
            work.DeleteData(sqlServerTableName, condition, parameters);
        }

        /// <summary><c>DELETE FROM [table] WHERE condition</c>, with the name quoted and the condition as written.</summary>
        internal static string DeleteSql(string tableName, string condition) =>
            $"DELETE FROM {SqlIdentifier.Quote(tableName)} WHERE {RequireCondition(condition, nameof(condition))}";
    }

    public partial class SQLServerTransaction
    {
        /// <inheritdoc cref="SQLServerInstance.DeleteData(string, string)"/>
        public void DeleteData(string sqlServerTableName, string condition = "")
        {
            DeleteData(sqlServerTableName, condition, null);
        }

        /// <inheritdoc cref="SQLServerInstance.DeleteData(string, string, Dictionary{string, object}?)"/>
        public void DeleteData(string sqlServerTableName, string condition, Dictionary<string, object>? parameters)
        {
            string sql = SQLServerInstance.DeleteSql(sqlServerTableName, condition);

            using var command = new SqlCommand(sql, _connection, Transaction);
            CommandParameters.Add(command, parameters);
            command.ExecuteNonQuery();
        }
    }
}

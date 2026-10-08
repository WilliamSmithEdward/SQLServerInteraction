using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Updates the records of a SQL Server table that match a condition.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="valuesToUpdate">Column names and the values to set. Each key is one column name, plain or bracketed (<c>My Field</c> or <c>[My Field]</c>), and is quoted as an identifier; the values are sent as parameters named <c>@__value_0</c>, <c>@__value_1</c> and so on. Null is sent as NULL.</param>
        /// <param name="condition">The rows to update: SQL text without the WHERE keyword, inserted as written. To update every row, pass <c>1 = 1</c>. To send values as parameters, use the overload that takes them.</param>
        /// <exception cref="ArgumentException">A table or column name is not a valid name, there are no values, or <paramref name="condition"/> is empty or whitespace.</exception>
        public void UpdateData(string sqlServerTableName, Dictionary<string, object> valuesToUpdate, string condition = "")
        {
            UpdateData(sqlServerTableName, valuesToUpdate, condition, null);
        }

        /// <summary>
        /// Updates the records of a SQL Server table that match a condition, with parameters for the values in it.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="valuesToUpdate">Column names and the values to set. Each key is one column name, plain or bracketed (<c>My Field</c> or <c>[My Field]</c>), and is quoted as an identifier; the values are sent as parameters named <c>@__value_0</c>, <c>@__value_1</c> and so on. Null is sent as NULL.</param>
        /// <param name="condition">The rows to update: SQL text without the WHERE keyword, inserted as written. Put values in <paramref name="parameters"/> rather than in the text. To update every row, pass <c>1 = 1</c>.</param>
        /// <param name="parameters">Parameters for <paramref name="condition"/>, or null for none. Names work with or without the @, and null is sent as NULL.</param>
        /// <exception cref="ArgumentException">A table or column name is not a valid name, there are no values, or <paramref name="condition"/> is empty or whitespace.</exception>
        public void UpdateData(string sqlServerTableName, Dictionary<string, object> valuesToUpdate, string condition, Dictionary<string, object>? parameters)
        {
            using var work = Connect();
            work.UpdateData(sqlServerTableName, valuesToUpdate, condition, parameters);
        }

        internal static string UpdateSql(string tableName, IEnumerable<string> columnNames, string condition)
        {
            var columns = QuoteColumns(columnNames, nameof(columnNames));
            string setClause = string.Join(", ", columns.Select((column, i) => $"{column} = {CommandParameters.ValuePrefix}{i}"));
            return $"UPDATE {SqlIdentifier.Quote(tableName)} SET {setClause} WHERE {RequireCondition(condition, nameof(condition))}";
        }
    }

    public partial class SQLServerTransaction
    {
        /// <inheritdoc cref="SQLServerInstance.UpdateData(string, Dictionary{string, object}, string)"/>
        public void UpdateData(string sqlServerTableName, Dictionary<string, object> valuesToUpdate, string condition = "")
        {
            UpdateData(sqlServerTableName, valuesToUpdate, condition, null);
        }

        /// <inheritdoc cref="SQLServerInstance.UpdateData(string, Dictionary{string, object}, string, Dictionary{string, object}?)"/>
        public void UpdateData(string sqlServerTableName, Dictionary<string, object> valuesToUpdate, string condition, Dictionary<string, object>? parameters)
        {
            string sql = SQLServerInstance.UpdateSql(sqlServerTableName, valuesToUpdate.Keys, condition);

            using var command = new SqlCommand(sql, _connection, Transaction);
            SQLServerInstance.AddValueParameters(command, valuesToUpdate.Values);
            CommandParameters.Add(command, parameters);

            command.ExecuteNonQuery();
        }
    }
}

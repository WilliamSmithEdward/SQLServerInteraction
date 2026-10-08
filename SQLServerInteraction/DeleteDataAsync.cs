using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously deletes the records of a SQL Server table that match a condition.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="condition">The rows to delete: SQL text without the WHERE keyword, inserted as written. To delete every row, pass <c>1 = 1</c>. To send values as parameters, use the overload that takes them.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The table name is not a valid name, or <paramref name="condition"/> is empty or whitespace.</exception>
        public Task DeleteDataAsync(string sqlServerTableName, string condition = "")
        {
            return DeleteDataAsync(sqlServerTableName, condition, null);
        }

        /// <summary>
        /// Asynchronously deletes the records of a SQL Server table that match a condition, with parameters for the values in it.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="condition">The rows to delete: SQL text without the WHERE keyword, inserted as written. Put values in <paramref name="parameters"/> rather than in the text. To delete every row, pass <c>1 = 1</c>.</param>
        /// <param name="parameters">Parameters for <paramref name="condition"/>, or null for none. Names work with or without the @, and null is sent as NULL.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The table name is not a valid name, or <paramref name="condition"/> is empty or whitespace.</exception>
        public async Task DeleteDataAsync(string sqlServerTableName, string condition, Dictionary<string, object>? parameters)
        {
            using var work = await ConnectAsync();
            await work.DeleteDataAsync(sqlServerTableName, condition, parameters);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <inheritdoc cref="SQLServerInstance.DeleteDataAsync(string, string)"/>
        public Task DeleteDataAsync(string sqlServerTableName, string condition = "", CancellationToken cancellationToken = default)
        {
            return DeleteDataAsync(sqlServerTableName, condition, null, cancellationToken);
        }

        /// <inheritdoc cref="SQLServerInstance.DeleteDataAsync(string, string, Dictionary{string, object}?)"/>
        public async Task DeleteDataAsync(string sqlServerTableName, string condition, Dictionary<string, object>? parameters, CancellationToken cancellationToken = default)
        {
            string sql = SQLServerInstance.DeleteSql(sqlServerTableName, condition);

            using var command = new SqlCommand(sql, _connection, Transaction);
            CommandParameters.Add(command, parameters);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}

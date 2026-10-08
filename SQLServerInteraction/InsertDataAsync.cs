using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Inserts data into a SQL Server table using a dictionary of column names and corresponding values.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="values">Column names and the values to insert. Each key is one column name, plain or bracketed (<c>My Field</c> or <c>[My Field]</c>), and is quoted as an identifier; the values are sent as parameters. Null is sent as NULL.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">A table or column name is not a valid name, or there are no values.</exception>
        public async Task InsertDataAsync(string sqlServerTableName, Dictionary<string, object> values, CancellationToken cancellationToken = default)
        {
            string sql = SQLServerInstance.InsertSql(sqlServerTableName, values.Keys);

            using var command = new SqlCommand(sql, _connection, Transaction);
            SQLServerInstance.AddValueParameters(command, values.Values);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public partial class SQLServerInstance
    {
        /// <inheritdoc cref="SQLServerTransaction.InsertDataAsync(string, Dictionary{string, object}, CancellationToken)"/>
        public Task InsertDataAsync(string sqlServerTableName, Dictionary<string, object> values, CancellationToken cancellationToken = default) =>
            RunAsync(work => work.InsertDataAsync(sqlServerTableName, values, cancellationToken), cancellationToken);

        /// <summary>
        /// <c>INSERT INTO [table] ([a], [b]) VALUES (@__value_0, @__value_1)</c>, with every name quoted.
        /// </summary>
        internal static string InsertSql(string tableName, IEnumerable<string> columnNames)
        {
            var columns = QuoteColumns(columnNames, nameof(columnNames));
            string values = string.Join(", ", columns.Select((_, i) => CommandParameters.ValuePrefix + i));
            return $"INSERT INTO {SqlIdentifier.Quote(tableName)} ({string.Join(", ", columns)}) VALUES ({values})";
        }

        /// <summary>Each name as one quoted identifier, refusing an empty list.</summary>
        internal static List<string> QuoteColumns(IEnumerable<string> columnNames, string parameterName)
        {
            var columns = columnNames.Select(name => SqlIdentifier.Quote(name, maxParts: 1)).ToList();
            if (columns.Count == 0)
                throw new ArgumentException("At least one column is required.", parameterName);
            return columns;
        }

        /// <summary>The values for <see cref="InsertSql"/> and <see cref="UpdateSql"/>, in the order of their columns, with null sent as NULL.</summary>
        internal static void AddValueParameters(SqlCommand command, IEnumerable<object?> values)
        {
            int i = 0;
            foreach (var value in values)
            {
                command.Parameters.AddWithValue(CommandParameters.ValuePrefix + i++, CommandParameters.Value(value));
            }
        }
    }
}

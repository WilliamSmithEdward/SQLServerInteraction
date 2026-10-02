using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Inserts data into a SQL Server table synchronously using a dictionary of column names and corresponding values.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="values">Column names and the values to insert. Each key is one column name, plain or bracketed (<c>My Field</c> or <c>[My Field]</c>), and is quoted as an identifier; the values are sent as parameters. Null is sent as NULL.</param>
        /// <exception cref="ArgumentException">A table or column name is not a valid name, or there are no values.</exception>
        public void InsertData(string sqlServerTableName, Dictionary<string, object> values)
        {
            string sql = InsertSql(sqlServerTableName, values.Keys);

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            AddValueParameters(command, values.Values);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// <c>INSERT INTO [table] ([a], [b]) VALUES (@__value_0, @__value_1)</c>, with every name quoted.
        /// </summary>
        private static string InsertSql(string tableName, IEnumerable<string> columnNames)
        {
            var columns = QuoteColumns(columnNames, nameof(columnNames));
            string values = string.Join(", ", columns.Select((_, i) => CommandParameters.ValuePrefix + i));
            return $"INSERT INTO {SqlIdentifier.Quote(tableName)} ({string.Join(", ", columns)}) VALUES ({values})";
        }

        /// <summary>Each name as one quoted identifier, refusing an empty list.</summary>
        private static List<string> QuoteColumns(IEnumerable<string> columnNames, string parameterName)
        {
            var columns = columnNames.Select(name => SqlIdentifier.Quote(name, maxParts: 1)).ToList();
            if (columns.Count == 0)
                throw new ArgumentException("At least one column is required.", parameterName);
            return columns;
        }

        /// <summary>The values for <see cref="InsertSql"/> and <see cref="UpdateSql"/>, in the order of their columns, with null sent as NULL.</summary>
        private static void AddValueParameters(SqlCommand command, IEnumerable<object?> values)
        {
            int i = 0;
            foreach (var value in values)
            {
                command.Parameters.AddWithValue(CommandParameters.ValuePrefix + i++, CommandParameters.Value(value));
            }
        }
    }
}

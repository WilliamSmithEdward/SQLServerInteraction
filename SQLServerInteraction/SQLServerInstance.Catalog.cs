using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    // What the schema and server lookups share: one way to read a schema
    // collection, one way to read a catalog query's first column, and one way
    // to read a catalog scalar, each on a connection of its own.
    public partial class SQLServerInstance
    {
        /// <summary>
        /// One column of a SqlClient schema collection, such as <c>Tables</c> or
        /// <c>Columns</c>, with empty values left out.
        /// </summary>
        private Task<List<string>> SchemaColumnAsync(string collection, string?[]? restrictions, string column, CancellationToken cancellationToken) =>
            WithConnectionAsync(async connection =>
            {
                var table = restrictions == null
                    ? await connection.GetSchemaAsync(collection, cancellationToken)
                    : await connection.GetSchemaAsync(collection, restrictions, cancellationToken);

                return table.Rows.Cast<DataRow>()
                    .Select(row => row[column].ToString() ?? "")
                    .Where(value => value.Length > 0)
                    .ToList();
            }, cancellationToken);

        /// <summary>
        /// Every row of a catalog query, read through <paramref name="read"/>. The SQL is
        /// the library's own: a literal, or a literal with a quoted table name in it.
        /// </summary>
        private Task<List<T>> CatalogRowsAsync<T>(string sql, Action<SqlCommand>? parameters, Func<SqlDataReader, T> read, CancellationToken cancellationToken) =>
            WithConnectionAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                parameters?.Invoke(command);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                var rows = new List<T>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    rows.Add(read(reader));
                }
                return rows;
            }, cancellationToken);

        /// <summary>
        /// The first column of the first row of a catalog query, or null for no rows or
        /// NULL. The SQL is the library's own: a literal, or a literal with a quoted
        /// table name in it.
        /// </summary>
        private Task<object?> CatalogScalarAsync(string sql, Action<SqlCommand>? parameters, CancellationToken cancellationToken) =>
            WithConnectionAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                parameters?.Invoke(command);
                object? value = await command.ExecuteScalarAsync(cancellationToken);
                return value == DBNull.Value ? null : value;
            }, cancellationToken);

        /// <summary>A catalog query's rows, each read as a non-empty string from its first column.</summary>
        private async Task<List<string>> CatalogStringsAsync(string sql, Action<SqlCommand>? parameters, CancellationToken cancellationToken)
        {
            var values = await CatalogRowsAsync(sql, parameters, reader => reader.IsDBNull(0) ? "" : reader.GetString(0), cancellationToken);
            return values.Where(value => value.Length > 0).ToList();
        }

        /// <summary>Adds a table name's parts as the <c>@database</c>, <c>@schema</c> and <c>@table</c> parameters.</summary>
        private static Action<SqlCommand> TableNameParameters(string tableName) =>
            command => CommandParameters.AddTableName(command, tableName);
    }
}

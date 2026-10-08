using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Executes a SQL query with parameters and maps the result set to a list of objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of objects to create and populate from the query result. Every public property needs a column of its name, or of the name in its <see cref="SQLServerInstance.ColumnAttribute"/>.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional dictionary of SQL parameters. Names work with or without the @, and null is sent as NULL.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is a list of objects of type T populated with data from the query result.</returns>
        public async Task<List<T>> ExecuteQueryToObjectListAsync<T>(string sql, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default) where T : new()
        {
            var results = new List<T>();

            using var command = new SqlCommand(sql, _connection, Transaction);
            CommandParameters.Add(command, parameters);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(ReadObject<T>(reader));
            }

            return results;
        }

        /// <summary>
        /// The current row as a <typeparamref name="T"/>: each public property set from
        /// the column of its name or its <see cref="SQLServerInstance.ColumnAttribute"/> name,
        /// converted to the property's type, with NULL leaving the property as constructed.
        /// </summary>
        private static T ReadObject<T>(SqlDataReader reader) where T : new()
        {
            var obj = new T();

            foreach (var property in typeof(T).GetProperties())
            {
                string columnName = Attribute.GetCustomAttribute(property, typeof(SQLServerInstance.ColumnAttribute)) is SQLServerInstance.ColumnAttribute attribute ? attribute.Name : property.Name;

                if (!reader.IsDBNull(reader.GetOrdinal(columnName)))
                {
                    property.SetValue(obj, Convert.ChangeType(reader[columnName], Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType));
                }
            }

            return obj;
        }
    }

    public partial class SQLServerInstance
    {
        /// <inheritdoc cref="SQLServerTransaction.ExecuteQueryToObjectListAsync{T}(string, Dictionary{string, object}?, CancellationToken)"/>
        public Task<List<T>> ExecuteQueryToObjectListAsync<T>(string sql, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default) where T : new() =>
            RunAsync(work => work.ExecuteQueryToObjectListAsync<T>(sql, parameters, cancellationToken), cancellationToken);
    }
}

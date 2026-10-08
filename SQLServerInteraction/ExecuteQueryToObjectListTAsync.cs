using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously executes a SQL query with parameters and maps the result set to a list of objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of objects to create and populate from the query result. Every public property needs a column of its name, or of the name in its <see cref="ColumnAttribute"/>.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional dictionary of SQL parameters. Names work with or without the @, and null is sent as NULL.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of objects of type T populated with data from the query result.</returns>
        public async Task<List<T>> ExecuteQueryToObjectListAsync<T>(string sql, Dictionary<string, object>? parameters = null) where T : new()
        {
            using var work = await ConnectAsync();
            return await work.ExecuteQueryToObjectListAsync<T>(sql, parameters);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <inheritdoc cref="SQLServerInstance.ExecuteQueryToObjectListAsync{T}(string, Dictionary{string, object}?)"/>
        public async Task<List<T>> ExecuteQueryToObjectListAsync<T>(string sql, Dictionary<string, object>? parameters = null) where T : new()
        {
            var results = new List<T>();

            using var command = new SqlCommand(sql, _connection, Transaction);
            CommandParameters.Add(command, parameters);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(ReadObject<T>(reader));
            }

            return results;
        }
    }
}

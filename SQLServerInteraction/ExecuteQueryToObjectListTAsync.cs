using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously executes a SQL query with parameters and maps the result set to a list of objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of objects to create and populate from the query result.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional dictionary of SQL parameters.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of objects of type T populated with data from the query result.</returns>
        public async Task<List<T>> ExecuteQueryToObjectListAsync<T>(string sql, Dictionary<string, object>? parameters = null) where T : new()
        {
            var results = new List<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using var command = new SqlCommand(sql, connection);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var obj = new T();
                    var properties = typeof(T).GetProperties();

                    foreach (var property in properties)
                    {
                        string columnName = Attribute.GetCustomAttribute(property, typeof(ColumnAttribute)) is ColumnAttribute attribute ? attribute.Name : property.Name;

                        if (!reader.IsDBNull(reader.GetOrdinal(columnName)))
                        {
                            property.SetValue(obj, Convert.ChangeType(reader[columnName], Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType));
                        }
                    }

                    results.Add(obj);
                }
            }

            return results;
        }
    }
}

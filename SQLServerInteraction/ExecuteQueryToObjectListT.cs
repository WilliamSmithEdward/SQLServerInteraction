using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a SQL query and maps the result set to a list of objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of objects to create and populate from the query result.</typeparam>
        /// <param name="sql">The SQL query to execute.</param>
        /// <returns>A list of objects of type T populated with data from the query result.</returns>
        public List<T> ExecuteQueryToObjectList<T>(string sql) where T : new()
        {
            var results = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var obj = new T();
                var properties = typeof(T).GetProperties();

                foreach (var property in properties)
                {
                    string columnName = Attribute.GetCustomAttribute(property, typeof(ColumnAttribute)) is ColumnAttribute attribute ? attribute.Name : property.Name;

                    if (reader[columnName] != DBNull.Value)
                    {
                        property.SetValue(obj, Convert.ChangeType(reader[columnName], property.PropertyType));
                    }
                }

                results.Add(obj);
            }

            return results;
        }
    }    
}

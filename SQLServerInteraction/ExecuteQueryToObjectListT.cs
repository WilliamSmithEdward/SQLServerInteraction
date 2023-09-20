using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{

    public partial class SQLServerInstance
    {
        public List<T> ExecuteQueryToObjectListT<T>(string sql) where T : new()
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
                    var attribute = Attribute.GetCustomAttribute(property, typeof(ColumnAttribute)) as ColumnAttribute;

                    string columnName = attribute != null ? attribute.Name : property.Name;

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

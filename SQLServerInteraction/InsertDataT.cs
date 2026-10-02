using Microsoft.Data.SqlClient;
using System.Reflection;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Inserts data into a SQL Server table synchronously.
        /// </summary>
        /// <typeparam name="T">The type of data to insert. Every public instance property with a getter becomes a column, named by its <see cref="ColumnAttribute"/> or else by the property's name. Static properties and indexers are left out.</typeparam>
        /// <param name="data">The data to be inserted. A null property value is sent as NULL.</param>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <exception cref="ArgumentException">The table name or a column name is not a valid name, or <typeparamref name="T"/> has no such properties.</exception>
        public void InsertData<T>(T data, string sqlServerTableName) where T : class
        {
            var properties = InsertProperties<T>();
            string sql = InsertSql(sqlServerTableName, properties.Select(p => p.Column));

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            AddValueParameters(command, properties.Select(p => p.Property.GetValue(data)));

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// The properties <see cref="InsertData{T}(T, string)"/> writes, each with its column name:
        /// public instance properties with a public getter and no index parameters.
        /// </summary>
        private static List<(PropertyInfo Property, string Column)> InsertProperties<T>() =>
            typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
                .Select(p => (p, p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name))
                .ToList();
    }
}

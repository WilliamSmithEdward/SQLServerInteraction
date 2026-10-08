using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Inserts data into a SQL Server table asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of data to insert. Every public instance property with a getter becomes a column, named by its <see cref="ColumnAttribute"/> or else by the property's name. Static properties and indexers are left out.</typeparam>
        /// <param name="data">The data to be inserted. A null property value is sent as NULL.</param>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The table name or a column name is not a valid name, or <typeparamref name="T"/> has no such properties.</exception>
        public async Task InsertDataAsync<T>(T data, string sqlServerTableName) where T : class
        {
            using var work = await ConnectAsync();
            await work.InsertDataAsync(data, sqlServerTableName);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Inserts data into a SQL Server table asynchronously.
        /// </summary>
        /// <param name="data">The data to be inserted. A null property value is sent as NULL.</param>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The table name or a column name is not a valid name, or <typeparamref name="T"/> has no such properties.</exception>
        public async Task InsertDataAsync<T>(T data, string sqlServerTableName, CancellationToken cancellationToken = default) where T : class
        {
            var properties = SQLServerInstance.InsertProperties<T>();
            string sql = SQLServerInstance.InsertSql(sqlServerTableName, properties.Select(p => p.Column));

            using var command = new SqlCommand(sql, _connection, Transaction);
            SQLServerInstance.AddValueParameters(command, properties.Select(p => p.Property.GetValue(data)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}

using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Inserts data into a SQL Server table asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of data to insert. Every public property becomes a column of the same name; <see cref="ColumnAttribute"/> is not used.</typeparam>
        /// <param name="data">The data to be inserted.</param>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The table name is not a valid name, or <typeparamref name="T"/> has no public properties.</exception>
        public async Task InsertDataAsync<T>(T data, string sqlServerTableName) where T : class
        {
            var properties = typeof(T).GetProperties();
            string sql = InsertSql(sqlServerTableName, properties.Select(p => p.Name));

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            AddValueParameters(command, properties.Select(p => p.GetValue(data) ?? DBNull.Value));

            await command.ExecuteNonQueryAsync();
        }
    }
}

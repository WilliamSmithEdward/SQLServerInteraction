using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Inserts data into a SQL Server table asynchronously using a dictionary of column names and corresponding values.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="values">Column names and the values to insert. Each key is one column name, plain or bracketed (<c>My Field</c> or <c>[My Field]</c>), and is quoted as an identifier; the values are sent as parameters. Null is sent as NULL.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">A table or column name is not a valid name, or there are no values.</exception>
        public async Task InsertDataAsync(string sqlServerTableName, Dictionary<string, object> values)
        {
            string sql = InsertSql(sqlServerTableName, values.Keys);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            AddValueParameters(command, values.Values);

            await command.ExecuteNonQueryAsync();
        }
    }
}

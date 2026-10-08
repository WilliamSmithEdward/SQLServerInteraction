using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a SQL command asynchronously in the SQL Server database.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteSQLAsync(string sql)
        {
            using var work = await ConnectAsync();
            await work.ExecuteSQLAsync(sql);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <inheritdoc cref="SQLServerInstance.ExecuteSQLAsync(string)"/>
        public async Task ExecuteSQLAsync(string sql)
        {
            using var command = new SqlCommand(sql, _connection, Transaction);
            await command.ExecuteNonQueryAsync();
        }
    }
}

using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a SQL command synchronously in the SQL Server database.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        public void ExecuteSQL(string sql)
        {
            using var work = Connect();
            work.ExecuteSQL(sql);
        }
    }

    public partial class SQLServerTransaction
    {
        /// <inheritdoc cref="SQLServerInstance.ExecuteSQL(string)"/>
        public void ExecuteSQL(string sql)
        {
            using var command = new SqlCommand(sql, _connection, Transaction);
            command.ExecuteNonQuery();
        }
    }
}

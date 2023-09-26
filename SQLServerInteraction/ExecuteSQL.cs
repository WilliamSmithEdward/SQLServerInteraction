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
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            using var command = new SqlCommand(sql, connection);

            command.ExecuteNonQuery();
        }
    }
}

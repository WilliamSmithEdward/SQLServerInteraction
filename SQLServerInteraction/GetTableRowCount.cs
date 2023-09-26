using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the row count of a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table for which to retrieve the row count.</param>
        /// <returns>The number of rows in the specified table.</returns>
        public int GetTableRowCount(string tableName)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string sql = $"SELECT COUNT(*) FROM {tableName}";

            using var command = new SqlCommand(sql, connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}

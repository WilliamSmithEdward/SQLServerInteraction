using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Checks whether a table with the specified name exists in the database.
        /// </summary>
        /// <param name="tableName">The name of the table to check.</param>
        /// <returns>True if the table exists; otherwise, false.</returns>
        public bool DoesTableExist(string tableName)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'", connection);

            return (int)command.ExecuteScalar() > 0;
        }
    }
}

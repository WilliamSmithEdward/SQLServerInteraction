using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Checks whether a table or view with the specified name exists in any schema of the database.
        /// </summary>
        /// <param name="tableName">The name of the table to check, without a schema, placed between quotes in the SQL as written, without escaping.</param>
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

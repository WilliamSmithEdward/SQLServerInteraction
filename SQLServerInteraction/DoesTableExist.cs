using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Checks whether a table or view with the specified name exists in the database.
        /// </summary>
        /// <param name="tableName">The name of the table, sent as parameters. A bare name (<c>Sales</c>) matches in any schema; <c>dbo.Sales</c> or <c>[dbo].[Sales]</c> matches that schema only.</param>
        /// <returns>True if the table exists; otherwise, false.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public bool DoesTableExist(string tableName)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @table AND (@schema IS NULL OR TABLE_SCHEMA = @schema) AND (@database IS NULL OR TABLE_CATALOG = @database)",
                connection);
            CommandParameters.AddTableName(command, tableName);

            return (int)command.ExecuteScalar() > 0;
        }
    }
}

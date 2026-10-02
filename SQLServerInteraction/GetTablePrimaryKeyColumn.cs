using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the primary key column of a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table, sent as parameters. A bare name (<c>Sales</c>) matches in any schema; <c>dbo.Sales</c> or <c>[dbo].[Sales]</c> matches that schema only.</param>
        /// <returns>The name of one primary key column for the specified table (only one, for a composite key), or null if not found.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public string? GetTablePrimaryKeyColumn(string tableName)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE OBJECTPROPERTY(OBJECT_ID(QUOTENAME(CONSTRAINT_SCHEMA) + '.' + QUOTENAME(CONSTRAINT_NAME)), 'IsPrimaryKey') = 1 AND TABLE_NAME = @table AND (@schema IS NULL OR TABLE_SCHEMA = @schema) AND (@database IS NULL OR TABLE_CATALOG = @database)",
                connection);
            CommandParameters.AddTableName(command, tableName);

            var result = command.ExecuteScalar();
            return result?.ToString();
        }
    }
}

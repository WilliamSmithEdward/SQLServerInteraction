using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the primary key column of a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table for which to retrieve the primary key column.</param>
        /// <returns>The name of the primary key column for the specified table, or null if not found.</returns>
        public string? GetTablePrimaryKeyColumn(string tableName)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string sql = $@"SELECT COLUMN_NAME 
                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                    WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 
                    AND TABLE_NAME = '{tableName}'";

            using var command = new SqlCommand(sql, connection);

            var result = command.ExecuteScalar();
            return result?.ToString();
        }
    }
}

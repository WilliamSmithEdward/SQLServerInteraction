using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the names and data types of columns for a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table, sent as parameters. A bare name (<c>Sales</c>) matches in any schema; <c>dbo.Sales</c> or <c>[dbo].[Sales]</c> matches that schema only.</param>
        /// <returns>A dictionary where keys are column names and values are data types for the specified table.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public Dictionary<string, string> GetTableColumns(string tableName)
        {
            var columns = new Dictionary<string, string>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(
                "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table AND (@schema IS NULL OR TABLE_SCHEMA = @schema) AND (@database IS NULL OR TABLE_CATALOG = @database) ORDER BY ORDINAL_POSITION",
                connection);
            CommandParameters.AddTableName(command, tableName);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                string columnName = reader["COLUMN_NAME"].ToString() ?? "";
                string dataType = reader["DATA_TYPE"].ToString() ?? "";
                if(!string.IsNullOrEmpty(columnName)) columns[columnName] = dataType;
            }

            return columns;
        }
    }
}

using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the primary key columns of a table in the SQL Server database, in key order.
        /// </summary>
        /// <param name="tableName">The name of the table, sent as parameters. A bare name (<c>Sales</c>) matches in any schema; <c>dbo.Sales</c> or <c>[dbo].[Sales]</c> matches that schema only.</param>
        /// <returns>The primary key's column names in the order the key lists them, or an empty list if the table has no primary key or does not exist. A bare name that several schemas use gives the columns of each, schema by schema.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public List<string> GetTablePrimaryKeyColumns(string tableName)
        {
            var columns = new List<string>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(
                "SELECT kcu.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu ON kcu.CONSTRAINT_CATALOG = tc.CONSTRAINT_CATALOG AND kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND tc.TABLE_NAME = @table AND (@schema IS NULL OR tc.TABLE_SCHEMA = @schema) AND (@database IS NULL OR tc.TABLE_CATALOG = @database) ORDER BY tc.TABLE_SCHEMA, kcu.ORDINAL_POSITION",
                connection);
            CommandParameters.AddTableName(command, tableName);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                columns.Add(reader.GetString(0));
            }

            return columns;
        }
    }
}

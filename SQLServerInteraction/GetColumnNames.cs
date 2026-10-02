using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the column names for a specified table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table, passed to SqlClient's schema lookup as restrictions. A bare name (<c>Sales</c>) matches in any schema; <c>dbo.Sales</c> or <c>[dbo].[Sales]</c> matches that schema only.</param>
        /// <returns>A list of column names for every table or view with that name.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public List<string> GetColumnNames(string tableName)
        {
            var (database, schema, table) = SqlIdentifier.ParseTable(tableName);

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var columns = new List<string>();
            var dt = connection.GetSchema("Columns", [database, schema, table]);

            foreach (DataRow row in dt.Rows)
            {
                string columnName = row["COLUMN_NAME"].ToString() ?? "";
                if (!string.IsNullOrEmpty(columnName)) columns.Add(columnName);
            }

            return columns;
        }
    }
}

using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the column names for a specified table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table for which to retrieve column names.</param>
        /// <returns>A list of column names for the specified table.</returns>
        public List<string> GetColumnNames(string tableName)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var columns = new List<string>();
            var dt = connection.GetSchema("Columns", new[] { null, null, tableName });

            foreach (DataRow row in dt.Rows)
            {
                string columnName = row["COLUMN_NAME"].ToString() ?? "";
                if (!string.IsNullOrEmpty(columnName)) columns.Add(columnName);
            }

            return columns;
        }
    }
}

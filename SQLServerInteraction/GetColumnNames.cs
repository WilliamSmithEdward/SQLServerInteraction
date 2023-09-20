using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
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

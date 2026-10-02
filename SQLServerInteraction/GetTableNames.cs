using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the names of tables and views in the SQL Server database.
        /// </summary>
        /// <returns>A list of table and view names in the database, without schema names.</returns>
        public List<string> GetTableNames()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var tables = new List<string>();
            var dt = connection.GetSchema("Tables");

            foreach (DataRow row in dt.Rows)
            {
                string tableName = row["TABLE_NAME"].ToString() ?? "";
                if (!string.IsNullOrEmpty(tableName)) tables.Add(tableName);
            }

            return tables;
        }
    }
}

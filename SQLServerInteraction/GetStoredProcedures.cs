using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the names of stored procedures in the SQL Server database.
        /// </summary>
        /// <returns>A list of stored procedure names in the database.</returns>
        public List<string> GetStoredProcedures()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var storedProcedures = new List<string>();
            var dt = connection.GetSchema("Procedures");

            foreach (DataRow row in dt.Rows)
            {
                string procedureName = row["ROUTINE_NAME"].ToString() ?? "";
                if (!string.IsNullOrEmpty(procedureName)) storedProcedures.Add(procedureName);
            }

            return storedProcedures;
        }
    }
}

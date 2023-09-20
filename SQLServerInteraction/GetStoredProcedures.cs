using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
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

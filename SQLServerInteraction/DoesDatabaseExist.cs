using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public bool DoesDatabaseExist(string databaseName)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var databases = connection.GetSchema("Databases");
            var databaseNames = databases.Rows.Cast<DataRow>().Select(row => row["database_name"].ToString());

            return databaseNames.Contains(databaseName, StringComparer.OrdinalIgnoreCase);
        }
    }
}

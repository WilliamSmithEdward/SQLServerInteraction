using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Checks whether a database with the specified name exists on the SQL Server instance.
        /// </summary>
        /// <param name="databaseName">The name of the database to check.</param>
        /// <returns>True if the database exists; otherwise, false.</returns>
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

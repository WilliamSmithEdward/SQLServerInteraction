using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves information about the current SQL Server database.
        /// </summary>
        /// <returns>A dictionary containing database information: DatabaseName, DatabaseId, CreationDate, and Collation.</returns>
        public Dictionary<string, string> GetDatabaseInformation()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var databaseInfo = new Dictionary<string, string>();

            // Retrieve database properties
            using var command = new SqlCommand("SELECT name, database_id, create_date, collation_name FROM sys.databases WHERE name = DB_NAME()", connection);
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                databaseInfo["DatabaseName"] = reader["name"]?.ToString() ?? "";
                databaseInfo["DatabaseId"] = reader["database_id"]?.ToString() ?? "";
                databaseInfo["CreationDate"] = reader["create_date"]?.ToString() ?? "";
                databaseInfo["Collation"] = reader["collation_name"]?.ToString() ?? "";
            }

            return databaseInfo;
        }
    }
}

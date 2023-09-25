using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Deletes records from a SQL Server table based on an optional condition.
        /// If no condition is provided, it defaults to deleting all records.
        /// </summary>
        /// <param name="sqlServerTableName">The name of the SQL Server table from which records will be deleted.</param>
        /// <param name="condition">An optional condition used to filter which records to delete. Defaults to "1 = 1," deleting all records if not provided.</param>
        public void DeleteData(string sqlServerTableName, string condition = "")
        {
            if (string.IsNullOrEmpty(condition)) condition = "1 = 1";

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string sql = $"DELETE FROM {sqlServerTableName} WHERE {condition}";

            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}

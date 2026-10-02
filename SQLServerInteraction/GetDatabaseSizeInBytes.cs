using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the total size of the current database's data and log files.
        /// </summary>
        /// <returns>The size in bytes: the sum of sys.master_files.size, which counts 8 KB pages, times 8192.</returns>
        public long GetDatabaseSizeInBytes()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand("SELECT SUM(CAST(size AS bigint)) * 8192 FROM sys.master_files WHERE database_id = DB_ID()", connection);

            return Convert.ToInt64(command.ExecuteScalar());
        }
    }
}

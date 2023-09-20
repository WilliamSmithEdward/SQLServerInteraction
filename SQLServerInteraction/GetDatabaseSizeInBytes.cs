using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public long GetDatabaseSizeInBytes()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand("SELECT SUM(size) * 8 FROM sys.master_files WHERE database_id = DB_ID()", connection);

            return Convert.ToInt64(command.ExecuteScalar());
        }
    }
}

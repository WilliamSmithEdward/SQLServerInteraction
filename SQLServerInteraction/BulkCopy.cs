using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public void BulkCopy(DataTable dt, string sqlServerTableName, bool flushTableBeforeCopy)
        {
            using var connection = new SqlConnection(_connectionString);
            using var bulkCopy = new SqlBulkCopy(connection);

            connection.Open();

            if (flushTableBeforeCopy) using (var command = new SqlCommand("DELETE FROM " + sqlServerTableName, connection)) command.ExecuteNonQuery();

            bulkCopy.DestinationTableName = sqlServerTableName;
            bulkCopy.WriteToServer(dt);
        }
    }
}

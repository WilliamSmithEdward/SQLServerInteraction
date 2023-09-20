using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public async Task BulkCopyAsync(DataTable dt, string sqlServerTableName, bool flushTableBeforeCopy)
        {
            using var connection = new SqlConnection(_connectionString);
            using var bulkCopy = new SqlBulkCopy(connection);

            await connection.OpenAsync();

            if (flushTableBeforeCopy)
            {
                using (var command = new SqlCommand("DELETE FROM " + sqlServerTableName, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }

            bulkCopy.DestinationTableName = sqlServerTableName;
            await bulkCopy.WriteToServerAsync(dt);
        }
    }
}

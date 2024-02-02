using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously performs a bulk copy operation to insert data from a DataTable into a SQL Server table.
        /// </summary>
        /// <param name="dataTable">The DataTable containing the data to be copied.</param>
        /// <param name="destinationTableName">The name of the destination SQL Server table.</param>
        /// <param name="flushTable">A flag indicating whether to delete all rows in the destination table before copying data. Defaults to false.</param>
        /// <param name="bulkCopyTimeout">Connection time-out value in seconds. Defaults to 30.</param>
        /// <param name="batchSize">Instructs the bulk copy operation to split the data into chunks when transferring. Defaults to no batching.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task BulkCopyAsync(DataTable dataTable, string destinationTableName, bool flushTable = false, int bulkCopyTimeout = 30, int? batchSize = null)
        {
            using var connection = new SqlConnection(_connectionString);
            using var bulkCopy = new SqlBulkCopy(connection);

            bulkCopy.BulkCopyTimeout = bulkCopyTimeout;
            if (batchSize.HasValue) bulkCopy.BatchSize = batchSize.Value;

            await connection.OpenAsync();

            if (flushTable) using (var command = new SqlCommand("DELETE FROM " + destinationTableName, connection)) await command.ExecuteNonQueryAsync();

            bulkCopy.DestinationTableName = destinationTableName;
            await bulkCopy.WriteToServerAsync(dataTable);
        }
    }
}

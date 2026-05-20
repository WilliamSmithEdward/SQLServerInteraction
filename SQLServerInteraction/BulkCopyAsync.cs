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
        /// <param name="useTransaction">A flag indicating whether to wrap the operation in a transaction to prevent readers from seeing partial data. Defaults to true.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task BulkCopyAsync(DataTable dataTable, string destinationTableName, bool flushTable = false, int bulkCopyTimeout = 30, int? batchSize = null, bool useTransaction = true)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            SqlTransaction? transaction = useTransaction ? connection.BeginTransaction() : null;

            try
            {
                using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);

                bulkCopy.BulkCopyTimeout = bulkCopyTimeout;
                if (batchSize.HasValue) bulkCopy.BatchSize = batchSize.Value;

                if (flushTable) using (var command = new SqlCommand("DELETE FROM " + destinationTableName, connection, transaction)) await command.ExecuteNonQueryAsync();

                bulkCopy.DestinationTableName = destinationTableName;
                await bulkCopy.WriteToServerAsync(dataTable);

                transaction?.Commit();
            }

            catch
            {
                transaction?.Rollback();
                throw;
            }

            finally
            {
                transaction?.Dispose();
            }
        }
    }
}

using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Performs a bulk copy operation to insert data from a DataTable into a SQL Server table.
        /// </summary>
        /// <param name="dataTable">The DataTable containing the data to be copied.</param>
        /// <param name="destinationTableName">The name of the destination SQL Server table. With <paramref name="flushTable"/>, it is inserted into the SQL as written, without quoting or escaping. Columns map by position.</param>
        /// <param name="flushTable">A flag indicating whether to delete rows in the destination table before copying data: all of them, or those matching <paramref name="flushWhereClauseCondition"/>. Defaults to false.</param>
        /// <param name="bulkCopyTimeout">Seconds the bulk copy operation may take before it times out. Defaults to 30.</param>
        /// <param name="batchSize">Instructs the bulk copy operation to split the data into chunks when transferring. Defaults to no batching.</param>
        /// <param name="useTransaction">A flag indicating whether to wrap the operation in a transaction to prevent readers from seeing partial data. Defaults to true.</param>
        /// <param name="flushWhereClauseCondition">An optional WHERE clause condition (without the WHERE keyword) to limit which rows are deleted when flushTable is true. If omitted, all rows are deleted. It is inserted into the SQL as written, without quoting or escaping.</param>
        public void BulkCopy(DataTable dataTable, string destinationTableName, bool flushTable = false, int bulkCopyTimeout = 30, int? batchSize = null, bool useTransaction = true, string? flushWhereClauseCondition = null)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            SqlTransaction? transaction = useTransaction ? connection.BeginTransaction() : null;

            try
            {
                using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);

                bulkCopy.BulkCopyTimeout = bulkCopyTimeout;
                if (batchSize.HasValue) bulkCopy.BatchSize = batchSize.Value;

                if (flushTable)
                {
                    var deleteSQL = "DELETE FROM " + destinationTableName + (string.IsNullOrWhiteSpace(flushWhereClauseCondition) ? "" : " WHERE " + flushWhereClauseCondition);
                    using var command = new SqlCommand(deleteSQL, connection, transaction);
                    command.ExecuteNonQuery();
                }

                bulkCopy.DestinationTableName = destinationTableName;
                bulkCopy.WriteToServer(dataTable);

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

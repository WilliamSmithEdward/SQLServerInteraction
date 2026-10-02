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
        /// <param name="destinationTableName">The name of the destination SQL Server table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier. Columns map by position.</param>
        /// <param name="flushTable">A flag indicating whether to delete rows in the destination table before copying data: all of them, or those matching <paramref name="flushWhereClauseCondition"/>. Defaults to false.</param>
        /// <param name="bulkCopyTimeout">Seconds the bulk copy operation may take before it times out. Defaults to 30.</param>
        /// <param name="batchSize">Instructs the bulk copy operation to split the data into chunks when transferring. Defaults to no batching.</param>
        /// <param name="useTransaction">A flag indicating whether to wrap the operation in a transaction to prevent readers from seeing partial data. Defaults to true.</param>
        /// <param name="flushWhereClauseCondition">An optional WHERE clause condition (without the WHERE keyword) to limit which rows are deleted when flushTable is true. If null, all rows are deleted. It is SQL text, inserted as written: put values in <paramref name="flushParameters"/> rather than in the text.</param>
        /// <param name="flushParameters">Optional parameters for <paramref name="flushWhereClauseCondition"/>. Names work with or without the @, and null is sent as NULL.</param>
        /// <exception cref="ArgumentException">The table name is not a valid name, or <paramref name="flushWhereClauseCondition"/> is empty or whitespace.</exception>
        public void BulkCopy(DataTable dataTable, string destinationTableName, bool flushTable = false, int bulkCopyTimeout = 30, int? batchSize = null, bool useTransaction = true, string? flushWhereClauseCondition = null, Dictionary<string, object>? flushParameters = null)
        {
            string table = SqlIdentifier.Quote(destinationTableName);
            string deleteSQL = "DELETE FROM " + table + (flushWhereClauseCondition == null ? "" : " WHERE " + RequireCondition(flushWhereClauseCondition, nameof(flushWhereClauseCondition)));

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
                    using var command = new SqlCommand(deleteSQL, connection, transaction);
                    CommandParameters.Add(command, flushParameters);
                    command.ExecuteNonQuery();
                }

                bulkCopy.DestinationTableName = table;
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

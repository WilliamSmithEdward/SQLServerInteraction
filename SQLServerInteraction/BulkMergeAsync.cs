using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously bulk copies a DataTable into a temporary table and merges it into a SQL Server table, in one transaction: a destination row whose key columns match a DataTable row is updated, a DataTable row with no match is inserted, and, when asked, a destination row with no match is deleted.
        /// </summary>
        /// <param name="dataTable">The rows to merge. Each column is matched to the destination column of the same name, so the columns can be in any order; every column must exist in the destination table. A column the DataTable does not have is left alone on an update and takes its default on an insert. Leave out an identity column: the merge inserts every column it carries, and SQL Server refuses to insert into an identity column, even when no row is new.</param>
        /// <param name="destinationTableName">The name of the destination SQL Server table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="keyColumns">The DataTable columns that identify a row, plain or bracketed (<c>My Key</c> or <c>[My Key]</c>). A destination row matches a DataTable row when every key column is equal; a NULL key never matches, so a row with one is inserted on every merge. Two DataTable rows with the same key fail the merge. The other columns are the values: set on a matched row, and inserted with the keys on a new one.</param>
        /// <param name="deleteUnmatched">Whether to delete every destination row that matches no DataTable row. Defaults to false.</param>
        /// <param name="timeout">Seconds the copy may take, and then seconds the merge may take, before it fails. Defaults to 30.</param>
        /// <param name="batchSize">Instructs the bulk copy operation to split the data into chunks when transferring. Defaults to no batching.</param>
        /// <param name="useTransaction">A flag indicating whether to run the copy and the merge in one transaction, rolled back if either fails. Defaults to true. Without it, the merge statement is still atomic, but its locks are released as soon as it finishes.</param>
        /// <returns>A task whose result is the number of destination rows inserted, updated and deleted.</returns>
        /// <exception cref="ArgumentException">The table name or a key column is not a valid name, the DataTable has no columns, there are no key columns, or a key column is not a column of the DataTable. Each is checked before anything runs.</exception>
        public async Task<int> BulkMergeAsync(DataTable dataTable, string destinationTableName, IEnumerable<string> keyColumns, bool deleteUnmatched = false, int timeout = 30, int? batchSize = null, bool useTransaction = true)
        {
            using var work = useTransaction ? await BeginTransactionAsync() : await ConnectAsync();
            int rows = await work.BulkMergeAsync(dataTable, destinationTableName, keyColumns, deleteUnmatched, timeout, batchSize);
            if (useTransaction) await work.CommitAsync();
            return rows;
        }
    }

    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Asynchronously bulk copies a DataTable into a temporary table and merges it into a SQL Server table, in this transaction: a destination row whose key columns match a DataTable row is updated, a DataTable row with no match is inserted, and, when asked, a destination row with no match is deleted.
        /// </summary>
        /// <param name="dataTable">The rows to merge. Each column is matched to the destination column of the same name, so the columns can be in any order; every column must exist in the destination table. A column the DataTable does not have is left alone on an update and takes its default on an insert. Leave out an identity column: the merge inserts every column it carries, and SQL Server refuses to insert into an identity column, even when no row is new.</param>
        /// <param name="destinationTableName">The name of the destination SQL Server table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="keyColumns">The DataTable columns that identify a row, plain or bracketed (<c>My Key</c> or <c>[My Key]</c>). A destination row matches a DataTable row when every key column is equal; a NULL key never matches, so a row with one is inserted on every merge. Two DataTable rows with the same key fail the merge. The other columns are the values: set on a matched row, and inserted with the keys on a new one.</param>
        /// <param name="deleteUnmatched">Whether to delete every destination row that matches no DataTable row. Defaults to false.</param>
        /// <param name="timeout">Seconds the copy may take, and then seconds the merge may take, before it fails. Defaults to 30.</param>
        /// <param name="batchSize">Instructs the bulk copy operation to split the data into chunks when transferring. Defaults to no batching.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the number of destination rows inserted, updated and deleted.</returns>
        /// <exception cref="ArgumentException">The table name or a key column is not a valid name, the DataTable has no columns, there are no key columns, or a key column is not a column of the DataTable. Each is checked before anything runs.</exception>
        public async Task<int> BulkMergeAsync(DataTable dataTable, string destinationTableName, IEnumerable<string> keyColumns, bool deleteUnmatched = false, int timeout = 30, int? batchSize = null, CancellationToken cancellationToken = default)
        {
            var (stagingSql, mergeSql) = SQLServerInstance.BulkMergeSql(dataTable, destinationTableName, keyColumns, deleteUnmatched);

            using (var staging = new SqlCommand(stagingSql, _connection, Transaction))
            {
                await staging.ExecuteNonQueryAsync(cancellationToken);
            }

            using (var bulkCopy = new SqlBulkCopy(_connection, SqlBulkCopyOptions.Default, Transaction))
            {
                bulkCopy.BulkCopyTimeout = timeout;
                if (batchSize.HasValue) bulkCopy.BatchSize = batchSize.Value;
                bulkCopy.DestinationTableName = SQLServerInstance.BulkMergeStagingTable;
                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
            }

            using var merge = new SqlCommand(mergeSql, _connection, Transaction);
            merge.CommandTimeout = timeout;
            return await merge.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}

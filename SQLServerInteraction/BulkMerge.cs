using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Bulk copies a DataTable into a temporary table and merges it into a SQL Server table, in one transaction: a destination row whose key columns match a DataTable row is updated, a DataTable row with no match is inserted, and, when asked, a destination row with no match is deleted.
        /// </summary>
        /// <param name="dataTable">The rows to merge. Each column is matched to the destination column of the same name, so the columns can be in any order; every column must exist in the destination table. A column the DataTable does not have is left alone on an update and takes its default on an insert. Leave out an identity column: the merge inserts every column it carries, and SQL Server refuses to insert into an identity column, even when no row is new.</param>
        /// <param name="destinationTableName">The name of the destination SQL Server table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="keyColumns">The DataTable columns that identify a row, plain or bracketed (<c>My Key</c> or <c>[My Key]</c>). A destination row matches a DataTable row when every key column is equal; a NULL key never matches, so a row with one is inserted on every merge. Two DataTable rows with the same key fail the merge. The other columns are the values: set on a matched row, and inserted with the keys on a new one.</param>
        /// <param name="deleteUnmatched">Whether to delete every destination row that matches no DataTable row. Defaults to false.</param>
        /// <param name="timeout">Seconds the copy may take, and then seconds the merge may take, before it fails. Defaults to 30.</param>
        /// <param name="batchSize">Instructs the bulk copy operation to split the data into chunks when transferring. Defaults to no batching.</param>
        /// <param name="useTransaction">A flag indicating whether to run the copy and the merge in one transaction, rolled back if either fails. Defaults to true. Without it, the merge statement is still atomic, but its locks are released as soon as it finishes.</param>
        /// <returns>The number of destination rows inserted, updated and deleted.</returns>
        /// <exception cref="ArgumentException">The table name or a key column is not a valid name, the DataTable has no columns, there are no key columns, or a key column is not a column of the DataTable. Each is checked before a connection is opened.</exception>
        public int BulkMerge(DataTable dataTable, string destinationTableName, IEnumerable<string> keyColumns, bool deleteUnmatched = false, int timeout = 30, int? batchSize = null, bool useTransaction = true)
        {
            var (stagingSql, mergeSql) = BulkMergeSql(dataTable, destinationTableName, keyColumns, deleteUnmatched);

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            SqlTransaction? transaction = useTransaction ? connection.BeginTransaction() : null;

            try
            {
                using (var staging = new SqlCommand(stagingSql, connection, transaction))
                {
                    staging.ExecuteNonQuery();
                }

                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.BulkCopyTimeout = timeout;
                    if (batchSize.HasValue) bulkCopy.BatchSize = batchSize.Value;
                    bulkCopy.DestinationTableName = BulkMergeStagingTable;
                    bulkCopy.WriteToServer(dataTable);
                }

                using var merge = new SqlCommand(mergeSql, connection, transaction);
                merge.CommandTimeout = timeout;
                int rows = merge.ExecuteNonQuery();

                transaction?.Commit();
                return rows;
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

        /// <summary>
        /// The temporary table the DataTable is copied into, which lives only in the
        /// connection that creates it and goes when the connection closes.
        /// </summary>
        private const string BulkMergeStagingTable = "[#SQLServerInteraction_BulkMerge]";

        /// <summary>
        /// The two statements of a bulk merge, checked before anything runs: the
        /// <c>SELECT TOP (0) ... INTO</c> that gives the staging table the destination's
        /// column types, and the MERGE from it.
        /// </summary>
        /// <exception cref="ArgumentException">The table name or a key column is not a valid name, the DataTable has no columns, there are no key columns, or a key column is not a column of the DataTable.</exception>
        internal static (string Staging, string Merge) BulkMergeSql(DataTable dataTable, string destinationTableName, IEnumerable<string> keyColumns, bool deleteUnmatched)
        {
            string target = SqlIdentifier.Quote(destinationTableName);
            var columns = dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToList();
            if (columns.Count == 0)
                throw new ArgumentException("The DataTable has no columns.", nameof(dataTable));

            var keys = new List<string>();
            foreach (string keyColumn in keyColumns)
            {
                string name = SqlIdentifier.Parse(keyColumn, maxParts: 1)[0];
                if (!dataTable.Columns.Contains(name))
                    throw new ArgumentException($"The DataTable has no column named {name}.", nameof(keyColumns));
                keys.Add(dataTable.Columns[name]!.ColumnName);
            }
            if (keys.Count == 0)
                throw new ArgumentException("At least one key column is required.", nameof(keyColumns));

            var values = columns.Where(column => !keys.Contains(column, StringComparer.Ordinal)).ToList();

            string staging = $"SELECT TOP (0) {string.Join(", ", columns.Select(SqlIdentifier.QuotePart))} INTO {BulkMergeStagingTable} FROM {target}";
            string merge = MergeSql(BulkMergeStagingTable, target, keys.Select(SqlIdentifier.QuotePart).ToList(), values.Select(SqlIdentifier.QuotePart).ToList(), deleteUnmatched);
            return (staging, merge);
        }
    }
}

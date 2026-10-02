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
        /// <param name="flushWhereClauseCondition">An optional WHERE clause condition (without the WHERE keyword) to limit which rows are deleted when flushTable is true. If null, all rows are deleted. It is SQL text, inserted as written: to send values as parameters, use the overload that takes them.</param>
        /// <exception cref="ArgumentException">The table name is not a valid name, or <paramref name="flushWhereClauseCondition"/> is empty or whitespace.</exception>
        public void BulkCopy(DataTable dataTable, string destinationTableName, bool flushTable = false, int bulkCopyTimeout = 30, int? batchSize = null, bool useTransaction = true, string? flushWhereClauseCondition = null)
        {
            BulkCopy(dataTable, destinationTableName, flushTable, flushWhereClauseCondition, null, bulkCopyTimeout, batchSize, useTransaction);
        }

        /// <summary>
        /// Performs a bulk copy operation to insert data from a DataTable into a SQL Server table, with parameters for the flush condition.
        /// </summary>
        /// <param name="dataTable">The DataTable containing the data to be copied.</param>
        /// <param name="destinationTableName">The name of the destination SQL Server table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier. Columns map by position.</param>
        /// <param name="flushTable">A flag indicating whether to delete rows in the destination table before copying data: all of them, or those matching <paramref name="flushWhereClauseCondition"/>.</param>
        /// <param name="bulkCopyTimeout">Seconds the bulk copy operation may take before it times out. Defaults to 30.</param>
        /// <param name="batchSize">Instructs the bulk copy operation to split the data into chunks when transferring. Defaults to no batching.</param>
        /// <param name="useTransaction">A flag indicating whether to wrap the operation in a transaction to prevent readers from seeing partial data. Defaults to true.</param>
        /// <param name="flushWhereClauseCondition">An optional WHERE clause condition (without the WHERE keyword) to limit which rows are deleted when flushTable is true. If null, all rows are deleted. It is SQL text, inserted as written: put values in <paramref name="flushParameters"/> rather than in the text.</param>
        /// <param name="flushParameters">Parameters for <paramref name="flushWhereClauseCondition"/>, or null for none. Names work with or without the @, and null is sent as NULL.</param>
        /// <param name="columnMappings">Optional column mappings: each key is a column of <paramref name="dataTable"/> and its value the destination column it is copied to. Only mapped columns are copied. A destination is a column name, not SQL: SqlBulkCopy matches it to a destination column name exactly, case included, after removing brackets from a bracketed name ([Sales Region]); a name with a space needs none. A source column matches without regard to case. A destination the table does not have fails when the copy runs, after any flush; with useTransaction, the flush is rolled back. Null (the default) maps the columns by position.</param>
        /// <exception cref="ArgumentException">The table name is not a valid name, <paramref name="flushWhereClauseCondition"/> is empty or whitespace, or <paramref name="columnMappings"/> is empty, names a column <paramref name="dataTable"/> does not have, or has an empty destination. Each is checked before a connection is opened, so nothing is deleted.</exception>
        public void BulkCopy(DataTable dataTable, string destinationTableName, bool flushTable, string? flushWhereClauseCondition, Dictionary<string, object>? flushParameters, int bulkCopyTimeout = 30, int? batchSize = null, bool useTransaction = true, IReadOnlyDictionary<string, string>? columnMappings = null)
        {
            string table = SqlIdentifier.Quote(destinationTableName);
            var mappings = BulkCopyColumnMappings(dataTable, columnMappings);
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
                mappings?.ForEach(mapping => bulkCopy.ColumnMappings.Add(mapping));
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

        /// <summary>
        /// The caller's column mappings as SqlBulkCopy mappings, checked before anything runs: null means none.
        /// </summary>
        /// <exception cref="ArgumentException">The mappings are empty, name a column the DataTable does not have, or have an empty destination.</exception>
        private static List<SqlBulkCopyColumnMapping>? BulkCopyColumnMappings(DataTable dataTable, IReadOnlyDictionary<string, string>? columnMappings)
        {
            if (columnMappings == null) return null;
            if (columnMappings.Count == 0)
                throw new ArgumentException("Column mappings, when given, need at least one column. Pass null to map the columns by position.", nameof(columnMappings));

            var mappings = new List<SqlBulkCopyColumnMapping>();
            foreach (var mapping in columnMappings)
            {
                if (!dataTable.Columns.Contains(mapping.Key))
                    throw new ArgumentException($"The DataTable has no column named {mapping.Key}.", nameof(columnMappings));
                if (string.IsNullOrWhiteSpace(mapping.Value))
                    throw new ArgumentException($"The column {mapping.Key} is mapped to an empty destination column name.", nameof(columnMappings));

                mappings.Add(new SqlBulkCopyColumnMapping(dataTable.Columns[mapping.Key]!.ColumnName, mapping.Value));
            }
            return mappings;
        }
    }
}

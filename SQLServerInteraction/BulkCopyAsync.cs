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
        /// <param name="destinationTableName">The name of the destination SQL Server table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier. Columns map by position.</param>
        /// <param name="flushTable">A flag indicating whether to delete rows in the destination table before copying data: all of them, or those matching <paramref name="flushWhereClauseCondition"/>. Defaults to false.</param>
        /// <param name="bulkCopyTimeout">Seconds the bulk copy operation may take before it times out. Defaults to 30.</param>
        /// <param name="batchSize">Instructs the bulk copy operation to split the data into chunks when transferring. Defaults to no batching.</param>
        /// <param name="useTransaction">A flag indicating whether to wrap the operation in a transaction to prevent readers from seeing partial data. Defaults to true.</param>
        /// <param name="flushWhereClauseCondition">An optional WHERE clause condition (without the WHERE keyword) to limit which rows are deleted when flushTable is true. If null, all rows are deleted. It is SQL text, inserted as written: to send values as parameters, use the overload that takes them.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The table name is not a valid name, or <paramref name="flushWhereClauseCondition"/> is empty or whitespace.</exception>
        public Task BulkCopyAsync(DataTable dataTable, string destinationTableName, bool flushTable = false, int bulkCopyTimeout = 30, int? batchSize = null, bool useTransaction = true, string? flushWhereClauseCondition = null)
        {
            return BulkCopyAsync(dataTable, destinationTableName, flushTable, flushWhereClauseCondition, null, bulkCopyTimeout, batchSize, useTransaction);
        }

        /// <summary>
        /// Asynchronously performs a bulk copy operation to insert data from a DataTable into a SQL Server table, with parameters for the flush condition.
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
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The table name is not a valid name, <paramref name="flushWhereClauseCondition"/> is empty or whitespace, or <paramref name="columnMappings"/> is empty, names a column <paramref name="dataTable"/> does not have, or has an empty destination. Each is checked before anything runs, so nothing is deleted.</exception>
        public async Task BulkCopyAsync(DataTable dataTable, string destinationTableName, bool flushTable, string? flushWhereClauseCondition, Dictionary<string, object>? flushParameters, int bulkCopyTimeout = 30, int? batchSize = null, bool useTransaction = true, IReadOnlyDictionary<string, string>? columnMappings = null)
        {
            using var work = useTransaction ? await BeginTransactionAsync() : await ConnectAsync();
            await work.BulkCopyAsync(dataTable, destinationTableName, flushTable, flushWhereClauseCondition, flushParameters, bulkCopyTimeout, batchSize, columnMappings);
            if (useTransaction) await work.CommitAsync();
        }
    }

    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Asynchronously performs a bulk copy operation to insert data from a DataTable into a SQL Server table, in this transaction, with an optional flush of the destination first.
        /// </summary>
        /// <inheritdoc cref="SQLServerInstance.BulkCopyAsync(DataTable, string, bool, string?, Dictionary{string, object}?, int, int?, bool, IReadOnlyDictionary{string, string}?)" path="/param[@name!='useTransaction']|/returns|/exception"/>
        public async Task BulkCopyAsync(DataTable dataTable, string destinationTableName, bool flushTable = false, string? flushWhereClauseCondition = null, Dictionary<string, object>? flushParameters = null, int bulkCopyTimeout = 30, int? batchSize = null, IReadOnlyDictionary<string, string>? columnMappings = null, CancellationToken cancellationToken = default)
        {
            string table = SqlIdentifier.Quote(destinationTableName);
            var mappings = SQLServerInstance.BulkCopyColumnMappings(dataTable, columnMappings);
            string deleteSQL = SQLServerInstance.FlushSql(destinationTableName, flushWhereClauseCondition);

            if (flushTable)
            {
                using var command = new SqlCommand(deleteSQL, _connection, Transaction);
                CommandParameters.Add(command, flushParameters);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            using var bulkCopy = new SqlBulkCopy(_connection, SqlBulkCopyOptions.Default, Transaction);
            bulkCopy.BulkCopyTimeout = bulkCopyTimeout;
            if (batchSize.HasValue) bulkCopy.BatchSize = batchSize.Value;
            bulkCopy.DestinationTableName = table;
            mappings?.ForEach(mapping => bulkCopy.ColumnMappings.Add(mapping));
            await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
        }
    }
}

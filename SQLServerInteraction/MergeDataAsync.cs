using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously merges the rows of one SQL Server table into another with a MERGE statement: a target row whose key columns match a source row is updated, a source row with no match is inserted, and, when asked, a target row with no match is deleted.
        /// </summary>
        /// <param name="sourceTableName">The name of the table or view the rows come from, such as <c>Staging</c>, <c>dbo.Staging</c> or <c>[dbo].[My Staging]</c>. It is quoted as an identifier.</param>
        /// <param name="targetTableName">The name of the table the rows go into, in the same forms. It is quoted as an identifier.</param>
        /// <param name="keyColumns">The columns that identify a row in both tables. Each is one column name, plain or bracketed (<c>My Key</c> or <c>[My Key]</c>), and is quoted as an identifier. A target row matches a source row when every key column is equal; a NULL key never matches, so a source row with one is inserted on every merge.</param>
        /// <param name="valueColumns">The other columns to carry over, named the same way: set on a matched row, and inserted with the keys on a new one. Empty leaves matched rows as they are and inserts the keys alone. Leave out an identity column: the merge inserts every column it carries, and SQL Server refuses to insert into an identity column, even when no row is new.</param>
        /// <param name="deleteUnmatched">Whether to delete every target row that matches no source row. Defaults to false.</param>
        /// <param name="useTransaction">A flag indicating whether to run the merge in a transaction. Defaults to true. The MERGE statement is atomic either way; the transaction holds its locks until it commits.</param>
        /// <returns>A task whose result is the number of rows inserted, updated and deleted.</returns>
        /// <exception cref="ArgumentException">A table or column name is not a valid name, there are no key columns, or a column is named in both lists.</exception>
        public async Task<int> MergeDataAsync(string sourceTableName, string targetTableName, IEnumerable<string> keyColumns, IEnumerable<string> valueColumns, bool deleteUnmatched = false, bool useTransaction = true)
        {
            using var work = useTransaction ? await BeginTransactionAsync() : await ConnectAsync();
            int rows = await work.MergeDataAsync(sourceTableName, targetTableName, keyColumns, valueColumns, deleteUnmatched);
            if (useTransaction) await work.CommitAsync();
            return rows;
        }
    }

    public partial class SQLServerTransaction
    {
        /// <summary>
        /// Asynchronously merges the rows of one SQL Server table into another with a MERGE statement, in this transaction: a target row whose key columns match a source row is updated, a source row with no match is inserted, and, when asked, a target row with no match is deleted.
        /// </summary>
        /// <inheritdoc cref="SQLServerInstance.MergeDataAsync(string, string, IEnumerable{string}, IEnumerable{string}, bool, bool)" path="/param[@name!='useTransaction']|/returns|/exception"/>
        public async Task<int> MergeDataAsync(string sourceTableName, string targetTableName, IEnumerable<string> keyColumns, IEnumerable<string> valueColumns, bool deleteUnmatched = false, CancellationToken cancellationToken = default)
        {
            string sql = SQLServerInstance.MergeSql(sourceTableName, targetTableName, keyColumns, valueColumns, deleteUnmatched);

            using var command = new SqlCommand(sql, _connection, Transaction);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}

using Microsoft.Data.SqlClient;
using System.Text;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Merges the rows of one SQL Server table into another with a MERGE statement: a target row whose key columns match a source row is updated, a source row with no match is inserted, and, when asked, a target row with no match is deleted.
        /// </summary>
        /// <param name="sourceTableName">The name of the table or view the rows come from, such as <c>Staging</c>, <c>dbo.Staging</c> or <c>[dbo].[My Staging]</c>. It is quoted as an identifier.</param>
        /// <param name="targetTableName">The name of the table the rows go into, in the same forms. It is quoted as an identifier.</param>
        /// <param name="keyColumns">The columns that identify a row in both tables. Each is one column name, plain or bracketed (<c>My Key</c> or <c>[My Key]</c>), and is quoted as an identifier. A target row matches a source row when every key column is equal; a NULL key never matches, so a source row with one is inserted on every merge.</param>
        /// <param name="valueColumns">The other columns to carry over, named the same way: set on a matched row, and inserted with the keys on a new one. Empty leaves matched rows as they are and inserts the keys alone. Leave out an identity column: the merge inserts every column it carries, and SQL Server refuses to insert into an identity column, even when no row is new.</param>
        /// <param name="deleteUnmatched">Whether to delete every target row that matches no source row. Defaults to false.</param>
        /// <param name="useTransaction">A flag indicating whether to run the merge in a transaction. Defaults to true. The MERGE statement is atomic either way; the transaction holds its locks until it commits.</param>
        /// <returns>The number of rows inserted, updated and deleted.</returns>
        /// <exception cref="ArgumentException">A table or column name is not a valid name, there are no key columns, or a column is named in both lists.</exception>
        public int MergeData(string sourceTableName, string targetTableName, IEnumerable<string> keyColumns, IEnumerable<string> valueColumns, bool deleteUnmatched = false, bool useTransaction = true)
        {
            string sql = MergeSql(sourceTableName, targetTableName, keyColumns, valueColumns, deleteUnmatched);

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            SqlTransaction? transaction = useTransaction ? connection.BeginTransaction() : null;

            try
            {
                using var command = new SqlCommand(sql, connection, transaction);
                int rows = command.ExecuteNonQuery();

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
        /// The MERGE statement for the caller's names, each quoted as an identifier.
        /// </summary>
        /// <exception cref="ArgumentException">A name is not a valid name, there are no key columns, or a column is named in both lists.</exception>
        private static string MergeSql(string sourceTableName, string targetTableName, IEnumerable<string> keyColumns, IEnumerable<string> valueColumns, bool deleteUnmatched)
        {
            var keys = QuoteColumns(keyColumns, nameof(keyColumns));
            var values = valueColumns.Select(name => SqlIdentifier.Quote(name, maxParts: 1)).ToList();

            string? both = values.Find(value => keys.Contains(value, StringComparer.OrdinalIgnoreCase));
            if (both != null)
                throw new ArgumentException($"The column {both} is a key column and a value column; name it once.", nameof(valueColumns));

            return MergeSql(SqlIdentifier.Quote(sourceTableName), SqlIdentifier.Quote(targetTableName), keys, values, deleteUnmatched);
        }

        /// <summary>
        /// <c>MERGE INTO target WITH (HOLDLOCK) AS T USING source AS S ON T.[key] = S.[key]
        /// WHEN MATCHED THEN UPDATE SET T.[value] = S.[value]
        /// WHEN NOT MATCHED BY TARGET THEN INSERT ([key], [value]) VALUES (S.[key], S.[value])
        /// WHEN NOT MATCHED BY SOURCE THEN DELETE;</c>
        /// with every name already quoted. The UPDATE clause is left out when there are no
        /// value columns, and the DELETE clause unless asked for. HOLDLOCK keeps a row another
        /// session inserts between the match and the insert from making a duplicate.
        /// </summary>
        internal static string MergeSql(string source, string target, IReadOnlyList<string> keys, IReadOnlyList<string> values, bool deleteUnmatched)
        {
            var columns = keys.Concat(values).ToList();
            var sql = new StringBuilder();

            sql.Append($"MERGE INTO {target} WITH (HOLDLOCK) AS T USING {source} AS S ON ");
            sql.Append(string.Join(" AND ", keys.Select(key => $"T.{key} = S.{key}")));

            if (values.Count > 0)
            {
                sql.Append(" WHEN MATCHED THEN UPDATE SET ");
                sql.Append(string.Join(", ", values.Select(value => $"T.{value} = S.{value}")));
            }

            sql.Append($" WHEN NOT MATCHED BY TARGET THEN INSERT ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(column => "S." + column))})");

            if (deleteUnmatched)
                sql.Append(" WHEN NOT MATCHED BY SOURCE THEN DELETE");

            return sql.Append(';').ToString();
        }
    }
}

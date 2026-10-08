using Microsoft.Data.SqlClient;
using System.Text;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Runs a query and writes its result set to a CSV file, created or overwritten, in UTF-8 without a byte order mark. The first line holds the column names, unquoted; each data value is written in double quotes with embedded quotes doubled, NULL as <c>""</c>, and formatted with the current culture.
        /// </summary>
        /// <param name="destinationFilePath">The path of the file to write.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task ExportDataToCSVAsync(string destinationFilePath, string sql, CancellationToken cancellationToken = default) =>
            WithConnectionAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                using var writer = new StreamWriter(destinationFilePath);

                await writer.WriteLineAsync(string.Join(",", Enumerable.Range(0, reader.FieldCount).Select(reader.GetName)));

                var record = new object[reader.FieldCount];
                while (await reader.ReadAsync(cancellationToken))
                {
                    reader.GetValues(record);
                    await writer.WriteLineAsync(string.Join(",", record.Select(field => "\"" + field?.ToString()?.Replace("\"", "\"\"") + "\"")));
                }

                return true;
            }, cancellationToken);
    }
}

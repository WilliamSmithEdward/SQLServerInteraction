using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Text.Json;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Asynchronously exports data from a SQL query to a CSV file.
        /// </summary>
        /// <param name="destinationFilePath">The file path where the CSV data will be saved.</param>
        /// <param name="sql">The SQL query to retrieve data for export.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExportDataToCSVAsync(string destinationFilePath, string sql)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var columnNames = GetColumnNames(reader);

            using var writer = new StreamWriter(destinationFilePath);

            await writer.WriteLineAsync(string.Join(",", columnNames));

            while (await reader.ReadAsync())
            {
                var record = new object[reader.FieldCount];

                reader.GetValues(record);

                var sb = new StringBuilder();

                foreach (var field in record) 
                { 
                    sb.Append("\"" + field?.ToString()?.Replace("\"", "\"\"") + "\",");
                }

                await writer.WriteLineAsync(sb.ToString().TrimEnd(','));
            }
        }

        private static string[] GetColumnNames(IDataReader reader)
        {
            var columnNames = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnNames[i] = reader.GetName(i);
            }
            return columnNames;
        }
    }
}

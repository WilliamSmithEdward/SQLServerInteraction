using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public async Task ExportDataToCSVAsync(string destinationFilePath, string sourceTableName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string sql = $"SELECT * FROM {sourceTableName}";

                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                var columnNames = GetColumnNames(reader);

                using var writer = new StreamWriter(destinationFilePath);

                await writer.WriteLineAsync(string.Join(",", columnNames));

                while (await reader.ReadAsync())
                {
                    var record = new object[reader.FieldCount];

                    reader.GetValues(record);

                    var json = JsonSerializer.Serialize(record);

                    await writer.WriteLineAsync(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error exporting data to CSV: " + ex.Message);
                throw;
            }
        }

        private string[] GetColumnNames(IDataReader reader)
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

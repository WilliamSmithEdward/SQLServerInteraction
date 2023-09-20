using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public async Task<DataTable> ExecuteQueryAsync(string sql)
        {
            var dataTable = new DataTable();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            using var adapter = new SqlDataAdapter(command);

            await Task.Run(() => adapter.Fill(dataTable));

            return dataTable;
        }
    }
}

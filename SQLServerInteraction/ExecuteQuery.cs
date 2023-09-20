using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public DataTable ExecuteQuery(string sql)
        {
            var dataTable = new DataTable();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            using var adapter = new SqlDataAdapter(command);

            adapter.Fill(dataTable);

            return dataTable;
        }
    }
}

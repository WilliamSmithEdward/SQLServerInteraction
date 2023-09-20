using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public DataTable GetTableSchema(string tableName)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand($"SELECT * FROM {tableName} WHERE 1=0", connection);
            using var adapter = new SqlDataAdapter(command);

            var schemaTable = new DataTable();
            adapter.FillSchema(schemaTable, SchemaType.Source);

            return schemaTable;
        }
    }
}

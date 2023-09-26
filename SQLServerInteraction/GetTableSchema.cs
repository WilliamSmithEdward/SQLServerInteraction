using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the schema of a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table for which the schema is retrieved.</param>
        /// <returns>A <see cref="System.Data.DataTable"/> containing the schema of the specified table.</returns>
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

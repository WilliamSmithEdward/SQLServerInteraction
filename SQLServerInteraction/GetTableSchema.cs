using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the schema of a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <returns>A <see cref="System.Data.DataTable"/> containing the schema of the specified table.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public DataTable GetTableSchema(string tableName)
        {
            string sql = $"SELECT * FROM {SqlIdentifier.Quote(tableName)} WHERE 1 = 0";

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(sql, connection);
            using var adapter = new SqlDataAdapter(command);

            var schemaTable = new DataTable();
            adapter.FillSchema(schemaTable, SchemaType.Source);

            return schemaTable;
        }
    }
}

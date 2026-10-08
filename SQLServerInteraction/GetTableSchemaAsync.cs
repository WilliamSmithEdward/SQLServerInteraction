using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// An empty DataTable with the columns of a table, from <c>SELECT * ... WHERE 1 = 0</c>.
        /// </summary>
        /// <param name="tableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the DataTable with the columns and no rows.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public Task<DataTable> GetTableSchemaAsync(string tableName, CancellationToken cancellationToken = default)
        {
            string sql = $"SELECT * FROM {SqlIdentifier.Quote(tableName)} WHERE 1 = 0";

            return WithConnectionAsync(async connection =>
            {
                using var command = new SqlCommand(sql, connection);
                using var adapter = new SqlDataAdapter(command);
                using var registration = cancellationToken.Register(command.Cancel);

                var schemaTable = new DataTable();
                await Task.Run(() => adapter.FillSchema(schemaTable, SchemaType.Source), cancellationToken);
                return schemaTable;
            }, cancellationToken);
        }
    }
}

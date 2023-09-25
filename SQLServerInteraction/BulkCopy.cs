using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Performs a bulk copy operation to insert data from a DataTable into a SQL Server table.
        /// </summary>
        /// <param name="dataTable">The DataTable containing the data to be copied.</param>
        /// <param name="destinationTableName">The name of the destination SQL Server table.</param>
        /// <param name="flushTable">A flag indicating whether to delete all rows in the destination table before copying data. Defaults to false.</param>
        public void BulkCopy(DataTable dataTable, string destinationTableName, bool flushTable = false)
        {
            using var connection = new SqlConnection(_connectionString);
            using var bulkCopy = new SqlBulkCopy(connection);

            connection.Open();

            if (flushTable) using (var command = new SqlCommand("DELETE FROM " + destinationTableName, connection)) command.ExecuteNonQuery();

            bulkCopy.DestinationTableName = destinationTableName;
            bulkCopy.WriteToServer(dataTable);
        }
    }
}

using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the names of non-clustered indexes for a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted as an identifier and sent to OBJECT_ID as a parameter, so a bare name means the default schema.</param>
        /// <returns>A list of non-clustered index names for the specified table.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public List<string> GetTableIndexs(string tableName)
        {
            var indexes = new List<string>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(
                "SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID(@name) AND is_primary_key = 0 AND type_desc = 'NONCLUSTERED'",
                connection);
            command.Parameters.AddWithValue("@name", SqlIdentifier.Quote(tableName));

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                string indexName = reader["name"].ToString() ?? "";
                if(!string.IsNullOrEmpty(indexName)) indexes.Add(indexName);
            }

            return indexes;
        }
    }
}

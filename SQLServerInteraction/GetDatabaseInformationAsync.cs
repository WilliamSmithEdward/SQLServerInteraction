namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// The current database's name, id, creation date and collation, from <c>sys.databases</c>.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is a dictionary with <c>DatabaseName</c>, <c>DatabaseId</c>, <c>CreationDate</c> (formatted with the current culture) and <c>Collation</c>.</returns>
        public async Task<Dictionary<string, string>> GetDatabaseInformationAsync(CancellationToken cancellationToken = default)
        {
            var rows = await CatalogRowsAsync(
                "SELECT name, database_id, create_date, collation_name FROM sys.databases WHERE name = DB_NAME()",
                null,
                reader => new Dictionary<string, string>
                {
                    ["DatabaseName"] = reader["name"]?.ToString() ?? "",
                    ["DatabaseId"] = reader["database_id"]?.ToString() ?? "",
                    ["CreationDate"] = reader["create_date"]?.ToString() ?? "",
                    ["Collation"] = reader["collation_name"]?.ToString() ?? "",
                },
                cancellationToken);

            return rows.FirstOrDefault() ?? [];
        }
    }
}

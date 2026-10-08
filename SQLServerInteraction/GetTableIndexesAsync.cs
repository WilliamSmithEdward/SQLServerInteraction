namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// The names of a table's nonclustered indexes that are not its primary key, from <c>sys.indexes</c>.
        /// </summary>
        /// <param name="tableName">The name of the table, such as <c>Sales</c>, <c>dbo.Sales</c> or <c>[dbo].[My Sales]</c>. It is quoted and passed to <c>OBJECT_ID</c> as a parameter, so a bare name means the default schema.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the index names.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public Task<List<string>> GetTableIndexesAsync(string tableName, CancellationToken cancellationToken = default)
        {
            string name = SqlIdentifier.Quote(tableName);
            return CatalogStringsAsync(
                "SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID(@name) AND is_primary_key = 0 AND type_desc = 'NONCLUSTERED'",
                command => command.Parameters.AddWithValue("@name", name), cancellationToken);
        }
    }
}

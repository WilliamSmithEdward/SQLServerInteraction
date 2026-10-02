namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Retrieves the first primary key column of a table in the SQL Server database.
        /// </summary>
        /// <param name="tableName">The name of the table, sent as parameters. A bare name (<c>Sales</c>) matches in any schema; <c>dbo.Sales</c> or <c>[dbo].[Sales]</c> matches that schema only.</param>
        /// <returns>The first column of the primary key in key order, which for a composite key is one of several (see <see cref="GetTablePrimaryKeyColumns"/>), or null if the table has no primary key or does not exist.</returns>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        public string? GetTablePrimaryKeyColumn(string tableName)
        {
            return GetTablePrimaryKeyColumns(tableName).FirstOrDefault();
        }
    }
}

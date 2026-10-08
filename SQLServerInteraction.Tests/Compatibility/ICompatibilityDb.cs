namespace SQLServerInteraction.Tests.Compatibility
{
    /// <summary>
    /// The operations the compatibility scenarios run, named by what they do rather
    /// than by either version's method names. One implementation wraps the 1.1.1
    /// package (in SQLServerInteraction.V1Tests), the other the current library, so
    /// the same scenario can run on both and record what each version does.
    /// </summary>
    public interface ICompatibilityDb
    {
        /// <summary>The library version under the adapter, as the scenarios report it.</summary>
        string Version { get; }

        /// <summary>Inserts one row from a column-to-value dictionary.</summary>
        Task InsertAsync(string tableName, Dictionary<string, object> values);

        /// <summary>Deletes the rows matching a condition given without the WHERE keyword.</summary>
        Task DeleteAsync(string tableName, string condition);

        /// <summary>Runs a non-query with a parameter dictionary.</summary>
        Task NonQueryWithParametersAsync(string sql, Dictionary<string, object> parameters);

        /// <summary>The first column of the first row, read as a nullable int.</summary>
        Task<int?> NullableIntScalarAsync(string sql);

        /// <summary>The first column of the first row, read as an int.</summary>
        Task<int> IntScalarAsync(string sql);

        /// <summary>Whether a table of the given name exists.</summary>
        Task<bool> TableExistsAsync(string tableName);

        /// <summary>The database size as the version's size method reports it, in whatever unit it uses.</summary>
        Task<long> DatabaseSizeAsync();

        /// <summary>The connection string the version builds for a SQL Server login.</summary>
        string BuildConnectionString(string server, string database, string user, string password);

        /// <summary>
        /// <c>SELECT Id FROM table</c> ordered by Id descending and paginated, with the
        /// ORDER BY left out when <paramref name="withOrderBy"/> is false.
        /// </summary>
        string BuildPagedQuery(string table, bool withOrderBy, int page, int pageSize);

        /// <summary><c>SELECT Category</c> plus a COUNT and an AVG aggregate, FROM Products, GROUP BY Category.</summary>
        string BuildAggregateQuery();

        /// <summary><c>SELECT * FROM T WHERE A = 1</c> followed by a nested condition holding <c>B = 2 OR C = 3</c>.</summary>
        string BuildNestedConditionQuery();

        /// <summary>The SQL a builder gives on a first and a second call to Build.</summary>
        (string First, string Second) BuildTwice();
    }
}

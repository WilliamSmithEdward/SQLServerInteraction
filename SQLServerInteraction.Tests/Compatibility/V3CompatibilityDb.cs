namespace SQLServerInteraction.Tests.Compatibility
{
    /// <summary>The scenarios' operations over the current library.</summary>
    public sealed class V3CompatibilityDb(string connectionString) : ICompatibilityDb
    {
        private readonly SQLServerInstance _db = new(connectionString);

        public string Version => "3.0.0";

        public Task InsertAsync(string tableName, Dictionary<string, object> values) => _db.InsertDataAsync(tableName, values);

        public Task DeleteAsync(string tableName, string condition) => _db.DeleteDataAsync(tableName, condition);

        public Task NonQueryWithParametersAsync(string sql, Dictionary<string, object> parameters) => _db.ExecuteNonQueryWithParametersAsync(sql, parameters);

        public Task<int?> NullableIntScalarAsync(string sql) => _db.ExecuteScalarAsync<int?>(sql);

        public Task<int> IntScalarAsync(string sql) => _db.ExecuteScalarAsync<int>(sql);

        public Task<bool> TableExistsAsync(string tableName) => _db.DoesTableExistAsync(tableName);

        public Task<long> DatabaseSizeAsync() => _db.GetDatabaseSizeInBytesAsync();

        public string BuildConnectionString(string server, string database, string user, string password) =>
            new SQLServerConnectionString(server, database, user, password).GetConnectionString();

        public string BuildPagedQuery(string table, bool withOrderBy, int page, int pageSize)
        {
            var builder = new QueryBuilder();
            builder.Select("Id");
            builder.From(table);
            if (withOrderBy) builder.OrderBy("Id", QuerySortOrder.Descending);
            builder.Paginate(page, pageSize);
            return builder.Build().SQL!;
        }

        public string BuildAggregateQuery()
        {
            var builder = new QueryBuilder();
            builder.Select("Category");
            builder.Count("*", "Products");
            builder.Avg("Price", "AveragePrice");
            builder.From("Products");
            builder.GroupBy("Category");
            return builder.Build().SQL!;
        }

        public string BuildNestedConditionQuery()
        {
            var builder = new QueryBuilder();
            builder.Select("*");
            builder.From("T");
            builder.Where("A = 1");
            builder.StartNestedCondition();
            builder.And("B = 2");
            builder.Or("C = 3");
            builder.EndNestedCondition();
            return builder.Build().SQL!;
        }

        public (string First, string Second) BuildTwice()
        {
            var builder = new QueryBuilder();
            builder.Select("Id");
            builder.From("T");
            builder.Where("Id = 1");
            return (builder.Build().SQL!, builder.Build().SQL!);
        }
    }

    /// <summary>The compatibility scenarios on the current library, asserting the 3.0.0 column.</summary>
    public class V3CompatibilityTests(DatabaseFixture database) : CompatibilityScenarioTests(database)
    {
        protected override ICompatibilityDb Compat => new V3CompatibilityDb(Database.ConnectionString);

        protected override string Expected(CompatibilityScenario scenario) => scenario.ExpectedV3;
    }
}

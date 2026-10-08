using SQLServerInteraction.Tests;
using SQLServerInteraction.Tests.Compatibility;

namespace SQLServerInteraction.V1Tests
{
    /// <summary>
    /// The scenarios' operations over the 1.1.1 package. Where 1.x had only a
    /// synchronous method, it is called as it was; the scenarios do not depend on
    /// how the call reaches the server.
    /// </summary>
    public sealed class V1CompatibilityDb(string connectionString) : ICompatibilityDb
    {
        private readonly SQLServerInstance _db = new(connectionString);

        public string Version => "1.1.1";

        public Task InsertAsync(string tableName, Dictionary<string, object> values) => _db.InsertDataAsync(tableName, values);

        public Task DeleteAsync(string tableName, string condition) => _db.DeleteDataAsync(tableName, condition);

        public Task NonQueryWithParametersAsync(string sql, Dictionary<string, object> parameters) => _db.ExecuteNonQueryWithParametersAsync(sql, parameters);

        public Task<int?> NullableIntScalarAsync(string sql) => _db.ExecuteScalarAsync<int?>(sql);

        public Task<int> IntScalarAsync(string sql) => _db.ExecuteScalarAsync<int>(sql);

        public Task<bool> TableExistsAsync(string tableName) => Task.FromResult(_db.DoesTableExist(tableName));

        public Task<long> DatabaseSizeAsync() => Task.FromResult(_db.GetDatabaseSizeInBytes());

        public string BuildConnectionString(string server, string database, string user, string password) =>
            new SQLServerConnectionString(server, database, user, password).GetConnectionString();

        public string BuildPagedQuery(string table, bool withOrderBy, int page, int pageSize)
        {
            var builder = new QueryBuilder();
            builder.Select("Id");
            builder.From(table);
            if (withOrderBy) builder.OrderBy("Id", SQLServerInteraction.SortOrder.Descending);
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

    /// <summary>The compatibility scenarios on the 1.1.1 package, asserting the 1.1.1 column.</summary>
    public class V1CompatibilityTests(DatabaseFixture database) : CompatibilityScenarioTests(database)
    {
        protected override ICompatibilityDb Compat => new V1CompatibilityDb(Database.ConnectionString);

        protected override string Expected(CompatibilityScenario scenario) => scenario.ExpectedV1;
    }
}

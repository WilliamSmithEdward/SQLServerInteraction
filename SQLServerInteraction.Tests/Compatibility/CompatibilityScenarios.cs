
namespace SQLServerInteraction.Tests.Compatibility
{
    /// <summary>
    /// One scenario the two versions are compared on: what it does, and the outcome
    /// each version gives. An outcome is a short string, such as a count, a value, a
    /// piece of SQL, or the name of the exception thrown, so the two columns read as
    /// a table. A scenario whose two outcomes differ records a breaking change.
    /// </summary>
    public sealed record CompatibilityScenario(string Name, string ExpectedV1, string ExpectedV3, Func<CompatibilityContext, Task<string>> Run)
    {
        public bool Changed => ExpectedV1 != ExpectedV3;

        public override string ToString() => Name;
    }

    /// <summary>What a scenario runs with: the version under test and the scratch database.</summary>
    public sealed class CompatibilityContext(ICompatibilityDb db, DatabaseFixture database)
    {
        public ICompatibilityDb Db { get; } = db;

        public DatabaseFixture Database { get; } = database;

        /// <summary>A table with an int key and a nullable name, named for the scenario unless a name is given.</summary>
        public string Customers(string? name = null) =>
            Database.CreateTable("Id int NOT NULL PRIMARY KEY, Name nvarchar(50) NULL", name);

        public int Count(string table) =>
            (int)Database.Scalar($"SELECT COUNT(*) FROM dbo.{SqlQuote(table)}")!;

        /// <summary>"ok" when the work completes, or the name of the exception it throws.</summary>
        public static async Task<string> Outcome(Func<Task> work)
        {
            try
            {
                await work();
                return "ok";
            }
            catch (Exception exception)
            {
                return exception.GetType().Name;
            }
        }

        /// <summary>The outcome, and then the row count when the work completed.</summary>
        public async Task<string> OutcomeAndCount(string table, Func<Task> work)
        {
            string outcome = await Outcome(work);
            return outcome == "ok" ? Rows(Count(table)) : outcome;
        }

        public static string Rows(int count) => count == 1 ? "1 row" : $"{count} rows";

        private static string SqlQuote(string name) => "[" + name.Replace("]", "]]") + "]";
    }

    /// <summary>
    /// The catalogue. Each scenario is written once, against <see cref="ICompatibilityDb"/>,
    /// and runs in SQLServerInteraction.V1Tests on the 1.1.1 package and in this
    /// project on the current library, each asserting its own column.
    /// </summary>
    public static class CompatibilityScenarios
    {
        public static IReadOnlyList<CompatibilityScenario> All { get; } =
        [
            // Unchanged behavior, so that the table shows what stayed the same as well as what moved.

            new("An insert with plain column names, then a count",
                ExpectedV1: "1 row", ExpectedV3: "1 row",
                async c =>
                {
                    string table = c.Customers();
                    return await c.OutcomeAndCount(table, () => c.Db.InsertAsync(table, new Dictionary<string, object> { ["Id"] = 1, ["Name"] = "Contoso" }));
                }),

            new("A scalar read as an int",
                ExpectedV1: "5", ExpectedV3: "5",
                async c => (await c.Db.IntScalarAsync("SELECT 5")).ToString()),

            new("A bare table name in the table lookup",
                ExpectedV1: "true", ExpectedV3: "true",
                async c => (await c.Db.TableExistsAsync(c.Customers())).ToString().ToLowerInvariant()),

            new("A delete with a condition",
                ExpectedV1: "1 row", ExpectedV3: "1 row",
                async c =>
                {
                    string table = c.Customers();
                    c.Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'A'), (2, N'B')");
                    return await c.OutcomeAndCount(table, () => c.Db.DeleteAsync(table, "Id = 1"));
                }),

            // Names: 1.x pasted them into the SQL; the current library quotes them.

            new("A column name with a space in an insert dictionary",
                ExpectedV1: "SqlException", ExpectedV3: "1 row",
                async c =>
                {
                    string table = c.Database.CreateTable("Id int NOT NULL PRIMARY KEY, [Customer Name] nvarchar(50) NULL");
                    return await c.OutcomeAndCount(table, () => c.Db.InsertAsync(table, new Dictionary<string, object> { ["Id"] = 1, ["Customer Name"] = "Contoso" }));
                }),

            new("A table name with a space",
                ExpectedV1: "SqlException", ExpectedV3: "1 row",
                async c =>
                {
                    string table = c.Customers("My Customers " + Guid.NewGuid().ToString("N")[..8]);
                    return await c.OutcomeAndCount(table, () => c.Db.InsertAsync(table, new Dictionary<string, object> { ["Id"] = 1 }));
                }),

            new("A schema-qualified table name in the table lookup",
                ExpectedV1: "false", ExpectedV3: "true",
                async c => (await c.Db.TableExistsAsync("dbo." + c.Customers())).ToString().ToLowerInvariant()),

            // Values: 1.x dropped nulls and named parameters after the keys.

            new("A null value in an insert dictionary",
                ExpectedV1: "SqlException", ExpectedV3: "1 row",
                async c =>
                {
                    string table = c.Customers();
                    return await c.OutcomeAndCount(table, () => c.Db.InsertAsync(table, new Dictionary<string, object> { ["Id"] = 1, ["Name"] = null! }));
                }),

            new("A null parameter value in a non-query",
                ExpectedV1: "SqlException", ExpectedV3: "1 row",
                async c =>
                {
                    string table = c.Customers();
                    return await c.OutcomeAndCount(table, () => c.Db.NonQueryWithParametersAsync(
                        $"INSERT INTO dbo.{table} (Id, Name) VALUES (@Id, @Name)",
                        new Dictionary<string, object> { ["Id"] = 1, ["Name"] = null! }));
                }),

            new("A parameter key that already carries the @",
                ExpectedV1: "SqlException", ExpectedV3: "1 row",
                async c =>
                {
                    string table = c.Customers();
                    return await c.OutcomeAndCount(table, () => c.Db.NonQueryWithParametersAsync(
                        $"INSERT INTO dbo.{table} (Id) VALUES (@Id)",
                        new Dictionary<string, object> { ["@Id"] = 1 }));
                }),

            new("An empty delete condition",
                ExpectedV1: "0 rows", ExpectedV3: "ArgumentException",
                async c =>
                {
                    string table = c.Customers();
                    c.Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'A'), (2, N'B')");
                    string outcome = await CompatibilityContext.Outcome(() => c.Db.DeleteAsync(table, ""));
                    return outcome == "ok" ? CompatibilityContext.Rows(c.Count(table)) : outcome;
                }),

            new("A scalar read as a nullable int",
                ExpectedV1: "InvalidCastException", ExpectedV3: "5",
                async c =>
                {
                    try
                    {
                        return (await c.Db.NullableIntScalarAsync("SELECT 5"))?.ToString() ?? "null";
                    }
                    catch (Exception exception)
                    {
                        return exception.GetType().Name;
                    }
                }),

            new("The unit of the database size",
                ExpectedV1: "kilobytes", ExpectedV3: "bytes",
                async c =>
                {
                    long pagesBefore = Pages(c);
                    long size = await c.Db.DatabaseSizeAsync();
                    long pagesAfter = Pages(c);
                    foreach (long pages in new[] { pagesBefore, pagesAfter })
                    {
                        if (size == pages * 8) return "kilobytes";
                        if (size == pages * 8192) return "bytes";
                    }
                    return $"{size} for {pagesBefore} pages";
                }),

            // Connection strings: 1.x concatenated text; the current library uses SqlConnectionStringBuilder.

            new("A connection string with a semicolon in the password",
                ExpectedV1: "Server=srv;Database=db;User Id=user;Password=p;w;Encrypt=True;;",
                ExpectedV3: "Data Source=srv;Initial Catalog=db;User ID=user;Password=\"p;w\";Encrypt=True",
                c => Task.FromResult(c.Db.BuildConnectionString("srv", "db", "user", "p;w"))),

            // QueryBuilder: the same calls, different text.

            new("A paged query ordered descending",
                ExpectedV1: "SELECT Id FROM T ORDER BY Id DESCENDING OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY",
                ExpectedV3: "SELECT Id FROM T ORDER BY Id DESC OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY",
                c => Task.FromResult(c.Db.BuildPagedQuery("T", withOrderBy: true, page: 2, pageSize: 10).Trim())),

            new("A paged query with no ORDER BY",
                ExpectedV1: "SELECT Id FROM T OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY",
                ExpectedV3: "InvalidOperationException",
                async c =>
                {
                    try
                    {
                        return await Task.FromResult(c.Db.BuildPagedQuery("T", withOrderBy: false, page: 2, pageSize: 10).Trim());
                    }
                    catch (Exception exception)
                    {
                        return exception.GetType().Name;
                    }
                }),

            new("Aggregates in the SELECT list",
                ExpectedV1: "SELECT Category COUNT(*) AS ProductsAVG(Price) AS AveragePriceFROM Products GROUP BY Category",
                ExpectedV3: "SELECT Category, COUNT(*) AS Products, AVG(Price) AS AveragePrice FROM Products GROUP BY Category",
                c => Task.FromResult(c.Db.BuildAggregateQuery().Trim())),

            new("A nested condition",
                ExpectedV1: "SELECT * FROM T WHERE A = 1 AND B = 2 OR C = 3 ()",
                ExpectedV3: "SELECT * FROM T WHERE A = 1 AND (B = 2 OR C = 3)",
                c => Task.FromResult(c.Db.BuildNestedConditionQuery().Trim())),

            new("Build called twice on one builder",
                ExpectedV1: "same", ExpectedV3: "same",
                c =>
                {
                    var (first, second) = c.Db.BuildTwice();
                    return Task.FromResult(first == second ? "same" : "different");
                }),
        ];

        public static CompatibilityScenario Find(string name) => All.Single(scenario => scenario.Name == name);

        /// <summary>The database's size in 8 KB pages, read directly, for the size-unit scenario.</summary>
        private static long Pages(CompatibilityContext c) =>
            Convert.ToInt64(c.Database.Scalar("SELECT SUM(CAST(size AS bigint)) FROM sys.master_files WHERE database_id = DB_ID()"));
    }
}

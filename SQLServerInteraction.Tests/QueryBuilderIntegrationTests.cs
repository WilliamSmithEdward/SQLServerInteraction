namespace SQLServerInteraction.Tests
{
    /// <summary>
    /// SQL from QueryBuilder runs on a real server, with the values AddParameter recorded.
    /// </summary>
    public class QueryBuilderIntegrationTests(DatabaseFixture database) : IntegrationTest(database)
    {
        public class Row
        {
            public int Id { get; set; }

            public string? Band { get; set; }

            public int Items { get; set; }
        }

        [Fact]
        public async Task A_paged_query_with_a_CASE_a_nested_condition_and_parameters_runs()
        {
            string table = Database.CreateTable("Id int NOT NULL, Price int NOT NULL, Region nvarchar(10) NOT NULL");
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, 5, N'West'), (2, 50, N'West'), (3, 500, N'East'), (4, 7, N'East'), (5, 70, N'North')");

            var builder = new QueryBuilder();
            builder.Select("Id");
            builder.StartCaseStatement("");
            builder.AddCaseWhen("Price < @Cheap", "'low'");
            builder.AddCaseElse("'high'");
            builder.EndCaseStatement("Band");
            builder.Count("*", "Items");
            builder.From($"dbo.{table}");
            builder.Where("Price > 0");
            builder.StartNestedCondition();
            builder.And("Region = @Region");
            builder.Or("Price > @Floor");
            builder.EndNestedCondition();
            builder.GroupBy("Id, Price");
            builder.OrderBy("Id", QuerySortOrder.Descending);
            builder.Paginate(page: 2, pageSize: 2);
            builder.AddParameter("Cheap", 10);
            builder.AddParameter("Region", "West");
            builder.AddParameter("@Floor", 60);

            QueryBuildResult result = builder.Build();
            List<Row> rows = await Db.ExecuteQueryToObjectListAsync<Row>(result.SQL!, result.ParameterValues);

            // Matching: 1, 2 (West), 3 and 5 (over 60); descending 5, 3, 2, 1; page 2 is 2, 1.
            Assert.Equal([2, 1], rows.Select(r => r.Id));
            Assert.Equal(["high", "low"], rows.Select(r => r.Band));
            Assert.All(rows, r => Assert.Equal(1, r.Items));
        }

        [Fact]
        public void A_subquery_runs_with_its_parameters()
        {
            string orders = Database.CreateTable("Id int NOT NULL, CustomerId int NOT NULL");
            string customers = Database.CreateTable("CustomerId int NOT NULL, Region nvarchar(10) NOT NULL");
            Database.Execute($"INSERT INTO dbo.{customers} VALUES (1, N'West'), (2, N'East'); INSERT INTO dbo.{orders} VALUES (10, 1), (11, 2), (12, 1)");

            var builder = new QueryBuilder();
            builder.Select("Id");
            builder.From($"dbo.{orders}");
            builder.Where("CustomerId IN");
            QueryBuilder subquery = builder.CreateSubquery();
            subquery.Select("CustomerId");
            subquery.From($"dbo.{customers}");
            subquery.Where("Region = @Region");
            subquery.AddParameter("Region", "West");
            builder.OrderBy("Id");

            QueryBuildResult result = builder.Build();

            Assert.Equal([10, 12], Db.ExecuteQueryToObjectList<IdRow>(result.SQL!, result.ParameterValues).Select(r => r.Id));
        }

        public class IdRow
        {
            public int Id { get; set; }
        }
    }
}

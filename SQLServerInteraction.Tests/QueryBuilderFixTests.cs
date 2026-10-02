namespace SQLServerInteraction.Tests
{
    /// <summary>
    /// QueryBuilder output that was wrong before 2.0.0, and output that worked and must not change.
    /// </summary>
    public class QueryBuilderFixTests
    {
        private static QueryBuilder Products()
        {
            var builder = new QueryBuilder();
            builder.Select("*");
            builder.From("Products");
            return builder;
        }

        [Fact]
        public void Output_that_worked_before_is_unchanged_to_the_character()
        {
            var builder = new QueryBuilder();
            builder.Select("CustomerId, OrderDate");
            builder.From("Orders o");
            builder.Join("Customers c", "o.CustomerId = c.CustomerId", JoinType.Left);
            builder.Where("Status = 'Shipped'");
            builder.And("Total > @Min");
            builder.GroupBy("CustomerId, OrderDate");
            builder.AddParameter("Min", 100);

            QueryBuildResult result = builder.Build();

            Assert.Equal("SELECT CustomerId, OrderDate FROM Orders o LEFT JOIN Customers c ON o.CustomerId = c.CustomerId WHERE Status = 'Shipped' AND Total > @Min GROUP BY CustomerId, OrderDate ", result.SQL);
            Assert.Equal("@Min", result.Parameters);
        }

        [Fact]
        public void OrderBy_writes_ASC_or_DESC()
        {
            var ascending = Products();
            ascending.OrderBy("Price");
            var descending = Products();
            descending.OrderBy("Category, Price", QuerySortOrder.Descending);

            Assert.Equal("SELECT * FROM Products ORDER BY Price ASC ", ascending.Build().SQL);
            Assert.Equal("SELECT * FROM Products ORDER BY Category, Price DESC ", descending.Build().SQL);
        }

        [Fact]
        public void Paginate_follows_the_ORDER_BY()
        {
            var builder = Products();
            builder.OrderBy("Price", QuerySortOrder.Descending);
            builder.Paginate(page: 3, pageSize: 10);

            Assert.Equal("SELECT * FROM Products ORDER BY Price DESC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY ", builder.Build().SQL);
        }

        [Fact]
        public void Paginate_needs_an_ORDER_BY_and_a_positive_page()
        {
            var builder = Products();
            builder.Paginate(1, 10);

            Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Paginate(0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Paginate(1, 0));
        }

        [Fact]
        public void Aggregates_join_the_SELECT_list_with_commas()
        {
            var alone = new QueryBuilder();
            alone.Select();
            alone.Count("Revenue", "CountOfRevenue");
            alone.Sum("Revenue", "TotalRevenue");
            alone.From("Sales");

            var withColumns = new QueryBuilder();
            withColumns.Select("Region");
            withColumns.From("Sales");
            withColumns.Avg("Revenue", "AverageRevenue");
            withColumns.Min("Revenue", "Least");
            withColumns.Max("Revenue", "Most");
            withColumns.Count("*", "Rows");
            withColumns.GroupBy("Region");

            Assert.Equal("SELECT COUNT(Revenue) AS CountOfRevenue, SUM(Revenue) AS TotalRevenue FROM Sales ", alone.Build().SQL);
            Assert.Equal("SELECT Region, AVG(Revenue) AS AverageRevenue, MIN(Revenue) AS Least, MAX(Revenue) AS Most, COUNT(*) AS Rows FROM Sales GROUP BY Region ", withColumns.Build().SQL);
        }

        [Fact]
        public void A_nested_condition_is_wrapped_in_parentheses()
        {
            var builder = new QueryBuilder();
            builder.Select("*");
            builder.From("Users");
            builder.Where("Active = 1");
            builder.StartNestedCondition();
            builder.And("Age > @MinAge");
            builder.Or("Age < @MaxAge");
            builder.EndNestedCondition();
            builder.And("Region = 'West'");

            Assert.Equal("SELECT * FROM Users WHERE Active = 1 AND (Age > @MinAge OR Age < @MaxAge) AND Region = 'West' ", builder.Build().SQL);
        }

        [Fact]
        public void Nested_conditions_can_nest_and_are_closed_by_Build()
        {
            var builder = new QueryBuilder();
            builder.Select("*");
            builder.From("T");
            builder.StartNestedCondition();
            builder.Where("A = 1");
            builder.StartNestedCondition();
            builder.Or("B = 2");
            builder.And("C = 3");
            builder.EndNestedCondition();

            Assert.Equal("SELECT * FROM T WHERE (A = 1 OR (B = 2 AND C = 3)) ", builder.Build().SQL);
        }

        [Fact]
        public void An_empty_or_unstarted_nested_condition_cannot_end()
        {
            var builder = Products();
            Assert.Throws<InvalidOperationException>(() => builder.EndNestedCondition());

            builder.StartNestedCondition();
            Assert.Throws<InvalidOperationException>(() => builder.EndNestedCondition());
        }

        [Fact]
        public void A_searched_CASE_lands_in_the_SELECT_list()
        {
            var builder = new QueryBuilder();
            builder.Select("FirstName, LastName");
            builder.From("Users");
            builder.StartCaseStatement("");
            builder.AddCaseWhen("Age < 18", "'Minor'");
            builder.AddCaseWhen("Age >= 18 AND Age < 60", "'Adult'");
            builder.AddCaseElse("'Senior'");
            builder.EndCaseStatement("AgeCategory");

            Assert.Equal("SELECT FirstName, LastName, CASE WHEN Age < 18 THEN 'Minor' WHEN Age >= 18 AND Age < 60 THEN 'Adult' ELSE 'Senior' END AS AgeCategory FROM Users ", builder.Build().SQL);
        }

        [Fact]
        public void A_simple_CASE_written_into_the_SELECT_list_reads_as_before()
        {
            var builder = new QueryBuilder();
            builder.Select("Id,");
            builder.StartCaseStatement("Status");
            builder.AddCaseWhen("1", "'Open'");
            builder.AddCaseElse("'Closed'");
            builder.EndCaseStatement("StatusName");
            builder.From("Orders");

            Assert.Equal("SELECT Id, CASE Status WHEN 1 THEN 'Open' ELSE 'Closed' END AS StatusName FROM Orders ", builder.Build().SQL);
        }

        [Fact]
        public void A_subquery_embeds_its_SQL_and_parameters_where_it_was_created()
        {
            var builder = new QueryBuilder();
            builder.Select("*");
            builder.From("Orders");
            builder.Where("CustomerId IN");
            QueryBuilder subquery = builder.CreateSubquery();
            builder.And("Total > @Min");
            builder.AddParameter("Min", 10);
            subquery.Select("CustomerId");
            subquery.From("Customers");
            subquery.Where("Region = @Region");
            subquery.AddParameter("@Region", "West");

            QueryBuildResult result = builder.Build();

            Assert.Equal("SELECT * FROM Orders WHERE CustomerId IN (SELECT CustomerId FROM Customers WHERE Region = @Region) AND Total > @Min ", result.SQL);
            Assert.Equal("@Region, @Min", result.Parameters);
            Assert.Equal(new Dictionary<string, object> { ["@Region"] = "West", ["@Min"] = 10 }, result.ParameterValues);
        }

        [Fact]
        public void Build_does_not_change_the_builder()
        {
            var builder = Products();
            builder.Where("Id IN");
            QueryBuilder subquery = builder.CreateSubquery();
            subquery.Select("ProductId");
            subquery.From("Sales");
            builder.StartNestedCondition();
            builder.And("A = 1");
            builder.Or("B = 2");
            builder.EndNestedCondition();
            builder.OrderBy("Price");
            builder.Paginate(2, 5);
            builder.AddParameter("P", 1);

            QueryBuildResult first = builder.Build();
            QueryBuildResult second = builder.Build();

            Assert.Equal("SELECT * FROM Products WHERE Id IN (SELECT ProductId FROM Sales) AND (A = 1 OR B = 2) ORDER BY Price ASC OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY ", first.SQL);
            Assert.Equal(first.SQL, second.SQL);
            Assert.Equal(first.Parameters, second.Parameters);
            Assert.Equal(first.ParameterValues, second.ParameterValues);
        }

        [Fact]
        public void AddParameter_values_are_returned_with_the_SQL()
        {
            var builder = Products();
            builder.Where("Price > @MinPrice AND Category = @Category AND Code = @Code");
            builder.AddParameter("MinPrice", 50);
            builder.AddParameter("@Category", "Tools");
            builder.AddParameter("Code", null!);

            QueryBuildResult result = builder.Build();

            Assert.Equal("@MinPrice, @Category, @Code", result.Parameters);
            Assert.Equal(50, result.ParameterValues["@MinPrice"]);
            Assert.Equal("Tools", result.ParameterValues["@Category"]);
            Assert.Equal(DBNull.Value, result.ParameterValues["@Code"]);
        }

        [Theory]
        [InlineData("*", "Id IN (SELECT Id FROM Other)")]
        [InlineData("'FROM'", "1 = 1")]
        [InlineData("[FROM]", "1 = 1")]
        [InlineData("1 -- FROM", "1 = 1")]
        public void Build_needs_a_FROM_outside_subqueries_quotes_and_comments(string columns, string where)
        {
            var builder = new QueryBuilder();
            builder.Select(columns);
            builder.Where(where);

            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Fact]
        public void A_FROM_written_into_the_SELECT_text_still_counts()
        {
            var builder = new QueryBuilder();
            builder.Select("* FROM Orders");

            Assert.Equal("SELECT * FROM Orders ", builder.Build().SQL);
        }

#pragma warning disable CS0618 // Union, Intersect and Except are obsolete; their output is pinned here.
        [Fact]
        public void The_obsolete_set_operators_still_put_their_keyword_first()
        {
            var first = Products();
            var second = Products();
            second.Union();
            var third = Products();
            third.Except();

            Assert.Equal("SELECT * FROM Products UNION SELECT * FROM Products ", first.Build().SQL + second.Build().SQL);
            Assert.StartsWith("EXCEPT SELECT", third.Build().SQL);
        }
#pragma warning restore CS0618
    }
}

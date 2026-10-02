namespace SQLServerInteraction.Tests
{
    public class QueryBuilderTests
    {
        [Fact]
        public void Select_From_Where_And_Or_append_in_call_order()
        {
            var builder = new QueryBuilder();
            builder.Select("CustomerId, OrderDate");
            builder.From("Orders");
            builder.Where("Status = 'Shipped'");
            builder.And("TotalAmount > @MinAmount");
            builder.Or("TotalAmount > 1000");

            Assert.Equal(
                "SELECT CustomerId, OrderDate FROM Orders WHERE Status = 'Shipped' AND TotalAmount > @MinAmount OR TotalAmount > 1000",
                builder.Build().SQL!.Trim());
        }

        [Fact]
        public void Select_with_no_columns_selects_everything()
        {
            var builder = new QueryBuilder();
            builder.Select();
            builder.From("Orders");

            Assert.Equal("SELECT * FROM Orders", builder.Build().SQL!.Trim());
        }

        [Fact]
        public void Join_and_GroupBy_emit_their_clauses()
        {
            var builder = new QueryBuilder();
            builder.Select("Customers.Name, COUNT(*) AS Orders");
            builder.From("Orders");
            builder.Join("Customers", "Orders.CustomerId = Customers.CustomerId", JoinType.Left);
            builder.GroupBy("Customers.Name");

            Assert.Equal(
                "SELECT Customers.Name, COUNT(*) AS Orders FROM Orders LEFT JOIN Customers ON Orders.CustomerId = Customers.CustomerId GROUP BY Customers.Name",
                builder.Build().SQL!.Trim());
        }

        [Fact]
        public void Build_refuses_a_query_without_SELECT_and_FROM()
        {
            var builder = new QueryBuilder();
            builder.Where("1 = 1");

            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }
    }
}

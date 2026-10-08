using System.Data;
using Microsoft.Data.SqlClient;

namespace SQLServerInteraction.Tests
{
    public class QueryTests(DatabaseFixture database) : IntegrationTest(database)
    {
        private string CreateOrders()
        {
            string table = Database.CreateTable("OrderId int NOT NULL PRIMARY KEY, Customer nvarchar(50) NULL, Total decimal(10, 2) NOT NULL");
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', 19.99), (2, N'Fabrikam', 5.00), (3, NULL, 7.50)");
            return table;
        }

        [Fact]
        public async Task ExecuteQuery_returns_every_row_and_column()
        {
            string table = CreateOrders();

            DataTable result = await Db.ExecuteQueryAsync($"SELECT OrderId, Customer, Total FROM dbo.{table} ORDER BY OrderId");

            Assert.Equal(3, result.Rows.Count);
            Assert.Equal(["OrderId", "Customer", "Total"], result.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            Assert.Equal("Fabrikam", result.Rows[1]["Customer"]);
            Assert.Equal(DBNull.Value, result.Rows[2]["Customer"]);
        }

        [Fact]
        public async Task ExecuteQueryAsync_returns_every_row()
        {
            string table = CreateOrders();

            DataTable result = await Db.ExecuteQueryAsync($"SELECT OrderId FROM dbo.{table}");

            Assert.Equal(3, result.Rows.Count);
        }

        [Fact]
        public async Task ExecuteQuery_of_T_returns_the_first_column()
        {
            string table = CreateOrders();

            Assert.Equal([1, 2, 3], await Db.ExecuteQueryAsync<int>($"SELECT OrderId, Total FROM dbo.{table} ORDER BY OrderId"));
            Assert.Equal([19.99m, 5.00m, 7.50m], await Db.ExecuteQueryAsync<decimal>($"SELECT Total FROM dbo.{table} ORDER BY OrderId"));
        }

        [Fact]
        public async Task ExecuteQuery_of_a_nullable_type_converts_and_gives_null_for_NULL()
        {
            string table = CreateOrders();

            Assert.Equal([1L, 2L, null], await Db.ExecuteQueryAsync<long?>($"SELECT CASE WHEN Customer IS NULL THEN NULL ELSE OrderId END FROM dbo.{table} ORDER BY OrderId"));
            Assert.Equal([19.99m, 5.00m, 7.50m], await Db.ExecuteQueryAsync<decimal?>($"SELECT Total FROM dbo.{table} ORDER BY OrderId"));
            Assert.Equal(["Contoso", "Fabrikam", ""], await Db.ExecuteQueryAsync<string>($"SELECT Customer FROM dbo.{table} ORDER BY OrderId"));
            await Assert.ThrowsAsync<InvalidCastException>(() => Db.ExecuteQueryAsync<int>($"SELECT CAST(NULL AS int)"));
        }

        [Fact]
        public async Task ExecuteScalar_converts_the_first_value_and_gives_default_for_no_rows()
        {
            string table = CreateOrders();

            Assert.Equal(3, await Db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.{table}"));
            Assert.Equal(32.49m, await Db.ExecuteScalarAsync<decimal>($"SELECT SUM(Total) FROM dbo.{table}"));
            Assert.Equal(0, await Db.ExecuteScalarAsync<int>($"SELECT OrderId FROM dbo.{table} WHERE OrderId = 99"));
        }

        public class Order
        {
            public int OrderId { get; set; }

            [SQLServerInstance.Column("Customer")]
            public string? Name { get; set; }

            public decimal? Total { get; set; }
        }

        [Fact]
        public async Task ExecuteQueryToObjectList_maps_columns_and_sends_parameters()
        {
            string table = CreateOrders();
            string sql = $"SELECT OrderId, Customer, Total FROM dbo.{table} WHERE Total > @Min ORDER BY OrderId";

            List<Order> orders = await Db.ExecuteQueryToObjectListAsync<Order>(sql, new Dictionary<string, object> { ["Min"] = 6m });
            List<Order> sameWithAt = await Db.ExecuteQueryToObjectListAsync<Order>(sql, new Dictionary<string, object> { ["@Min"] = 6m });

            Assert.Equal([1, 3], orders.Select(o => o.OrderId));
            Assert.Equal("Contoso", orders[0].Name);
            Assert.Null(orders[1].Name);
            Assert.Equal(7.50m, orders[1].Total);
            Assert.Equal([1, 3], sameWithAt.Select(o => o.OrderId));
        }

        [Fact]
        public async Task ExecuteParameterizedQuery_sends_SqlParameters()
        {
            string table = CreateOrders();

            await Db.ExecuteParameterizedQueryAsync($"DELETE FROM dbo.{table} WHERE OrderId = @OrderId", [new SqlParameter("@OrderId", 2)]);

            Assert.Equal(2, Database.Scalar($"SELECT COUNT(*) FROM dbo.{table}"));
        }

        [Fact]
        public async Task ExecuteTransaction_commits_all_or_rolls_back_all()
        {
            string table = CreateOrders();

            await Db.ExecuteTransactionAsync([
                $"UPDATE dbo.{table} SET Total = Total - 1 WHERE OrderId = 1",
                $"UPDATE dbo.{table} SET Total = Total + 1 WHERE OrderId = 2",
            ]);

            await Assert.ThrowsAnyAsync<SqlException>(() => Db.ExecuteTransactionAsync([
                $"UPDATE dbo.{table} SET Total = 0 WHERE OrderId = 1",
                $"INSERT INTO dbo.{table} VALUES (1, N'duplicate key', 0)",
            ]));

            Assert.Equal(18.99m, Database.Scalar($"SELECT Total FROM dbo.{table} WHERE OrderId = 1"));
            Assert.Equal(6.00m, Database.Scalar($"SELECT Total FROM dbo.{table} WHERE OrderId = 2"));
        }

        [Fact]
        public async Task ExportDataToCSVAsync_writes_a_header_and_quoted_values()
        {
            string table = CreateOrders();
            string path = Path.Combine(Path.GetTempPath(), $"sqlsi-{Guid.NewGuid():N}.csv");

            try
            {
                await Db.ExportDataToCSVAsync(path, $"SELECT OrderId, Customer FROM dbo.{table} WHERE OrderId IN (1, 3) ORDER BY OrderId");

                Assert.Equal(["OrderId,Customer", "\"1\",\"Contoso\"", "\"3\",\"\""], await File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}

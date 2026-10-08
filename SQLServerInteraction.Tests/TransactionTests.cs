using System.Data;
using Microsoft.Data.SqlClient;

namespace SQLServerInteraction.Tests
{
    public class TransactionTests(DatabaseFixture database) : IntegrationTest(database)
    {
        private string CreateCustomers()
        {
            string table = Database.CreateTable("CustomerId int NOT NULL PRIMARY KEY, Name nvarchar(50) NULL, Region nvarchar(20) NULL");
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', N'West'), (2, N'Fabrikam', N'East')");
            return table;
        }

        private int Count(string table) => (int)Database.Scalar($"SELECT COUNT(*) FROM dbo.{table}")!;

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static DataTable Rows(params (int Id, string Name)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("CustomerId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Region", typeof(string));
            foreach (var (id, name) in rows) table.Rows.Add(id, name, "North");
            return table;
        }

        [Fact]
        public async Task A_delete_and_an_insert_commit_together()
        {
            string table = CreateCustomers();

            await using (var transaction = await Db.BeginTransactionAsync())
            {
                await transaction.DeleteDataAsync(table, "CustomerId = @Id", new Dictionary<string, object> { ["Id"] = 1 });
                await transaction.InsertDataAsync(table, new Dictionary<string, object> { ["CustomerId"] = 3, ["Name"] = "Northwind" });
                await transaction.UpdateDataAsync(table, new Dictionary<string, object> { ["Region"] = "South" }, "CustomerId = 2");

                // The transaction sees its own work before the commit.
                Assert.Equal(2, await transaction.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.{table}"));
                Assert.Equal(["Fabrikam", "Northwind"], await transaction.ExecuteQueryAsync<string>($"SELECT Name FROM dbo.{table} ORDER BY CustomerId"));

                await transaction.CommitAsync();
            }

            Assert.Equal(2, Count(table));
            Assert.Equal("South", Database.Scalar($"SELECT Region FROM dbo.{table} WHERE CustomerId = 2"));
            Assert.Equal("Northwind", Database.Scalar($"SELECT Name FROM dbo.{table} WHERE CustomerId = 3"));
        }

        [Fact]
        public async Task Disposing_without_a_commit_rolls_back()
        {
            string table = CreateCustomers();

            await using (var transaction = await Db.BeginTransactionAsync())
            {
                await transaction.DeleteDataAsync(table, "1 = 1");
                await transaction.ExecuteSQLAsync($"INSERT INTO dbo.{table} VALUES (9, N'Gone', NULL)");
                Assert.Equal(1, await transaction.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.{table}"));
            }

            Assert.Equal(2, Count(table));
            Assert.Null(Database.Scalar($"SELECT Name FROM dbo.{table} WHERE CustomerId = 9"));
        }

        [Fact]
        public async Task A_failure_inside_a_using_block_rolls_back_the_earlier_work()
        {
            string table = CreateCustomers();

            await Assert.ThrowsAnyAsync<SqlException>(async () =>
            {
                await using var transaction = await Db.BeginTransactionAsync();
                await transaction.DeleteDataAsync(table, "CustomerId = 1");
                await transaction.InsertDataAsync(table, new Dictionary<string, object> { ["CustomerId"] = 2, ["Name"] = "Duplicate key" });
                await transaction.CommitAsync();
            });

            Assert.Equal(2, Count(table));
        }

        [Fact]
        public async Task Rollback_undoes_the_work_and_ends_the_transaction()
        {
            string table = CreateCustomers();

            await using var transaction = await Db.BeginTransactionAsync(IsolationLevel.Serializable);
            await transaction.DeleteDataAsync(table, "1 = 1");
            await transaction.RollbackAsync();

            Assert.Equal(2, Count(table));
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.DeleteDataAsync(table, "1 = 1"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        }

        [Fact]
        public async Task Nothing_runs_after_a_commit_or_a_dispose()
        {
            string table = CreateCustomers();

            var transaction = await Db.BeginTransactionAsync();
            await transaction.InsertDataAsync(table, new Dictionary<string, object> { ["CustomerId"] = 3 });
            await transaction.CommitAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.ExecuteScalarAsync<int>("SELECT 1"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.RollbackAsync());

            transaction.Dispose();
            transaction.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => transaction.ExecuteSQLAsync("SELECT 1"));
            Assert.Equal(3, Count(table));
        }

        [Fact]
        public async Task The_async_methods_run_in_one_transaction_too()
        {
            string table = CreateCustomers();
            string staging = Database.CreateTable("CustomerId int NOT NULL PRIMARY KEY, Name nvarchar(50) NULL, Region nvarchar(20) NULL");

            await using (var transaction = await Db.BeginTransactionAsync(IsolationLevel.ReadCommitted, Ct))
            {
                await transaction.BulkCopyAsync(Rows((10, "Litware"), (11, "Adatum")), staging, cancellationToken: Ct);
                Assert.Equal(2, await transaction.MergeDataAsync(staging, table, ["CustomerId"], ["Name", "Region"], cancellationToken: Ct));
                Assert.Equal(1, await transaction.BulkMergeAsync(Rows((12, "Tailspin")), table, ["CustomerId"], cancellationToken: Ct));
                await transaction.ExecuteNonQueryWithParametersAsync($"UPDATE dbo.{table} SET Region = @Region WHERE CustomerId = @Id",
                    new Dictionary<string, object> { ["Region"] = "South", ["Id"] = 1 }, Ct);
                await transaction.ExecuteParameterizedQueryAsync($"DELETE FROM dbo.{table} WHERE CustomerId = @Id", [new SqlParameter("@Id", 2)], Ct);
                await transaction.DeleteDataAsync(staging, "1 = 1", Ct);
                await transaction.IndexCreateAsync(table, "Region", Ct);
                await transaction.IndexDropAsync(table, "IX_Region", Ct);

                var rows = await transaction.ExecuteQueryAsync($"SELECT CustomerId FROM dbo.{table} ORDER BY CustomerId", Ct);
                Assert.Equal([1, 10, 11, 12], rows.Rows.Cast<DataRow>().Select(r => (int)r[0]));

                await transaction.CommitAsync(Ct);
            }

            Assert.Equal(4, Count(table));
            Assert.Equal(0, Count(staging));
            Assert.Equal("South", Database.Scalar($"SELECT Region FROM dbo.{table} WHERE CustomerId = 1"));
        }

        [Fact]
        public async Task Disposing_asynchronously_without_a_commit_rolls_back()
        {
            string table = CreateCustomers();

            await using (var transaction = await Db.BeginTransactionAsync(Ct))
            {
                await transaction.InsertDataAsync(new Customer { CustomerId = 3, Name = "Northwind" }, table, Ct);
                await transaction.ExecuteSQLAsync($"DELETE FROM dbo.{table} WHERE CustomerId = 1", Ct);
                Assert.Equal(2, (await transaction.ExecuteQueryToObjectListAsync<Customer>($"SELECT CustomerId, Name, Region FROM dbo.{table}", cancellationToken: Ct)).Count);
            }

            Assert.Equal(2, Count(table));
            Assert.Equal("Contoso", Database.Scalar($"SELECT Name FROM dbo.{table} WHERE CustomerId = 1"));
        }

        public class Customer
        {
            public int CustomerId { get; set; }

            public string? Name { get; set; }

            public string? Region { get; set; }
        }

        [Fact]
        public async Task ExecuteTransaction_commits_every_command_or_none()
        {
            string table = CreateCustomers();

            await Db.ExecuteTransactionAsync([$"DELETE FROM dbo.{table} WHERE CustomerId = 1", $"INSERT INTO dbo.{table} VALUES (3, N'Northwind', NULL)"]);
            Assert.Equal(2, Count(table));

            await Assert.ThrowsAnyAsync<SqlException>(() => Db.ExecuteTransactionAsync([$"DELETE FROM dbo.{table} WHERE CustomerId = 2", "SELECT 1/0"]));
            Assert.Equal(2, Count(table));
        }

        [Fact]
        public async Task A_savepoint_undoes_the_work_after_it_and_keeps_the_rest()
        {
            string table = CreateCustomers();

            await using (var transaction = await Db.BeginTransactionAsync())
            {
                await transaction.InsertDataAsync(table, new Dictionary<string, object> { ["CustomerId"] = 3, ["Name"] = "Kept" });
                await transaction.SaveAsync("after third");
                await transaction.DeleteDataAsync(table, "1 = 1");
                await transaction.SaveAsync("[after third]"); // the same name again moves the savepoint
                await transaction.InsertDataAsync(table, new Dictionary<string, object> { ["CustomerId"] = 4, ["Name"] = "Undone" });
                await transaction.RollbackToAsync("after third");

                Assert.Equal(0, await transaction.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.{table}"));
                await transaction.IndexCreateAsync(table, "Name");
                await transaction.CommitAsync();
            }

            Assert.Equal(0, Count(table));
            Assert.Equal(["IX_Name"], await Db.GetTableIndexesAsync(table));
        }

        [Fact]
        public async Task A_savepoint_works_asynchronously_and_refuses_a_bad_name()
        {
            string table = CreateCustomers();

            await using var transaction = await Db.BeginTransactionAsync(Ct);
            await transaction.SaveAsync("start", Ct);
            await transaction.DeleteDataAsync(table, "1 = 1", Ct);
            await transaction.RollbackToAsync("start", Ct);
            Assert.Equal(2, await transaction.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.{table}", Ct));

            await Assert.ThrowsAsync<ArgumentException>(() => transaction.SaveAsync(new string('x', 33), Ct));
            await Assert.ThrowsAsync<ArgumentException>(() => transaction.SaveAsync("a.b"));
            await Assert.ThrowsAsync<ArgumentException>(() => transaction.RollbackToAsync(""));

            await transaction.CommitAsync(Ct);
            Assert.Equal(2, Count(table));
        }

        [Fact]
        public async Task BulkMerge_can_run_more_than_once_on_one_transaction()
        {
            string table = CreateCustomers();

            await using var transaction = await Db.BeginTransactionAsync(Ct);
            Assert.Equal(1, await transaction.BulkMergeAsync(Rows((3, "Northwind")), table, ["CustomerId"]));
            Assert.Equal(2, await transaction.BulkMergeAsync(Rows((3, "Northwind Traders"), (4, "Litware")), table, ["CustomerId"], cancellationToken: Ct));
            Assert.Equal(5, await transaction.BulkMergeAsync(Rows((5, "Adatum")), table, ["CustomerId"], deleteUnmatched: true)); // one inserted, four deleted
            await transaction.CommitAsync(Ct);

            Assert.Equal(1, Count(table));
            Assert.Equal("Adatum", Database.Scalar($"SELECT Name FROM dbo.{table} WHERE CustomerId = 5"));
        }

        [Fact]
        public async Task A_failed_commit_closes_the_transaction_with_a_reason()
        {
            string table = CreateCustomers();

            await using var transaction = await Db.BeginTransactionAsync();
            await transaction.DeleteDataAsync(table, "1 = 1");
            short spid = await transaction.ExecuteScalarAsync<short>("SELECT @@SPID");
            Database.Execute($"KILL {spid}");

            await Assert.ThrowsAnyAsync<Exception>(() => transaction.CommitAsync());
            var afterwards = await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.RollbackAsync());
            Assert.Contains("commit failed", afterwards.Message);
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.ExecuteScalarAsync<int>("SELECT 1"));

            transaction.Dispose();
            Assert.Equal(2, Count(table));
        }

        [Fact]
        public async Task A_cancelled_token_stops_a_method_and_the_transaction_can_still_roll_back()
        {
            string table = CreateCustomers();
            var cancelled = new CancellationToken(canceled: true);

            await using var transaction = await Db.BeginTransactionAsync(Ct);
            await transaction.DeleteDataAsync(table, "CustomerId = 1", Ct);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transaction.DeleteDataAsync(table, "CustomerId = 2", cancelled));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transaction.ExecuteQueryAsync($"SELECT * FROM dbo.{table}", cancelled));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Db.BeginTransactionAsync(cancelled));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Db.ExecuteScalarAsync<int>("SELECT 1", cancelled));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Db.GetTableNamesAsync(cancelled));

            await transaction.RollbackAsync(Ct);
            Assert.Equal(2, Count(table));
        }
    }
}

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
        public void A_delete_and_an_insert_commit_together()
        {
            string table = CreateCustomers();

            using (var transaction = Db.BeginTransaction())
            {
                transaction.DeleteData(table, "CustomerId = @Id", new Dictionary<string, object> { ["Id"] = 1 });
                transaction.InsertData(table, new Dictionary<string, object> { ["CustomerId"] = 3, ["Name"] = "Northwind" });
                transaction.UpdateData(table, new Dictionary<string, object> { ["Region"] = "South" }, "CustomerId = 2");

                // The transaction sees its own work before the commit.
                Assert.Equal(2, transaction.ExecuteScalar<int>($"SELECT COUNT(*) FROM dbo.{table}"));
                Assert.Equal(["Fabrikam", "Northwind"], transaction.ExecuteQuery<string>($"SELECT Name FROM dbo.{table} ORDER BY CustomerId"));

                transaction.Commit();
            }

            Assert.Equal(2, Count(table));
            Assert.Equal("South", Database.Scalar($"SELECT Region FROM dbo.{table} WHERE CustomerId = 2"));
            Assert.Equal("Northwind", Database.Scalar($"SELECT Name FROM dbo.{table} WHERE CustomerId = 3"));
        }

        [Fact]
        public void Disposing_without_a_commit_rolls_back()
        {
            string table = CreateCustomers();

            using (var transaction = Db.BeginTransaction())
            {
                transaction.DeleteData(table, "1 = 1");
                transaction.ExecuteSQL($"INSERT INTO dbo.{table} VALUES (9, N'Gone', NULL)");
                Assert.Equal(1, transaction.ExecuteScalar<int>($"SELECT COUNT(*) FROM dbo.{table}"));
            }

            Assert.Equal(2, Count(table));
            Assert.Null(Database.Scalar($"SELECT Name FROM dbo.{table} WHERE CustomerId = 9"));
        }

        [Fact]
        public void A_failure_inside_a_using_block_rolls_back_the_earlier_work()
        {
            string table = CreateCustomers();

            Assert.ThrowsAny<SqlException>(() =>
            {
                using var transaction = Db.BeginTransaction();
                transaction.DeleteData(table, "CustomerId = 1");
                transaction.InsertData(table, new Dictionary<string, object> { ["CustomerId"] = 2, ["Name"] = "Duplicate key" });
                transaction.Commit();
            });

            Assert.Equal(2, Count(table));
        }

        [Fact]
        public void Rollback_undoes_the_work_and_ends_the_transaction()
        {
            string table = CreateCustomers();

            using var transaction = Db.BeginTransaction(IsolationLevel.Serializable);
            transaction.DeleteData(table, "1 = 1");
            transaction.Rollback();

            Assert.Equal(2, Count(table));
            Assert.Throws<InvalidOperationException>(() => transaction.DeleteData(table, "1 = 1"));
            Assert.Throws<InvalidOperationException>(() => transaction.Commit());
        }

        [Fact]
        public void Nothing_runs_after_a_commit_or_a_dispose()
        {
            string table = CreateCustomers();

            var transaction = Db.BeginTransaction();
            transaction.InsertData(table, new Dictionary<string, object> { ["CustomerId"] = 3 });
            transaction.Commit();

            Assert.Throws<InvalidOperationException>(() => transaction.ExecuteScalar<int>("SELECT 1"));
            Assert.Throws<InvalidOperationException>(() => transaction.Rollback());

            transaction.Dispose();
            transaction.Dispose();

            Assert.Throws<ObjectDisposedException>(() => transaction.ExecuteSQL("SELECT 1"));
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

            Db.ExecuteTransaction([$"DELETE FROM dbo.{table} WHERE CustomerId = 1", $"INSERT INTO dbo.{table} VALUES (3, N'Northwind', NULL)"]);
            Assert.Equal(2, Count(table));

            await Assert.ThrowsAnyAsync<SqlException>(() => Db.ExecuteTransactionAsync([$"DELETE FROM dbo.{table} WHERE CustomerId = 2", "SELECT 1/0"]));
            Assert.Equal(2, Count(table));
        }

        [Fact]
        public void A_savepoint_undoes_the_work_after_it_and_keeps_the_rest()
        {
            string table = CreateCustomers();

            using (var transaction = Db.BeginTransaction())
            {
                transaction.InsertData(table, new Dictionary<string, object> { ["CustomerId"] = 3, ["Name"] = "Kept" });
                transaction.Save("after third");
                transaction.DeleteData(table, "1 = 1");
                transaction.Save("[after third]"); // the same name again moves the savepoint
                transaction.InsertData(table, new Dictionary<string, object> { ["CustomerId"] = 4, ["Name"] = "Undone" });
                transaction.RollbackTo("after third");

                Assert.Equal(0, transaction.ExecuteScalar<int>($"SELECT COUNT(*) FROM dbo.{table}"));
                transaction.IndexCreate(table, "Name");
                transaction.Commit();
            }

            Assert.Equal(0, Count(table));
            Assert.Equal(["IX_Name"], Db.GetTableIndexs(table));
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
            Assert.Throws<ArgumentException>(() => transaction.Save("a.b"));
            Assert.Throws<ArgumentException>(() => transaction.RollbackTo(""));

            await transaction.CommitAsync(Ct);
            Assert.Equal(2, Count(table));
        }

        [Fact]
        public async Task BulkMerge_can_run_more_than_once_on_one_transaction()
        {
            string table = CreateCustomers();

            await using var transaction = await Db.BeginTransactionAsync(Ct);
            Assert.Equal(1, transaction.BulkMerge(Rows((3, "Northwind")), table, ["CustomerId"]));
            Assert.Equal(2, await transaction.BulkMergeAsync(Rows((3, "Northwind Traders"), (4, "Litware")), table, ["CustomerId"], cancellationToken: Ct));
            Assert.Equal(5, transaction.BulkMerge(Rows((5, "Adatum")), table, ["CustomerId"], deleteUnmatched: true)); // one inserted, four deleted
            await transaction.CommitAsync(Ct);

            Assert.Equal(1, Count(table));
            Assert.Equal("Adatum", Database.Scalar($"SELECT Name FROM dbo.{table} WHERE CustomerId = 5"));
        }

        [Fact]
        public void A_failed_commit_closes_the_transaction_with_a_reason()
        {
            string table = CreateCustomers();

            using var transaction = Db.BeginTransaction();
            transaction.DeleteData(table, "1 = 1");
            short spid = transaction.ExecuteScalar<short>("SELECT @@SPID");
            Database.Execute($"KILL {spid}");

            Assert.ThrowsAny<Exception>(() => transaction.Commit());
            var afterwards = Assert.Throws<InvalidOperationException>(() => transaction.Rollback());
            Assert.Contains("commit failed", afterwards.Message);
            Assert.Throws<InvalidOperationException>(() => transaction.ExecuteScalar<int>("SELECT 1"));

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

            await transaction.RollbackAsync(Ct);
            Assert.Equal(2, Count(table));
        }
    }
}

using System.Data;
using Microsoft.Data.SqlClient;

namespace SQLServerInteraction.Tests
{
    public class DataModificationTests(DatabaseFixture database) : IntegrationTest(database)
    {
        private string CreateCustomers() =>
            Database.CreateTable("CustomerId int NOT NULL PRIMARY KEY, Name nvarchar(50) NULL, Region nvarchar(20) NULL");

        private int Count(string table, string where = "1 = 1") =>
            (int)Database.Scalar($"SELECT COUNT(*) FROM dbo.{table} WHERE {where}")!;

        [Fact]
        public async Task InsertData_inserts_the_dictionary_as_one_row()
        {
            string table = CreateCustomers();

            await Db.InsertDataAsync("dbo." + table, new Dictionary<string, object> { ["CustomerId"] = 1, ["Name"] = "Contoso", ["Region"] = "West" });
            await Db.InsertDataAsync(table, new Dictionary<string, object> { ["CustomerId"] = 2, ["Name"] = "Fabrikam", ["Region"] = DBNull.Value });

            Assert.Equal("Contoso", Database.Scalar($"SELECT Name FROM dbo.{table} WHERE CustomerId = 1"));
            Assert.Null(Database.Scalar($"SELECT Region FROM dbo.{table} WHERE CustomerId = 2"));
        }

        public class Customer
        {
            public int CustomerId { get; set; }

            public string? Name { get; set; }

            public string? Region { get; set; }
        }

        [Fact]
        public async Task InsertData_of_T_inserts_the_properties_as_one_row()
        {
            string table = CreateCustomers();

            await Db.InsertDataAsync(new Customer { CustomerId = 1, Name = "Contoso", Region = "West" }, "dbo." + table);
            await Db.InsertDataAsync(new Customer { CustomerId = 2, Name = "Fabrikam" }, table);

            Assert.Equal("West", Database.Scalar($"SELECT Region FROM dbo.{table} WHERE CustomerId = 1"));
            Assert.Null(Database.Scalar($"SELECT Region FROM dbo.{table} WHERE CustomerId = 2"));
        }

        [Fact]
        public async Task UpdateData_changes_only_the_rows_matching_the_condition()
        {
            string table = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', N'West'), (2, N'Fabrikam', N'East')");

            await Db.UpdateDataAsync(table, new Dictionary<string, object> { ["Region"] = "North" }, "CustomerId = 1");
            await Db.UpdateDataAsync("dbo." + table, new Dictionary<string, object> { ["Name"] = "Fabrikam Ltd" }, "CustomerId = 2");

            Assert.Equal("North", Database.Scalar($"SELECT Region FROM dbo.{table} WHERE CustomerId = 1"));
            Assert.Equal("East", Database.Scalar($"SELECT Region FROM dbo.{table} WHERE CustomerId = 2"));
            Assert.Equal("Fabrikam Ltd", Database.Scalar($"SELECT Name FROM dbo.{table} WHERE CustomerId = 2"));
        }

        [Fact]
        public async Task DeleteData_deletes_only_the_rows_matching_the_condition()
        {
            string table = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', N'West'), (2, N'Fabrikam', N'East'), (3, N'Northwind', N'East')");

            await Db.DeleteDataAsync(table, "CustomerId = 1");
            await Db.DeleteDataAsync("dbo." + table, "Region = 'East' AND CustomerId = 2");

            Assert.Equal(1, Count(table));
            Assert.Equal(1, Count(table, "CustomerId = 3"));
        }

        [Fact]
        public async Task ExecuteSQL_and_ExecuteNonQueryWithParameters_run_commands()
        {
            string table = CreateCustomers();

            await Db.ExecuteSQLAsync($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', N'West')");
            await Db.ExecuteSQLAsync($"INSERT INTO dbo.{table} VALUES (2, N'Fabrikam', N'East')");
            await Db.ExecuteNonQueryWithParametersAsync($"UPDATE dbo.{table} SET Region = @Region WHERE CustomerId = @Id",
                new Dictionary<string, object> { ["Region"] = "South", ["Id"] = 1 });
            await Db.ExecuteNonQueryWithParametersAsync($"DELETE FROM dbo.{table} WHERE CustomerId = @Id",
                new Dictionary<string, object> { ["Id"] = 2 });

            Assert.Equal("South", Database.Scalar($"SELECT Region FROM dbo.{table} WHERE CustomerId = 1"));
            Assert.Equal(1, Count(table));
        }

        [Fact]
        public async Task ExecuteStoredProcedure_runs_the_procedure_with_its_parameters()
        {
            string table = CreateCustomers();
            string procedure = "P_" + Guid.NewGuid().ToString("N");
            Database.Execute($"CREATE PROCEDURE dbo.{procedure} @Id int, @Name nvarchar(50) AS INSERT INTO dbo.{table} (CustomerId, Name) VALUES (@Id, @Name)");

            await Db.ExecuteStoredProcedureAsync("dbo." + procedure, [new SqlParameter("@Id", 1), new SqlParameter("@Name", "Contoso")]);
            await Db.ExecuteStoredProcedureAsync("dbo." + procedure, [new SqlParameter("@Id", 2), new SqlParameter("@Name", "Fabrikam")]);

            Assert.Equal(2, Count(table));
            Assert.Equal(["@Id", "@Name"], await Db.GetStoredProcedureParametersAsync("dbo." + procedure));
        }

        [Fact]
        public async Task ExecuteScriptFromFileAsync_runs_the_file_as_one_batch()
        {
            string table = CreateCustomers();
            string path = Path.Combine(Path.GetTempPath(), $"sqlsi-{Guid.NewGuid():N}.sql");
            await File.WriteAllTextAsync(path,
                $"INSERT INTO dbo.{table} VALUES (1, N'Contoso', NULL);\nINSERT INTO dbo.{table} VALUES (2, N'Fabrikam', NULL);\n",
                TestContext.Current.CancellationToken);

            try
            {
                await Db.ExecuteScriptFromFileAsync(path);
            }
            finally
            {
                File.Delete(path);
            }

            Assert.Equal(2, Count(table));
        }

        private static DataTable Rows(params (int Id, string Name)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("CustomerId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Region", typeof(string));
            foreach (var (id, name) in rows) table.Rows.Add(id, name, "West");
            return table;
        }

        [Fact]
        public async Task BulkCopy_appends_and_flushes()
        {
            string table = CreateCustomers();

            await Db.BulkCopyAsync(Rows((1, "Contoso"), (2, "Fabrikam")), "dbo." + table);
            Assert.Equal(2, Count(table));

            await Db.BulkCopyAsync(Rows((3, "Northwind")), table, flushTable: true);
            Assert.Equal(1, Count(table));

            await Db.BulkCopyAsync(Rows((4, "Litware")), table, flushTable: true, flushWhereClauseCondition: "CustomerId = 3", batchSize: 1, useTransaction: false);
            Assert.Equal(1, Count(table, "CustomerId = 4"));
            Assert.Equal(1, Count(table));
        }

        [Fact]
        public async Task BulkCopy_rolls_back_the_flush_when_the_copy_fails()
        {
            string table = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', N'West')");

            await Assert.ThrowsAnyAsync<Exception>(() => Db.BulkCopyAsync(Rows((2, "A"), (2, "B")), table, flushTable: true));

            Assert.Equal(1, Count(table, "CustomerId = 1"));
        }

        private string? Value(string table, string column, int customerId) =>
            (string?)Database.Scalar($"SELECT [{column}] FROM dbo.{table} WHERE CustomerId = {customerId}");

        [Fact]
        public async Task MergeData_updates_matched_rows_inserts_the_rest_and_deletes_when_asked()
        {
            string target = CreateCustomers();
            string source = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{target} VALUES (1, N'Contoso', N'West'), (2, N'Fabrikam', N'East')");
            Database.Execute($"INSERT INTO dbo.{source} VALUES (2, N'Fabrikam Ltd', N'East'), (3, N'Northwind', N'North')");

            int rows = await Db.MergeDataAsync(source, "dbo." + target, ["CustomerId"], ["Name", "Region"]);

            Assert.Equal(2, rows);
            Assert.Equal(3, Count(target));
            Assert.Equal("Contoso", Value(target, "Name", 1));
            Assert.Equal("Fabrikam Ltd", Value(target, "Name", 2));
            Assert.Equal("North", Value(target, "Region", 3));

            Database.Execute($"UPDATE dbo.{source} SET Name = N'Northwind Traders', Region = N'South' WHERE CustomerId = 3");
            rows = await Db.MergeDataAsync("dbo." + source, target, ["[CustomerId]"], ["Region"], deleteUnmatched: true, useTransaction: false);

            Assert.Equal(3, rows); // two matched rows updated, one unmatched row deleted
            Assert.Equal(2, Count(target));
            Assert.Equal(0, Count(target, "CustomerId = 1"));
            Assert.Equal("Northwind", Value(target, "Name", 3)); // Name was not a value column
            Assert.Equal("South", Value(target, "Region", 3));
        }

        [Fact]
        public async Task MergeData_with_no_value_columns_inserts_the_missing_keys_only()
        {
            string target = CreateCustomers();
            string source = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{target} VALUES (1, N'Contoso', N'West')");
            Database.Execute($"INSERT INTO dbo.{source} VALUES (1, N'Changed', N'Changed'), (2, N'Fabrikam', N'East')");

            int rows = await Db.MergeDataAsync(source, target, ["CustomerId"], []);

            Assert.Equal(1, rows);
            Assert.Equal("Contoso", Value(target, "Name", 1));
            Assert.Null(Value(target, "Name", 2));
        }

        [Fact]
        public async Task BulkMerge_copies_the_rows_and_merges_them_by_key()
        {
            string table = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', N'East'), (2, N'Fabrikam', N'East')");

            int rows = await Db.BulkMergeAsync(Rows((2, "Fabrikam Ltd"), (3, "Northwind")), "dbo." + table, ["CustomerId"]);

            Assert.Equal(2, rows);
            Assert.Equal(3, Count(table));
            Assert.Equal("Contoso", Value(table, "Name", 1));
            Assert.Equal("Fabrikam Ltd", Value(table, "Name", 2));
            Assert.Equal("West", Value(table, "Region", 2));

            rows = await Db.BulkMergeAsync(Rows((3, "Northwind Traders")), table, ["[CustomerId]"], deleteUnmatched: true, timeout: 60, batchSize: 1, useTransaction: false);

            Assert.Equal(3, rows); // one matched row updated, two unmatched rows deleted
            Assert.Equal(1, Count(table));
            Assert.Equal("Northwind Traders", Value(table, "Name", 3));
        }

        [Fact]
        public async Task BulkMerge_matches_columns_by_name_and_leaves_the_others_alone()
        {
            string table = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', N'East')");
            var rows = new DataTable();
            rows.Columns.Add("Name", typeof(string));
            rows.Columns.Add("CustomerId", typeof(int));
            rows.Rows.Add("Contoso Ltd", 1);
            rows.Rows.Add("Fabrikam", 2);

            Assert.Equal(2, await Db.BulkMergeAsync(rows, table, ["CustomerId"]));

            Assert.Equal("Contoso Ltd", Value(table, "Name", 1));
            Assert.Equal("East", Value(table, "Region", 1));
            Assert.Null(Value(table, "Region", 2));
        }

        [Fact]
        public async Task BulkMerge_into_an_identity_table_keys_on_another_column_and_leaves_the_identity_out()
        {
            string table = Database.CreateTable("CustomerId int IDENTITY(10, 10) NOT NULL PRIMARY KEY, Name nvarchar(50) NOT NULL UNIQUE, Region nvarchar(20) NULL");
            Database.Execute($"INSERT INTO dbo.{table} (Name, Region) VALUES (N'Contoso', N'West')");
            var rows = new DataTable();
            rows.Columns.Add("Name", typeof(string));
            rows.Columns.Add("Region", typeof(string));
            rows.Rows.Add("Contoso", "East");
            rows.Rows.Add("Fabrikam", "North");

            Assert.Equal(2, await Db.BulkMergeAsync(rows, table, ["Name"]));

            Assert.Equal("East", Value(table, "Region", 10));
            Assert.Equal("Fabrikam", Value(table, "Name", 20));

            // The identity column cannot be carried: SQL Server refuses the merge's insert into it even when every row matches.
            await Assert.ThrowsAnyAsync<SqlException>(() => Db.BulkMergeAsync(Rows((10, "Contoso")), table, ["CustomerId"]));
            Assert.Equal(2, Count(table));
        }

        [Fact]
        public async Task A_failed_merge_changes_nothing()
        {
            string table = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', N'West')");

            // Two rows with one key: MERGE refuses to update the same row twice.
            await Assert.ThrowsAnyAsync<SqlException>(() => Db.BulkMergeAsync(Rows((1, "A"), (1, "B")), table, ["CustomerId"]));
            await Assert.ThrowsAnyAsync<SqlException>(() => Db.BulkMergeAsync(Rows((1, "A"), (1, "B")), table, ["CustomerId"], useTransaction: false));
            // A column the table does not have fails before anything is copied.
            var rows = Rows((1, "A"));
            rows.Columns.Add("NoSuchColumn", typeof(int));
            await Assert.ThrowsAnyAsync<SqlException>(() => Db.BulkMergeAsync(rows, table, ["CustomerId"]));

            Assert.Equal("Contoso", Value(table, "Name", 1));
            Assert.Equal(1, Count(table));
        }

        [Fact]
        public async Task A_merge_with_bad_arguments_is_refused_before_anything_runs()
        {
            string table = CreateCustomers();

            await Assert.ThrowsAsync<ArgumentException>(() => Db.MergeDataAsync(table, table, [], ["Name"]));
            await Assert.ThrowsAsync<ArgumentException>(() => Db.MergeDataAsync(table, table, ["CustomerId"], ["Name", "customerid"]));
            await Assert.ThrowsAsync<ArgumentException>(() => Db.MergeDataAsync("[" + table, table, ["CustomerId"], []));
            await Assert.ThrowsAsync<ArgumentException>(() => Db.BulkMergeAsync(Rows((1, "A")), table, []));
            await Assert.ThrowsAsync<ArgumentException>(() => Db.BulkMergeAsync(Rows((1, "A")), table, ["NoSuchColumn"]));
            await Assert.ThrowsAsync<ArgumentException>(() => Db.BulkMergeAsync(new DataTable(), table, ["CustomerId"]));
            await Assert.ThrowsAsync<ArgumentException>(() => Db.BulkMergeAsync(Rows((1, "A")), "a.b.c.d", ["CustomerId"]));

            Assert.Equal(0, Count(table));
        }
    }
}

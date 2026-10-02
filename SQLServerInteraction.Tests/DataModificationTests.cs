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

            Db.InsertData("dbo." + table, new Dictionary<string, object> { ["CustomerId"] = 1, ["Name"] = "Contoso", ["Region"] = "West" });
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

            Db.InsertData(new Customer { CustomerId = 1, Name = "Contoso", Region = "West" }, "dbo." + table);
            await Db.InsertDataAsync(new Customer { CustomerId = 2, Name = "Fabrikam" }, table);

            Assert.Equal("West", Database.Scalar($"SELECT Region FROM dbo.{table} WHERE CustomerId = 1"));
            Assert.Null(Database.Scalar($"SELECT Region FROM dbo.{table} WHERE CustomerId = 2"));
        }

        [Fact]
        public async Task UpdateData_changes_only_the_rows_matching_the_condition()
        {
            string table = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', N'West'), (2, N'Fabrikam', N'East')");

            Db.UpdateData(table, new Dictionary<string, object> { ["Region"] = "North" }, "CustomerId = 1");
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

            Db.DeleteData(table, "CustomerId = 1");
            await Db.DeleteDataAsync("dbo." + table, "Region = 'East' AND CustomerId = 2");

            Assert.Equal(1, Count(table));
            Assert.Equal(1, Count(table, "CustomerId = 3"));
        }

        [Fact]
        public async Task ExecuteSQL_and_ExecuteNonQueryWithParameters_run_commands()
        {
            string table = CreateCustomers();

            Db.ExecuteSQL($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', N'West')");
            await Db.ExecuteSQLAsync($"INSERT INTO dbo.{table} VALUES (2, N'Fabrikam', N'East')");
            Db.ExecuteNonQueryWithParameters($"UPDATE dbo.{table} SET Region = @Region WHERE CustomerId = @Id",
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

            Db.ExecuteStoredProcedure("dbo." + procedure, [new SqlParameter("@Id", 1), new SqlParameter("@Name", "Contoso")]);
            await Db.ExecuteStoredProcedureAsync("dbo." + procedure, [new SqlParameter("@Id", 2), new SqlParameter("@Name", "Fabrikam")]);

            Assert.Equal(2, Count(table));
            Assert.Equal(["@Id", "@Name"], Db.GetStoredProcedureParameters("dbo." + procedure));
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

            Db.BulkCopy(Rows((1, "Contoso"), (2, "Fabrikam")), "dbo." + table);
            Assert.Equal(2, Count(table));

            await Db.BulkCopyAsync(Rows((3, "Northwind")), table, flushTable: true);
            Assert.Equal(1, Count(table));

            Db.BulkCopy(Rows((4, "Litware")), table, flushTable: true, flushWhereClauseCondition: "CustomerId = 3", batchSize: 1, useTransaction: false);
            Assert.Equal(1, Count(table, "CustomerId = 4"));
            Assert.Equal(1, Count(table));
        }

        [Fact]
        public void BulkCopy_rolls_back_the_flush_when_the_copy_fails()
        {
            string table = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Contoso', N'West')");

            Assert.ThrowsAny<Exception>(() => Db.BulkCopy(Rows((2, "A"), (2, "B")), table, flushTable: true));

            Assert.Equal(1, Count(table, "CustomerId = 1"));
        }
    }
}

using System.Data;

namespace SQLServerInteraction.Tests
{
    /// <summary>
    /// BulkCopy's columnMappings, on a real server.
    /// </summary>
    public class BulkCopyMappingTests(DatabaseFixture database) : IntegrationTest(database)
    {
        private string CreateCustomers() =>
            Database.CreateTable("Id int NOT NULL, Name nvarchar(50) NULL, [Sales Region] nvarchar(20) NULL, Notes nvarchar(50) NULL");

        private static DataTable Source()
        {
            var table = new DataTable();
            table.Columns.Add("Region", typeof(string));
            table.Columns.Add("CustomerName", typeof(string));
            table.Columns.Add("Code", typeof(int));
            table.Columns.Add("Ignored", typeof(string));
            table.Rows.Add("West", "Contoso", 1, "not copied");
            table.Rows.Add("East", "Fabrikam", 2, "not copied");
            return table;
        }

        private object? Value(string table, string column, int id) =>
            Database.Scalar($"SELECT [{column}] FROM dbo.{table} WHERE Id = @Id", new Microsoft.Data.SqlClient.SqlParameter("@Id", id));

        private int Count(string table) => (int)Database.Scalar($"SELECT COUNT(*) FROM dbo.{table}")!;

        [Fact]
        public async Task Mapped_columns_are_copied_by_name_whatever_their_order()
        {
            string table = CreateCustomers();

            await Db.BulkCopyAsync(Source(), table, flushTable: false, flushWhereClauseCondition: null, flushParameters: null,
                columnMappings: new Dictionary<string, string> { ["Code"] = "Id", ["CustomerName"] = "Name", ["Region"] = "Sales Region" });

            Assert.Equal(2, Count(table));
            Assert.Equal("Fabrikam", Value(table, "Name", 2));
            Assert.Equal("West", Value(table, "Sales Region", 1));
            Assert.Null(Value(table, "Notes", 1));
        }

        [Fact]
        public async Task Only_the_mapped_subset_is_copied_and_the_rest_take_their_defaults()
        {
            string table = CreateCustomers();

            await Db.BulkCopyAsync(Source(), table, flushTable: false, flushWhereClauseCondition: null, flushParameters: null,
                columnMappings: new Dictionary<string, string> { ["Code"] = "Id" });

            Assert.Equal(2, Count(table));
            Assert.Null(Value(table, "Name", 1));
            Assert.Null(Value(table, "Sales Region", 2));
        }

        [Fact]
        public async Task A_bracketed_destination_loses_its_brackets_and_a_source_matches_without_regard_to_case()
        {
            string table = CreateCustomers();

            await Db.BulkCopyAsync(Source(), table, flushTable: false, flushWhereClauseCondition: null, flushParameters: null,
                columnMappings: new Dictionary<string, string> { ["code"] = "Id", ["REGION"] = "[Sales Region]" });

            Assert.Equal("East", Value(table, "Sales Region", 2));
        }

        [Fact]
        public async Task A_destination_must_match_the_column_name_in_case_and_a_mismatch_rolls_back_the_flush()
        {
            string table = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{table} (Id) VALUES (99)");

            await Assert.ThrowsAsync<InvalidOperationException>(() => Db.BulkCopyAsync(Source(), table, flushTable: true, flushWhereClauseCondition: null, flushParameters: null,
                columnMappings: new Dictionary<string, string> { ["Code"] = "ID" }));

            Assert.Equal(1, Count(table));
        }

        [Fact]
        public async Task A_destination_the_table_does_not_have_fails_on_the_server_and_rolls_back_the_flush()
        {
            string table = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{table} (Id) VALUES (99)");

            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => Db.BulkCopyAsync(Source(), table, flushTable: true, flushWhereClauseCondition: null, flushParameters: null,
                columnMappings: new Dictionary<string, string> { ["Code"] = "NoSuchColumn" }));

            Assert.Equal(1, Count(table));
        }

        public static TheoryData<string, Dictionary<string, string>> InvalidMappings => new()
        {
            { "empty", new Dictionary<string, string>() },
            { "unknown source column", new Dictionary<string, string> { ["Code"] = "Id", ["Missing"] = "Name" } },
            { "empty destination", new Dictionary<string, string> { ["Code"] = "" } },
            { "whitespace destination", new Dictionary<string, string> { ["Code"] = "Id", ["CustomerName"] = "  " } },
        };

        [Theory]
        [MemberData(nameof(InvalidMappings))]
        public async Task Invalid_mappings_throw_before_the_table_is_flushed(string reason, Dictionary<string, string> mappings)
        {
            string table = CreateCustomers();
            Database.Execute($"INSERT INTO dbo.{table} (Id) VALUES (99)");

            await Assert.ThrowsAsync<ArgumentException>(() => Db.BulkCopyAsync(Source(), table, true, null, null, columnMappings: mappings));
            await Assert.ThrowsAsync<ArgumentException>(() => Db.BulkCopyAsync(Source(), table, true, null, null, columnMappings: mappings));

            Assert.True(Count(table) == 1, reason);
        }
    }
}

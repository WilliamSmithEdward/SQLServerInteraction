using System.Data;
using Microsoft.Data.SqlClient;

namespace SQLServerInteraction.Tests
{
    /// <summary>
    /// Names that would have changed the SQL before 2.0.0 are now read as names,
    /// on a real server.
    /// </summary>
    public class InjectionTests(DatabaseFixture database) : IntegrationTest(database)
    {
        private string CreateVictim() => Database.CreateTable("Id int NOT NULL");

        private bool Exists(string table) =>
            (int)Database.Scalar("SELECT COUNT(*) FROM sys.tables WHERE name = @name", new SqlParameter("@name", table))! == 1;

        /// <summary>A table named <c>x]; DROP TABLE &lt;victim&gt;; --</c>, and the victim it names.</summary>
        private (string Hostile, string Victim) CreateHostileTable()
        {
            string victim = CreateVictim();
            string hostile = Database.CreateTable("Id int NOT NULL PRIMARY KEY, [Customer Name] nvarchar(50) NULL", $"x]; DROP TABLE {victim}; --");
            return (hostile, victim);
        }

        [Fact]
        public async Task A_hostile_table_name_is_a_name_to_every_method_that_takes_one()
        {
            var (hostile, victim) = CreateHostileTable();

            Db.InsertData(hostile, new Dictionary<string, object> { ["Id"] = 1, ["Customer Name"] = "Contoso" });
            await Db.InsertDataAsync("dbo." + SqlIdentifier.QuotePart(hostile), new Dictionary<string, object> { ["Id"] = 2, ["[Customer Name]"] = "Fabrikam" });
            Db.UpdateData(hostile, new Dictionary<string, object> { ["Customer Name"] = "Contoso Ltd" }, "Id = 1");
            await Db.UpdateDataAsync(hostile, new Dictionary<string, object> { ["Customer Name"] = "Fabrikam Ltd" }, "Id = 2");

            Assert.True(Db.DoesTableExist(hostile));
            Assert.Equal(2, Db.GetTableRowCount(hostile));
            Assert.Equal(["Id", "Customer Name"], Db.GetTableSchema(hostile).Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            Assert.Equal(["Id", "Customer Name"], Db.GetTableColumns(hostile).Keys);
            Assert.Equal(["Id", "Customer Name"], Db.GetColumnNames(hostile));
            Assert.Equal("Id", Db.GetTablePrimaryKeyColumn(hostile));

            Db.IndexCreate(hostile, "Customer Name");
            Assert.Equal(["IX_Customer Name"], Db.GetTableIndexs(hostile));
            Db.IndexDrop(hostile, "IX_Customer Name");
            Assert.Empty(Db.GetTableIndexs(hostile));

            Db.DeleteData(hostile, "Id = 1");
            await Db.DeleteDataAsync(hostile, "Id = 2");
            Assert.Equal(0, Db.GetTableRowCount(hostile));

            Assert.True(Exists(victim));
        }

        [Fact]
        public async Task BulkCopy_reads_a_hostile_table_name_as_a_name()
        {
            var (hostile, victim) = CreateHostileTable();
            var rows = new DataTable();
            rows.Columns.Add("Id", typeof(int));
            rows.Columns.Add("Customer Name", typeof(string));
            rows.Rows.Add(1, "Contoso");

            Db.BulkCopy(rows, hostile, flushTable: true);
            await Db.BulkCopyAsync(rows, hostile, flushTable: true, flushWhereClauseCondition: "Id = @Id", flushParameters: new Dictionary<string, object> { ["Id"] = 1 });

            Assert.Equal(1, Db.GetTableRowCount(hostile));
            Assert.True(Exists(victim));
        }

        [Fact]
        public async Task A_hostile_column_name_is_a_name()
        {
            string victim = CreateVictim();
            string column = $"c]] = 1; DROP TABLE {victim}; --";
            string table = Database.CreateTable($"Id int NOT NULL, [{column}] int NULL");

            Db.InsertData(table, new Dictionary<string, object> { ["Id"] = 1, [$"[{column}]"] = 5 });
            await Db.InsertDataAsync(table, new Dictionary<string, object> { ["Id"] = 2, [$"[{column}]"] = 7 });
            Db.UpdateData(table, new Dictionary<string, object> { [$"[{column}]"] = 6 }, "Id = 1");

            Assert.Equal(6, Database.Scalar($"SELECT [{column}] FROM dbo.{table} WHERE Id = 1"));
            Assert.Equal(7, Database.Scalar($"SELECT [{column}] FROM dbo.{table} WHERE Id = 2"));
            Assert.True(Exists(victim));
        }

        /// <summary>The victim a ColumnAttribute names, which must be a constant.</summary>
        private const string ColumnAttributeVictim = "VictimOfInsertDataOfT";

        public class HostileRow
        {
            public int Id { get; set; }

            [SQLServerInstance.Column("v]; DROP TABLE " + ColumnAttributeVictim + "; --")]
            public string? Value { get; set; }
        }

        [Fact]
        public async Task InsertData_of_T_reads_hostile_table_and_column_names_as_names()
        {
            Database.Execute($"CREATE TABLE dbo.{ColumnAttributeVictim} (Id int NOT NULL)");
            string victim = CreateVictim();
            string table = Database.CreateTable(
                $"Id int NOT NULL, [v]]; DROP TABLE {ColumnAttributeVictim}; --] nvarchar(50) NULL", $"x]; DROP TABLE {victim}; --");

            Db.InsertData(new HostileRow { Id = 1, Value = "a" }, table);
            await Db.InsertDataAsync(new HostileRow { Id = 2, Value = "b" }, "dbo." + SqlIdentifier.QuotePart(table));

            Assert.Equal(2, Db.GetTableRowCount(table));
            Assert.True(Exists(victim));
            Assert.True(Exists(ColumnAttributeVictim));
        }

        [Fact]
        public void A_quote_in_a_looked_up_name_does_not_end_the_string()
        {
            CreateVictim();

            // Before 2.0.0 this became TABLE_NAME = 'x' OR 1 = 1 --', true in any database with a table.
            Assert.False(Db.DoesTableExist("x' OR 1 = 1 --"));
            Assert.Empty(Db.GetTableColumns("x' OR 1 = 1 --"));
            Assert.Null(Db.GetTablePrimaryKeyColumn("x' OR 1 = 1 --"));
            Assert.Empty(Db.GetTableIndexs("x') OR 1 = 1 --"));
        }

        [Fact]
        public void A_schema_qualified_name_matches_that_schema_only()
        {
            string schema = "Sales Ops " + Guid.NewGuid().ToString("N")[..8];
            string table = "T_" + Guid.NewGuid().ToString("N");
            Database.Execute($"EXEC (N'CREATE SCHEMA {SqlIdentifier.QuotePart(schema).Replace("'", "''")}')");
            Database.Execute($"CREATE TABLE {SqlIdentifier.QuotePart(schema)}.{table} (Code int NOT NULL PRIMARY KEY, Label nvarchar(10) NULL)");
            Database.Execute($"CREATE TABLE dbo.{table} (Id int NOT NULL PRIMARY KEY)");
            string qualified = $"[{schema}].{table}";

            Assert.True(Db.DoesTableExist(qualified));
            Assert.True(Db.DoesTableExist($"{Database.DatabaseName}.dbo.{table}"));
            Assert.False(Db.DoesTableExist($"guest.{table}"));
            Assert.Equal(["Code", "Label"], Db.GetTableColumns(qualified).Keys);
            Assert.Equal(["Code", "Label"], Db.GetColumnNames(qualified));
            Assert.Equal(["Id"], Db.GetColumnNames("dbo." + table));
            Assert.Equal("Code", Db.GetTablePrimaryKeyColumn(qualified));
            Assert.Equal("Id", Db.GetTablePrimaryKeyColumn("[dbo]." + table));
        }

        [Fact]
        public async Task Condition_values_can_be_parameters()
        {
            string table = Database.CreateTable("Id int NOT NULL, Name nvarchar(50) NULL");
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'O''Brien'), (2, N'Smith'), (3, NULL)");

            Db.UpdateData(table, new Dictionary<string, object> { ["Id"] = 10 }, "Name = @name", new Dictionary<string, object> { ["name"] = "O'Brien" });
            await Db.UpdateDataAsync(table, new Dictionary<string, object> { ["Id"] = 20 }, "Name = @name", new Dictionary<string, object> { ["@name"] = "Smith" });
            Assert.Equal(2, Database.Scalar($"SELECT COUNT(*) FROM dbo.{table} WHERE Id IN (10, 20)"));

            Db.DeleteData(table, "Id = @id", new Dictionary<string, object> { ["id"] = 10 });
            await Db.DeleteDataAsync(table, "Name IS NULL OR Name = @name", new Dictionary<string, object> { ["name"] = null! });
            Assert.Equal(1, Database.Scalar($"SELECT COUNT(*) FROM dbo.{table}"));
        }

        [Fact]
        public async Task An_empty_condition_is_refused_instead_of_affecting_every_row()
        {
            string table = Database.CreateTable("Id int NOT NULL");
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1), (2)");
            var rows = new DataTable();
            rows.Columns.Add("Id", typeof(int));

            Assert.Throws<ArgumentException>(() => Db.DeleteData(table, ""));
            await Assert.ThrowsAsync<ArgumentException>(() => Db.DeleteDataAsync(table, "  "));
            Assert.Throws<ArgumentException>(() => Db.UpdateData(table, new Dictionary<string, object> { ["Id"] = 0 }, ""));
            await Assert.ThrowsAsync<ArgumentException>(() => Db.UpdateDataAsync(table, new Dictionary<string, object> { ["Id"] = 0 }, "\t"));
            Assert.Throws<ArgumentException>(() => Db.BulkCopy(rows, table, flushTable: true, flushWhereClauseCondition: ""));
            await Assert.ThrowsAsync<ArgumentException>(() => Db.BulkCopyAsync(rows, table, flushTable: true, flushWhereClauseCondition: " "));
            Assert.Equal(2, Database.Scalar($"SELECT COUNT(*) FROM dbo.{table} WHERE Id IN (1, 2)"));

            Db.DeleteData(table, "1 = 1");
            Assert.Equal(0, Database.Scalar($"SELECT COUNT(*) FROM dbo.{table}"));
        }

        [Fact]
        public void A_malformed_name_is_refused_before_anything_runs()
        {
            string table = Database.CreateTable("Id int NOT NULL");

            Assert.Throws<ArgumentException>(() => Db.InsertData("[" + table, new Dictionary<string, object> { ["Id"] = 1 }));
            Assert.Throws<ArgumentException>(() => Db.InsertData(table, new Dictionary<string, object> { ["a.b"] = 1 }));
            Assert.Throws<ArgumentException>(() => Db.InsertData(table, new Dictionary<string, object>()));
            Assert.Throws<ArgumentException>(() => Db.GetTableRowCount("a.b.c.d"));
            Assert.Throws<ArgumentException>(() => Db.DoesTableExist(""));
            Assert.Equal(0, Database.Scalar($"SELECT COUNT(*) FROM dbo.{table}"));
        }
    }
}

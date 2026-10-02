using System.Data;

namespace SQLServerInteraction.Tests
{
    public class SchemaTests(DatabaseFixture database) : IntegrationTest(database)
    {
        private string CreateProducts() =>
            Database.CreateTable("ProductId int NOT NULL PRIMARY KEY, Name nvarchar(50) NOT NULL, Price decimal(10, 2) NULL");

        [Fact]
        public void Table_lookups_find_a_table_by_its_bare_name()
        {
            string table = CreateProducts();

            Assert.True(Db.DoesTableExist(table));
            Assert.False(Db.DoesTableExist("T_" + Guid.NewGuid().ToString("N")));
            Assert.Contains(table, Db.GetTableNames());
            Assert.Equal(["ProductId", "Name", "Price"], Db.GetColumnNames(table));
            Assert.Equal(new Dictionary<string, string> { ["ProductId"] = "int", ["Name"] = "nvarchar", ["Price"] = "decimal" }, Db.GetTableColumns(table));
            Assert.Equal("ProductId", Db.GetTablePrimaryKeyColumn(table));
        }

        [Fact]
        public void GetTableRowCount_and_GetTableSchema_read_the_table()
        {
            string table = CreateProducts();
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Widget', 2.50), (2, N'Gadget', NULL)");

            Assert.Equal(2, Db.GetTableRowCount(table));
            Assert.Equal(2, Db.GetTableRowCount("dbo." + table));

            DataTable schema = Db.GetTableSchema("dbo." + table);
            Assert.Equal(0, schema.Rows.Count);
            Assert.Equal(["ProductId", "Name", "Price"], schema.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            Assert.Equal("ProductId", Assert.Single(schema.PrimaryKey).ColumnName);
        }

        [Fact]
        public void IndexCreate_and_IndexDrop_manage_a_nonclustered_index()
        {
            string table = CreateProducts();

            Db.IndexCreate(table, "Name");
            Assert.Equal(["IX_Name"], Db.GetTableIndexs(table));

            Db.IndexDrop(table, "IX_Name");
            Assert.Empty(Db.GetTableIndexs(table));
        }

        [Fact]
        public void GetStoredProcedures_lists_a_new_procedure()
        {
            string procedure = "P_" + Guid.NewGuid().ToString("N");
            Database.Execute($"CREATE PROCEDURE dbo.{procedure} AS SELECT 1");

            Assert.Contains(procedure, Db.GetStoredProcedures());
        }

        [Fact]
        public void Database_information_describes_the_connected_database()
        {
            Dictionary<string, string> info = Db.GetDatabaseInformation();

            Assert.Equal(Database.DatabaseName, info["DatabaseName"]);
            Assert.True(Db.DoesDatabaseExist(Database.DatabaseName));
            Assert.True(Db.DoesDatabaseExist(Database.DatabaseName.ToLowerInvariant()));
            Assert.False(Db.DoesDatabaseExist("SQLSI_Missing_" + Guid.NewGuid().ToString("N")));
            Assert.True(Db.GetDatabaseSizeInBytes() > 0);
        }
    }
}

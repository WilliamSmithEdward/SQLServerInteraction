using System.Data;

namespace SQLServerInteraction.Tests
{
    public class SchemaTests(DatabaseFixture database) : IntegrationTest(database)
    {
        private string CreateProducts() =>
            Database.CreateTable("ProductId int NOT NULL PRIMARY KEY, Name nvarchar(50) NOT NULL, Price decimal(10, 2) NULL");

        [Fact]
        public async Task Table_lookups_find_a_table_by_its_bare_name()
        {
            string table = CreateProducts();

            Assert.True(await Db.DoesTableExistAsync(table));
            Assert.False(await Db.DoesTableExistAsync("T_" + Guid.NewGuid().ToString("N")));
            Assert.Contains(table, await Db.GetTableNamesAsync());
            Assert.Equal(["ProductId", "Name", "Price"], await Db.GetColumnNamesAsync(table));
            Assert.Equal(new Dictionary<string, string> { ["ProductId"] = "int", ["Name"] = "nvarchar", ["Price"] = "decimal" }, await Db.GetTableColumnsAsync(table));
            Assert.Equal("ProductId", await Db.GetTablePrimaryKeyColumnAsync(table));
        }

        [Fact]
        public async Task GetTableRowCount_and_GetTableSchema_read_the_table()
        {
            string table = CreateProducts();
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'Widget', 2.50), (2, N'Gadget', NULL)");

            Assert.Equal(2, await Db.GetTableRowCountAsync(table));
            Assert.Equal(2, await Db.GetTableRowCountAsync("dbo." + table));

            DataTable schema = await Db.GetTableSchemaAsync("dbo." + table);
            Assert.Equal(0, schema.Rows.Count);
            Assert.Equal(["ProductId", "Name", "Price"], schema.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            Assert.Equal("ProductId", Assert.Single(schema.PrimaryKey).ColumnName);
        }

        [Fact]
        public async Task IndexCreate_and_IndexDrop_manage_a_nonclustered_index()
        {
            string table = CreateProducts();

            await Db.IndexCreateAsync(table, "Name");
            Assert.Equal(["IX_Name"], await Db.GetTableIndexesAsync(table));

            await Db.IndexDropAsync(table, "IX_Name");
            Assert.Empty(await Db.GetTableIndexesAsync(table));
        }

        [Fact]
        public async Task GetStoredProcedures_lists_a_new_procedure()
        {
            string procedure = "P_" + Guid.NewGuid().ToString("N");
            Database.Execute($"CREATE PROCEDURE dbo.{procedure} AS SELECT 1");

            Assert.Contains(procedure, await Db.GetStoredProceduresAsync());
        }

        [Fact]
        public async Task Database_information_describes_the_connected_database()
        {
            Dictionary<string, string> info = await Db.GetDatabaseInformationAsync();

            Assert.Equal(Database.DatabaseName, info["DatabaseName"]);
            Assert.True(await Db.DoesDatabaseExistAsync(Database.DatabaseName));
            Assert.True(await Db.DoesDatabaseExistAsync(Database.DatabaseName.ToLowerInvariant()));
            Assert.False(await Db.DoesDatabaseExistAsync("SQLSI_Missing_" + Guid.NewGuid().ToString("N")));
            Assert.True(await Db.GetDatabaseSizeInBytesAsync() > 0);
        }
    }
}

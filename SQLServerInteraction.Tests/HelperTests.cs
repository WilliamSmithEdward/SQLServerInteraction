using System.Data;
using Microsoft.Data.SqlClient;

namespace SQLServerInteraction.Tests
{
    public class HelperTests
    {
        [Theory]
        [InlineData("Name", "@Name")]
        [InlineData("@Name", "@Name")]
        public void A_parameter_name_gets_an_at_sign_only_when_it_has_none(string key, string name)
        {
            Assert.Equal(name, CommandParameters.Name(key));
        }

        [Fact]
        public void CommandParameters_sends_null_as_DBNull()
        {
            using var command = new SqlCommand();

            CommandParameters.Add(command, new Dictionary<string, object> { ["A"] = 1, ["@B"] = null! });

            Assert.Equal(["@A", "@B"], command.Parameters.Cast<SqlParameter>().Select(p => p.ParameterName));
            Assert.Equal(DBNull.Value, command.Parameters["@B"].Value);
        }

        [Fact]
        public void ChangeType_converts_to_the_underlying_type_of_a_nullable()
        {
            Assert.Equal(3, ValueConversion.ChangeType<int?>(3L));
            Assert.Equal(3, ValueConversion.ChangeType<int>(3L));
            Assert.Equal(2.5m, ValueConversion.ChangeType<decimal?>(2.5));
            Assert.Equal("7", ValueConversion.ChangeType<string>(7));
        }

        [Fact]
        public void MergeSql_updates_matched_rows_inserts_the_rest_and_deletes_when_asked()
        {
            string sql = SQLServerInstance.MergeSql("[dbo].[Staging]", "[dbo].[Orders]", ["[Id]", "[Line]"], ["[Total]"], deleteUnmatched: true);

            Assert.Equal(
                "MERGE INTO [dbo].[Orders] WITH (HOLDLOCK) AS T USING [dbo].[Staging] AS S ON T.[Id] = S.[Id] AND T.[Line] = S.[Line]" +
                " WHEN MATCHED THEN UPDATE SET T.[Total] = S.[Total]" +
                " WHEN NOT MATCHED BY TARGET THEN INSERT ([Id], [Line], [Total]) VALUES (S.[Id], S.[Line], S.[Total])" +
                " WHEN NOT MATCHED BY SOURCE THEN DELETE;",
                sql);
        }

        [Fact]
        public void MergeSql_leaves_out_the_update_without_values_and_the_delete_unless_asked()
        {
            string sql = SQLServerInstance.MergeSql("[S]", "[T]", ["[Id]"], [], deleteUnmatched: false);

            Assert.Equal("MERGE INTO [T] WITH (HOLDLOCK) AS T USING [S] AS S ON T.[Id] = S.[Id] WHEN NOT MATCHED BY TARGET THEN INSERT ([Id]) VALUES (S.[Id]);", sql);
        }

        [Fact]
        public void BulkMergeSql_stages_the_DataTable_columns_and_merges_the_rest_by_the_keys()
        {
            var rows = new DataTable();
            rows.Columns.Add("Total", typeof(decimal));
            rows.Columns.Add("Order Id", typeof(int));
            rows.Columns.Add("a.b", typeof(string));

            var (staging, merge) = SQLServerInstance.BulkMergeSql(rows, "dbo.Orders", ["[order id]"], deleteUnmatched: false);

            Assert.Equal("DROP TABLE IF EXISTS [#SQLServerInteraction_BulkMerge]; SELECT TOP (0) [Total], [Order Id], [a.b] INTO [#SQLServerInteraction_BulkMerge] FROM [dbo].[Orders]", staging);
            Assert.Equal(
                "MERGE INTO [dbo].[Orders] WITH (HOLDLOCK) AS T USING [#SQLServerInteraction_BulkMerge] AS S ON T.[Order Id] = S.[Order Id]" +
                " WHEN MATCHED THEN UPDATE SET T.[Total] = S.[Total], T.[a.b] = S.[a.b]" +
                " WHEN NOT MATCHED BY TARGET THEN INSERT ([Order Id], [Total], [a.b]) VALUES (S.[Order Id], S.[Total], S.[a.b]);",
                merge);
        }

        [Fact]
        public void ChangeType_gives_default_for_null_and_DBNull()
        {
            Assert.Null(ValueConversion.ChangeType<int?>(DBNull.Value));
            Assert.Null(ValueConversion.ChangeType<int?>(null));
            Assert.Equal(0, ValueConversion.ChangeType<int>(DBNull.Value));
            Assert.Null(ValueConversion.ChangeType<string>(DBNull.Value));
        }
    }
}

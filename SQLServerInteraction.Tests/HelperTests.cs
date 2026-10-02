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
        public void ChangeType_gives_default_for_null_and_DBNull()
        {
            Assert.Null(ValueConversion.ChangeType<int?>(DBNull.Value));
            Assert.Null(ValueConversion.ChangeType<int?>(null));
            Assert.Equal(0, ValueConversion.ChangeType<int>(DBNull.Value));
            Assert.Null(ValueConversion.ChangeType<string>(DBNull.Value));
        }
    }
}

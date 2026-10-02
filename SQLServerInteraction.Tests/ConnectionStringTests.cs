using Microsoft.Data.SqlClient;

namespace SQLServerInteraction.Tests
{
    public class ConnectionStringTests
    {
        private static SqlConnectionStringBuilder Parse(SQLServerConnectionString value) => new(value.GetConnectionString());

        [Fact]
        public void A_trusted_connection_uses_integrated_security_and_encrypts_by_default()
        {
            var parsed = Parse(new SQLServerConnectionString("your-server", "YourDatabase"));

            Assert.Equal("your-server", parsed.DataSource);
            Assert.Equal("YourDatabase", parsed.InitialCatalog);
            Assert.True(parsed.IntegratedSecurity);
            Assert.Equal(SqlConnectionEncryptOption.Mandatory, parsed.Encrypt);
            Assert.Equal("", parsed.UserID);
        }

        [Fact]
        public void A_login_carries_the_user_and_password()
        {
            var parsed = Parse(new SQLServerConnectionString("your-server", "YourDatabase", "your-user", "your-password", encrypt: false));

            Assert.False(parsed.IntegratedSecurity);
            Assert.Equal("your-user", parsed.UserID);
            Assert.Equal("your-password", parsed.Password);
            Assert.Equal(SqlConnectionEncryptOption.Optional, parsed.Encrypt);
        }

        [Fact]
        public void A_semicolon_or_quote_in_a_value_is_escaped()
        {
            var value = new SQLServerConnectionString("your-server", "Your;Database", "user'name", "pa;ss\"wo'rd=1");

            var parsed = Parse(value);

            Assert.Equal("Your;Database", parsed.InitialCatalog);
            Assert.Equal("user'name", parsed.UserID);
            Assert.Equal("pa;ss\"wo'rd=1", parsed.Password);
            Assert.DoesNotContain(";;", value.GetConnectionString());
        }

        [Fact]
        public void A_value_cannot_add_a_keyword()
        {
            var parsed = Parse(new SQLServerConnectionString("your-server;Initial Catalog=other", "YourDatabase"));

            Assert.Equal("your-server;Initial Catalog=other", parsed.DataSource);
            Assert.Equal("YourDatabase", parsed.InitialCatalog);
        }

        [Fact]
        public void Additional_parameters_are_merged()
        {
            var parsed = Parse(new SQLServerConnectionString("your-server", "YourDatabase", "u", "p",
                additionalParameters: "TrustServerCertificate=True;Application Name=Reports;"));

            Assert.True(parsed.TrustServerCertificate);
            Assert.Equal("Reports", parsed.ApplicationName);
            Assert.Equal("u", parsed.UserID);
        }

        [Fact]
        public void An_additional_keyword_replaces_the_one_a_property_set_as_appending_did()
        {
            var parsed = Parse(new SQLServerConnectionString("your-server", "YourDatabase",
                additionalParameters: "Encrypt=Strict;Trusted_Connection=False;Database=Other"));

            Assert.Equal(SqlConnectionEncryptOption.Strict, parsed.Encrypt);
            Assert.False(parsed.IntegratedSecurity);
            Assert.Equal("Other", parsed.InitialCatalog);
            Assert.Equal("your-server", parsed.DataSource);
        }

        [Theory]
        [InlineData("NotAKeyword=1")]
        [InlineData("TrustServerCertificate")]
        [InlineData("Encrypt=Sometimes")]
        public void Additional_parameters_that_are_not_a_connection_string_throw(string additional)
        {
            var value = new SQLServerConnectionString("your-server", "YourDatabase", "u", "p", additionalParameters: additional);

            Assert.Throws<ArgumentException>(() => value.GetConnectionString());
        }

        [Fact]
        public void SQLServerInstance_takes_the_built_string()
        {
            _ = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase", "u", "p;q"));
            Assert.Throws<ArgumentException>(() => new SQLServerInstance(
                new SQLServerConnectionString("your-server", "YourDatabase", additionalParameters: "Bogus=x")));
        }
    }
}

namespace SQLServerInteraction.Tests
{
    public class SqlIdentifierTests
    {
        [Theory]
        [InlineData("Sales", "[Sales]")]
        [InlineData("dbo.Sales", "[dbo].[Sales]")]
        [InlineData("[dbo].[Sales]", "[dbo].[Sales]")]
        [InlineData("[my db].dbo.[a.b]", "[my db].[dbo].[a.b]")]
        [InlineData("My Sales", "[My Sales]")]
        [InlineData("[a]]b]", "[a]]b]")]
        [InlineData("x]; DROP TABLE t; --", "[x]]; DROP TABLE t; --]")]
        [InlineData("it's", "[it's]")]
        [InlineData("#temp", "[#temp]")]
        public void Quote_brackets_every_part_and_doubles_closing_brackets(string name, string quoted)
        {
            Assert.Equal(quoted, SqlIdentifier.Quote(name));
        }

        [Fact]
        public void Parse_returns_the_parts_without_brackets()
        {
            Assert.Equal(["my db", "dbo", "a.b"], SqlIdentifier.Parse("[my db].dbo.[a.b]"));
            Assert.Equal(["a]b"], SqlIdentifier.Parse("[a]]b]"));
        }

        [Fact]
        public void ParseTable_names_the_parts_it_was_given()
        {
            Assert.Equal((null, null, "Sales"), SqlIdentifier.ParseTable("Sales"));
            Assert.Equal((null, "dbo", "Sales"), SqlIdentifier.ParseTable("dbo.Sales"));
            Assert.Equal(("db", "dbo", "Sales"), SqlIdentifier.ParseTable("[db].[dbo].Sales"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("a..b")]
        [InlineData(".a")]
        [InlineData("a.")]
        [InlineData("[]")]
        [InlineData("[abc")]
        [InlineData("[a]b")]
        [InlineData("a.b.c.d")]
        public void Parse_refuses_a_malformed_name(string name)
        {
            Assert.Throws<ArgumentException>(() => SqlIdentifier.Parse(name));
        }

        [Fact]
        public void Parse_refuses_null()
        {
            Assert.Throws<ArgumentException>(() => SqlIdentifier.Parse(null!));
        }

        [Fact]
        public void A_part_may_have_128_characters_but_not_129()
        {
            Assert.Equal(128, SqlIdentifier.Parse(new string('a', 128))[0].Length);
            Assert.Equal(128, SqlIdentifier.Parse("dbo.[" + new string(']', 128).Replace("]", "]]") + "]")[1].Length);
            Assert.Throws<ArgumentException>(() => SqlIdentifier.Parse(new string('a', 129)));
            Assert.Throws<ArgumentException>(() => SqlIdentifier.Parse("dbo." + new string('a', 129)));
        }

        [Fact]
        public void A_column_or_index_name_has_one_part()
        {
            Assert.Equal("[a.b]", SqlIdentifier.Quote("[a.b]", maxParts: 1));
            Assert.Throws<ArgumentException>(() => SqlIdentifier.Quote("a.b", maxParts: 1));
        }
    }
}

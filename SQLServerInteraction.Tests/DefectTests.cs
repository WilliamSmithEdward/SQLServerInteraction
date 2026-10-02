namespace SQLServerInteraction.Tests
{
    /// <summary>
    /// Behaviour that was wrong before 2.0.0, on a real server.
    /// </summary>
    public class DefectTests(DatabaseFixture database) : IntegrationTest(database)
    {
        [Fact]
        public void GetDatabaseSizeInBytes_returns_bytes()
        {
            long pages = Convert.ToInt64(Database.Scalar("SELECT SUM(CAST(size AS bigint)) FROM sys.master_files WHERE database_id = DB_ID()"));

            Assert.Equal(pages * 8192, Db.GetDatabaseSizeInBytes());
        }

        [Fact]
        public async Task ExecuteScalar_of_a_nullable_type_converts_or_gives_null()
        {
            Assert.Equal(3, Db.ExecuteScalar<int?>("SELECT CAST(3 AS bigint)"));
            Assert.Null(Db.ExecuteScalar<int?>("SELECT CAST(NULL AS int)"));
            Assert.Null(Db.ExecuteScalar<int?>("SELECT 1 WHERE 1 = 0"));
            Assert.Equal(new DateTime(2026, 1, 2), await Db.ExecuteScalarAsync<DateTime?>("SELECT CAST('2026-01-02' AS date)"));
            Assert.Null(await Db.ExecuteScalarAsync<DateTime?>("SELECT CAST(NULL AS date)"));
        }

        private string CreatePeople() =>
            Database.CreateTable("Id int NOT NULL PRIMARY KEY, [Full Name] nvarchar(50) NULL, Age int NULL");

        [Fact]
        public async Task Parameter_names_work_with_or_without_the_at_sign_and_null_is_NULL()
        {
            string table = CreatePeople();
            Database.Execute($"INSERT INTO dbo.{table} VALUES (1, N'A', 30), (2, N'B', 40)");

            Db.ExecuteNonQueryWithParameters($"UPDATE dbo.{table} SET Age = @Age WHERE Id = @Id",
                new Dictionary<string, object> { ["@Age"] = 31, ["Id"] = 1 });
            await Db.ExecuteNonQueryWithParametersAsync($"UPDATE dbo.{table} SET Age = @Age WHERE Id = @Id",
                new Dictionary<string, object> { ["@Age"] = null!, ["@Id"] = 2 });

            Assert.Equal(31, Database.Scalar($"SELECT Age FROM dbo.{table} WHERE Id = 1"));
            Assert.Null(Database.Scalar($"SELECT Age FROM dbo.{table} WHERE Id = 2"));
        }

        [Fact]
        public async Task A_null_dictionary_value_is_inserted_and_updated_as_NULL()
        {
            string table = CreatePeople();

            Db.InsertData(table, new Dictionary<string, object> { ["Id"] = 1, ["Full Name"] = null!, ["Age"] = 30 });
            await Db.InsertDataAsync(table, new Dictionary<string, object> { ["Id"] = 2, ["Full Name"] = "B", ["Age"] = null! });
            Db.UpdateData(table, new Dictionary<string, object> { ["Age"] = null! }, "Id = 1");
            await Db.UpdateDataAsync(table, new Dictionary<string, object> { ["Full Name"] = null! }, "Id = 2");

            Assert.Equal(2, Database.Scalar($"SELECT COUNT(*) FROM dbo.{table} WHERE [Full Name] IS NULL AND Age IS NULL"));
        }

        public class Person
        {
            public static int Created { get; set; }

            public int Id { get; set; }

            [SQLServerInstance.Column("Full Name")]
            public string? Name { get; set; }

            public int? Age { get; set; }

            public string this[int index] => index.ToString();
        }

        [Fact]
        public async Task InsertData_of_T_honours_ColumnAttribute_and_leaves_out_static_properties()
        {
            string table = CreatePeople();

            Db.InsertData(new Person { Id = 1, Name = "Contoso", Age = 30 }, table);
            await Db.InsertDataAsync(new Person { Id = 2, Name = null, Age = null }, "dbo." + table);

            Assert.Equal("Contoso", Database.Scalar($"SELECT [Full Name] FROM dbo.{table} WHERE Id = 1"));
            Assert.Null(Database.Scalar($"SELECT [Full Name] FROM dbo.{table} WHERE Id = 2"));
        }

        [Fact]
        public void The_primary_key_columns_come_in_key_order()
        {
            string table = Database.CreateTable("A int NOT NULL, B int NOT NULL, C int NOT NULL, CONSTRAINT [PK_" + Guid.NewGuid().ToString("N") + "] PRIMARY KEY (C, A)");
            string noKey = Database.CreateTable("A int NULL");

            Assert.Equal(["C", "A"], Db.GetTablePrimaryKeyColumns(table));
            Assert.Equal("C", Db.GetTablePrimaryKeyColumn(table));
            Assert.Equal(["C", "A"], Db.GetTablePrimaryKeyColumns("dbo." + table));
            Assert.Empty(Db.GetTablePrimaryKeyColumns(noKey));
            Assert.Null(Db.GetTablePrimaryKeyColumn(noKey));
        }

        [Fact]
        public void A_built_connection_string_with_a_semicolon_in_the_password_connects()
        {
            string login = "L_" + Guid.NewGuid().ToString("N");
            string password = "Aa1;'\"=" + Guid.NewGuid().ToString("N");
            Database.Execute($"CREATE LOGIN [{login}] WITH PASSWORD = N'{password.Replace("'", "''")}', CHECK_POLICY = OFF; CREATE USER [{login}] FOR LOGIN [{login}];");

            try
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Database.ConnectionString);
                var db = new SQLServerInstance(new SQLServerConnectionString(builder.DataSource, Database.DatabaseName, login, password,
                    additionalParameters: "TrustServerCertificate=True"));

                Assert.Equal(login, db.ExecuteScalar<string>("SELECT SUSER_SNAME()"));
            }
            finally
            {
                Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
                Database.Execute($"DROP USER [{login}]; DROP LOGIN [{login}];");
            }
        }
    }
}

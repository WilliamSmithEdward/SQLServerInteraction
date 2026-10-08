using Microsoft.Data.SqlClient;

namespace SQLServerInteraction.Tests
{
    /// <summary>
    /// Backup and restore send the database name and the path as parameters, so
    /// a database named with <c>]</c> and <c>'</c>, and a path with <c>'</c>, work.
    /// Each test uses a database of its own, since a restore needs it to itself.
    /// </summary>
    public class BackupRestoreTests(DatabaseFixture database) : IntegrationTest(database), IAsyncLifetime
    {
        private readonly string _database = $"SQLSI ]x' {Guid.NewGuid():N}";

        private string Quoted => SqlIdentifier.QuotePart(_database);

        private string ConnectionString => TestServer.ConnectionString(_database);

        public ValueTask InitializeAsync()
        {
            // SIMPLE recovery lets RESTORE replace the database without a tail-log backup.
            Master($"CREATE DATABASE {Quoted}; ALTER DATABASE {Quoted} SET RECOVERY SIMPLE;");
            Execute("CREATE TABLE dbo.Kept (Id int NOT NULL); INSERT INTO dbo.Kept VALUES (1), (2);");
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            SqlConnection.ClearAllPools();
            Master($"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE {Quoted} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {Quoted}; END",
                new SqlParameter("@name", _database));
            return ValueTask.CompletedTask;
        }

        private static void Master(string sql, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(TestServer.ConnectionString("master"));
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);
            command.ExecuteNonQuery();
        }

        private void Execute(string sql)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private int KeptRows()
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            using var command = new SqlCommand("IF OBJECT_ID(N'dbo.Kept') IS NULL SELECT -1 ELSE SELECT COUNT(*) FROM dbo.Kept", connection);
            return (int)command.ExecuteScalar();
        }

        private static string BackupPath() => $"/var/opt/mssql/data/it's {Guid.NewGuid():N}.bak";

        [Fact]
        public async Task BackupDatabase_and_RestoreDatabase_take_any_database_name_and_path()
        {
            var db = new SQLServerInstance(ConnectionString);
            string path = BackupPath();

            Assert.Null(await db.GetLastBackupDateTimeAsync());
            await db.BackupDatabaseAsync(path);
            Assert.NotNull(await db.GetLastBackupDateTimeAsync());

            Execute("DROP TABLE dbo.Kept");
            Assert.Equal(-1, KeptRows());

            SqlConnection.ClearAllPools();
            await db.RestoreDatabaseAsync(path);

            Assert.Equal(2, KeptRows());
        }

        [Fact]
        public async Task The_async_versions_take_any_database_name_and_path()
        {
            var db = new SQLServerInstance(ConnectionString);
            string path = BackupPath();

            await db.BackupDatabaseAsync(path);
            Execute("DELETE FROM dbo.Kept");

            SqlConnection.ClearAllPools();
            await db.RestoreDatabaseAsync(path);

            Assert.Equal(2, KeptRows());
        }
    }
}

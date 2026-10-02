using Microsoft.Data.SqlClient;

[assembly: AssemblyFixture(typeof(SQLServerInteraction.Tests.DatabaseFixture))]

namespace SQLServerInteraction.Tests
{
    /// <summary>
    /// One scratch database for the whole test run, created on the test server
    /// and dropped at the end. Each test makes its own tables in it, so tests
    /// can run in parallel. Nothing is created when no server is configured.
    /// </summary>
    public sealed class DatabaseFixture : IAsyncLifetime
    {
        public string DatabaseName { get; } = "SQLSI_Tests_" + Guid.NewGuid().ToString("N");

        public string ConnectionString => TestServer.ConnectionString(DatabaseName);

        public SQLServerInstance Instance => new(ConnectionString);

        public async ValueTask InitializeAsync()
        {
            if (!TestServer.IsConfigured) return;

            await ExecuteAsync(TestServer.ConnectionString("master"), $"CREATE DATABASE [{DatabaseName}]");
        }

        public async ValueTask DisposeAsync()
        {
            if (!TestServer.IsConfigured) return;

            SqlConnection.ClearAllPools();
            await ExecuteAsync(TestServer.ConnectionString("master"),
                $"IF DB_ID(N'{DatabaseName}') IS NOT NULL BEGIN ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}]; END");
        }

        /// <summary>
        /// Creates a table with a name no other test uses and returns the name.
        /// </summary>
        /// <param name="columns">The column definitions, as in CREATE TABLE.</param>
        /// <param name="name">A name to use instead of a generated one, as T-SQL would write it unquoted.</param>
        public string CreateTable(string columns, string? name = null)
        {
            name ??= "T_" + Guid.NewGuid().ToString("N");
            Execute($"CREATE TABLE [dbo].[{name.Replace("]", "]]")}] ({columns})");
            return name;
        }

        /// <summary>
        /// Runs SQL against the scratch database with SqlClient directly, so a
        /// test can set up and check state without the library under test.
        /// </summary>
        public void Execute(string sql, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// The first column of the first row, read with SqlClient directly.
        /// </summary>
        public object? Scalar(string sql, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);
            object? result = command.ExecuteScalar();
            return result == DBNull.Value ? null : result;
        }

        private static async Task ExecuteAsync(string connectionString, string sql)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

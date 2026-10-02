using Microsoft.Data.SqlClient;

namespace SQLServerInteraction.Tests
{
    /// <summary>
    /// The SQL Server the integration tests use: always the local container
    /// that scripts/test/run-integration-tests.sh starts, on 127.0.0.1. Only
    /// the port and the SA password come from the environment, so the tests
    /// cannot be pointed at any other server.
    /// </summary>
    internal static class TestServer
    {
        public const string PasswordVariable = "SQLSERVERINTERACTION_TEST_SA_PASSWORD";
        public const string PortVariable = "SQLSERVERINTERACTION_TEST_PORT";

        public const string NotConfigured =
            "No test SQL Server: set " + PasswordVariable + " (scripts/test/run-integration-tests.sh does).";

        private static string? Password => Environment.GetEnvironmentVariable(PasswordVariable);

        private static int Port =>
            int.TryParse(Environment.GetEnvironmentVariable(PortVariable), out int port) ? port : 14333;

        public static bool IsConfigured => !string.IsNullOrEmpty(Password);

        public static string ConnectionString(string database) => new SqlConnectionStringBuilder
        {
            DataSource = $"127.0.0.1,{Port}",
            InitialCatalog = database,
            UserID = "sa",
            Password = Password,
            Encrypt = true,
            TrustServerCertificate = true,
            ConnectTimeout = 30,
        }.ConnectionString;
    }
}

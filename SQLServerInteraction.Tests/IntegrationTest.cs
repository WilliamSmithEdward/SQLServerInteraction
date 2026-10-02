namespace SQLServerInteraction.Tests
{
    /// <summary>
    /// A test class that needs the test SQL Server. Each of its tests is
    /// skipped, not passed, when no server is configured; CI fails a run in
    /// which any test was skipped.
    /// </summary>
    public abstract class IntegrationTest
    {
        protected IntegrationTest(DatabaseFixture database)
        {
            Assert.SkipUnless(TestServer.IsConfigured, TestServer.NotConfigured);
            Database = database;
            Db = database.Instance;
        }

        /// <summary>The scratch database, for setting up and checking state with SqlClient directly.</summary>
        protected DatabaseFixture Database { get; }

        /// <summary>The library under test, connected to the scratch database.</summary>
        protected SQLServerInstance Db { get; }
    }
}

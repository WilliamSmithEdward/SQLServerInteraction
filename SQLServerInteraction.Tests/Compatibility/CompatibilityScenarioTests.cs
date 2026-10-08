namespace SQLServerInteraction.Tests.Compatibility
{
    /// <summary>
    /// Runs every scenario in <see cref="CompatibilityScenarios"/> against one version
    /// of the library and checks that version's recorded outcome. A subclass in each
    /// test project supplies the adapter and picks the column.
    /// </summary>
    public abstract class CompatibilityScenarioTests(DatabaseFixture database) : IntegrationTest(database)
    {
        /// <summary>The version under test, behind the version-neutral adapter.</summary>
        protected abstract ICompatibilityDb Compat { get; }

        /// <summary>The outcome recorded for the version under test.</summary>
        protected abstract string Expected(CompatibilityScenario scenario);

        public static TheoryData<string> ScenarioNames => [.. CompatibilityScenarios.All.Select(scenario => scenario.Name)];

        [Theory]
        [MemberData(nameof(ScenarioNames), MemberType = typeof(CompatibilityScenarioTests))]
        public async Task The_scenario_gives_the_outcome_recorded_for_this_version(string name)
        {
            var scenario = CompatibilityScenarios.Find(name);

            string actual = await scenario.Run(new CompatibilityContext(Compat, Database));

            Assert.Equal(Expected(scenario), actual);
        }
    }

    /// <summary>
    /// The catalogue's own shape, checked without a server: names are unique, every
    /// outcome is written down, and the table records both kinds of scenario.
    /// </summary>
    public class CompatibilityCatalogueTests
    {
        [Fact]
        public void Every_scenario_has_a_unique_name_and_two_recorded_outcomes()
        {
            var all = CompatibilityScenarios.All;

            Assert.Equal(all.Count, all.Select(scenario => scenario.Name).Distinct().Count());
            Assert.All(all, scenario => Assert.False(string.IsNullOrWhiteSpace(scenario.ExpectedV1)));
            Assert.All(all, scenario => Assert.False(string.IsNullOrWhiteSpace(scenario.ExpectedV3)));
        }

        [Fact]
        public void The_catalogue_records_what_changed_and_what_did_not()
        {
            Assert.Contains(CompatibilityScenarios.All, scenario => scenario.Changed);
            Assert.Contains(CompatibilityScenarios.All, scenario => !scenario.Changed);
        }
    }
}

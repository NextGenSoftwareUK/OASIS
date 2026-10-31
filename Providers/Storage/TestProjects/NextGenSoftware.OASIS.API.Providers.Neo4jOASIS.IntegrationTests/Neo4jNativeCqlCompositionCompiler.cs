using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static CosmicProjectionCompositionPlan<Holon, TResult> CompositionCompilerPlan<TResult>(CosmicProjectionQuery<Holon, TResult> query)
    {
        var grouped = Provider.CqlGroupedCapabilities;
        var profile = new CosmicProjectionCompositionCapabilities(
            CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging,
            grouped.FilteringCapabilities.Features, grouped.FilteringCapabilities.Keys, grouped.OrderingCapabilities.KeyTypes);
        var plan = CosmicProjectionCompositionPlanner.Plan(query, ProviderType.Neo4jOASIS,
            new[] { new CosmicProjectionCompositionTarget(ProviderType.Neo4jOASIS,
                Provider.CqlCapabilities, Provider.CqlProjectionCapabilities, profile) });
        Assert.IsFalse(plan.IsError, plan.Message); return plan.Result;
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeCompositionCompilerPreservesSourceAndSelectedWindows(bool filterFirst)
        => WithNativeGraph(async fixture =>
        {
            var source = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Skip(1).Take(3)
                .Select(h => new { h.Name, h.Version });
            var query = (filterFirst ? source.Where(row => row.Name != "B").Take(1)
                : source.Take(1).Where(row => row.Name != "B")).Result;
            var command = Provider.CompileCqlComposition(CompositionCompilerPlan(query),
                new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000));
            using var session = fixture.Driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read));
            var records = await (await session.RunAsync(command.Text, command.Parameters)).ToListAsync();
            Assert.AreEqual(3, records.Count(row => row["phase"].As<long>() == -1));
            var final = records.Where(row => row["phase"].As<long>() == 1).ToArray();
            Assert.AreEqual(filterFirst ? 2 : 1, final.Length); // Header plus identities.
            Assert.AreEqual(filterFirst ? 1L : 0L, final[0]["nativeCount"].As<long>());
            Assert.AreEqual(filterFirst ? 5L : 4L, final[0]["stageWork"].As<long>());
            if (filterFirst) Assert.AreEqual(1L, final[1]["sourcePosition"].As<long>());
        }, configure: root =>
        {
            root.Name = "A"; var children = root.Children.ToArray();
            children[0].Name = "B"; children[1].Name = "C";
            children[0].Children.Single().Name = "D";
        });

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeCompositionCompilerPublishesExplicitEmptyStageHeaders(bool emptySource)
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id);
            if (emptySource) source = source.Where(h => h.Name == "absent source");
            var query = source.Select(h => h.Name).Take(0).Skip(0).Result;
            var command = Provider.CompileCqlComposition(CompositionCompilerPlan(query),
                new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000));
            using var session = fixture.Driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read));
            var records = await (await session.RunAsync(command.Text, command.Parameters)).ToListAsync();
            Assert.AreEqual(emptySource ? 3 : 4, records.Count);
            Assert.AreEqual(emptySource ? 0L : 1L, records[0]["nativeCount"].As<long>());
            var headers = records.Where(row => row["phase"].As<long>() >= 0).ToArray();
            Assert.AreEqual(2, headers.Length);
            Assert.IsTrue(headers.All(row => row["position"].As<long>() == -1 && row["nativeCount"].As<long>() == 0));
        }, width: 0);

    [TestMethod]
    public Task NativeCompositionCompilerKeepsPriorRankOnRepeatedSortKeys()
        => WithNativeGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().OrderByDescending(h => h.Name)
                .Select(h => new { h.Name, h.Version }).OrderBy(row => row.Version)
                .ThenByDescending(row => row.Version).Skip(1).Take(2).Result;
            var command = Provider.CompileCqlComposition(CompositionCompilerPlan(query),
                new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000));
            using var session = fixture.Driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read));
            var records = await (await session.RunAsync(command.Text, command.Parameters)).ToListAsync();
            CollectionAssert.AreEqual(new long[] { 1, 2 }, records.Where(row => row["phase"].As<long>() == 2 && row["position"].As<long>() >= 0)
                .Select(row => row["sourcePosition"].As<long>()).ToArray());
        }, configure: root =>
        {
            root.Name = "A"; var children = root.Children.ToArray();
            children[0].Name = "B"; children[1].Name = "C";
            children[0].Children.Single().Name = "D";
        });

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeCompositionCompilerRetainsExhaustedBudgetEvidence(bool sourceBudget)
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => h.Name).Take(0).Result;
            var command = Provider.CompileCqlComposition(CompositionCompilerPlan(query),
                new CosmicProjectionCompositionExecutionLimits(sourceBudget ? 0 : 10, 10000, sourceBudget ? 100 : 0, 10000, 10, 10000));
            using var session = fixture.Driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read));
            var records = await (await session.RunAsync(command.Text, command.Parameters)).ToListAsync();
            Assert.AreEqual(1L, records[0]["nativeCount"].As<long>());
            Assert.AreEqual(sourceBudget ? 0L : 1L, records[0]["stageWork"].As<long>());
            Assert.AreEqual(1, records.Count(row => row["phase"].As<long>() == -1));
        }, width: 0);

    [TestMethod]
    public Task NativeCompositionCompilerCannotHideRequiredSourceErrorsWithZeroTake()
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .Select(h => CosmicQueryMetadata.Value<int>(h, "missing")).Take(0).Result;
            var command = Provider.CompileCqlComposition(CompositionCompilerPlan(query),
                new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000));
            using var session = fixture.Driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read));
            var records = await (await session.RunAsync(command.Text, command.Parameters)).ToListAsync();
            var source = records.Single(row => row["phase"].As<long>() == -1)["proof"].As<IDictionary<string, object>>();
            Assert.IsFalse((bool)source["domainValid"]);
            Assert.AreEqual(0L, records.Last()["nativeCount"].As<long>());
        }, width: 0);
}

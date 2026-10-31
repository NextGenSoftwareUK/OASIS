using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeNumericInputIsNotTruncatedByGraphPublicationBudget()
        => WithNativeGraph(async fixture =>
        {
            var graph = CosmicQueryGraphOptions.Create(maxChildDepth: 1, maximumNodes: 1).Result;
            var source = CosmicQuery<Holon>.Create().WithChildren(graph)
                .Where(h => h.Name.StartsWith("owned CQL graph child", StringComparison.Ordinal)).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => h.Version).Result));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(fixture.Root.Children.Sum(child => child.Version), result.Result);
        }, width: 137);

    [TestMethod]
    public Task NativeNumericTraversalFailureCannotHideOutsideSourceWindow()
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .WhereAnyDescendant(child => child.Name == "owned CQL grandchild", CosmicQueryTraversalOptions.Create(2, 2).Result)
                .OrderBy(h => h.Name).Skip(1).Take(1).Result;
            var invalid = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => h.Version).Result));
            Assert.AreEqual("CQL_QUERY_LIMIT", invalid.ErrorCode); Assert.IsFalse(invalid.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(DescendantPlan(fixture, depth: 2, nodes: 3).Query.Sum(h => h.Version).Result));
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(fixture.Root.Version, fresh.Result);
        });

    [TestMethod]
    public Task NativeNumericCancellationAfterActualFetchPublishesNothingAndAllowsReuse()
        => WithNativeGraph(async fixture =>
        {
            using var cancellation = new CancellationTokenSource(); var observed = false;
            fixture.AfterRootRow = () => { observed = true; cancellation.Cancel(); return Task.CompletedTask; };
            var plan = NumericPlan(DescendantPlan(fixture).Query.Sum(h => h.Version).Result);
            var cancelled = await fixture.Provider.ExecuteCosmicScalarAsync(plan, cancellation.Token);
            Assert.IsTrue(observed); Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode); Assert.IsFalse(cancelled.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(fixture.Root.Version, fresh.Result);
        });

    [TestMethod]
    public Task NativeNumericDeadlineIncludesActualCursorDeliveryAndAllowsReuse()
        => WithNativeGraph(async fixture =>
        {
            fixture.AfterRootRow = () => Task.Delay(TimeSpan.FromSeconds(31));
            var plan = NumericPlan(DescendantPlan(fixture).Query.Sum(h => h.Version).Result);
            var timedOut = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
            Assert.AreEqual("CQL_QUERY_TIMEOUT", timedOut.ErrorCode); Assert.IsFalse(timedOut.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(fixture.Root.Version, fresh.Result);
        });


}

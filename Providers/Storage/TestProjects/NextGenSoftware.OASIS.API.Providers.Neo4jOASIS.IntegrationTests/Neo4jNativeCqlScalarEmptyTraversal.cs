using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [DataTestMethod]
    [DataRow(CosmicScalarOperation.Count)]
    [DataRow(CosmicScalarOperation.LongCount)]
    [DataRow(CosmicScalarOperation.Any)]
    [DataRow(CosmicScalarOperation.Min)]
    [DataRow(CosmicScalarOperation.Max)]
    [DataRow(CosmicScalarOperation.Sum)]
    [DataRow(CosmicScalarOperation.Average)]
    public Task NativeScalarTakeZeroCannotBypassProtectedDescendantLimits(CosmicScalarOperation operation)
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .WhereAnyDescendant(h => h.Name != "absent", CosmicQueryTraversalOptions.Create(2, 1).Result)
                .Take(0).Result;

            // Every scalar must validate the protected native scope, even when
            // its mathematical input window is empty. Empty-result semantics
            // cannot replace a traversal limit error.
            switch (operation)
            {
                case CosmicScalarOperation.Count: await Reject(source.Count().Result); break;
                case CosmicScalarOperation.LongCount: await Reject(source.LongCount().Result); break;
                case CosmicScalarOperation.Any: await Reject(source.Any().Result); break;
                case CosmicScalarOperation.Min: await Reject(source.Min(h => h.Version).Result); break;
                case CosmicScalarOperation.Max: await Reject(source.Max(h => h.Version).Result); break;
                case CosmicScalarOperation.Sum: await Reject(source.Sum(h => h.Version).Result); break;
                case CosmicScalarOperation.Average: await Reject(source.Average(h => h.Version).Result); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }

            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                CosmicQuery<Holon>.Create().Where(h => h.Id == id).Count().Result));
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception);
            Assert.AreEqual(1, fresh.Result);

            async Task Reject<TResult>(CosmicScalarQuery<Holon, TResult> query)
            {
                var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(query));
                Assert.IsTrue(result.IsError, "Take(0) cannot suppress protected validation.");
                Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode, result.Message);
                Assert.IsFalse(result.IsLoaded);
            }
        });

    [DataTestMethod]
    [DataRow(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape.Rows)]
    [DataRow(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape.Graph)]
    [DataRow(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape.Projection)]
    public Task NativeEmptyWindowCannotBypassProtectedDescendantLimits(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape shape)
        => WithNativeGraph(fixture => global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowAcceptance.VerifyAsync(fixture.Provider, fixture.Root, shape));
}

using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Theory]
    [InlineData(CosmicScalarOperation.Count)]
    [InlineData(CosmicScalarOperation.LongCount)]
    [InlineData(CosmicScalarOperation.Any)]
    [InlineData(CosmicScalarOperation.Min)]
    [InlineData(CosmicScalarOperation.Max)]
    [InlineData(CosmicScalarOperation.Sum)]
    [InlineData(CosmicScalarOperation.Average)]
    public Task NativeScalarTakeZeroCannotBypassProtectedDescendantLimits(CosmicScalarOperation operation)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .WhereAnyDescendant(h => h.Name != "absent", CosmicQueryTraversalOptions.Create(2, 1).Result)
                .Take(0).Result;

            // This control checks the same protected-scope contract as SQLite
            // and Neo4j, without assuming that MongoDB needs their root fix.
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
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception);
            Assert.Equal(1, fresh.Result);

            async Task Reject<TResult>(CosmicScalarQuery<Holon, TResult> query)
            {
                var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(query));
                Assert.True(result.IsError, "Take(0) cannot suppress protected validation.");
                Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode);
                Assert.False(result.IsLoaded);
            }
        });

    [Theory]
    [InlineData(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape.Rows)]
    [InlineData(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape.Graph)]
    [InlineData(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape.Projection)]
    public Task NativeEmptyWindowCannotBypassProtectedDescendantLimits(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape shape)
        => WithMongoGraph(fixture => global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowAcceptance.VerifyAsync(fixture.Provider, fixture.Root, shape));
}

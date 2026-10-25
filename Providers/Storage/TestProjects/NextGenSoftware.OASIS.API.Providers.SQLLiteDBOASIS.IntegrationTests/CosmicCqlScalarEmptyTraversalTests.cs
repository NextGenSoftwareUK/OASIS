using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(CosmicScalarOperation.Count)]
    [InlineData(CosmicScalarOperation.LongCount)]
    [InlineData(CosmicScalarOperation.Any)]
    [InlineData(CosmicScalarOperation.Min)]
    [InlineData(CosmicScalarOperation.Max)]
    [InlineData(CosmicScalarOperation.Sum)]
    [InlineData(CosmicScalarOperation.Average)]
    public async Task NativeScalarTakeZeroCannotBypassProtectedDescendantLimits(CosmicScalarOperation operation)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph();
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        var id = root.Id;
        var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .WhereAnyDescendant(h => h.Name != "absent", CosmicQueryTraversalOptions.Create(2, 1).Result)
            .Take(0).Result;

        // The output is mathematically empty, but the protected traversal must
        // still reject its excessive native scope before any scalar is published.
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

        var fresh = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            CosmicQuery<Holon>.Create().Where(h => h.Id == id).Count().Result));
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception);
        fresh.Result.Should().Be(1);

        async Task Reject<TResult>(CosmicScalarQuery<Holon, TResult> query)
        {
            var result = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider, query));
            result.IsError.Should().BeTrue("Take(0) cannot suppress protected validation");
            result.ErrorCode.Should().Be("CQL_QUERY_LIMIT", result.Message);
            result.IsLoaded.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape.Rows)]
    [InlineData(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape.Graph)]
    [InlineData(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape.Projection)]
    public async Task NativeEmptyWindowCannotBypassProtectedDescendantLimits(global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowShape shape)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph();
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        await global::OASIS.ProviderTests.Fixtures.CqlEmptyWindowAcceptance.VerifyAsync(provider, root, shape);
    }
}

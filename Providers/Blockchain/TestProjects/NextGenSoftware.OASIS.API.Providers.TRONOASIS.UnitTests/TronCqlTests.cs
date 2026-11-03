using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronCqlTests
{
    private static CosmicQuery<Holon> Query(Guid[] ids) =>
        CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TRONOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.TRONOASIS, ids).Result).Result;

    [Fact]
    public void SelectionIsFrozenAndIntersectedWithExactAuthority()
    {
        var allowed = Guid.NewGuid();
        var foreign = Guid.NewGuid();
        var ids = new[] { allowed, foreign };
        var query = Query(new[] { allowed }).Where(h => ids.Contains(h.Id)).Result;
        ids[0] = Guid.NewGuid();
        var compiled = TronCqlCompiler.Compile(query, ProviderType.TRONOASIS);
        Assert.False(compiled.IsError, compiled.Message);
        Assert.Equal(new[] { allowed }, compiled.Result);
        Assert.True(TronCqlCompiler.Compile(query, ProviderType.AWSS3OASIS).IsError);
        Assert.Empty(TronCqlCompiler.Compile(Query(Array.Empty<Guid>()), ProviderType.TRONOASIS).Result);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("metadata")]
    [InlineData("order")]
    [InlineData("graph")]
    [InlineData("bound")]
    public void UnsupportedShapesRejectBeforeNativeRead(string shape)
    {
        var query = Query(Enumerable.Range(0, shape == "bound" ? 101 : 3).Select(_ => Guid.NewGuid()).ToArray());
        query = shape switch
        {
            "name" => query.Where(h => h.Name == "coffee").Result,
            "metadata" => query.Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result,
            "order" => query.OrderBy(h => h.Id).Result,
            "graph" => query.WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth: 1).Result).Result,
            _ => query
        };
        var result = TronCqlCompiler.Compile(query, ProviderType.TRONOASIS);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task InactiveAndPreCancelledQueriesNeverAllocateRuntime()
    {
        var provider = new TRONOASIS(rpcEndpoint: "http://127.0.0.1:1", network: "local");
        var plan = CosmicQueryPlanner.Plan(Query(new[] { Guid.NewGuid() }), ProviderType.TRONOASIS,
            new[] { new CosmicQueryTarget(ProviderType.TRONOASIS, provider.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        Assert.Equal("CQL_PROVIDER_INACTIVE", (await provider.ExecuteCosmicQueryAsync(plan.Result)).ErrorCode);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var stopped = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancelled.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", stopped.ErrorCode);
        Assert.IsType<OperationCanceledException>(stopped.Exception);
        Assert.False(provider.IsProviderActivated);
    }
}

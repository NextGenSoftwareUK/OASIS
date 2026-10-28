using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicIndexedSourcePlanningTests
{
    private static CosmicChainQuerySource Source(string network="genesis-A") => new(
        ProviderType.SolanaOASIS,network,ProviderType.SQLLiteDBOASIS,
        Guid.Parse("685cbeef-372c-4dd0-96ca-0ce027165bef"),1,new string('A',64),0);
    private static CosmicChainQueryReadRequirement Requirement() => new(Source(),new(90,"block-90"),10,TimeSpan.FromSeconds(10));
    private static CosmicQueryScope Scope => CosmicQueryScope.Named(ProviderType.SolanaOASIS).Result;
    private static CosmicQuery<Holon> Indexed(CosmicChainQueryReadRequirement? requirement=null)
        => CosmicQuery<Holon>.Create(Scope).WithIndexedSource(requirement ?? Requirement()).Result;
    private static CosmicQueryCapabilities Capabilities => new(CosmicQueryFeatures.None,Array.Empty<CosmicQueryFieldCapability>());

    [Fact]
    public void CompositionKeepsTheExactFrozenRequirementAndDoesNotMutateTheOriginal()
    {
        var original=CosmicQuery<Holon>.Create(Scope);
        var requirement=Requirement();
        var indexed=original.WithIndexedSource(requirement).Result;
        var graph=CosmicQueryGraphOptions.Create().Result;
        var access=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var composed=new[] {
            indexed.Where(h=>h.Version>1).Result,
            indexed.OrderBy(h=>h.Version).ThenBy(h=>h.Name).Result,
            indexed.Skip(1).Take(2).Result,
            indexed.WithChildren(graph).Result.WithoutChildren(),
            indexed.WithReadAccess(access).Result,
            indexed.WithScope(Scope)
        };
        Assert.Null(original.IndexedSource);
        foreach(var query in composed) Assert.Same(requirement,query.IndexedSource);
        Assert.Same(requirement,indexed.Count().Result.Source.IndexedSource);
        Assert.Same(requirement,indexed.Select(h=>h.Name).Result.Source.IndexedSource);
        Assert.Same(indexed,indexed.WithIndexedSource(requirement).Result);
        Assert.Equal("CQL_SOURCE_REBIND_FORBIDDEN",indexed.WithIndexedSource(Requirement()).ErrorCode);
    }

    [Fact]
    public void CallerMustExplicitlyNameOneLogicalSourceAndCannotBroadenItLater()
    {
        Assert.Equal("CQL_SOURCE_SCOPE_INVALID",CosmicQuery<Holon>.Create().WithIndexedSource(Requirement()).ErrorCode);
        Assert.Equal("CQL_SOURCE_SCOPE_INVALID",CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).WithIndexedSource(Requirement()).ErrorCode);
        Assert.Equal("CQL_SOURCE_SCOPE_INVALID",CosmicQuery<Holon>.Create(
            CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result).WithIndexedSource(Requirement()).ErrorCode);
        var widened=Indexed().WithScope(CosmicQueryScope.AllEligible);
        Assert.Equal("CQL_SOURCE_SCOPE_INVALID",CosmicQueryPlanner.Plan(widened,ProviderType.SolanaOASIS,
            new[]{new CosmicQueryTarget(ProviderType.SolanaOASIS,Capabilities,Source())}).ErrorCode);
    }

    [Theory]
    [InlineData("native-target")] [InlineData("native-query")] [InlineData("wrong-index")]
    public void ModeAndBindingAreAdmittedAgainstTheAuthorizedTargetRatherThanCallerClaims(string change)
    {
        var query=change=="native-query" ? CosmicQuery<Holon>.Create(Scope) : Indexed();
        var profile=change=="native-target" ? null : Source(change=="wrong-index" ? "genesis-B" : "genesis-A");
        Assert.Equal("CQL_SOURCE_BINDING_MISMATCH",CosmicQueryPlanner.Plan(query,ProviderType.SolanaOASIS,
            new[]{new CosmicQueryTarget(ProviderType.SolanaOASIS,Capabilities,profile)}).ErrorCode);
        Assert.Throws<ArgumentException>(()=>new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS,Capabilities,Source()));
    }

    [Fact]
    public async Task RowScalarAndProjectionPlansPreserveBindingButCannotFallThroughToNativeExecution()
    {
        var query=Indexed();
        var rows=CosmicQueryPlanner.Plan(query,ProviderType.SolanaOASIS,
            new[]{new CosmicQueryTarget(ProviderType.SolanaOASIS,Capabilities,Source())});
        Assert.False(rows.IsError,rows.Message);
        Assert.Same(query.IndexedSource,rows.Result.Query.IndexedSource);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Capabilities.Admit(query).ErrorCode);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Capabilities.AdmitPlan(rows.Result).ErrorCode);
        var scalar=CosmicScalarPlanner.Plan(query.Count().Result,ProviderType.SolanaOASIS,
            new[]{new CosmicScalarTarget(ProviderType.SolanaOASIS,Capabilities,
                new CosmicScalarCapabilities(new[]{CosmicScalarOperation.Count}),Source())});
        Assert.False(scalar.IsError,scalar.Message);
        Assert.NotNull(scalar.Result.SourcePlan.Targets[0].IndexedSource);
        var projection=CosmicProjectionPlanner.Plan(query.Select(h=>h.Name).Result,ProviderType.SolanaOASIS,
            new[]{new CosmicProjectionTarget(ProviderType.SolanaOASIS,Capabilities,
                new CosmicProjectionCapabilities(new[]{new CosmicProjectionFieldCapability("Name",typeof(string))},Array.Empty<Type>()),Source())});
        Assert.False(projection.IsError,projection.Message);
        Assert.NotNull(projection.Result.SourcePlan.Targets[0].IndexedSource);

        var adapter=new RejectNativeCalls();
        var adapters=new ICosmicQueryStorageProvider[]{adapter};
        Assert.Equal("CQL_INDEXED_EXECUTION_UNSUPPORTED",(await CosmicQueryExecutor.ExecuteAsync(rows.Result,adapters)).ErrorCode);
        Assert.Equal("CQL_INDEXED_EXECUTION_UNSUPPORTED",(await CosmicScalarExecutor.ExecuteAsync(scalar.Result,adapters)).ErrorCode);
        Assert.Equal("CQL_INDEXED_EXECUTION_UNSUPPORTED",(await CosmicProjectionExecutor.ExecuteAsync(projection.Result,adapters)).ErrorCode);
        Assert.Equal(0,adapter.NativeCalls);
    }

    private sealed class RejectNativeCalls : ICosmicQueryStorageProvider
    {
        public int NativeCalls;
        public ProviderType QueryProviderType => ProviderType.SolanaOASIS;
        public CosmicQueryCapabilities QueryCapabilities => Capabilities;
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        {
            NativeCalls++;
            throw new InvalidOperationException("An indexed query must never call the native row contract.");
        }
    }
}

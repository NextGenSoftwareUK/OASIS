using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed partial class CosmicSetQueryExecutorTests
{
    private sealed class SelectedLinqNative:ICosmicQueryStorageProvider,ICosmicQueryProjectedSetStorageProvider
    {
        private readonly Native source=new();
        public int Calls;
        public object LastPlan;
        public ProviderType QueryProviderType=>source.QueryProviderType;
        public CosmicQueryCapabilities QueryCapabilities=>source.QueryCapabilities;
        public CosmicProjectionCapabilities ProjectionCapabilities=>new(
            new[]{new CosmicProjectionFieldCapability("Name",typeof(string))},Array.Empty<Type>());
        public CosmicProjectionCompositionCapabilities CompositionCapabilities=>new(
            CosmicSelectedStageFeatures.Filter|CosmicSelectedStageFeatures.Ordering|
            CosmicSelectedStageFeatures.Paging|CosmicSelectedStageFeatures.Reverse,
            CosmicQueryFeatures.None,Array.Empty<CosmicQueryMetadataCapability>(),new[]{typeof(string)});
        public CosmicProjectedSetCapabilities ProjectedSetCapabilities=>new(
            CosmicSetQueryFeatures.Union|CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Intersect|
            CosmicSetQueryFeatures.Except,new[]{typeof(string)},new[]{typeof(string)},CompositionCapabilities);
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
            CosmicQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
            =>throw new InvalidOperationException("Do not enumerate selected-set inputs independently.");
        public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T,TResult>(
            CosmicProjectionCompositionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
            CancellationToken cancellationToken=default) where T:IHolon
            =>throw new InvalidOperationException("Selected sets must dispatch only the native set contract.");
        public Task<OASISResult<CosmicProjectedSetPacket>> ExecuteCosmicProjectedSetAsync<T,TResult>(
            CosmicProjectedSetPlan<T,TResult> plan,CosmicProjectedSetExecutionLimits limits,
            CancellationToken cancellationToken=default) where T:IHolon
        {
            Calls++;LastPlan=plan;
            CosmicProjectionCompositionPacket Empty(CosmicProjectionQuery<T,TResult> input)=>new(
                Array.Empty<CosmicProjectionInputRow>(),
                input.ResultStages.Select(_=>new CosmicProjectionStageProof(Array.Empty<Guid>())).ToArray(),
                Array.Empty<CosmicProjectionCompositionRow>(),0,0);
            return Task.FromResult(new OASISResult<CosmicProjectedSetPacket>(new(
                Empty(plan.Query.Left),Empty(plan.Query.Right),new(Array.Empty<CosmicProjectedSetOccurrence>()),
                plan.Query.Output.ResultStages.Select(_=>new CosmicProjectedSetStageProof(Array.Empty<CosmicProjectedSetOccurrence>())).ToArray(),
                Array.Empty<CosmicProjectionRow>(),0)){IsLoaded=true});
        }
    }
    [Theory]
    [InlineData(CosmicSetQueryKind.Union)] [InlineData(CosmicSetQueryKind.Concat)]
    [InlineData(CosmicSetQueryKind.Intersect)] [InlineData(CosmicSetQueryKind.Except)]
    public async Task LinqSelectedSetsPreserveIndependentWindowsAndOneNativeDispatch(CosmicSetQueryKind kind)
    {
        var native=new SelectedLinqNative();var manager=SetManager();
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var root=CosmicLinq.Create(manager,native,source,17).Result;
        var left=root.Skip(1).Take(3).Select(row=>row.Name);
        var right=root.Skip(2).Take(4).Select(row=>row.Name).Take(2);
        IQueryable<string> query=kind switch {
            CosmicSetQueryKind.Union=>left.Union(right),CosmicSetQueryKind.Concat=>left.Concat(right),
            CosmicSetQueryKind.Intersect=>left.Intersect(right),_=>left.Except(right)};
        query=query.OrderBy(value=>value).Skip(1).Take(2).Reverse();
        Assert.Equal(0,native.Calls);
        var result=await query.ToCosmicListAsync();
        Assert.False(result.IsError,result.Message+result.Exception);Assert.True(result.IsLoaded);
        Assert.Equal(1,native.Calls);
        var plan=Assert.IsType<CosmicProjectedSetPlan<Holon,string>>(native.LastPlan);
        Assert.Equal(kind,plan.Query.Kind);
        Assert.Equal(1,plan.Query.Left.Source.Offset);Assert.Equal(3,plan.Query.Left.Source.Limit);
        Assert.Equal(2,plan.Query.Right.Source.Offset);Assert.Equal(4,plan.Query.Right.Source.Limit);
        Assert.Single(plan.Query.Right.ResultStages);Assert.Equal(4,plan.Query.Output.ResultStages.Count);
        Assert.Equal(17,plan.Options.Bounds.MaximumLeftInputRows);Assert.Equal(17,plan.Options.Bounds.MaximumRightInputRows);
    }
    [Fact]
    public void LinqSelectedSetsRejectLocalComparersAndForeignManagersWithoutDispatch()
    {
        var native=new SelectedLinqNative();var manager=SetManager();
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var left=CosmicLinq.Create(manager,native,source).Result.Select(row=>row.Name);
        var right=CosmicLinq.Create(SetManager(),native,source).Result.Select(row=>row.Name);
        Assert.Throws<CosmicLinqException>(()=>left.Concat(right));
        Assert.Throws<CosmicLinqException>(()=>left.Union(Array.Empty<string>()));
        Assert.Throws<CosmicLinqException>(()=>left.Union(left,StringComparer.Ordinal));
        Assert.Equal(0,native.Calls);
    }
    [Fact]
    public async Task LinqSelectedSetsRebindBothCapturedInputsPerRead()
    {
        var native=new SelectedLinqNative();var manager=SetManager();
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var root=CosmicLinq.Create(manager,native,source).Result;var leftName="first";var rightName="second";
        var query=root.Where(row=>row.Name==leftName).Select(row=>row.Name)
            .Concat(root.Where(row=>row.Name==rightName).Select(row=>row.Name));
        var first=await query.ToCosmicListAsync();Assert.False(first.IsError,first.Message);
        var before=Assert.IsType<CosmicProjectedSetPlan<Holon,string>>(native.LastPlan);
        leftName="changed-left";rightName="changed-right";
        var second=await query.ToCosmicListAsync();Assert.False(second.IsError,second.Message);
        var after=Assert.IsType<CosmicProjectedSetPlan<Holon,string>>(native.LastPlan);
        Assert.Equal("first",Assert.IsType<CosmicQueryComparison>(before.Query.Left.Source.BusinessPredicates[0]).Literal.Value);
        Assert.Equal("changed-left",Assert.IsType<CosmicQueryComparison>(after.Query.Left.Source.BusinessPredicates[0]).Literal.Value);
        Assert.Equal("second",Assert.IsType<CosmicQueryComparison>(before.Query.Right.Source.BusinessPredicates[0]).Literal.Value);
        Assert.Equal("changed-right",Assert.IsType<CosmicQueryComparison>(after.Query.Right.Source.BusinessPredicates[0]).Literal.Value);
        Assert.Equal(2,native.Calls);
    }
}

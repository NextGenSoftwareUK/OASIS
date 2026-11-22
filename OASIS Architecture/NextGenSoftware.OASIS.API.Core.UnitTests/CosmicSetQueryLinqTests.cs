using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed partial class CosmicSetQueryExecutorTests
{
    private sealed class LinqSetNative : ICosmicQueryStorageProvider, ICosmicSetQueryStorageProvider
    {
        private readonly Native source = new();
        public object LastPlan;
        public int Calls;
        public ProviderType QueryProviderType => source.QueryProviderType;
        public CosmicQueryCapabilities QueryCapabilities => source.QueryCapabilities;
        public CosmicSetQueryCapabilities SetQueryCapabilities => new(source.SetQueryCapabilities.Features,
            new CosmicEntityCompositionCapabilities(CosmicEntityStageFeatures.Filter|CosmicEntityStageFeatures.Ordering|
                CosmicEntityStageFeatures.Paging|CosmicEntityStageFeatures.Reverse,QueryCapabilities));
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
            CosmicQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
            => throw new InvalidOperationException("Set queries must not enumerate either flat input.");
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicSetQueryAsync<T>(
            CosmicSetQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        {
            Calls++; LastPlan=plan;
            return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(source.Data){IsLoaded=true});
        }
    }
    [Theory]
    [InlineData(CosmicSetQueryKind.Union)]
    [InlineData(CosmicSetQueryKind.Concat)]
    [InlineData(CosmicSetQueryKind.Intersect)]
    [InlineData(CosmicSetQueryKind.Except)]
    public async Task LinqSetOperatorsPreserveInputAndOutputWindows(CosmicSetQueryKind kind)
    {
        var native=new LinqSetNative();var manager=SetManager();
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var root=CosmicLinq.Create(manager,native,source,17).Result;
        var left=root.Skip(1).Take(3);var right=root.Skip(2).Take(4);
        IQueryable<Holon> query=kind switch {
            CosmicSetQueryKind.Union=>left.Union(right),
            CosmicSetQueryKind.Concat=>left.Concat(right),
            CosmicSetQueryKind.Intersect=>left.Intersect(right),
            _=>left.Except(right)
        };
        query=query.OrderBy(h=>h.Name).Skip(1).Take(2).Reverse();
        Assert.Equal(0,native.Calls);
        var result=await query.ToCosmicListAsync();
        Assert.False(result.IsError,result.Message);Assert.True(result.IsLoaded);
        Assert.Equal(1,native.Calls);
        var plan=Assert.IsType<CosmicSetQueryPlan<Holon>>(native.LastPlan);
        Assert.Equal(kind,plan.Query.Kind);
        Assert.Equal(1,plan.Query.Left.Offset);Assert.Equal(3,plan.Query.Left.Limit);
        Assert.Equal(2,plan.Query.Right.Offset);Assert.Equal(4,plan.Query.Right.Limit);
        Assert.Equal(17,plan.Options.MaximumLeftInputRows);
        Assert.Equal(17,plan.Options.MaximumRightInputRows);
        Assert.Equal(4,plan.Query.Output.Stages.Count);
    }
    [Fact]
    public void LinqSetsRejectLocalComparersAndForeignContextsWithoutDispatch()
    {
        var native=new LinqSetNative();var manager=SetManager();
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var root=CosmicLinq.Create(manager,native,source).Result;
        var other=CosmicLinq.Create(SetManager(),native,source).Result;
        Assert.Throws<CosmicLinqException>(()=>root.Union(Array.Empty<Holon>()));
        Assert.Throws<CosmicLinqException>(()=>root.Concat(other));
        Assert.Throws<CosmicLinqException>(()=>root.Union(root,EqualityComparer<Holon>.Default));
        Assert.Equal(0,native.Calls);
    }
    [Fact]
    public async Task LinqSetRebindsBothInputExpressionsOnEachRead()
    {
        var native=new LinqSetNative();var manager=SetManager();
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var root=CosmicLinq.Create(manager,native,source).Result;
        var leftName="first";var rightName="second";
        var query=root.Where(h=>h.Name==leftName).Concat(root.Where(h=>h.Name==rightName));
        var first=await query.ToCosmicListAsync();Assert.False(first.IsError,first.Message);
        var before=Assert.IsType<CosmicSetQueryPlan<Holon>>(native.LastPlan);
        leftName="changed-left";rightName="changed-right";
        var second=await query.ToCosmicListAsync();Assert.False(second.IsError,second.Message);
        var after=Assert.IsType<CosmicSetQueryPlan<Holon>>(native.LastPlan);
        Assert.NotSame(before.Query.Left,after.Query.Left);Assert.NotSame(before.Query.Right,after.Query.Right);
        Assert.Equal("first",Assert.IsType<CosmicQueryComparison>(before.Query.Left.BusinessPredicates[0]).Literal.Value);
        Assert.Equal("changed-left",Assert.IsType<CosmicQueryComparison>(after.Query.Left.BusinessPredicates[0]).Literal.Value);
        Assert.Equal("second",Assert.IsType<CosmicQueryComparison>(before.Query.Right.BusinessPredicates[0]).Literal.Value);
        Assert.Equal("changed-right",Assert.IsType<CosmicQueryComparison>(after.Query.Right.BusinessPredicates[0]).Literal.Value);
        Assert.Equal(2,native.Calls);
    }
}

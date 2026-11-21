using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicSetQueryContractTests
{
    private static CosmicQuery<Holon> Root(Guid actor,ProviderType provider=ProviderType.MongoDBOASIS)
        =>CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
    private static CosmicQueryCapabilities Profile()=>new(
        CosmicQueryFeatures.And|CosmicQueryFeatures.Or|CosmicQueryFeatures.BooleanConstant|
        CosmicQueryFeatures.Ordering|CosmicQueryFeatures.Paging,
        new[]{
            new CosmicQueryFieldCapability("IsPublic",typeof(bool),CosmicQueryComparisonOperator.Equal),
            new CosmicQueryFieldCapability("CreatedByAvatarId",typeof(Guid),CosmicQueryComparisonOperator.Equal),
            new CosmicQueryFieldCapability("Name",typeof(string),new[]{CosmicQueryComparisonOperator.Equal},
                Array.Empty<CosmicQueryStringOperator>(),supportsOrdering:true)});
    private static CosmicSetQueryCapabilities Sets(CosmicEntityStageFeatures stages=CosmicEntityStageFeatures.Paging)
        =>new(CosmicSetQueryFeatures.Union,new CosmicEntityCompositionCapabilities(stages,Profile()));
    [Fact]
    public void AllFourOperationsPreserveIndependentInputsAndPostSetStages()
    {
        var actor=Guid.NewGuid();
        var left=Root(actor).OrderBy(h=>h.Name).Skip(1).Take(2).Result;
        var right=Root(actor).OrderByDescending(h=>h.Name).Take(3).Result;
        foreach(var kind in Enum.GetValues<CosmicSetQueryKind>())
        {
            var captured=CosmicSetQuery<Holon>.Create(left,right,kind);
            Assert.False(captured.IsError,captured.Message);
            var output=captured.Result.Take(1).Result.Where(h=>h.Name=="after-window").Result;
            Assert.Same(left,output.Left);Assert.Same(right,output.Right);Assert.Equal(kind,output.Kind);
            Assert.Equal(1,left.Offset);Assert.Equal(2,left.Limit);Assert.Equal(3,right.Limit);
            Assert.Empty(captured.Result.Output.Stages);Assert.Equal(2,output.Output.Stages.Count);
            Assert.IsType<CosmicEntityWindowStage>(output.Output.Stages[0]);
            Assert.IsType<CosmicEntityFilterStage<Holon>>(output.Output.Stages[1]);
        }
    }
    [Fact]
    public void DifferentActorsAuthoritiesAndUnboundSourcesAreNotCombined()
    {
        var actor=Guid.NewGuid();var root=Root(actor);
        Assert.Equal("CQL_SET_ACCESS_MISMATCH",CosmicSetQuery<Holon>.Create(root,Root(Guid.NewGuid()),CosmicSetQueryKind.Union).ErrorCode);
        Assert.Equal("CQL_SET_SCOPE_MISMATCH",CosmicSetQuery<Holon>.Create(root,Root(actor,ProviderType.SQLLiteDBOASIS),CosmicSetQueryKind.Union).ErrorCode);
        var unbound=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result).Result;
        Assert.Equal("CQL_SET_ACCESS_MISMATCH",CosmicSetQuery<Holon>.Create(unbound,unbound,CosmicSetQueryKind.Union).ErrorCode);
        Assert.Equal("CQL_SET_QUERY_INVALID",CosmicSetQuery<Holon>.Create(root,root,(CosmicSetQueryKind)99).ErrorCode);
    }
    [Fact]
    public void EmptyOuterPageCannotHideUnsupportedRightOperand()
    {
        var actor=Guid.NewGuid();var left=Root(actor).Where(h=>h.Name=="left").Result;
        var right=Root(actor).Where(h=>h.Name.Contains("unsupported")).Result;
        var target=new CosmicQueryTarget(ProviderType.MongoDBOASIS,Profile());
        var first=CosmicQueryPlanner.Plan(left,ProviderType.MongoDBOASIS,new[]{target});
        Assert.False(first.IsError,first.Message);
        var query=CosmicSetQuery<Holon>.Create(left,right,CosmicSetQueryKind.Union).Result.Take(0).Result;
        var plan=CosmicSetQueryPlanner.Plan(query,ProviderType.MongoDBOASIS,target,Sets());
        Assert.True(plan.IsError);Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",plan.ErrorCode);
    }
    [Fact]
    public void SetAndOutputCapabilitiesAreIndependentFromSourcePaging()
    {
        var actor=Guid.NewGuid();var left=Root(actor).Take(2).Result;var right=Root(actor).Take(3).Result;
        var target=new CosmicQueryTarget(ProviderType.MongoDBOASIS,Profile());
        var query=CosmicSetQuery<Holon>.Create(left,right,CosmicSetQueryKind.Union).Result.Take(1).Result;
        var accepted=CosmicSetQueryPlanner.Plan(query,ProviderType.MongoDBOASIS,target,Sets());
        Assert.False(accepted.IsError,accepted.Message);
        Assert.Same(left,accepted.Result.Left.Query);Assert.Same(right,accepted.Result.Right.Query);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",CosmicSetQueryPlanner.Plan(query,ProviderType.MongoDBOASIS,target,Sets(CosmicEntityStageFeatures.None)).ErrorCode);
        var concat=CosmicSetQuery<Holon>.Create(left,right,CosmicSetQueryKind.Concat).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",CosmicSetQueryPlanner.Plan(concat,ProviderType.MongoDBOASIS,target,Sets()).ErrorCode);
    }
}

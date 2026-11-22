using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicProjectedSetPlannerTests
{
    private static CosmicQuery<Holon> Root()=>CosmicQuery<Holon>
        .Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
    private static CosmicProjectionCompositionCapabilities Stages(CosmicSelectedStageFeatures stages)
        =>new(stages,CosmicQueryFeatures.None,Array.Empty<CosmicQueryMetadataCapability>(),Array.Empty<Type>());
    private static CosmicProjectionCompositionTarget Target(CosmicSelectedStageFeatures stages)
        =>new(ProviderType.MongoDBOASIS,new CosmicQueryCapabilities(CosmicQueryFeatures.None,Array.Empty<CosmicQueryFieldCapability>()),
            new CosmicProjectionCapabilities(new[]{new CosmicProjectionFieldCapability("Name",typeof(string))},Array.Empty<Type>()),
            Stages(stages));
    private static CosmicProjectedSetCapabilities Sets()=>new(CosmicSetQueryFeatures.Union,
        new[]{typeof(string)},new[]{typeof(string)},Stages(CosmicSelectedStageFeatures.Paging));
    [Fact]
    public void PlainOperandsUseProjectionAdmissionAndRetainIndependentBounds()
    {
        var selected=Root().Select(h=>h.Name).Result;
        var query=CosmicProjectedSetQuery<Holon,string>.Create(selected,selected,CosmicSetQueryKind.Union).Result;
        var options=new CosmicProjectedSetOptions(new CosmicSetQueryOptions(maximumLeftInputRows:3,maximumRightInputRows:5));
        var planned=CosmicProjectedSetPlanner.Plan(query,ProviderType.MongoDBOASIS,Target(CosmicSelectedStageFeatures.None),Sets(),options);
        Assert.False(planned.IsError,planned.Message);
        Assert.Same(selected,planned.Result.Left.Query);Assert.Same(selected,planned.Result.Right.Query);
        Assert.Equal(3,planned.Result.Left.Options.MaximumInputRows);
        Assert.Equal(5,planned.Result.Right.Options.MaximumInputRows);
        Assert.Empty(planned.Result.Left.Query.ResultStages);Assert.Empty(planned.Result.Right.Query.ResultStages);
    }
    [Fact]
    public void UnsupportedRightPipelineCannotHideBehindZeroOutput()
    {
        var selected=Root().Select(h=>h.Name).Result;
        var right=selected.Skip(1).Result;
        var query=CosmicProjectedSetQuery<Holon,string>.Create(selected,right,CosmicSetQueryKind.Union).Result.Take(0).Result;
        var refused=CosmicProjectedSetPlanner.Plan(query,ProviderType.MongoDBOASIS,Target(CosmicSelectedStageFeatures.None),Sets());
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",refused.ErrorCode);
        var admitted=CosmicProjectedSetPlanner.Plan(query,ProviderType.MongoDBOASIS,Target(CosmicSelectedStageFeatures.Paging),Sets());
        Assert.False(admitted.IsError,admitted.Message);
        Assert.Same(right,admitted.Result.Right.Query);
        Assert.Equal(0,Assert.IsType<CosmicSelectedWindowStage>(Assert.Single(admitted.Result.Query.Output.ResultStages)).Count);
    }
}

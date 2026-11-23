using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicLateSetCompositionProofTests
{
    [Fact]
    public void NativeOccurrenceProofCannotPushFilterBeforeTake()
    {
        Assert.Contains(typeof(CosmicSetProjectionPlanner).GetMethods(),
            method=>method.Name=="Plan" && method.GetParameters().Length==5);
        var source=CosmicQuery<Holon>.Create().Result;
        var set=CosmicSetQuery<Holon>.Create(source,source,CosmicSetQueryKind.Concat).Result;
        var query=set.Select(row=>row.Name).Result.Take(1).Result.Where(value=>value=="a").Result;
        var baseSelection=CosmicProjectionCompositionExtensions.Copy(query.Selection,Array.Empty<CosmicSelectedStage>()).Result;
        var sourcePlan=new CosmicQueryPlan<Holon>(source,Array.Empty<CosmicQueryTarget>());
        var setPlan=new CosmicSetQueryPlan<Holon>(set,sourcePlan,sourcePlan,
            new CosmicSetQueryOptions(maximumLeftInputRows:2,maximumRightInputRows:2,maximumResultRows:4));
        var selection=new CosmicProjectionPlan<Holon,string>(baseSelection,sourcePlan,Array.Empty<CosmicProjectionTarget>());
        var plan=new CosmicSetProjectionPlan<Holon,string>(query,setPlan,selection);
        var limits=CosmicProjectionCompositionExecutionLimits.From(plan.SelectedOptions,4,1048576);
        var input=new[]{new CosmicProjectionRow(new object[]{"b"}),new CosmicProjectionRow(new object[]{"a"}),
            new CosmicProjectionRow(new object[]{"a"}),new CosmicProjectionRow(new object[]{"b"})};
        var valid=new CosmicSetProjectionCompositionPacket(input,
            new[]{new CosmicSetProjectionSelectedStageProof(new[]{0}),new CosmicSetProjectionSelectedStageProof(Array.Empty<int>())},
            Array.Empty<CosmicProjectionRow>(),5);
        Assert.False(CosmicSetProjectionCompositionResults.Validate(plan,valid,limits).IsError);
        var pushed=new CosmicSetProjectionCompositionPacket(input,
            new[]{new CosmicSetProjectionSelectedStageProof(new[]{1}),new CosmicSetProjectionSelectedStageProof(new[]{1})},
            new[]{input[1]},5);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",CosmicSetProjectionCompositionResults.Validate(plan,pushed,limits).ErrorCode);
    }
}

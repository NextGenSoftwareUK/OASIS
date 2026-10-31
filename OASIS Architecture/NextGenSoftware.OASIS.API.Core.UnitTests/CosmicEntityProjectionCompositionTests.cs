using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityProjectionTests
{
    [Fact]
    public void SelectedStagesRetainTheExactEntityPageAndIndependentOrdering()
    {
        var initial=Plan(new Native()).Query;
        ProjectionDto.Constructors=0;
        var selected=initial.Where(row=>row.Revision>=2).Result
            .OrderBy(row=>row.Name).Result.ThenByDescending(row=>row.Revision).Result
            .Skip(1).Result.Take(2).Result.Reverse().Result;

        Assert.Same(initial.Source,selected.Source);
        Assert.Same(initial.Selection.Source,selected.Selection.Source);
        Assert.Empty(initial.Selection.ResultStages);
        Assert.Equal(3,selected.Source.Stages.Count);
        Assert.Equal(3,selected.Source.Source.Limit);
        Assert.Collection(selected.Selection.ResultStages,
            stage=>Assert.IsType<CosmicSelectedFilterStage>(stage),
            stage=>Assert.Equal(2,Assert.IsType<CosmicSelectedOrderStage>(stage).Orderings.Count),
            stage=>Assert.Equal(CosmicSelectedWindowKind.Skip,Assert.IsType<CosmicSelectedWindowStage>(stage).Kind),
            stage=>Assert.Equal(CosmicSelectedWindowKind.Take,Assert.IsType<CosmicSelectedWindowStage>(stage).Kind),
            stage=>Assert.IsType<CosmicSelectedReverseStage>(stage));
        Assert.Equal(0,ProjectionDto.Constructors);
    }

    [Fact]
    public void SelectedCaptureFreezesValuesAndUsesExistingColumnAndStageBounds()
    {
        var initial=Plan(new Native()).Query;
        var revision=2;
        var selected=initial.Where(row=>row.Revision>=revision).Result;
        revision=99;
        var comparison=Assert.IsType<CosmicQueryComparison>(Assert.IsType<CosmicSelectedFilterStage>(selected.Selection.ResultStages[0]).Predicate);
        Assert.Equal(2L,comparison.Literal.Value);
        Assert.True(initial.OrderBy(row=>row.Name.Length).IsError);
        Assert.True(initial.ThenBy(row=>row.Name).IsError);
        Assert.True(initial.Skip(-1).IsError);
        Assert.True(initial.Take(CosmicProjectionResults.MaximumResultLimit+1).IsError);
        var bounded=initial;
        for(var index=0;index<CosmicProjectionCompositionExtensions.MaximumStages;index++)
            bounded=bounded.Take(1).Result;
        Assert.Equal("CQL_QUERY_LIMIT",bounded.Reverse().ErrorCode);
        Assert.Empty(initial.Selection.ResultStages);
    }

    [Fact]
    public void SelectedCaptureCannotBeFlattenedIntoTheExistingNativeEntityProjectionContract()
    {
        var native=new Native();
        var selected=Plan(native).Query.Take(0).Result;
        var plan=CosmicEntityProjectionPlanner.Plan(selected,native.QueryProviderType,
            new CosmicProjectionTarget(native.QueryProviderType,native.QueryCapabilities,native.ProjectionCapabilities),native.EntityCompositionCapabilities);
        Assert.True(plan.IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",plan.ErrorCode);
        Assert.Equal(0,native.Calls);
        Assert.Empty(Plan(native).Query.Selection.ResultStages);
    }
}


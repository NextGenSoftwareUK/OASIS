using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityCompositionTests
{
    [Fact]
    public void EntityReversalRequiresIndependentPermissionAndKeepsSourceAndStageOrder()
    {
        var native=new Native();var source=Source().Take(3).Result;
        var query=source.Compose().Result.OrderBy(row=>row.Name).Result.Reverse().Result.Take(2).Result;
        Assert.Same(source,query.Source);Assert.Equal(3,query.Stages.Count);
        Assert.IsType<CosmicEntityReverseStage>(query.Stages[1]);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",native.EntityCompositionCapabilities.Admit(query).ErrorCode);
        var enrolled=new CosmicEntityCompositionCapabilities(native.EntityCompositionCapabilities.Stages|CosmicEntityStageFeatures.Reverse,native.QueryCapabilities);
        Assert.False(enrolled.Admit(query).IsError);
    }
    [Fact]
    public void ReversalCannotExtendAnEarlierOrderingWithThenByOrBypassStageLimit()
    {
        var source=Source().Compose().Result.OrderBy(row=>row.Name).Result.Reverse().Result;
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED",source.ThenBy(row=>row.Name).ErrorCode);
        var pipeline=Source().Compose().Result;
        for(var stage=0;stage<CosmicEntityCompositionQuery<Holon>.MaximumStages;stage++)pipeline=pipeline.Reverse().Result;
        Assert.Equal("CQL_QUERY_LIMIT",pipeline.Reverse().ErrorCode);
    }
}

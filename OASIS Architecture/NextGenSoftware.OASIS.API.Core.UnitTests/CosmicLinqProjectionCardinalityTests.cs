using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicLinqProjectionElementTests
{
    [Fact]
    public void SelectedCardinalityUsesNativeAcknowledgementsWithoutDtoConstruction()
    {
        var native = new Native();
        var selected = Root(native).Select(row => new Dto { Label = row.Name });
        Dto.Constructions = 0;
        Assert.Equal(3, selected.Count());
        Assert.Equal(2L, selected.Skip(1).LongCount());
        Assert.True(selected.Any());
        Assert.False(selected.Take(0).Any());
        Assert.Equal(0, Dto.Constructions);
        Assert.Equal(4, native.Calls);
    }

    [Fact]
    public void SelectedCardinalityRequiresIndependentEnrollmentBeforeNativeIo()
    {
        var native = new Native { CardinalityEnabled = false };
        var selected = Root(native).Select(row => row.Name);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Assert.Throws<CosmicLinqException>(() => selected.Count()).Code);
        Assert.Equal(0, native.Calls);
    }

    [Fact]
    public void MissingAndContradictoryCardinalityCannotBecomeCountOrAny()
    {
        var native = new Native { MissingCardinality = true };
        var selected = Root(native).Select(row => new Dto { Label = row.Name });
        Dto.Constructions = 0;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Assert.Throws<CosmicLinqException>(() => selected.Count()).Code);
        native.MissingCardinality = false;
        native.CountOverride = 0;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Assert.Throws<CosmicLinqException>(() => selected.Any()).Code);
        Assert.Equal(0, Dto.Constructions);
    }

    [Fact]
    public async Task NativeTerminalScanConsumesTheRemainingStageWorkBudget()
    {
        var native = new Native();
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result)
            .Select(row => row.Name).Take(1).Result;
        var plan = CosmicProjectedCardinalityPlanner.Plan<Holon, string, int>(query, CosmicScalarOperation.Count,
            native.QueryProviderType, new[] { new CosmicProjectionCompositionTarget(native.QueryProviderType,
                native.QueryCapabilities, native.ProjectionCapabilities, native.CompositionCapabilities) },
            new CosmicProjectionCompositionOptions(maximumStageRows: 3));
        Assert.False(plan.IsError, plan.Message);
        var result = await CosmicProjectedCardinalityExecutor.ExecuteAsync(plan.Result, new[] { native });
        Assert.Equal("CQL_RESULT_LIMIT", result.ErrorCode);
        Assert.Equal(1, native.Calls);
    }

    [Fact]
    public void CardinalityDomainsRetainExactLongCountsAndCheckedIntCounts()
    {
        Assert.Equal(long.MaxValue, CosmicProjectedCardinalityResults.FromCount<long>(CosmicScalarOperation.LongCount, long.MaxValue).Result);
        Assert.Equal("CQL_SCALAR_OVERFLOW", CosmicProjectedCardinalityResults.FromCount<int>(CosmicScalarOperation.Count, (long)int.MaxValue + 1).ErrorCode);
        Assert.False(CosmicProjectedCardinalityResults.FromCount<bool>(CosmicScalarOperation.Any, 0).Result);
        Assert.True(CosmicProjectedCardinalityResults.FromCount<bool>(CosmicScalarOperation.Any, long.MaxValue).Result);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectedCardinalityResults.FromCount<long>(CosmicScalarOperation.LongCount, -1).ErrorCode);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectedCardinalityResults.FromCount<long>(CosmicScalarOperation.Count, 1).ErrorCode);
    }
}

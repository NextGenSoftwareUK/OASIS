using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicLinqProjectionElementTests
{
    [Fact]
    public void SelectedExtremaRequiresIndependentEnrollmentAndNeverConstructsRecords()
    {
        var native = new Native { Values = new[] { "c", "a", "b" } };
        var selected = Root(native).Select(row => new Dto { Label = row.Name }).Skip(1);
        Dto.Constructions = 0;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Assert.Throws<CosmicLinqException>(() => selected.Min(row => row.Label)).Code);
        Assert.Equal(0, native.Calls);
        native.ExtremaEnabled = true;
        Assert.Equal("a", selected.Min(row => row.Label));
        Assert.Equal("b", selected.Max(row => row.Label));
        Assert.Equal(0, Dto.Constructions);
    }

    [Fact]
    public void SelectedExtremaIgnoresNullsAfterWindowsAndKeepsEmptyResultDomains()
    {
        var native = new Native { ExtremaEnabled = true, Values = new[] { "a", null, "z" } };
        var selected = Root(native).Select(row => row.Name).Skip(1);
        Assert.Equal("z", selected.Min());
        Assert.Equal("z", selected.Max());
        Assert.Null(selected.Take(1).Min());
        Assert.Null(selected.Take(0).Max());
        native.NumericValues = new object?[] { null, 7, 2 };
        var optional = Root(native).Select(row => CosmicQueryMetadata.Value<int?>(row, "optional")).Take(3);
        Assert.Equal(2, optional.Min());
        Assert.Equal(7, optional.Max());
        Assert.Null(optional.Take(0).Min());
        native.NumericValues = new object?[] { 3, 1, 2 };
        var required = Root(native).Select(row => row.Version).Skip(1);
        Assert.Equal(1, required.Min());
        Assert.Equal(2, required.Max());
        Assert.Equal("CQL_EMPTY_SEQUENCE", Assert.Throws<CosmicLinqException>(() => required.Take(0).Min()).Code);
    }

    [Fact]
    public void SelectedExtremaProofFailureAndLegacyOwnershipNeverReturnDefaults()
    {
        var native = new Native { ExtremaEnabled = true, Corrupt = true };
        var selected = Root(native).Select(row => new Dto { Label = row.Name }).Take(2);
        Dto.Constructions = 0;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Assert.Throws<CosmicLinqException>(() => selected.Min(row => row.Label)).Code);
        Assert.Equal(0, Dto.Constructions);
        native.Corrupt = false;
        native.Failure = "CQL_QUERY_LIMIT";
        Assert.Equal("CQL_QUERY_LIMIT", Assert.Throws<CosmicLinqException>(() => selected.Max(row => row.Label)).Code);
        var legacy = Root(native, "legacy").Select(row => row.Name).Take(2);
        Assert.Equal("CQL_V2_REQUIRED", Assert.Throws<CosmicLinqException>(() => legacy.Min()).Code);
    }
    [Fact]
    public async Task SelectedExtremaReadmitsLiveDomainsBeforeCallingNativeStorage()
    {
        var native = new Native { ExtremaEnabled = true };
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result)
            .Select(row => row.Name).Take(2).Result;
        var targets = new[] { new CosmicProjectionCompositionTarget(native.QueryProviderType,
            native.QueryCapabilities, native.ProjectionCapabilities, native.CompositionCapabilities) };
        var planned = CosmicProjectedExtremumPlanner.Plan<Holon, string, string>(query, 0,
            CosmicScalarOperation.Min, native.QueryProviderType, targets);
        Assert.False(planned.IsError, planned.Message);
        native.ExtremaEnabled = false;
        var rejected = await CosmicProjectedExtremumExecutor.ExecuteAsync(planned.Result,
            new ICosmicQueryStorageProvider[] { native });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", rejected.ErrorCode);
        Assert.Equal(0, native.Calls);
        Assert.Equal("CQL_PLAN_INVALID", CosmicProjectedExtremumPlanner.Plan<Holon, string, int>(query, 0,
            CosmicScalarOperation.Min, native.QueryProviderType, targets).ErrorCode);
        Assert.Equal("CQL_PLAN_INVALID", CosmicProjectedExtremumPlanner.Plan<Holon, string, string>(query, 0,
            CosmicScalarOperation.Sum, native.QueryProviderType, targets).ErrorCode);
        for (var i = 0; i < 29; i++) query = query.Take(1).Result;
        Assert.Equal("CQL_QUERY_LIMIT", CosmicProjectedExtremumPlanner.Plan<Holon, string, string>(query, 0,
            CosmicScalarOperation.Max, native.QueryProviderType, targets).ErrorCode);
    }

}

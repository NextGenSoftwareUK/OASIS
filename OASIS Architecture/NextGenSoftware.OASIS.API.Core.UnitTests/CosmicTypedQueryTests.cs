using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicTypedQueryTests
{
    public sealed class TypedRow : Holon { [CustomOASISProperty] public int Rating { get; set; } }
    public sealed class ThrowingRow : Holon { [CustomOASISProperty] public int Rating { get => 0; set => throw new InvalidOperationException("owned setter failure"); } }
    private sealed class Adapter : ICosmicQueryStorageProvider
    {
        public int Calls; public IHolon Row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["Rating"] = 7 } };
        public ProviderType QueryProviderType => ProviderType.SQLLiteDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan, int maximumResults = 100,
            CancellationToken cancellationToken = default) where T : IHolon
        { Calls++; return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(new[] { Row })); }
    }
    private static CosmicQueryPlan<T> Plan<T>(Adapter adapter) where T : IHolon
        => CosmicQueryPlanner.Plan(CosmicQuery<T>.Create(), adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) }).Result;
    private static HolonManager Manager(string mode = HyperDriveModes.V2)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = mode } };
        return new HolonManager(null, dna, new ProviderManager(null, dna));
    }
    [Fact]
    public async Task TypedQueriesHydrateDetachedValuesAndPublishCleanTrackingWithProvenance()
    {
        var adapter = new Adapter(); var result = await Manager().ExecuteQueryAsync(Plan<TypedRow>(adapter), new[] { adapter });
        Assert.False(result.IsError, result.Message + result.Exception); Assert.Equal(1, result.Result.TotalRows);
        var group = Assert.Single(result.Result.Providers); Assert.Equal(ProviderType.SQLLiteDBOASIS, group.Provider);
        var row = Assert.Single(group.Rows); Assert.Equal(7, row.Rating); Assert.False(HolonChangeTracking.HasChanges(row));
        Assert.NotSame(adapter.Row, row); adapter.Row.MetaData["Rating"] = 99; Assert.Equal(7, row.Rating);
        row.Rating = 8; Assert.True(HolonChangeTracking.HasChanges(row));
    }
    [Fact]
    public async Task TypedSetterFailureReturnsAnExplicitErrorWithoutMutatingProviderRows()
    {
        var adapter = new Adapter(); var id = adapter.Row.Id;
        var result = await Manager().ExecuteQueryAsync(Plan<ThrowingRow>(adapter), new[] { adapter });
        Assert.True(result.IsError); Assert.False(result.IsLoaded); Assert.Null(result.Result);
        Assert.Equal("COSMIC_METADATA_HYDRATION_FAILED", result.ErrorCode); Assert.NotNull(result.Exception);
        Assert.Contains("owned setter failure", result.Exception.ToString()); Assert.Equal(id, adapter.Row.Id); Assert.Equal(7, adapter.Row.MetaData["Rating"]);
    }
    [Fact]
    public async Task TypedCqlRejectsUnsupportedModeBeforeAdapterIO()
    {
        var adapter = new Adapter(); var result = await Manager(HyperDriveModes.Legacy).ExecuteQueryAsync(Plan<TypedRow>(adapter), new[] { adapter });
        Assert.Equal("CQL_V2_REQUIRED", result.ErrorCode); Assert.Null(result.Result); Assert.Equal(0, adapter.Calls);
    }
}

using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed partial class CosmicSetQueryExecutorTests
{
    private static HolonManager SetManager(string mode = HyperDriveModes.V2)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = mode } };
        return new HolonManager(null, dna, new ProviderManager(null, dna));
    }
    [Fact]
    public async Task TypedSetConcatKeepsIndependentOccurrencesThroughHydration()
    {
        var native = new Native();
        var row = native.Data[0]; row.Name = "original";
        native.Data = new[] { row, row };
        var result = await SetManager().ExecuteTypedSetQueryAsync(Plan(native, CosmicSetQueryKind.Concat),
            new ICosmicQueryStorageProvider[] { native });
        Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded);
        var rows = Assert.Single(result.Result.Providers).Rows;
        Assert.Equal(2, rows.Count);
        Assert.NotSame(row, rows[0]); Assert.NotSame(rows[0], rows[1]);
        rows[0].Name = "changed";
        Assert.Equal("original", rows[1].Name); Assert.Equal("original", row.Name);
        Assert.Equal(1, native.SetCalls); Assert.Equal(0, native.FlatCalls);
    }
    [Fact]
    public async Task TypedAndRawSetRequireV2BeforeNativeDispatch()
    {
        var native = new Native(); var plan = Plan(native, CosmicSetQueryKind.Union);
        var manager = SetManager("V1");
        var raw = await manager.ExecuteSetQueryAsync(plan, new ICosmicQueryStorageProvider[] { native });
        var typed = await manager.ExecuteTypedSetQueryAsync(plan, new ICosmicQueryStorageProvider[] { native });
        Assert.Equal("CQL_V2_REQUIRED", raw.ErrorCode); Assert.Equal("CQL_V2_REQUIRED", typed.ErrorCode);
        Assert.Equal(0, native.SetCalls); Assert.Equal(0, native.FlatCalls);
    }
    [Fact]
    public async Task TypedSetRejectsForeignAndIncompleteNativeAcknowledgements()
    {
        var native = new Native { Loaded = false };
        var plan = Plan(native, CosmicSetQueryKind.Union); var manager = SetManager();
        var incomplete = await manager.ExecuteTypedSetQueryAsync(plan, new ICosmicQueryStorageProvider[] { native });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", incomplete.ErrorCode);
        native.Loaded = true;
        native.Data = new IHolon[] { new Holon { Id = Guid.NewGuid(), IsPublic = false, CreatedByAvatarId = Guid.NewGuid() } };
        var forbidden = await manager.ExecuteTypedSetQueryAsync(plan, new ICosmicQueryStorageProvider[] { native });
        Assert.Equal("CQL_VISIBILITY_VIOLATION", forbidden.ErrorCode);
    }
}

using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicCustomPropertyStorageProjectionTests
{
    public sealed class CustomOwner : Holon
    {
        [CustomOASISProperty, JsonProperty("wire/score")] public long Score { get; set; }
        [CustomOASISProperty(StoreAsJsonString = true)] public int JsonRating { get; set; }
        [CustomOASISProperty(StoreAsJsonString = true)] public List<string> Tags { get; set; } = new();
        [CustomOASISProperty] public Dictionary<string, int> Address { get; set; } = new();
        [CustomOASISProperty] public string? Note { get; set; }
        [CustomOASISProperty, JsonIgnore] public int Hidden => 7;
    }

    private static (CustomOwner Model, Dictionary<string, string> Map) Extract()
    {
        var model = new CustomOwner { Id = Guid.NewGuid(), Score = 9007199254740993L, JsonRating = 37,
            Tags = new() { "first", "second" }, Address = new() { ["Zip"] = 1, ["City"] = 2 } };
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        HolonManager.ExtractCustomPropertiesForStorage(model, map);
        return (model, map);
    }

    private static HolonMetadataStorageSelection Report(bool encrypted = false, IEnumerable<string>? queryable = null,
        bool materialization = false)
        => new HolonMetadataStorageSelection(encrypted, queryable ?? Array.Empty<string>(), materialization)
            .WithMetadataProperties(Extract().Map);

    [Fact]
    public void ActualExtractorReportsCanonicalAliasAndExactStoredInteger()
    {
        var (model, map) = Extract();
        Assert.Equal(nameof(CustomOwner.Score), map["wire/score"]);
        Assert.Equal(9007199254740993L, Assert.IsType<long>(model.MetaData[nameof(CustomOwner.Score)]));
        Assert.Equal(new[] { "/MetaData/Score" }, Report().StoredPaths(new[] { "/wire~1score" }));
    }

    [Fact]
    public void ActualExtractorOwnsJsonStringAndNullEncoding()
    {
        var (model, _) = Extract();
        Assert.Equal("37", Assert.IsType<string>(model.MetaData[nameof(CustomOwner.JsonRating)]));
        Assert.Equal("[\"first\",\"second\"]", Assert.IsType<string>(model.MetaData[nameof(CustomOwner.Tags)]));
        Assert.True(model.MetaData.ContainsKey(nameof(CustomOwner.Note))); Assert.Null(model.MetaData[nameof(CustomOwner.Note)]);
        Assert.Equal(new[] { "/MetaData/JsonRating" }, Report().StoredPaths(new[] { "/JsonRating" }));
    }

    [Fact]
    public void NestedLogicalEditsShareOneWholeEncodedCell()
        => Assert.Equal(new[] { "/MetaData/Address" }, Report().StoredPaths(new[] { "/Address/City", "/Address/Zip" }));

    [Fact]
    public void NativeAndUnmappedMetadataFieldsKeepTheirStoragePaths()
        => Assert.Equal(new[] { "/Description", "/MetaData/Score", "/MetaData/status" },
            Report().StoredPaths(new[] { "/Description", "/wire~1score", "/MetaData/status" }));

    [Fact]
    public void ProtectedCustomValueUsesTheEncryptionOwnersWholeMetadataMask()
        => Assert.Equal(new[] { "/MetaData" }, Report(true).StoredPaths(new[] { "/wire~1score" }));

    [Fact]
    public void QueryableCustomCellRetainsItsExactMetadataPath()
        => Assert.Equal(new[] { "/MetaData/Score" }, Report(true, new[] { "Score" }).StoredPaths(new[] { "/wire~1score" }));

    [Fact]
    public void MultiplePrivateCustomCellsWidenOnlyOnce()
        => Assert.Equal(new[] { "/Description", "/MetaData" },
            Report(true).StoredPaths(new[] { "/wire~1score", "/JsonRating", "/Description" }));

    [Fact]
    public void ProjectionReportIsDetachedAndDoesNotMutateTheNoneSingleton()
    {
        var (_, map) = Extract(); var report = HolonMetadataStorageSelection.None.WithMetadataProperties(map);
        map["wire/score"] = "changed";
        Assert.Equal(new[] { "/MetaData/Score" }, report.StoredPaths(new[] { "/wire~1score" }));
        Assert.Equal(new[] { "/wire~1score" }, HolonMetadataStorageSelection.None.StoredPaths(new[] { "/wire~1score" }));
    }

    [Fact]
    public void CustomAndExactBackingMetadataCellCannotBothBeSelected()
        => Assert.Throws<ArgumentException>(() => Report().StoredPaths(new[] { "/wire~1score", "/MetaData/Score" }));

    [Fact]
    public void CustomAndWholeMetadataCannotBothBeSelected()
        => Assert.Throws<ArgumentException>(() => Report().StoredPaths(new[] { "/wire~1score", "/MetaData" }));

    [Fact]
    public void DuplicateLogicalInputsAreNotHiddenByStorageCoalescing()
        => Assert.Throws<ArgumentException>(() => Report().StoredPaths(new[] { "/wire~1score", "/wire~1score" }));

    [Fact]
    public void OpaqueMetadataStillRequiresAuthorizedMaterialization()
        => Assert.Throws<InvalidOperationException>(() => Report(true, materialization: true).StoredPaths(new[] { "/wire~1score" }));

    [Fact]
    public void IgnoredReadonlyCustomFieldGetsNoEditableProjection()
    {
        var (model, map) = Extract();
        Assert.Equal(7, model.MetaData[nameof(CustomOwner.Hidden)]); Assert.False(map.ContainsKey(nameof(CustomOwner.Hidden)));
        Assert.DoesNotContain("9007199254740993", map.Values);
    }

    [Fact]
    public void BaseHolonKeepsNativePathsAndAnEmptyAttachmentReusesItsReport()
    {
        var map = new Dictionary<string, string>();
        HolonManager.ExtractCustomPropertiesForStorage(new Holon { Id = Guid.NewGuid() }, map);
        Assert.Empty(map);
        Assert.Same(HolonMetadataStorageSelection.None, HolonMetadataStorageSelection.None.WithMetadataProperties(map));
        Assert.Equal(new[] { "/Description" }, HolonMetadataStorageSelection.None.StoredPaths(new[] { "/Description" }));
    }
}

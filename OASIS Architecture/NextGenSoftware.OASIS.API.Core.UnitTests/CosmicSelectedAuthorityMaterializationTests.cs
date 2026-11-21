using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicSelectedAuthorityMaterializationTests
{
    public sealed class TypedOwner : Holon
    {
        [CustomOASISProperty] public int Rating { get; set; }
        [CustomOASISProperty] public long Exact { get; set; }
    }

    [Fact]
    public void DeclaredMaterializationRetainsNativeIdentityExactValuesAndEncryptionContext()
    {
        var original = new Holon { Id = Guid.NewGuid(), Version = 3, VersionId = Guid.NewGuid(), Name = "native",
            CreatedByAvatarId = Guid.NewGuid(), MetaData = new() { ["Rating"] = 7, ["Exact"] = 9007199254740993L } };
        var revision = Guid.NewGuid();
        original.ProviderMetaData[ProviderType.SQLLiteDBOASIS] = new()
            { [CosmicConcurrency.ProviderRevisionMetadataKey] = revision.ToString("D") };
        var detached = HolonChangeTracking.Clone(original);
        var settings = new EncryptionSettings { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('t', 32) };
        ((Holon)detached).DataEncryptionOverride = settings;

        var materialized = Assert.IsType<TypedOwner>(HolonManager.MaterializeV2SelectedAuthority(detached, typeof(TypedOwner)));

        Assert.Equal(original.Id, materialized.Id); Assert.Equal(original.CreatedByAvatarId, materialized.CreatedByAvatarId);
        Assert.Equal(original.VersionId, materialized.VersionId); Assert.Equal(3, materialized.Version);
        Assert.Equal(revision, CosmicConcurrency.GetProviderRevisionId(materialized, ProviderType.SQLLiteDBOASIS));
        Assert.Equal(7, materialized.Rating); Assert.Equal(9007199254740993L, materialized.Exact);
        Assert.Same(settings, materialized.DataEncryptionOverride);
        materialized.MetaData["Rating"] = 99;
        Assert.Equal(7, original.MetaData["Rating"]);
    }

    [Fact]
    public void SelectionUsesHydratedPrivateValuesWithoutPublishingThemToTheCaller()
    {
        var id = Guid.NewGuid();
        var native = new Holon { Id = id, Name = "native", Description = "before",
            MetaData = new() { ["Rating"] = 7, ["Exact"] = 9007199254740993L, ["private"] = "hidden" } };
        var caller = new TypedOwner { Id = id, Name = "excluded local name", Description = "selected", Rating = 900 };
        var beforeCaller = StoredRecordJson.SerializeRuntime(caller);
        var typed = HolonManager.MaterializeV2SelectedAuthority(HolonChangeTracking.Clone(native), typeof(TypedOwner));

        var selected = new HolonPropertyWriteSelection(new[] { "/Description" }).Apply(typed, caller);

        Assert.False(selected.IsError, selected.Message + selected.Exception);
        var merged = Assert.IsType<TypedOwner>(selected.Result);
        Assert.Equal("native", merged.Name); Assert.Equal("selected", merged.Description); Assert.Equal(7, merged.Rating);
        Assert.Equal("hidden", merged.MetaData["private"]);
        Assert.Equal(beforeCaller, StoredRecordJson.SerializeRuntime(caller)); Assert.False(caller.MetaData.ContainsKey("private"));
    }

    [Theory]
    [InlineData("9007199254740993")]
    [InlineData("0.5")]
    public void UnrepresentableTypedMetadataIsRefused(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var native = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["Rating"] = document.RootElement.Clone() } };
        var snapshot = StoredRecordJson.SerializeRuntime(native);

        Assert.ThrowsAny<Exception>(() => HolonManager.MaterializeV2SelectedAuthority(
            HolonChangeTracking.Clone(native), typeof(TypedOwner)));

        Assert.Equal(snapshot, StoredRecordJson.SerializeRuntime(native));
    }

    [Fact]
    public void StoredMembershipIsRetainedWithoutGrantingACompleteRuntimeView()
    {
        var native = new Holon { Id = Guid.NewGuid() };
        native.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1, native.Id,
            new[] { new HolonChildRole("Children", new[] { Guid.NewGuid() }) }));
        var typed = Assert.IsType<TypedOwner>(HolonManager.MaterializeV2SelectedAuthority(
            HolonChangeTracking.Clone(native), typeof(TypedOwner)));

        Assert.Equal(native.ChildRoleManifestJson, typed.ChildRoleManifestJson); Assert.Empty(typed.Children);
        Assert.Throws<InvalidOperationException>(() => HolonChangeTracking.ValidateChildSelectionForSave(typed));
    }

    [Fact]
    public void CanonicalSelectionStillRejectsDifferentRuntimeContracts()
    {
        var native = new Holon { Id = Guid.NewGuid(), Description = "before" };
        var caller = new TypedOwner { Id = native.Id, Description = "after" };

        var result = new HolonPropertyWriteSelection(new[] { "/Description" }).Apply(native, caller);

        Assert.True(result.IsError); Assert.Equal("HOLON_PROPERTY_SELECTION_INVALID", result.ErrorCode);
        Assert.Equal("before", native.Description);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(IHolon))]
    public void NonConcreteHolonContractsAreRefused(Type contract)
        => Assert.Throws<ArgumentException>(() => HolonManager.MaterializeV2SelectedAuthority(
            new Holon { Id = Guid.NewGuid() }, contract));
}

using System;
using System.Linq;
using System.Text.Json;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicCanonicalPermissionChangesTests
{
    private static Holon Record() => new() { Id = Guid.NewGuid(), Name = "before", Description = "unchanged" };
    private static JsonElement Cell(string json) { using var document = JsonDocument.Parse(json); return document.RootElement.Clone(); }

    [Fact]
    public void EquivalentNestedJsonFormattingDoesNotRequireUnrelatedFieldPermission()
    {
        var before = Record();
        before.MetaData["private"] = Cell("{\"a\":9007199254740993,\"b\":[2,3]}");
        var after = HolonChangeTracking.Clone(before);
        after.MetaData["private"] = Cell("{\n \"a\":9007199254740993,\n \"b\":[2,3]\n}");
        Assert.Empty(HolonFieldProjection.Changes(before, after));
    }

    [Fact]
    public void RuntimeOnlyEncryptionOverridesAreNotPersistentFieldChanges()
    {
        var before = Record(); var after = (Holon)HolonChangeTracking.Clone(before);
        after.DataEncryptionOverride = new EncryptionSettings { BCryptEncryptionEnabled = true };
        Assert.Empty(HolonFieldProjection.Changes(before, after));
    }

    [Fact]
    public void ScalarChangeDoesNotAcquirePermissionsForEquivalentPrivateMetadata()
    {
        var before = Record(); before.MetaData["private"] = Cell("{\"nested\":[1,2]}");
        var after = HolonChangeTracking.Clone(before);
        after.MetaData["private"] = Cell("{ \"nested\" : [1,2] }");
        after.Description = "after";
        Assert.Equal(new[] { ("description", HolonPermission.Write) }, HolonFieldProjection.Changes(before, after));
    }

    [Fact]
    public void RemovedMetadataRequiresDelete()
    {
        var before = Record(); before.MetaData["private"] = 5L;
        var after = HolonChangeTracking.Clone(before); after.MetaData.Remove("private");
        Assert.Equal(new[] { ("metadata.private", HolonPermission.Delete) }, HolonFieldProjection.Changes(before, after));
    }

    [Fact]
    public void AddedNullMetadataRequiresWriteRatherThanLookingAbsent()
    {
        var before = Record(); var after = HolonChangeTracking.Clone(before); after.MetaData["nullable"] = null;
        Assert.Equal(new[] { ("metadata.nullable", HolonPermission.Write) }, HolonFieldProjection.Changes(before, after));
    }

    public sealed class RenamedRecord : Holon
    {
        [JsonProperty("wire-caption")] public string Caption { get; set; }
    }

    [Fact]
    public void CanonicalJsonNameStillUsesTheUnderlyingPermissionField()
    {
        var before = new RenamedRecord { Id = Guid.NewGuid(), Caption = "before" };
        var after = (RenamedRecord)HolonChangeTracking.Clone(before); after.Caption = "after";
        Assert.Equal(new[] { ("caption", HolonPermission.Write) }, HolonFieldProjection.Changes(before, after));
    }

    [Fact]
    public void AuditAndReadOnlyDerivedDeletedStateAreNotContentFields()
    {
        var before = Record(); var after = HolonChangeTracking.Clone(before);
        after.DeletedDate = new DateTime(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc);
        after.ModifiedDate = after.DeletedDate;
        Assert.Empty(HolonFieldProjection.Changes(before, after));
    }

    [Fact]
    public void ChangedAccessMetadataRetainsItsManageScope()
    {
        var before = Record(); before.MetaData[HolonPermissions.MetadataKey] = "{}";
        var after = HolonChangeTracking.Clone(before); after.MetaData[HolonPermissions.MetadataKey] = "{\"public\":[\"read\"]}";
        var change = Assert.Single(HolonFieldProjection.Changes(before, after));
        Assert.Equal("metadata." + HolonPermissions.MetadataKey, change.Field);
        Assert.Equal(HolonPermission.Write, change.Needed);
        Assert.True(HolonFieldProjection.IsAccessMetadata(change.Field["metadata.".Length..]));
    }
}

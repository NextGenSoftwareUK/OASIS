using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicNullablePropertyEntryTests
{
    public sealed class NullableOwner : Holon
    {
        public string? Note { get; set; }
        [JsonProperty("nullable/name")] public int? Optional { get; set; }
        [JsonIgnore] public string? Ignored { get; set; }
        public string Skipped { get; set; } = "present but suppressed";
        public bool ShouldSerializeSkipped() => false;
    }

    private static NullableOwner Tracked(string? note = null)
    {
        var value = new NullableOwner { Id = Guid.NewGuid(), Note = note };
        HolonChangeTracking.AcceptChanges(value, false);
        return value;
    }

    private static HolonPropertyValues Inspect(NullableOwner value, string path = "/Note")
    {
        var result = HolonChangeTracking.GetPropertyValues(value, path);
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.NotNull(result.Result);
        return result.Result;
    }

    [Fact]
    public void OmittedNullableRootHasOriginalAndCurrentNullEntries()
    {
        var entry = Inspect(Tracked());
        Assert.True(entry.OriginalExists); Assert.True(entry.CurrentExists);
        Assert.Equal("null", entry.OriginalJson); Assert.Equal("null", entry.CurrentJson);
        Assert.False(entry.IsModified);
    }

    [Fact]
    public void TypedNullEditRemainsInspectableAndResetRestoresOriginal()
    {
        var value = Tracked("before");
        Assert.False(HolonChangeTracking.SetCurrentPropertyValue<string?>(value, "/Note", null).IsError);
        var edited = Inspect(value);
        Assert.Equal("\"before\"", edited.OriginalJson); Assert.Equal("null", edited.CurrentJson);
        Assert.True(edited.IsValueModified);
        Assert.False(HolonChangeTracking.ResetPropertyValue(value, "/Note").IsError);
        Assert.Equal("before", value.Note); Assert.False(Inspect(value).IsModified);
    }

    [Fact]
    public void EqualNullCanBeForcedAndClearedWithoutInventingAValue()
    {
        var value = Tracked();
        Assert.False(HolonChangeTracking.SetPropertyModified(value, "/Note").IsError);
        Assert.True(Inspect(value).IsExplicitlyModified);
        Assert.False(HolonChangeTracking.SetPropertyModified(value, "/Note", false).IsError);
        Assert.Null(value.Note); Assert.False(Inspect(value).IsModified);
    }

    [Fact]
    public void MissingMetadataKeyIsNotNormalizedIntoANullEntry()
    {
        var value = Tracked();
        Assert.True(HolonChangeTracking.GetPropertyValues(value, "/MetaData/missing").IsError);
        value.MetaData["status"] = "added";
        var result = HolonChangeTracking.GetPropertyValues(value, "/MetaData/status");
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.False(result.Result.OriginalExists); Assert.True(result.Result.CurrentExists);
        Assert.Null(result.Result.OriginalJson); Assert.Equal("\"added\"", result.Result.CurrentJson);
    }

    [Fact]
    public void UnknownAndIgnoredRootsRemainInvalid()
    {
        var value = Tracked();
        Assert.True(HolonChangeTracking.GetPropertyValues(value, "/Unknown").IsError);
        Assert.True(HolonChangeTracking.GetPropertyValues(value, "/Ignored").IsError);
    }

    [Fact]
    public void NullableValueTypeUsesItsCanonicalEscapedAlias()
    {
        var value = Tracked(); var entry = Inspect(value, "/nullable~1name");
        Assert.Equal("null", entry.OriginalJson); Assert.Equal("null", entry.CurrentJson);
        Assert.False(entry.IsModified);
        Assert.True(HolonChangeTracking.GetPropertyValues(value, "/Optional").IsError);
    }

    [Fact]
    public void ConditionallySuppressedNonNullMemberIsNotInventedAsNull()
    {
        var value = Tracked();
        Assert.Equal("present but suppressed", value.Skipped);
        Assert.True(HolonChangeTracking.GetPropertyValues(value, "/Skipped").IsError);
    }

    [Fact]
    public void RebasedNullOriginalRemainsInspectableAndResettable()
    {
        var value = Tracked("before"); value.Note = "current";
        var rebased = HolonChangeTracking.SetOriginalPropertyValue<string?>(value, "/Note", null);
        Assert.False(rebased.IsError, rebased.Message + rebased.Exception);
        var entry = Inspect(value);
        Assert.Equal("null", entry.OriginalJson); Assert.Equal("\"current\"", entry.CurrentJson);
        Assert.True(entry.IsModified);
        Assert.False(HolonChangeTracking.ResetPropertyValue(value, "/Note").IsError);
        Assert.Null(value.Note); Assert.False(Inspect(value).IsModified);
    }
}

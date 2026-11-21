using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicOriginalPropertyEditingTests
{
    private static Holon Record()
    {
        var value = new Holon { Id = Guid.NewGuid(), Name = "original", Description = "original description",
            Version = 1, VersionId = Guid.NewGuid() };
        CosmicConcurrency.SetProviderRevisionId(value, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        HolonChangeTracking.AcceptChanges(value, false);
        return value;
    }
    private static void Good(OASISResult<bool> result) => Assert.False(result.IsError, result.Message + result.Exception);
    private static HolonPropertyValues Property(IHolon value, string path)
    {
        var result = HolonChangeTracking.GetPropertyValues(value, path);
        Assert.False(result.IsError, result.Message + result.Exception);
        return result.Result;
    }

    [Fact]
    public void OriginalScalarEditDoesNotEditCurrentOrNativeObservation()
    {
        var value = Record(); var current = StoredRecordJson.SerializeRuntime(value);
        Assert.True(HolonChangeTracking.TryGetAcceptedRevision(value, ProviderType.SQLLiteDBOASIS, out var revision));
        Good(HolonChangeTracking.SetOriginalPropertyValue(value, "/Name", "comparison"));
        Assert.Equal(current, StoredRecordJson.SerializeRuntime(value));
        Assert.Equal("\"comparison\"", Property(value, "/Name").OriginalJson);
        Assert.Equal("comparison", value.Original.Name);
        Assert.True(HolonChangeTracking.TryGetAcceptedRevision(value, ProviderType.SQLLiteDBOASIS, out var after));
        Assert.Equal(revision, after);
        Assert.True(HolonChangeTracking.HasChanges(value, false));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RebasingToCurrentRetainsExplicitFlags(bool force)
    {
        var value = Record(); value.Name = "current";
        if (force) Good(HolonChangeTracking.SetPropertyModified(value, "/Name"));
        Good(HolonChangeTracking.SetOriginalPropertyValue(value, "/Name", "current"));
        Assert.Equal(force, Property(value, "/Name").IsExplicitlyModified);
        Assert.False(Property(value, "/Name").IsValueModified);
        Assert.Equal(force, HolonChangeTracking.HasChanges(value, false));
    }

    [Fact]
    public void NullAndAbsenceRemainDifferentOriginalStates()
    {
        var value = Record(); value.MetaData["nullable"] = null;
        Good(HolonChangeTracking.SetOriginalPropertyValue<object>(value, "/MetaData/nullable", null));
        Assert.True(Property(value, "/MetaData/nullable").OriginalExists);
        Assert.Equal("null", Property(value, "/MetaData/nullable").OriginalJson);
        Good(HolonChangeTracking.RemoveOriginalPropertyValue(value, "/MetaData/nullable"));
        Assert.False(Property(value, "/MetaData/nullable").OriginalExists);
        Assert.True(Property(value, "/MetaData/nullable").CurrentExists);
        Assert.True(value.MetaData.ContainsKey("nullable"));
    }

    [Fact]
    public void BulkOriginalInputsAndCompatibilityViewStayDetached()
    {
        var value = Record();
        var list = new List<int> { 1, 2 };
        var input = new Dictionary<string, object> { ["items"] = list };
        Good(HolonChangeTracking.SetOriginalPropertyValues(value, new Dictionary<string, object>
            { ["/Name"] = "comparison", ["/MetaData/branch"] = input }));
        var snapshot = Property(value, "/MetaData/branch").OriginalJson;
        list.Add(3); input["more"] = true;
        Assert.Equal(snapshot, Property(value, "/MetaData/branch").OriginalJson);
        value.Original.MetaData.Clear(); value.Original.Name = "forged view";
        Assert.Equal(snapshot, Property(value, "/MetaData/branch").OriginalJson);
        Assert.Equal("\"comparison\"", Property(value, "/Name").OriginalJson);
        Assert.Equal("original", value.Name);
    }

    [Theory]
    [InlineData("/Id")] [InlineData("/Version")] [InlineData("/ProviderMetaData")]
    [InlineData("/ChildRoleManifestJson")] [InlineData("/ParentHolonId")] [InlineData("/CreatedByAvatarId")]
    public void StoreOwnedOriginalRootsCannotBeEdited(string path)
    {
        var value = Record(); var original = value.Original; var current = StoredRecordJson.SerializeRuntime(value);
        Assert.True(HolonChangeTracking.SetOriginalPropertyValue(value, path, "forbidden").IsError);
        Assert.Same(original, value.Original); Assert.Equal(current, StoredRecordJson.SerializeRuntime(value));
    }

    [Fact]
    public void OriginalEditingRequiresTracking()
    {
        var result = HolonChangeTracking.SetOriginalPropertyValue(new Holon(), "/Name", "comparison");
        Assert.True(result.IsError); Assert.Equal("HOLON_TRACKING_BASELINE_MISSING", result.ErrorCode);
    }

    [Fact]
    public void ScalarOriginalCannotBeRemovedAsMetadata()
    {
        var value = Record(); var original = value.Original;
        Assert.True(HolonChangeTracking.RemoveOriginalPropertyValue(value, "/Name").IsError);
        Assert.Same(original, value.Original);
        Assert.Equal("\"original\"", Property(value, "/Name").OriginalJson);
    }

    [Fact]
    public void InvalidBulkDoesNotPublishEarlierValidEdits()
    {
        var value = Record(); var original = value.Original;
        Good(HolonChangeTracking.SetPropertyModified(value, "/Description"));
        var result = HolonChangeTracking.SetOriginalPropertyValues(value, new Dictionary<string, object>
            { ["/Name"] = "would be first", ["/Unknown"] = "invalid" });
        Assert.True(result.IsError); Assert.Same(original, value.Original);
        Assert.Equal("\"original\"", Property(value, "/Name").OriginalJson);
        Assert.True(Property(value, "/Description").IsExplicitlyModified);
    }

    [Fact]
    public void OverlappingBulkPathsCannotPartiallyRebase()
    {
        var value = Record(); var original = value.Original;
        var result = HolonChangeTracking.SetOriginalPropertyValues(value, new Dictionary<string, object>
            { ["/MetaData"] = new Dictionary<string, object>(), ["/MetaData/key"] = 1L });
        Assert.True(result.IsError); Assert.Same(original, value.Original);
    }

    [Fact]
    public void CurrentResetUsesTheEditedOriginal()
    {
        var value = Record(); value.Name = "current";
        Good(HolonChangeTracking.SetOriginalPropertyValue(value, "/Name", "rebased"));
        Good(HolonChangeTracking.ResetPropertyValue(value, "/Name"));
        Assert.Equal("rebased", value.Name); Assert.False(HolonChangeTracking.HasChanges(value, false));
    }

    public sealed class TypedRecord : Holon
    {
        public byte[] Payload { get; set; } = new byte[] { 5, 6 };
        public decimal Amount { get; set; } = 1.25m;
    }

    [Fact]
    public void BytesDecimalsAndLargeIntegersRemainExactAndDetached()
    {
        var value = new TypedRecord { Id = Guid.NewGuid() };
        HolonChangeTracking.AcceptChanges(value, false);
        var bytes = new byte[] { 1, 2 };
        Good(HolonChangeTracking.SetOriginalPropertyValues(value, new Dictionary<string, object>
        {
            ["/Payload"] = bytes, ["/Amount"] = 9007199254740993.125m,
            ["/MetaData/big"] = 9007199254740993L
        }));
        bytes[0] = 99;
        Assert.Equal(new byte[] { 1, 2 }, ((TypedRecord)value.Original).Payload);
        Assert.Equal(9007199254740993.125m, ((TypedRecord)value.Original).Amount);
        Assert.Equal("9007199254740993", Property(value, "/MetaData/big").OriginalJson);
        Assert.Equal(new byte[] { 5, 6 }, value.Payload); Assert.Equal(1.25m, value.Amount);
    }

    [Fact]
    public void CompatibilityOriginalCannotForgeTheAcceptedNativeRevision()
    {
        var value = Record();
        Assert.True(HolonChangeTracking.TryGetAcceptedRevision(value, ProviderType.SQLLiteDBOASIS, out var before));
        Good(HolonChangeTracking.SetOriginalPropertyValue(value, "/Name", "comparison"));
        CosmicConcurrency.SetProviderRevisionId(value.Original, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        Assert.True(HolonChangeTracking.TryGetAcceptedRevision(value, ProviderType.SQLLiteDBOASIS, out var after));
        Assert.Equal(before, after);
    }

    [Fact]
    public void OriginalRebaseCannotUpgradeIncompleteChildren()
    {
        var value = Record(); HolonChangeTracking.MarkIncompleteChildSelection(value);
        Good(HolonChangeTracking.SetOriginalPropertyValue(value, "/Name", "comparison"));
        Assert.Throws<InvalidOperationException>(() => HolonChangeTracking.ValidateChildSelectionForSave(value));
    }

    [Fact]
    public void PendingSelectedAcknowledgementRetainsOriginalRebaseAndAdvancesRealRevision()
    {
        var value = Record(); value.Description = "submitted";
        Good(HolonChangeTracking.SetPropertyModified(value, "/Description"));
        using var plan = HolonSavePlan.Create(new[] { value }, false, false, 0, Guid.NewGuid(), conditionalIntent: true);
        plan.SelectPropertyValues(new Dictionary<Guid, IReadOnlyList<string>> { [value.Id] = new[] { "/Description" } });
        var acknowledged = HolonChangeTracking.Clone(plan.Roots[0]);
        acknowledged.Version = 2; acknowledged.VersionId = Guid.NewGuid(); var revision = Guid.NewGuid();
        CosmicConcurrency.SetProviderRevisionId(acknowledged, ProviderType.SQLLiteDBOASIS, revision);
        Good(HolonChangeTracking.SetOriginalPropertyValue(value, "/Description", "in-flight rebase"));
        plan.Publish(new[] { acknowledged }, _ => new OASISResult<bool>(true));
        Assert.Equal("submitted", value.Description);
        Assert.Equal("\"in-flight rebase\"", Property(value, "/Description").OriginalJson);
        Assert.False(Property(value, "/Description").IsExplicitlyModified);
        Assert.True(HolonChangeTracking.TryGetAcceptedRevision(value, ProviderType.SQLLiteDBOASIS, out var actual));
        Assert.Equal(revision, actual); Assert.True(HolonChangeTracking.HasChanges(value, false));
    }
}

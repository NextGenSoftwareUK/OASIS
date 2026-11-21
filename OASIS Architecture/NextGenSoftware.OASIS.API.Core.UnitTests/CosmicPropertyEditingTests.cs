using FluentAssertions;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicPropertyEditingTests
{
    private static Holon Tracked()
    {
        var value = new Holon { Id = Guid.NewGuid(), Name = "original", Description = "description",
            MetaData = new() { ["exact"] = 9007199254740993L, ["nullable"] = null!,
                ["branch"] = new Dictionary<string, object> { ["a"] = 1L, ["b"] = 2L },
                ["ordered"] = new[] { 1, 2 } } };
        value.AcceptChanges();
        return value;
    }

    private static HolonPropertyValues Property(Holon value, string path)
    {
        var result = HolonChangeTracking.GetPropertyValues(value, path);
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        return result.Result;
    }

    private static void Set<T>(Holon value, string path, T current)
    {
        var result = HolonChangeTracking.SetCurrentPropertyValue(value, path, current);
        result.IsError.Should().BeFalse(result.Message + result.Exception);
    }

    private static void Reset(Holon value, string path)
    {
        var result = HolonChangeTracking.ResetPropertyValue(value, path);
        result.IsError.Should().BeFalse(result.Message + result.Exception);
    }

    [Fact]
    public void CurrentValueEditingUsesTheCanonicalContractAndPreservesOriginalsAndOtherEdits()
    {
        var value = Tracked();
        Set(value, "/Name", "edited"); Set(value, "/MetaData/exact", 9007199254740994L);
        value.Description = "keep this edit";
        value.Name.Should().Be("edited"); value.Description.Should().Be("keep this edit");
        Property(value, "/Name").OriginalJson.Should().Be("\"original\"");
        Property(value, "/MetaData/exact").OriginalJson.Should().Be("9007199254740993");
        Property(value, "/MetaData/exact").CurrentJson.Should().Be("9007199254740994");
        value.HasHolonChanged().Should().BeTrue();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ResetOrIsModifiedFalseRestoresOnlyTheChosenProperty(bool useFlag)
    {
        var value = Tracked();
        Set(value, "/Name", "discard"); Set(value, "/Description", "keep");
        HolonChangeTracking.SetPropertyModified(value, "/Name").IsError.Should().BeFalse();
        if (useFlag) HolonChangeTracking.SetPropertyModified(value, "/Name", false).IsError.Should().BeFalse();
        else Reset(value, "/Name");
        value.Name.Should().Be("original"); value.Description.Should().Be("keep");
        Property(value, "/Name").IsModified.Should().BeFalse();
        Property(value, "/Description").IsValueModified.Should().BeTrue();
    }

    [Fact]
    public void NullableRootValuesCanBeAssignedAndRestoredWithoutBecomingEmptyStrings()
    {
        var value = Tracked();
        Set<string?>(value, "/Description", null);
        value.Description.Should().BeNull();
        Reset(value, "/Description"); value.Description.Should().Be("description");
        value.Description = null; value.AcceptChanges();
        Set(value, "/Description", "temporary");
        Reset(value, "/Description"); value.Description.Should().BeNull();
        value.HasHolonChanged().Should().BeFalse();
    }

    [Fact]
    public void MetadataResetDistinguishesRemovalFromNullAndPrunesOnlyNewEmptyContainers()
    {
        var value = Tracked(); value.MetaData.Remove("nullable");
        Reset(value, "/MetaData/nullable");
        value.MetaData.ContainsKey("nullable").Should().BeTrue();
        value.MetaData["nullable"].Should().BeNull();
        Set(value, "/MetaData/new/temporary", 7L);
        Reset(value, "/MetaData/new/temporary");
        value.MetaData.Should().NotContainKey("new");
        value.HasHolonChanged().Should().BeFalse();
    }

    [Fact]
    public void ResetOfAChildInARemovedObjectDoesNotRestoreUnselectedSiblings()
    {
        var value = Tracked(); value.MetaData.Remove("branch");
        Reset(value, "/MetaData/branch/a");
        Property(value, "/MetaData/branch/a").CurrentJson.Should().Be("1");
        Property(value, "/MetaData/branch/b").CurrentExists.Should().BeFalse();
        value.HasHolonChanged().Should().BeTrue();
        Reset(value, "/MetaData/branch/b");
        value.HasHolonChanged().Should().BeFalse();
    }

    [Fact]
    public void ScalarAncestorConflictsRefuseBeforeMutationAndPreservePendingFlags()
    {
        var value = Tracked(); value.MetaData["branch"] = "keep scalar";
        HolonChangeTracking.SetPropertyModified(value, "/MetaData/branch/a").IsError.Should().BeFalse();
        var before = StoredRecordJson.Serialize(value);
        HolonChangeTracking.ResetPropertyValue(value, "/MetaData/branch/a").IsError.Should().BeTrue();
        StoredRecordJson.Serialize(value).Should().Be(before);
        Property(value, "/MetaData/branch/a").IsExplicitlyModified.Should().BeTrue();
    }

    [Fact]
    public void ResetOfAMetadataSubtreeClearsItsFlagsButKeepsIndependentIntent()
    {
        var value = Tracked();
        foreach (var path in new[] { "/MetaData", "/MetaData/exact", "/Name" })
            HolonChangeTracking.SetPropertyModified(value, path).IsError.Should().BeFalse();
        Set(value, "/MetaData/exact", 7L);
        Reset(value, "/MetaData");
        Property(value, "/MetaData").IsModified.Should().BeFalse();
        Property(value, "/MetaData/exact").IsModified.Should().BeFalse();
        Property(value, "/Name").IsExplicitlyModified.Should().BeTrue();
        Reset(value, "/Name"); value.HasHolonChanged().Should().BeFalse();
    }

    [Fact]
    public void ArraysAreWholeValuesAndIncomingMutableBytesAreDetached()
    {
        var value = Tracked();
        Set(value, "/MetaData/ordered", new[] { 2, 1 });
        value.MetaData["ordered"].Should().BeOfType<int[]>().Which.Should().Equal(2, 1);
        Property(value, "/MetaData/ordered").IsValueModified.Should().BeTrue();
        Reset(value, "/MetaData/ordered"); value.HasHolonChanged().Should().BeFalse();
        var bytes = new byte[] { 1, 2, 3 };
        var editedBytes = HolonChangeTracking.SetCurrentPropertyValue(value, "/MetaData/bytes", bytes);
        var diagnostic = "";
        if (editedBytes.IsError)
        {
            var beforeToken = HolonChangeTracking.Values(value);
            var captured = StoredRecordJson.CapturePropertyInput(bytes, value.GetType(), beforeToken,
                new[] { "MetaData", "bytes" });
            var desired = (Newtonsoft.Json.Linq.JObject)beforeToken.DeepClone();
            ((Newtonsoft.Json.Linq.JObject)desired["MetaData"]!)["bytes"] = captured;
            var hydrated = (Holon)StoredRecordJson.DeserializeToken(desired, value.GetType());
            var actual = HolonChangeTracking.Values(hydrated);
            var changes = HolonValueDelta.Compare(desired, actual);
            diagnostic = "Controlled byte fixture: captured=" + captured.ToString(Formatting.None) +
                "; decoded=" + actual["MetaData"]?["bytes"]?.ToString(Formatting.None) +
                "; CLR=" + hydrated.MetaData["bytes"]?.GetType().FullName +
                "; changes=" + string.Join(";", changes.Select(change =>
                    change.Path + ":" + change.PreviousJson + "->" + change.CurrentJson));
        }
        editedBytes.IsError.Should().BeFalse(editedBytes.Message + editedBytes.Exception + diagnostic);
        value.MetaData["bytes"].Should().BeOfType<byte[]>().Which.Should().Equal(1, 2, 3);
        var before = StoredRecordJson.Serialize(value); bytes[0] = 99;
        StoredRecordJson.Serialize(value).Should().Be(before);
    }

    [Fact]
    public void DecimalAndDoubleValuesRoundTripWithoutRoundingExistingLongMetadata()
    {
        var value = Tracked();
        Set(value, "/MetaData/decimal", 7.1234567890123456789012345678m);
        Set(value, "/MetaData/double", Math.BitIncrement(1d));
        Property(value, "/MetaData/exact").CurrentJson.Should().Be("9007199254740993");
        Property(value, "/MetaData/decimal").CurrentJson.Should().Be("7.1234567890123456789012345678");
        Property(value, "/MetaData/double").CurrentJson.Should().Be("1.0000000000000002");
    }

    [Fact]
    public void PropertyAliasesAreResolvedByTheStorageSerializerWithoutCaseGuessing()
    {
        var value = new AliasedHolon { Name = "owned" }; value.AcceptChanges();
        Set(value, "/score_label", "changed");
        value.ScoreLabel.Should().Be("changed");
        HolonChangeTracking.SetCurrentPropertyValue(value, "/ScoreLabel", "bad").IsError.Should().BeTrue();
        Reset(value, "/score_label"); value.ScoreLabel.Should().Be("initial");
    }

    [Fact]
    public void CoercionAndDelegatesAreRejectedWithoutChangingCurrentValuesOrFlags()
    {
        var value = Tracked();
        HolonChangeTracking.SetPropertyModified(value, "/Name").IsError.Should().BeFalse();
        var before = StoredRecordJson.Serialize(value);
        HolonChangeTracking.SetCurrentPropertyValue(value, "/Name", 123).IsError.Should().BeTrue();
        HolonChangeTracking.SetCurrentPropertyValue(value, "/MetaData/delegate", (Func<int>)(() => 1))
            .IsError.Should().BeTrue();
        StoredRecordJson.Serialize(value).Should().Be(before);
        Property(value, "/Name").IsExplicitlyModified.Should().BeTrue();
    }

    [Theory]
    [InlineData("/Id")] [InlineData("/Version")] [InlineData("/ProviderMetaData")]
    [InlineData("/CreatedByAvatarId")] [InlineData("/ModifiedDate")]
    [InlineData("/ChildRoleManifestJson")] [InlineData("/ParentHolonId")]
    public void IdentityRevisionAuditAndRelationshipEvidenceAreReadOnly(string path)
    {
        var value = Tracked(); var before = StoredRecordJson.Serialize(value);
        HolonChangeTracking.SetCurrentPropertyValue(value, path, "forged").IsError.Should().BeTrue();
        HolonChangeTracking.ResetPropertyValue(value, path).IsError.Should().BeTrue();
        HolonChangeTracking.SetPropertyModified(value, path).IsError.Should().BeTrue();
        StoredRecordJson.Serialize(value).Should().Be(before);
        value.HasHolonChanged().Should().BeFalse();
    }

    [Theory]
    [InlineData("")] [InlineData("Name")] [InlineData("/MetaData/ordered/0")]
    [InlineData("/Children")] [InlineData("/IsChanged")]
    public void InvalidOrNonpersistentCurrentEditsRefuseBeforeCallerMutation(string path)
    {
        var value = Tracked(); var before = StoredRecordJson.Serialize(value);
        HolonChangeTracking.SetCurrentPropertyValue(value, path, 7).IsError.Should().BeTrue();
        StoredRecordJson.Serialize(value).Should().Be(before);
    }

    [Fact]
    public void EditingDetachedInstancesDoesNotCreateABaselineOrInvokeSaving()
    {
        var value = new Holon { Name = "detached" };
        HolonChangeTracking.SetCurrentPropertyValue(value, "/Name", "edited")
            .ErrorCode.Should().Be("HOLON_TRACKING_BASELINE_MISSING");
        value.Name.Should().Be("detached"); value.Original.Should().BeNull();
    }

    public sealed class AliasedHolon : Holon
    {
        [JsonProperty("score_label")] public string ScoreLabel { get; set; } = "initial";
    }
}

using FluentAssertions;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicPropertyTrackingTests
{
    private static Holon Tracked()
    {
        var value = new Holon { Id = Guid.NewGuid(), Name = "original", Description = "description",
            MetaData = new() { ["value"] = 9007199254740993L, ["null"] = null!,
                ["a/b~c"] = "escaped", ["list"] = new[] { 1, 2 } } };
        value.AcceptChanges();
        return value;
    }

    private static HolonPropertyValues Inspect(IHolon holon, string path)
    {
        var result = HolonChangeTracking.GetPropertyValues(holon, path);
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        return result.Result;
    }

    private static void Mark(IHolon holon, string path, bool modified = true)
        => HolonChangeTracking.SetPropertyModified(holon, path, modified).IsError.Should().BeFalse();

    [Fact]
    public void PropertySnapshotsDistinguishMissingNullAndExactValuesWithoutExposingBaseline()
    {
        var value = Tracked();
        var original = Inspect(value, "/MetaData/value");
        original.OriginalJson.Should().Be("9007199254740993");
        original.IsModified.Should().BeFalse();
        value.MetaData["value"] = 9007199254740994L;
        value.MetaData["added"] = null!;
        value.MetaData.Remove("null");
        value.Original.MetaData["value"] = "forged";
        Inspect(value, "/MetaData/value").OriginalJson.Should().Be("9007199254740993");
        Inspect(value, "/MetaData/value").CurrentJson.Should().Be("9007199254740994");
        Inspect(value, "/MetaData/added").OriginalExists.Should().BeFalse();
        Inspect(value, "/MetaData/added").CurrentExists.Should().BeTrue();
        Inspect(value, "/MetaData/added").CurrentJson.Should().Be("null");
        Inspect(value, "/MetaData/null").OriginalExists.Should().BeTrue();
        Inspect(value, "/MetaData/null").OriginalJson.Should().Be("null");
        Inspect(value, "/MetaData/null").CurrentExists.Should().BeFalse();
        Inspect(value, "/MetaData/a~1b~0c").CurrentJson.Should().Be("\"escaped\"");
        Inspect(value, "/MetaData/list").IsModified.Should().BeFalse();
        original.CurrentJson.Should().Be("9007199254740993", "old snapshots stay detached");
    }

    [Fact]
    public void ExplicitIntentMakesEqualValuesDirtyWithoutInventingAValueDelta()
    {
        var value = Tracked();
        Mark(value, "/Name");
        var entry = Inspect(value, "/Name");
        entry.IsExplicitlyModified.Should().BeTrue(); entry.IsValueModified.Should().BeFalse();
        entry.IsModified.Should().BeTrue(); value.HasHolonChanged().Should().BeTrue();
        HolonChangeTracking.PreviewValueChanges(value).Result.Should().BeEmpty();
        Mark(value, "/Name", false);
        value.HasHolonChanged().Should().BeFalse();
    }

    [Fact]
    public void ClearingAFlagCannotSuppressIndependentValueChanges()
    {
        var value = Tracked();
        Mark(value, "/Name"); value.Name = "edited";
        Mark(value, "/Name", false);
        var entry = Inspect(value, "/Name");
        entry.IsExplicitlyModified.Should().BeFalse(); entry.IsValueModified.Should().BeTrue();
        value.HasHolonChanged().Should().BeTrue();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExplicitLocalAcceptOrRejectClearsFlagsWithoutGrantingSelectionAuthority(bool reject)
    {
        var value = Tracked();
        Mark(value, "/Name"); value.Name = "edited";
        if (reject) HolonChangeTracking.RejectChanges(value, false).IsError.Should().BeFalse();
        else value.AcceptChanges(false);
        value.Name.Should().Be(reject ? "original" : "edited");
        Inspect(value, "/Name").IsModified.Should().BeFalse();
        value.HasHolonChanged().Should().BeFalse();
    }

    [Theory]
    [InlineData("")] [InlineData("Name")] [InlineData("/")]
    [InlineData("/Name/child")] [InlineData("/Children")] [InlineData("/IsChanged")]
    [InlineData("/MetaData/missing")] [InlineData("/MetaData/a~2b")] [InlineData("/MetaData/a~")]
    [InlineData("/MetaData/list/0")]
    public void InvalidOrNonpersistentPathsCannotPublishModificationIntent(string path)
    {
        var value = Tracked();
        HolonChangeTracking.GetPropertyValues(value, path).ErrorCode.Should().Be("HOLON_PROPERTY_INVALID");
        HolonChangeTracking.SetPropertyModified(value, path).ErrorCode.Should().Be("HOLON_PROPERTY_INVALID");
        value.HasHolonChanged().Should().BeFalse();
    }

    [Fact]
    public void DetachedInstancesCannotAcquireATrackingBaselineByMarkingAProperty()
    {
        var value = new Holon { Name = "untracked" };
        HolonChangeTracking.GetPropertyValues(value, "/Name").ErrorCode.Should().Be("HOLON_TRACKING_BASELINE_MISSING");
        HolonChangeTracking.SetPropertyModified(value, "/Name").ErrorCode.Should().Be("HOLON_TRACKING_BASELINE_MISSING");
        value.Original.Should().BeNull();
    }

    [Fact]
    public void ExplicitIntentAndPointerBoundsRejectBeforeChangingTracking()
    {
        var value = Tracked();
        for (var index = 0; index <= HolonChangeTracking.MaximumExplicitPropertyModifications; index++)
            value.MetaData["p" + index] = index;
        value.AcceptChanges();
        for (var index = 0; index < HolonChangeTracking.MaximumExplicitPropertyModifications; index++)
            Mark(value, "/MetaData/p" + index);
        HolonChangeTracking.SetPropertyModified(value, "/MetaData/p256").ErrorCode.Should().Be("HOLON_PROPERTY_LIMIT");
        Mark(value, "/MetaData/p0", false); Mark(value, "/MetaData/p256");
        Inspect(value, "/MetaData/p0").IsExplicitlyModified.Should().BeFalse();
        HolonChangeTracking.SetPropertyModified(value, "/" + new string('n', 1024)).ErrorCode.Should().Be("HOLON_PROPERTY_INVALID");
        HolonChangeTracking.SetPropertyModified(value, "/" + string.Join("/", Enumerable.Repeat("p", 33)))
            .ErrorCode.Should().Be("HOLON_PROPERTY_INVALID");
    }

    [Theory]
    [InlineData("unchanged")] [InlineData("new")] [InlineData("reasserted")]
    [InlineData("cleared")] [InlineData("scalar")]
    public void AcknowledgementAcceptsOnlyCapturedPropertyIntentAndPreservesLaterEdits(string mode)
    {
        var value = Tracked(); Mark(value, "/Name");
        using var plan = HolonSavePlan.Create(new[] { value }, false, false, 0);
        if (mode == "new") Mark(value, "/Description");
        if (mode == "reasserted") Mark(value, "/Name");
        if (mode == "cleared") Mark(value, "/Name", false);
        if (mode == "scalar") value.Name = "later edit";
        plan.Roots[0].Version = 2;
        plan.Publish(plan.Roots, _ => new OASISResult<bool>(true));
        value.Version.Should().Be(2);
        value.HasHolonChanged().Should().Be(mode is "new" or "reasserted" or "scalar");
        Inspect(value, "/Name").IsExplicitlyModified.Should().Be(mode == "reasserted");
        Inspect(value, "/Name").OriginalJson.Should().Be("\"original\"");
        value.Name.Should().Be(mode == "scalar" ? "later edit" : "original");
        if (mode == "new") Inspect(value, "/Description").IsExplicitlyModified.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FailureOrInvalidAcknowledgementCannotAcceptPendingPropertyFlags(bool invalidAcknowledgement)
    {
        var value = Tracked(); Mark(value, "/Name");
        using (var plan = HolonSavePlan.Create(new[] { value }, false, false, 0))
            if (invalidAcknowledgement)
            {
                var publish = () => plan.Publish(Array.Empty<IHolon>(), _ => new OASISResult<bool>(true));
                publish.Should().Throw<Exception>();
            }
        Inspect(value, "/Name").IsExplicitlyModified.Should().BeTrue();
        value.HasHolonChanged().Should().BeTrue();
    }

    [Fact]
    public void ExplicitFlagsCannotGrantCompleteChildReplacementAuthority()
    {
        var value = Tracked();
        HolonChangeTracking.MarkIncompleteChildSelection(value);
        Mark(value, "/Name");
        var plan = () => HolonSavePlan.Create(new[] { value }, true, true, 0);
        plan.Should().Throw<Exception>();
        Inspect(value, "/Name").IsExplicitlyModified.Should().BeTrue();
    }

    [Fact]
    public void ParentOnlyAcknowledgementCannotAcceptAnUnselectedChildsFlags()
    {
        var root = Tracked(); var child = Tracked();
        child.ParentHolonId = root.Id; root.Children.Add(child); root.AcceptChanges();
        Mark(root, "/Name"); Mark(child, "/Name");
        using var plan = HolonSavePlan.Create(new[] { root }, false, false, 0);
        plan.Publish(plan.Roots, _ => new OASISResult<bool>(true));
        HolonChangeTracking.HasChanges(root, false).Should().BeFalse();
        HolonChangeTracking.HasChanges(root, true).Should().BeTrue();
        Inspect(child, "/Name").IsExplicitlyModified.Should().BeTrue();
    }

    [Fact]
    public void ReassertedRemovalRemainsInspectableAndClearableAfterAcknowledgement()
    {
        var value = Tracked(); Mark(value, "/MetaData/value");
        value.MetaData.Remove("value");
        using var plan = HolonSavePlan.Create(new[] { value }, false, false, 0);
        Mark(value, "/MetaData/value");
        plan.Publish(plan.Roots, _ => new OASISResult<bool>(true));
        var property = Inspect(value, "/MetaData/value");
        property.OriginalExists.Should().BeFalse(); property.CurrentExists.Should().BeFalse();
        property.IsExplicitlyModified.Should().BeTrue();
        Mark(value, "/MetaData/value", false);
        value.HasHolonChanged().Should().BeFalse();
    }

    [Fact]
    public void SerializedCopiesCarryValuesButNoProcessLocalPropertyFlags()
    {
        var value = Tracked(); Mark(value, "/Name");
        var copy = StoredRecordJson.Deserialize<Holon>(StoredRecordJson.Serialize(value));
        copy.AcceptChanges();
        copy.HasHolonChanged().Should().BeFalse();
        Inspect(copy, "/Name").IsExplicitlyModified.Should().BeFalse();
        Inspect(value, "/Name").IsExplicitlyModified.Should().BeTrue();
    }
}

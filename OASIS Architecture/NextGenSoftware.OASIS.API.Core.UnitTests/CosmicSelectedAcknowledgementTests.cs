using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicSelectedAcknowledgementTests
{
    private const ProviderType Authority = ProviderType.SQLLiteDBOASIS;
    private static Holon Source()
    {
        var value = new Holon { Id = Guid.NewGuid(), Name = "visible name", Description = "before", Version = 1,
            MetaData = new() { ["selected"] = "before", ["sibling"] = "visible sibling" } };
        CosmicConcurrency.SetProviderRevisionId(value, Authority, Guid.NewGuid());
        HolonChangeTracking.AcceptChanges(value, false);
        return value;
    }
    private static HolonSavePlan Plan(IHolon source, params string[] paths)
    {
        var plan = HolonSavePlan.Create(new[] { source }, false, false, 0, Guid.NewGuid(), conditionalIntent: true);
        plan.SelectPropertyValues(new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = paths });
        return plan;
    }
    private static Holon Ack(HolonSavePlan plan)
    {
        var value = StoredRecordJson.Project<Holon>(plan.Roots.Single());
        value.Name = "private native name";
        value.CreatedByAvatarId = Guid.NewGuid();
        value.MetaData["hidden"] = "private native metadata";
        value.MetaData["sibling"] = "private native sibling";
        value.Version = 2; value.PreviousVersionId = value.VersionId; value.VersionId = Guid.NewGuid();
        CosmicConcurrency.SetProviderRevisionId(value, Authority, Guid.NewGuid());
        return value;
    }
    private static void Publish(HolonSavePlan plan, params IHolon[] values)
        => plan.Publish(values, _ => new OASISResult<bool>(true));
    private static HolonPropertyValues Property(IHolon value, string path)
    {
        var result = HolonChangeTracking.GetPropertyValues(value, path);
        Assert.False(result.IsError, result.Message + result.Exception);
        return result.Result;
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public void SelectedPublicationNeverCopiesExcludedNativeValues(bool dirtyExcluded)
    {
        var source = Source(); var owner = source.CreatedByAvatarId;
        source.Description = "saved";
        if (dirtyExcluded) source.Name = "local unsaved name";
        var name = source.Name;
        using var plan = Plan(source, "/Description"); var ack = Ack(plan);
        var revision = CosmicConcurrency.GetProviderRevisionId(ack, Authority)!.Value;
        Publish(plan, ack);
        Assert.Equal(name, source.Name); Assert.Equal("saved", source.Description);
        Assert.Equal("visible sibling", source.MetaData["sibling"]); Assert.False(source.MetaData.ContainsKey("hidden"));
        Assert.Equal(owner, source.CreatedByAvatarId); Assert.Equal(2, source.Version);
        Assert.Equal("visible name", source.Original.Name); Assert.Equal("saved", source.Original.Description);
        Assert.False(source.Original.MetaData.ContainsKey("hidden"));
        Assert.True(HolonChangeTracking.TryGetAcceptedRevision(source, Authority, out var observed)); Assert.Equal(revision, observed);
        Assert.Equal(dirtyExcluded, HolonChangeTracking.HasChanges(source, false));
    }

    [Theory]
    [InlineData("scalar")][InlineData("nested")][InlineData("removed")]
    public void SelectedPublicationPreservesEditsMadeWhileWriteWasPending(string shape)
    {
        var source = Source();
        var path = shape == "scalar" ? "/Description" : "/MetaData/selected";
        if (shape == "scalar") source.Description = "saved";
        else if (shape == "removed") source.MetaData.Remove("selected");
        else source.MetaData["selected"] = "saved";
        using var plan = Plan(source, path); var ack = Ack(plan);
        if (shape == "scalar") source.Description = "later";
        else source.MetaData["selected"] = "later";
        Publish(plan, ack);
        Assert.Equal("\"later\"", Property(source, path).CurrentJson);
        Assert.True(Property(source, path).IsValueModified);
        Assert.Equal(shape == "removed" ? null : "\"saved\"", Property(source, path).OriginalJson);
        Assert.True(HolonChangeTracking.HasChanges(source, false)); Assert.Equal(2, source.Version);
        Assert.False(source.MetaData.ContainsKey("hidden"));
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public void SelectedPublicationClearsOnlyCapturedSelectedFlags(bool reassert)
    {
        var source = Source();
        Assert.False(HolonChangeTracking.SetPropertyModified(source, "/Description").IsError);
        Assert.False(HolonChangeTracking.SetPropertyModified(source, "/Name").IsError);
        using var plan = Plan(source, "/Description"); var ack = Ack(plan);
        if (reassert) Assert.False(HolonChangeTracking.SetPropertyModified(source, "/Description").IsError);
        Publish(plan, ack);
        Assert.Equal(reassert, Property(source, "/Description").IsExplicitlyModified);
        Assert.True(Property(source, "/Name").IsExplicitlyModified);
        Assert.Equal("visible name", source.Name);
    }

    [Fact]
    public void NewComparisonBaselineDuringIoIsRetainedButNativeRevisionAdvances()
    {
        var source = Source(); source.Description = "saved";
        using var plan = Plan(source, "/Description"); var ack = Ack(plan);
        source.Description = "caller rebased"; HolonChangeTracking.AcceptChanges(source, false);
        Publish(plan, ack);
        Assert.Equal("caller rebased", source.Description); Assert.Equal("caller rebased", source.Original.Description);
        Assert.False(HolonChangeTracking.HasChanges(source, false));
        Assert.True(HolonChangeTracking.TryGetAcceptedRevision(source, Authority, out var observed));
        Assert.Equal(CosmicConcurrency.GetProviderRevisionId(ack, Authority), observed);
    }

    [Fact]
    public void SelectedMissingMetadataValueDoesNotImportHiddenSiblings()
    {
        var source = Source(); source.MetaData.Remove("selected");
        using var plan = Plan(source, "/MetaData/selected"); var ack = Ack(plan);
        Publish(plan, ack);
        Assert.False(source.MetaData.ContainsKey("selected")); Assert.False(source.MetaData.ContainsKey("hidden"));
        Assert.Equal("visible sibling", source.MetaData["sibling"]);
        Assert.False(source.Original.MetaData.ContainsKey("selected"));
        Assert.False(HolonChangeTracking.HasChanges(source, false));
    }

    [Fact]
    public void WholeBatchIdentityValidationPrecedesAnySelectedPublication()
    {
        var first = Source(); var second = Source(); first.Description = "saved first"; second.Description = "saved second";
        using var plan = HolonSavePlan.Create(new IHolon[] { first, second }, false, false, 0, Guid.NewGuid(), conditionalIntent: true);
        plan.SelectPropertyValues(new Dictionary<Guid, IReadOnlyList<string>> { [first.Id] = new[] { "/Description" }, [second.Id] = new[] { "/Description" } });
        var beforeFirst = StoredRecordJson.SerializeRuntime(first); var beforeSecond = StoredRecordJson.SerializeRuntime(second);
        var one = StoredRecordJson.Project<Holon>(plan.Roots[0]); var two = StoredRecordJson.Project<Holon>(plan.Roots[1]); two.Id = Guid.NewGuid();
        Assert.Throws<InvalidDataException>(() => Publish(plan, one, two));
        Assert.Equal(beforeFirst, StoredRecordJson.SerializeRuntime(first)); Assert.Equal(beforeSecond, StoredRecordJson.SerializeRuntime(second));
    }

    [Fact]
    public void CompatibilityOriginalMutationCannotForgeNativeRevisionObservation()
    {
        var source = Source(); var expected = CosmicConcurrency.GetProviderRevisionId(source, Authority)!.Value;
        CosmicConcurrency.SetProviderRevisionId(source.Original, Authority, Guid.NewGuid());
        Assert.True(HolonChangeTracking.TryGetAcceptedRevision(source, Authority, out var observed)); Assert.Equal(expected, observed);
    }

    [Fact]
    public void InvalidPublicationSelectionDoesNotAcquireNewSaveState()
    {
        var source = Source(); using var plan = HolonSavePlan.Create(new[] { source }, false, false, 0);
        Assert.Throws<ArgumentException>(() => plan.SelectPropertyValues(new Dictionary<Guid, IReadOnlyList<string>> { [Guid.NewGuid()] = new[] { "/Description" } }));
        Assert.False(HolonChangeTracking.HasChanges(source, false));
    }

    [Fact]
    public void RejectingCurrentEditsDuringIoDoesNotUndoTheCommittedNativeVersion()
    {
        var source = Source(); source.Description = "saved";
        using var plan = Plan(source, "/Description"); var ack = Ack(plan);
        Assert.False(HolonChangeTracking.RejectChanges(source, false).IsError);
        Publish(plan, ack);
        Assert.Equal("before", source.Description); Assert.Equal("saved", source.Original.Description);
        Assert.Equal(2, source.Version); Assert.True(HolonChangeTracking.HasChanges(source, false));
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public void ExactMetadataIntegersSurviveSelectedPublicationAndLaterEdits(bool laterEdit)
    {
        var source = Source(); source.MetaData["number"] = 9007199254740993L;
        HolonChangeTracking.AcceptChanges(source, false); source.MetaData["number"] = 9007199254740995L;
        using var plan = Plan(source, "/MetaData/number"); var ack = Ack(plan);
        if (laterEdit) source.MetaData["number"] = 9007199254740997L;
        Publish(plan, ack);
        Assert.Equal(laterEdit ? 9007199254740997L : 9007199254740995L, source.MetaData["number"]);
        Assert.Equal(9007199254740995L, source.Original.MetaData["number"]);
        Assert.Equal(laterEdit, HolonChangeTracking.HasChanges(source, false));
    }

    public class SetterProbe : Holon
    {
        private string _target = "before";
        [JsonIgnore] public bool Trigger { get; set; }
        public string Target { get => _target; set { _target = value; if (Trigger) Name = "out-of-scope setter effect"; } }
    }

    [Fact]
    public void SelectedSetterSideEffectsCannotSilentlyPublishTrackingAcceptance()
    {
        var source = new SetterProbe { Id = Guid.NewGuid(), Name = "visible", Version = 1 };
        var revision = Guid.NewGuid(); CosmicConcurrency.SetProviderRevisionId(source, Authority, revision);
        HolonChangeTracking.AcceptChanges(source, false); source.Target = "saved";
        using var plan = Plan(source, "/Target");
        var ack = StoredRecordJson.Project<SetterProbe>(plan.Roots.Single()); ack.Version = 2;
        CosmicConcurrency.SetProviderRevisionId(ack, Authority, Guid.NewGuid()); source.Trigger = true;
        Assert.Throws<InvalidOperationException>(() => Publish(plan, ack));
        Assert.True(HolonChangeTracking.TryGetAcceptedRevision(source, Authority, out var observed)); Assert.Equal(revision, observed);
        Assert.Equal("before", ((SetterProbe)source.Original).Target);
    }
}

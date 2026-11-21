using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTransformedPropertyPreparationTests
{
    [Theory]
    [InlineData(false, "name")][InlineData(true, "name")]
    [InlineData(false, "sibling")][InlineData(true, "sibling")]
    [InlineData(false, "long")][InlineData(true, "long")]
    public void SelectedPreparationReplayIgnoresExcludedValuesAndKeepsFirstPreparedImage(bool encrypted, string change)
    {
        var f = new Fixture(transform: encrypted);
        var firstImage = StoredRecordJson.SerializeRuntime(f.Value);
        var proposed = f.Logical();
        if (change == "name") proposed.Name = "new unselected local name";
        if (change == "sibling") proposed.MetaData["sibling"] = "new authoritative sibling";
        if (change == "long") proposed.MetaData["long"] = 9007199254740995L;
        var retry = f.Capture(proposed, f.Write, "/MetaData/selected", encrypted ? "/MetaData" : "/MetaData/selected", encrypted);
        var retryImage = StoredRecordJson.SerializeRuntime(proposed);
        Assert.NotEqual(firstImage, retryImage);
        var result = retry.RecoverProperty(f.Preparation.Candidate);
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.Equal(firstImage, StoredRecordJson.SerializeRuntime(Assert.Single(result.Result.WriteRequest.Roots)));
        Assert.Equal(retryImage, StoredRecordJson.SerializeRuntime(proposed));
        Assert.Equal(firstImage, StoredRecordJson.SerializeRuntime(f.Value));
        Assert.Equal(encrypted ? "/MetaData" : "/MetaData/selected", Assert.Single(result.Result.Selections[f.Value.Id]));
    }

    [Theory]
    [InlineData(false, "value")][InlineData(true, "value")]
    [InlineData(false, "missing")][InlineData(true, "missing")]
    [InlineData(false, "null")][InlineData(true, "null")]
    public void SelectedPreparationReplayStillRejectsChangedSelectedValues(bool encrypted, string change)
    {
        var f = new Fixture(transform: encrypted); var proposed = f.Logical();
        if (change == "value") proposed.MetaData["selected"] = "different selected intent";
        if (change == "missing") proposed.MetaData.Remove("selected");
        if (change == "null") proposed.MetaData["selected"] = null!;
        var retry = f.Capture(proposed, f.Write, "/MetaData/selected", encrypted ? "/MetaData" : "/MetaData/selected", encrypted);
        Probe.Constructors = 0;
        var result = retry.RecoverProperty(f.Preparation.Candidate);
        Assert.True(result.IsError); Assert.Null(result.Result);
        Assert.Equal("COSMIC_OPERATION_ID_REUSED", result.ErrorCode);
        Assert.Equal(0, Probe.Constructors);
    }

    [Fact]
    public void SelectedPreparationReplayKeepsUnselectedGraphNodesBoundToFullWriteIntent()
    {
        var f = new Fixture(transform: false);
        var root = f.Logical(); var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "original child" };
        root.Children.Add(child);
        var write = new ConditionalHolonWrite(Guid.NewGuid(), f.Write.ActorId, f.Write.Authority,
            new[] { new HolonWriteCondition(root.Id, Guid.Empty), new HolonWriteCondition(child.Id, Guid.Empty) });
        var paths = new Dictionary<Guid, IReadOnlyList<string>> { [root.Id] = new[] { "/MetaData/selected" } };
        var original = new HolonConditionalPreparation(new[] { root }, write, true, true, 0,
            _ => f.Policy, ConditionalHolonOperation.Save, paths).Seal();
        var retryRoot = f.Logical(); var retryChild = StoredRecordJson.Project<Holon>(child);
        retryChild.Name = "changed full-write child"; retryRoot.Children.Add(retryChild);
        var retry = new HolonConditionalPreparation(new[] { retryRoot }, write, true, true, 0,
            _ => f.Policy, ConditionalHolonOperation.Save, paths).Seal();
        Probe.Constructors = 0;
        var denied = retry.RecoverProperty(original.Candidate);
        Assert.True(denied.IsError); Assert.Null(denied.Result);
        Assert.Equal("COSMIC_OPERATION_ID_REUSED", denied.ErrorCode);
        Assert.Equal(0, Probe.Constructors);
    }
}

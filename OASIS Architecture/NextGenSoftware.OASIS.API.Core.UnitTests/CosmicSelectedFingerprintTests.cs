using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicSelectedFingerprintTests
{
    private sealed class Context
    {
        internal readonly Holon Root = new() { Id = Guid.NewGuid(), Name = "unselected",
            MetaData = new() { ["Score"] = 9007199254740993L, ["Other"] = "before" } };
        internal readonly Guid Actor = Guid.NewGuid(), Operation = Guid.NewGuid(), Revision = Guid.NewGuid();

        internal ConditionalHolonWriteRequest Request(IHolon[]? roots = null, Guid? actor = null,
            Guid? revision = null, ProviderType authority = ProviderType.MongoDBOASIS)
        {
            roots ??= new IHolon[] { Root };
            return new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Operation, actor ?? Actor, authority,
                roots.Select(node => new HolonWriteCondition(node.Id, revision ?? Revision))),
                ConditionalHolonOperation.Save, roots, false, false, 0);
        }

        internal ConditionalHolonPlan Selected(string path = "/MetaData/Score", IHolon[]? roots = null,
            Guid? actor = null, Guid? revision = null, ProviderType authority = ProviderType.MongoDBOASIS)
            => ConditionalHolonPlan.CreatePropertyWrite(new ConditionalHolonPropertyWriteRequest(
                Request(roots, actor, revision, authority), new Dictionary<Guid, IReadOnlyList<string>>
                { [Root.Id] = new[] { path } }), authority);
    }

    [Fact]
    public void UnselectedAuthoritativeSiblingsDoNotChangeSelectedIntent()
    {
        var c = new Context(); var fingerprint = c.Selected().Fingerprint;
        c.Root.Name = "newer native name"; c.Root.MetaData["Other"] = "newer native sibling";
        c.Root.MetaData["Added"] = "later native value";
        Assert.Equal(fingerprint, c.Selected().Fingerprint);
    }

    [Fact]
    public void ExactSelectedNumbersAndNullVersusAbsenceRemainBound()
    {
        var c = new Context(); var original = c.Selected().Fingerprint;
        c.Root.MetaData["Score"] = 9007199254740995L;
        Assert.NotEqual(original, c.Selected().Fingerprint);
        c.Root.MetaData["Score"] = null!; var presentNull = c.Selected().Fingerprint;
        c.Root.MetaData.Remove("Score");
        Assert.NotEqual(presentNull, c.Selected().Fingerprint);
    }

    [Fact]
    public void TheSelectedPathItselfRemainsBoundWhenValuesMatch()
    {
        var c = new Context(); c.Root.MetaData["Other"] = c.Root.MetaData["Score"];
        Assert.NotEqual(c.Selected().Fingerprint, c.Selected("/MetaData/Other").Fingerprint);
    }

    [Fact]
    public void ActorAuthorityAndOriginalConditionRemainBound()
    {
        var c = new Context(); var fingerprint = c.Selected().Fingerprint;
        Assert.NotEqual(fingerprint, c.Selected(actor: Guid.NewGuid()).Fingerprint);
        Assert.NotEqual(fingerprint, c.Selected(revision: Guid.NewGuid()).Fingerprint);
        Assert.NotEqual(fingerprint, c.Selected(authority: ProviderType.SQLLiteDBOASIS).Fingerprint);
    }

    [Fact]
    public void ParentEdgeAndRecordIdentityRemainBound()
    {
        var c = new Context(); var fingerprint = c.Selected().Fingerprint;
        c.Root.ParentHolonId = Guid.NewGuid();
        Assert.NotEqual(fingerprint, c.Selected().Fingerprint);
        c.Root.ParentHolonId = Guid.Empty; c.Root.Id = Guid.NewGuid();
        Assert.NotEqual(fingerprint, c.Selected().Fingerprint);
    }

    [Fact]
    public void FullSavesAndUnselectedNodesRetainWholeRecordBinding()
    {
        var c = new Context(); var full = ConditionalHolonPlan.Create(c.Request(), ProviderType.MongoDBOASIS);
        c.Root.Name = "changed full intent";
        Assert.NotEqual(full.Fingerprint, ConditionalHolonPlan.Create(c.Request(), ProviderType.MongoDBOASIS).Fingerprint);
        var other = new Holon { Id = Guid.NewGuid(), Name = "full node" };
        var roots = new IHolon[] { c.Root, other }; var mixed = c.Selected(roots: roots).Fingerprint;
        other.Name = "changed full node";
        Assert.NotEqual(mixed, c.Selected(roots: roots).Fingerprint);
    }

    [Fact]
    public void FingerprintProjectionDoesNotTrimPayloadOrMutateInputs()
    {
        var c = new Context(); var before = StoredRecordJson.SerializeRuntime(c.Root);
        var plan = c.Selected();
        Assert.Equal(before, StoredRecordJson.SerializeRuntime(c.Root));
        Assert.Equal("before", plan.Nodes.Single().Snapshot.MetaData["Other"]);
        c.Root.MetaData["Other"] = "later";
        Assert.Equal("before", plan.Nodes.Single().Snapshot.MetaData["Other"]);
    }

    [Fact]
    public void NestedCellSelectionIgnoresOnlyItsUnselectedSibling()
    {
        var c = new Context(); var address = new Dictionary<string, object> { ["zip"] = "one", ["city"] = "old" };
        c.Root.MetaData["address"] = address;
        var fingerprint = c.Selected("/MetaData/address/zip").Fingerprint;
        address["city"] = "new";
        Assert.Equal(fingerprint, c.Selected("/MetaData/address/zip").Fingerprint);
        address["zip"] = "two";
        Assert.NotEqual(fingerprint, c.Selected("/MetaData/address/zip").Fingerprint);
    }
}

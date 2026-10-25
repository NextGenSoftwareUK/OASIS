using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class CommittedReplicaSnapshotTests
{
    private static Holon Observed()
    {
        var root = new Holon { Id = Guid.NewGuid() };
        root.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id });
        root.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(
            HolonChildCollectionState.Capture(root).CreateManifest(root.Id, child => child.Id));
        root.AcceptChanges();
        return root;
    }

    [Fact]
    public void GenericImportDoesNotAcquireReplicaAuthority()
    {
        var copy = ImportGraphSnapshot.Create(new[] { Observed() }).CreateAttempt().Single();
        Action admit = () => HolonChangeTracking.ValidateChildSelectionForSave(copy);
        admit.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PartialAndColdAcknowledgementsCannotGrantAuthority(bool cold)
    {
        var root = Observed();
        if (cold) root = StoredRecordJson.Project<Holon>(root);
        else HolonChangeTracking.MarkIncompleteChildSelection(root);
        Action capture = () => ImportGraphSnapshot.CreateCommittedReplica(new[] { root }, true, true, 0);
        capture.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(false, true, 0, 0, false)]
    [InlineData(true, false, 0, 0, true)]
    [InlineData(true, true, 1, 0, true)]
    [InlineData(true, true, 1, 1, false)]
    public void UnselectedMembershipDoesNotAcquireAuthority(bool children, bool recursive, int depth, int initial, bool rootSelected)
    {
        var root = Observed();
        var child = (Holon)root.Children.Single();
        child.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = child.Id });
        child.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(
            HolonChildCollectionState.Capture(child).CreateManifest(child.Id, value => value.Id));
        child.AcceptChanges();
        var copy = ImportGraphSnapshot.CreateCommittedReplica(new[] { root }, children, recursive, depth, initial).CreateAttempt().Single();
        Action admit = () => HolonChangeTracking.ValidateChildSelectionForSave(copy);
        if (rootSelected)
        {
            admit.Should().NotThrow();
            var selectedChild = copy.Children.Single();
            selectedChild.Children.Should().BeEmpty();
            Action descendantAdmission = () => HolonChangeTracking.ValidateChildSelectionForSave(selectedChild);
            descendantAdmission.Should().Throw<InvalidOperationException>();
        }
        else
        {
            copy.Children.Should().BeEmpty();
            admit.Should().Throw<InvalidOperationException>();
        }
    }

    public sealed class NamedOwner : Holon
    {
        [Newtonsoft.Json.JsonIgnore] public List<IHolon> Members = new();
        public override IReadOnlyCollection<IHolon> AllChildren => Children.Concat(Members).ToArray();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections() => base.CaptureChildCollections()
            .Concat(new[] { new HolonChildCollection("Members", Members, rows => Members = rows.ToList()) }).ToArray();
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings() => base.GetChildRoleBindings()
            .Concat(new[] { HolonChildRoleBinding.Create<IHolon>("Members", rows => Members = rows.ToList()) }).ToArray();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void LogicalRolesSurviveLiveAndPreparedAcknowledgements(bool prepared)
    {
        var root = new NamedOwner { Id = Guid.NewGuid() };
        root.Members.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id });
        root.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(
            HolonChildCollectionState.Capture(root).CreateManifest(root.Id, value => value.Id));
        root.AcceptChanges();
        using var plan = prepared ? HolonSavePlan.Create(new[] { root }, true, true, 0) : null;
        var source = prepared ? plan!.Roots.Single() : root;
        var snapshot = ImportGraphSnapshot.CreateCommittedReplica(new[] { source }, true, true, 0);
        var first = (NamedOwner)snapshot.CreateAttempt().Single();
        first.Children.Should().BeEmpty();
        first.Members.Single().Id.Should().Be(root.Members.Single().Id);
        HolonChangeTracking.ValidateChildSelectionForSave(first);
        first.Members.Clear();
        var second = (NamedOwner)snapshot.CreateAttempt().Single();
        second.Members.Should().ContainSingle();
        root.Members.Should().ContainSingle();
    }
}

using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicPreparedRoleOwnershipTests
{
    public sealed class TypedOwner : Holon
    {
        private readonly List<IHolon> members = new();
        public void Add(IHolon child) => members.Add(child);
        public override IReadOnlyCollection<IHolon> AllChildren => Children.Concat(members).ToArray();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()
            => base.CaptureChildCollections().Concat(new[]
            {
                new HolonChildCollection("Members", members, values =>
                {
                    members.Clear();
                    members.AddRange(values);
                })
            }).ToArray();
    }

    private static Holon ObservedOwner()
    {
        var root = new Holon { Id = Guid.NewGuid() };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id };
        root.Children.Add(child);
        var manifest = HolonChildCollectionState.Capture(root).CreateManifest(root.Id, value => value.Id);
        root.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(manifest);
        HolonChildRoleHydration.ApplyFullySelected(root, manifest, id => child).IsError.Should().BeFalse();
        return root;
    }

    [Fact]
    public void PreparedTransportRetainsLogicalRolesInsteadOfRecapturingFlattenedChildren()
    {
        var root = new TypedOwner { Id = Guid.NewGuid() };
        var child = new Holon { Id = Guid.NewGuid() };
        root.Add(child);
        using var plan = HolonSavePlan.Create(new[] { root }, true, true, 0);
        var staged = plan.Roots.Single();
        staged.Children.Should().ContainSingle();
        HolonChangeTracking.TryGetPreparedChildSelection(staged, out var selection).Should().BeTrue();
        selection.Roles.Single(role => role.Name == "Members").ChildIds.Should().Equal(child.Id);
        selection.Roles.Single(role => role.Name == "Children").ChildIds.Should().BeEmpty();
        Action admit = () => HolonChangeTracking.ValidateChildSelectionForSave(staged);
        admit.Should().NotThrow();
    }

    [Fact]
    public void SerializedCopyCannotAcquirePreparedObservationAuthority()
    {
        var root = ObservedOwner();
        using var plan = HolonSavePlan.Create(new[] { root }, true, true, 0);
        var staged = plan.Roots.Single();
        Action admit = () => HolonChangeTracking.ValidateChildSelectionForSave(staged);
        admit.Should().NotThrow();
        var copy = StoredRecordJson.Project<Holon>(staged);
        HolonChangeTracking.TryGetPreparedChildSelection(copy, out _).Should().BeFalse();
        Action copiedAdmission = () => HolonChangeTracking.ValidateChildSelectionForSave(copy);
        copiedAdmission.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("child-identity")]
    [InlineData("parent")]
    [InlineData("member")]
    [InlineData("observation")]
    public void PreparedSelectionRejectsMutationOfBoundOwnership(string mutation)
    {
        var root = ObservedOwner();
        using var plan = HolonSavePlan.Create(new[] { root }, true, true, 0);
        var staged = (Holon)plan.Roots.Single();
        var child = staged.Children.Single();
        switch (mutation)
        {
            case "identity": staged.Id = Guid.NewGuid(); break;
            case "child-identity": child.Id = Guid.NewGuid(); break;
            case "parent": child.ParentHolonId = Guid.NewGuid(); break;
            case "member": staged.Children = new[] { new Holon { Id = child.Id, ParentHolonId = staged.Id } }; break;
            case "observation": staged.ChildRoleManifestJson = null; break;
        }
        Action admission = () => HolonChangeTracking.ValidateChildSelectionForSave(staged);
        admission.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void KnownPartialMarkerCannotBeUpgradedByPreparedEvidenceOrAcceptChanges()
    {
        using var plan = HolonSavePlan.Create(new[] { ObservedOwner() }, true, true, 0);
        var staged = (Holon)plan.Roots.Single();
        HolonChangeTracking.MarkIncompleteChildSelection(staged);
        staged.AcceptChanges();
        Action admission = () => HolonChangeTracking.ValidateChildSelectionForSave(staged);
        admission.Should().Throw<InvalidOperationException>();
        Action capture = () => HolonChangeTracking.TryGetPreparedChildSelection(staged, out _);
        capture.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DisposingThePlanRetiresItsPrivatePreparedEvidence()
    {
        var plan = HolonSavePlan.Create(new[] { ObservedOwner() }, true, true, 0);
        var staged = plan.Roots.Single();
        HolonChangeTracking.TryGetPreparedChildSelection(staged, out _).Should().BeTrue();
        plan.Dispose();
        HolonChangeTracking.TryGetPreparedChildSelection(staged, out _).Should().BeFalse();
    }
}

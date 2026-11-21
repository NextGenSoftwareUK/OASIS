using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicPreparedRoleIntentTests
{
    public sealed class RoleOwner : Holon
    {
        public List<IHolon> Components { get; set; } = new();
        public override ReadOnlyCollection<IHolon> AllChildren => Children.Concat(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections() => base.CaptureChildCollections()
            .Concat(new[] { new HolonChildCollection(nameof(Components), Components, members => Components = members.ToList()) }).ToArray();
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings() => base.GetChildRoleBindings()
            .Concat(new[] { HolonChildRoleBinding.Create<IHolon>(nameof(Components), members => Components = members.ToList(), source => source) }).ToArray();
    }

    private static HolonSavePlan Prepared()
    {
        var root = new RoleOwner { Id = Guid.NewGuid(), Description = "before" };
        var ordinary = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id };
        var component = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id };
        root.Children.Add(ordinary); root.Components.Add(component);
        var manifest = HolonChildCollectionState.Capture(root).CreateManifest(root.Id, member => member.Id);
        root.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(manifest);
        var values = new Dictionary<Guid, IHolon> { [ordinary.Id] = ordinary, [component.Id] = component };
        var hydrated = HolonChildRoleHydration.ApplyFullySelected(root, manifest, id => values[id]);
        Assert.False(hydrated.IsError, hydrated.Message + hydrated.Exception);
        HolonChangeTracking.AcceptChanges(root, true);
        root.Components.Add(new Holon { ParentHolonId = root.Id }); root.Description = "new scalar";
        return HolonSavePlan.Create(new[] { root }, true, true, 0, Guid.NewGuid(), conditionalIntent: true);
    }

    private static HolonChildRoleManifest ChangedRoles(HolonSavePlan plan)
    {
        var root = plan.Roots.Single();
        return new HolonChildRoleManifest(1, root.Id, new[] {
            new HolonChildRole("Children", root.Children.Select(member => member.Id)),
            new HolonChildRole("Components", Array.Empty<Guid>()) });
    }

    private static ConditionalHolonPropertyWriteRequest Request(HolonSavePlan plan,
        IReadOnlyDictionary<Guid, HolonChildRoleManifest>? roles = null)
    {
        var write = new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), ProviderType.MongoDBOASIS,
            plan.OwnershipInputs(ProviderType.MongoDBOASIS).Select(row =>
                new HolonWriteCondition(row.Id, row.Generated ? null : Guid.NewGuid())));
        var request = new ConditionalHolonWriteRequest(write, ConditionalHolonOperation.Save, plan.Roots,
            true, true, 0, roles ?? plan.RelationshipManifests);
        return new ConditionalHolonPropertyWriteRequest(request,
            new Dictionary<Guid, IReadOnlyList<string>> { [plan.Roots.Single().Id] = new[] { "/Description" } });
    }

    [Fact]
    public void ExactLivePreparedIntentIsAdmittedWithoutPersistingAnObservationGrant()
    {
        IHolon staged; HolonChildRoleManifest intended;
        using (var plan = Prepared())
        {
            staged = plan.Roots.Single(); intended = plan.RelationshipManifests[staged.Id];
            HolonChangeTracking.ValidateChildSelectionForConditionalWrite(staged, intended);
        }
        Assert.Throws<InvalidOperationException>(() => HolonChangeTracking.ValidateChildSelectionForConditionalWrite(staged, intended));
    }

    [Fact]
    public void DifferentRoleAssignmentCannotUseTheLivePreparedGrant()
    {
        using var plan = Prepared();
        var cause = Assert.Throws<InvalidOperationException>(() => HolonChangeTracking.ValidateChildSelectionForConditionalWrite(
            plan.Roots.Single(), ChangedRoles(plan)));
        Assert.Contains("admitted prepared role intent", cause.Message);
    }

    [Fact]
    public void SerializedTransportCannotAcquireTheLivePreparedGrant()
    {
        using var plan = Prepared(); var staged = plan.Roots.Single();
        var serialized = StoredRecordJson.Project<Holon>(staged);
        serialized.Children = staged.Children.Select(member => (IHolon)StoredRecordJson.Project<Holon>(member)).ToList();
        var cause = Assert.Throws<InvalidOperationException>(() => HolonChangeTracking.ValidateChildSelectionForConditionalWrite(
            serialized, plan.RelationshipManifests[staged.Id]));
        Assert.Contains("complete stored role observation", cause.Message);
    }

    [Fact]
    public void ChangedStoredObservationInvalidatesThePreparedGrant()
    {
        using var plan = Prepared(); var staged = (Holon)plan.Roots.Single();
        staged.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1, staged.Id,
            new[] { new HolonChildRole("Children", Array.Empty<Guid>()), new HolonChildRole("Components", Array.Empty<Guid>()) }));
        var cause = Assert.Throws<InvalidOperationException>(() => HolonChangeTracking.ValidateChildSelectionForConditionalWrite(
            staged, plan.RelationshipManifests[staged.Id]));
        Assert.Contains("stored role observation changed", cause.Message);
    }

    [Fact]
    public void IncompletePreparedViewIsStillRefused()
    {
        using var plan = Prepared(); var staged = plan.Roots.Single();
        HolonChangeTracking.MarkIncompleteChildSelection(staged);
        var cause = Assert.Throws<InvalidOperationException>(() => HolonChangeTracking.ValidateChildSelectionForConditionalWrite(
            staged, plan.RelationshipManifests[staged.Id]));
        Assert.Contains("fully observed", cause.Message);
    }

    [Fact]
    public void ReplacedChildReferenceCannotReusePreparedMembership()
    {
        using var plan = Prepared(); var staged = plan.Roots.Single(); var prior = staged.Children[0];
        staged.Children[0] = new Holon { Id = prior.Id, ParentHolonId = prior.ParentHolonId };
        var cause = Assert.Throws<InvalidOperationException>(() => HolonChangeTracking.ValidateChildSelectionForConditionalWrite(
            staged, plan.RelationshipManifests[staged.Id]));
        Assert.Contains("prepared child identities", cause.Message);
    }

    [Fact]
    public void SharedPlannerAdmitsBoundLogicalRolesBeforeTestingTransportMembership()
    {
        using var plan = Prepared(); var root = plan.Roots.Single();
        var native = ConditionalHolonPlan.CreatePropertyWrite(Request(plan), ProviderType.MongoDBOASIS);
        var manifest = native.Nodes.Single(node => node.Snapshot.Id == root.Id).RelationshipManifest;
        Assert.Equal(HolonChildRoleManifestJson.Serialize(plan.RelationshipManifests[root.Id]),
            HolonChildRoleManifestJson.Serialize(manifest));
        Assert.Equal(2, manifest.Roles.Count);
    }

    [Fact]
    public void SharedPlannerRejectsReassignedRolesWithTheSameChildIdentitySet()
    {
        using var plan = Prepared();
        var changed = plan.RelationshipManifests.ToDictionary(pair => pair.Key, pair => pair.Value);
        changed[plan.Roots.Single().Id] = ChangedRoles(plan);
        var request = Request(plan, changed);
        var cause = Assert.Throws<InvalidOperationException>(() => ConditionalHolonPlan.CreatePropertyWrite(request, ProviderType.MongoDBOASIS));
        Assert.Contains("admitted prepared role intent", cause.Message);
    }
}

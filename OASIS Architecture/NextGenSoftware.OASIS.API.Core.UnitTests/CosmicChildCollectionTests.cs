using System.Collections.ObjectModel;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicChildCollectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedBatchTrackingLeavesEarlierBaselinesUntouched(bool alreadyTracked)
    {
        var first = new Holon { Name = "accepted" };
        if (alreadyTracked) first.AcceptChanges();
        first.Name = "loaded";
        var result = new NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>>(
            new IHolon[] { first, new UnmappedAggregate() });
        var track = typeof(NextGenSoftware.OASIS.API.Core.Managers.HolonManager).GetMethod("TrackLoadedResult",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(IEnumerable<IHolon>));
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2 } };
        var manager = new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null, dna,
            new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null, dna));
        track.Invoke(manager, new object[] { result });
        result.IsError.Should().BeTrue(); result.Result.Should().BeNull();
        if (alreadyTracked)
        {
            first.RejectChanges().IsError.Should().BeFalse();
            first.Name.Should().Be("accepted");
        }
        else
        {
            first.IsNewHolon.Should().BeTrue();
            first.Original.Should().BeNull();
        }
    }

    [Fact]
    public void InvalidDescendantCannotPartiallyAcceptAnAncestorBaseline()
    {
        var root = new Holon { Name = "unaccepted" };
        root.Children.Add(new UnmappedAggregate());
        Action accept = () => root.AcceptChanges();
        accept.Should().Throw<InvalidOperationException>();
        root.IsNewHolon.Should().BeTrue();
        root.Original.Should().BeNull();
    }

    [Theory]
    [InlineData(false, true, 0, 0)]
    [InlineData(true, false, 0, 1)]
    [InlineData(true, true, 1, 1)]
    [InlineData(true, true, 2, 2)]
    [InlineData(true, true, 0, 2)]
    public void SaveSelectionAppliesDepthAndRecursionToTypedRoles(bool children, bool recursive, int depth, int selectedDepth)
    {
        var root = new TypedAggregate { Id = Guid.NewGuid() };
        var child = new TypedAggregate { Id = Guid.NewGuid() };
        var grandchild = new Holon { Id = Guid.NewGuid() };
        root.Add(child);
        child.Add(grandchild);
        using var plan = NextGenSoftware.OASIS.API.Core.Managers.HolonSavePlan.Create(new[] { root }, children, recursive, depth);
        var staged = plan.Roots.Single();
        staged.Children.Count().Should().Be(selectedDepth > 0 ? 1 : 0);
        if (selectedDepth > 0)
        {
            var stagedChild = staged.Children.Single();
            stagedChild.Id.Should().Be(child.Id);
            stagedChild.Children.Count().Should().Be(selectedDepth > 1 ? 1 : 0);
            if (selectedDepth > 1)
                stagedChild.Children.Single().Id.Should().Be(grandchild.Id);
        }
    }

    [Fact]
    public void RootOnlyPublicationDoesNotAcceptUnsubmittedTypedMembership()
    {
        var root = new TypedAggregate { Id = Guid.NewGuid() };
        var accepted = new Holon { Id = Guid.NewGuid() };
        root.Add(accepted);
        root.AcceptChanges();
        var pending = new Holon { Id = Guid.NewGuid() };
        root.Add(pending);
        using var plan = NextGenSoftware.OASIS.API.Core.Managers.HolonSavePlan.Create(new[] { root }, false, true, 0);
        plan.Publish(plan.Roots, _ => new NextGenSoftware.OASIS.Common.OASISResult<bool>(true));
        root.Typed.Should().Equal(accepted, pending);
        root.HasHolonChanged().Should().BeTrue();
        root.RejectChanges().IsError.Should().BeFalse();
        root.Typed.Should().Equal(accepted);
    }

    [Fact]
    public void PublicationAcceptsSubmittedRolesAndPreservesPendingMembership()
    {
        var submitted = new Holon { Id = Guid.NewGuid(), Name = "submitted" };
        var pending = new Holon { Id = Guid.NewGuid(), Name = "pending" };
        var root = new TypedAggregate { Id = Guid.NewGuid() };
        root.Add(submitted);
        root.AcceptChanges();
        using var plan = NextGenSoftware.OASIS.API.Core.Managers.HolonSavePlan.Create(new[] { root }, true, true, 0);
        plan.Roots[0].Children.Should().ContainSingle().Which.Id.Should().Be(submitted.Id);
        root.Remove(submitted);
        root.Add(pending);
        plan.Publish(plan.Roots, _ => new NextGenSoftware.OASIS.Common.OASISResult<bool>(true));
        root.Typed.Should().Equal(pending);
        root.HasHolonChanged().Should().BeTrue();
        root.RejectChanges().IsError.Should().BeFalse();
        root.Typed.Should().Equal(submitted);
        root.HasHolonChanged().Should().BeFalse();
    }

    public class TypedAggregate : Holon
    {
        private readonly List<Holon> _typed=new();
        public ReadOnlyCollection<Holon> Typed=>_typed.AsReadOnly();
        public void Add(Holon child)=>_typed.Add(child);
        public void Remove(Holon child)=>_typed.Remove(child);
        public override IReadOnlyCollection<IHolon> AllChildren=>Children.Concat(_typed).ToArray();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()
            =>base.CaptureChildCollections().Concat(new[]{new HolonChildCollection(nameof(Typed),_typed,members=>{
                _typed.Clear();_typed.AddRange(members.Cast<Holon>());
            })}).ToArray();
    }
    [Fact]
    public void SemanticChildEditsAreDetectedAndReceiveAnAcceptedBaseline()
    {
        var child=new Holon{Id=Guid.NewGuid(),Name="original"};var root=new TypedAggregate{Id=Guid.NewGuid()};root.Add(child);
        root.AcceptChanges();child.IsNewHolon.Should().BeFalse();root.HasHolonChanged().Should().BeFalse();
        child.Name="edited";root.HasHolonChanged().Should().BeTrue();
        root.HasHolonChanged(false).Should().BeFalse();
    }
    [Fact]
    public void RejectRestoresReadonlyTypedMembershipAndDescendantValues()
    {
        var child=new Holon{Id=Guid.NewGuid(),Name="original"};var root=new TypedAggregate{Id=Guid.NewGuid()};root.Add(child);
        var heldView=root.Typed;root.AcceptChanges();root.Remove(child);root.Add(new Holon{Name="new"});child.Name="edited";
        root.RejectChanges().IsError.Should().BeFalse();
        root.Typed.Should().ContainSingle().Which.Should().BeSameAs(child);
        heldView.Should().ContainSingle().Which.Should().BeSameAs(child);child.Name.Should().Be("original");
        root.HasHolonChanged().Should().BeFalse();
    }
    [Fact]
    public void MovingAChildBetweenRolesIsAChangeEvenWithIdenticalSemanticOrder()
    {
        var child=new Holon{Id=Guid.NewGuid()};var root=new TypedAggregate{Id=Guid.NewGuid()};root.Children.Add(child);
        root.AcceptChanges();root.Children.Clear();root.Add(child);
        root.AllChildren.Should().Equal(child);root.HasHolonChanged().Should().BeTrue();
        root.RejectChanges().IsError.Should().BeFalse();root.Children.Should().Equal(child);root.Typed.Should().BeEmpty();
    }
    [Fact]
    public void CollectionOrderAndRootOnlyUndoRetainTheirScope()
    {
        var first=new Holon{Id=Guid.NewGuid()};var second=new Holon{Id=Guid.NewGuid()};var root=new TypedAggregate{Id=Guid.NewGuid(),Name="original"};
        root.Add(first);root.Add(second);root.AcceptChanges();root.Remove(first);root.Add(first);root.Name="edited";
        root.RejectChanges(false).IsError.Should().BeFalse();root.Name.Should().Be("original");root.Typed.Should().Equal(second,first);
        root.HasHolonChanged(false).Should().BeFalse();root.HasHolonChanged().Should().BeTrue();
        root.RejectChanges().IsError.Should().BeFalse();root.Typed.Should().Equal(first,second);
    }
    [Fact]
    public void CapturedMembershipIsImmutableAndNotASecondPersistencePayload()
    {
        var child=new Holon{Id=Guid.NewGuid()};var root=new TypedAggregate{Id=Guid.NewGuid()};root.Add(child);
        var before=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.SerializeRuntime(root);
        var snapshot=HolonChildCollectionState.Capture(root);root.Add(new Holon());
        snapshot.Children.Should().Equal(child);snapshot.Collections.Single(role=>role.Name==nameof(TypedAggregate.Typed)).Members.Should().Equal(child);
        Action change=()=>((IList<IHolon>)snapshot.Children).Clear();change.Should().Throw<NotSupportedException>();
        NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.SerializeRuntime(root).Should().Be(before);
    }
    public class UnmappedAggregate : Holon
    {
        private readonly Holon _child=new Holon();
        public override IReadOnlyCollection<IHolon> AllChildren=>new[]{_child};
    }
    [Fact]
    public void UndeclaredSemanticMembershipFailsBeforeAcceptingAnyBaseline()
    {
        var root=new UnmappedAggregate();Action accept=()=>root.AcceptChanges();
        accept.Should().Throw<InvalidOperationException>().WithMessage("*undeclared*");root.IsNewHolon.Should().BeTrue();
    }
}

using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicConditionalPlanningTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparedRootParentCannotChangeAfterSubmission(bool emptyPreparedParent)
    {
        var source = new Holon { Id = Guid.NewGuid() };
        using var plan = HolonSavePlan.Create(new[] { source }, false, false, 0);
        plan.Roots[0].ParentHolonId = emptyPreparedParent ? Guid.Empty : Guid.NewGuid();
        HolonSavePlan.CapturePreparedParents(plan.Roots);
        plan.Roots[0].ParentHolonId = Guid.NewGuid();
        Action publish = () => plan.Publish(plan.Roots, _ => new NextGenSoftware.OASIS.Common.OASISResult<bool>(true));
        publish.Should().Throw<InvalidDataException>();
        source.Original.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AcknowledgedReparentingOutsideSubmittedIntentCannotPublish(bool childAcknowledgement)
    {
        var child = new Holon { Id = Guid.NewGuid(), Name = "child-before" };
        var source = new Holon { Id = Guid.NewGuid(), ParentHolonId = Guid.NewGuid(), Name = "root-before", Children = new List<IHolon> { child } };
        using var plan = HolonSavePlan.Create(new[] { source }, true, true, 0);
        var target = childAcknowledgement ? plan.Roots[0].Children.Single() : plan.Roots[0];
        target.ParentHolonId = Guid.NewGuid();
        plan.Roots[0].Name = "committed";
        Action publish = () => plan.Publish(plan.Roots, _ => new NextGenSoftware.OASIS.Common.OASISResult<bool>(true));
        publish.Should().Throw<InvalidDataException>();
        source.Name.Should().Be("root-before");
        source.Original.Should().BeNull();
        child.Original.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidAcknowledgementMembersCannotPublishCallerState(bool nullMember)
    {
        var source = new Holon { Id = Guid.NewGuid(), Name = "before" };
        using var plan = HolonSavePlan.Create(new[] { source }, false, false, 0);
        plan.Roots[0].Name = "committed";
        var acknowledgements = new IHolon[] { plan.Roots[0], nullMember ? null! : new Holon { Id = Guid.NewGuid() } };
        Action publish = () => plan.Publish(acknowledgements, _ => new NextGenSoftware.OASIS.Common.OASISResult<bool>(true));
        publish.Should().Throw<InvalidDataException>();
        source.Name.Should().Be("before");
        source.IsNewHolon.Should().BeTrue();
        source.Original.Should().BeNull();
    }

    [Fact]
    public void SaveConditionsRetainImmutableExplicitReplicationEnrollment()
    {
        var root=new Holon();using var plan=HolonSavePlan.Create(new[]{root},false,false,0,Guid.NewGuid());
        var targets=new List<string>{"target-b","target-a"};
        var enrollment=new HolonReplicationEnrollment("source",targets);targets.Clear();
        var conditions=plan.Conditions(new CosmicWriteOptions(Guid.NewGuid(),expectedRevisions:null,replicationEnrollment:enrollment),Guid.NewGuid(),ProviderType.SQLLiteDBOASIS);
        conditions.ReplicationEnrollment.Should().BeSameAs(enrollment);
        conditions.ReplicationEnrollment.TargetIds.Should().Equal("target-a","target-b");
    }

    [Theory]
    [InlineData("source-space")] [InlineData("source-empty")] [InlineData("source-long")]
    [InlineData("empty-targets")] [InlineData("duplicate-target")] [InlineData("blank-target")]
    public void ReplicationEnrollmentRejectsAmbiguousSourceOrTargetIdentities(string fault)
    {
        var source=fault=="source-space" ? " source":fault=="source-empty" ? "":fault=="source-long" ? new string('a',257):"source";
        var targets=fault=="empty-targets" ? Array.Empty<string>():fault=="duplicate-target" ? new[]{"a","a"}
            :fault=="blank-target" ? new[]{" "}:new[]{"target"};
        Action invalid=()=>new HolonReplicationEnrollment(source,targets);invalid.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AProviderObservationCannotBeUsedAsAnotherAuthorityBaseline()
    {
        var source = new Holon { Id = Guid.NewGuid(), VersionId = Guid.NewGuid() };
        CosmicConcurrency.SetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        source.AcceptChanges();
        using var plan = HolonSavePlan.Create(new[] { source }, false, false, 0);
        Action other = () => plan.Conditions(new CosmicWriteOptions(Guid.NewGuid()), Guid.NewGuid(), ProviderType.MongoDBOASIS);
        other.Should().Throw<ArgumentException>();
        Action unspecified = () => plan.Conditions(new CosmicWriteOptions(Guid.NewGuid()), Guid.NewGuid(), ProviderType.Default);
        unspecified.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ALogicalVersionAloneDoesNotAuthorizeAConditionalUpdate()
    {
        var source = new Holon { Id = Guid.NewGuid(), VersionId = Guid.NewGuid() };
        source.AcceptChanges();
        using var plan = HolonSavePlan.Create(new[] { source }, false, false, 0);
        Action update = () => plan.Conditions(new CosmicWriteOptions(Guid.NewGuid()), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS);
        update.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CommittedProviderTokenIsPublishedEvenWhenUserMetadataWasEditedDuringSave()
    {
        var source = new Holon { Id = Guid.NewGuid(), Name = "submitted" };
        source.MetaData["value"] = 1;
        CosmicConcurrency.SetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        source.AcceptChanges();
        using var plan = HolonSavePlan.Create(new[] { source }, false, false, 0);
        source.MetaData["value"] = 2;
        var committed = Guid.NewGuid();
        CosmicConcurrency.SetProviderRevisionId(plan.Roots[0], ProviderType.SQLLiteDBOASIS, committed);
        plan.Publish(plan.Roots, _ => new NextGenSoftware.OASIS.Common.OASISResult<bool>(true));
        CosmicConcurrency.GetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS).Should().Be(committed);
        source.MetaData["value"].Should().Be(2); source.HasHolonChanged().Should().BeTrue();
        plan.Conditions(new CosmicWriteOptions(Guid.NewGuid()), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS)
            .Conditions.Single().ExpectedRevisionId.Should().Be(committed);
    }

    [Fact]
    public void AllCommittedProviderTokensAreValidatedBeforeAnyCallerPublication()
    {
        var child = new Holon { Id = Guid.NewGuid(), Name = "child before" };
        var source = new Holon { Id = Guid.NewGuid(), Name = "root before", Children = new List<IHolon> { child } };
        using var plan = HolonSavePlan.Create(new[] { source }, true, true, 0);
        plan.Roots[0].Name = "committed root";
        plan.Roots[0].Children.Single().ProviderMetaData[ProviderType.SQLLiteDBOASIS] = new Dictionary<string, string> {
            [CosmicConcurrency.ProviderRevisionMetadataKey] = "malformed" };
        Action publish = () => plan.Publish(plan.Roots, _ => new NextGenSoftware.OASIS.Common.OASISResult<bool>(true));
        publish.Should().Throw<InvalidDataException>();
        source.Name.Should().Be("root before"); child.Name.Should().Be("child before");
    }

    [Fact]
    public void ConditionsUsePrivateAcceptedRevisionRatherThanMutableCallerTokens()
    {
        var accepted = Guid.NewGuid();
        var source = new Holon { Id = Guid.NewGuid(), VersionId = accepted };
        CosmicConcurrency.SetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS, accepted);
        source.AcceptChanges();
        source.VersionId = Guid.NewGuid();
        source.Original.VersionId = Guid.NewGuid();
        CosmicConcurrency.SetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        CosmicConcurrency.SetProviderRevisionId(source.Original, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        using var plan = HolonSavePlan.Create(new[] { source }, true, true, 0);
        plan.Conditions(new CosmicWriteOptions(Guid.NewGuid()), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS)
            .Conditions.Single().ExpectedRevisionId.Should().Be(accepted);
    }

    [Fact]
    public void RetryOfNewGraphHasStableIdentitiesWithoutPublishingThem()
    {
        var child = new Holon();
        var source = new Holon { Children = new List<IHolon> { child } };
        var operation = Guid.NewGuid();
        Guid rootId, childId;
        using (var first = HolonSavePlan.Create(new[] { source }, true, true, 0, operation))
        {
            rootId = first.Roots[0].Id;
            childId = first.Roots[0].Children.Single().Id;
            first.Conditions(new CosmicWriteOptions(operation), Guid.Empty, ProviderType.SQLLiteDBOASIS).Conditions
                .Should().OnlyContain(condition => condition.ExpectedRevisionId == null);
        }
        using var retry = HolonSavePlan.Create(new[] { source }, true, true, 0, operation);
        retry.Roots[0].Id.Should().Be(rootId);
        retry.Roots[0].Children.Single().Id.Should().Be(childId);
        rootId.Should().NotBe(childId);
        source.Id.Should().BeEmpty(); child.Id.Should().BeEmpty();
    }

    [Fact]
    public void UntrackedIdentityRequiresExplicitConditionAndLegacyEmptyRevisionMeansExisting()
    {
        var source = new Holon { Id = Guid.NewGuid() };
        using var plan = HolonSavePlan.Create(new[] { source }, false, true, 0);
        Action missing = () => plan.Conditions(new CosmicWriteOptions(Guid.NewGuid()), Guid.Empty, ProviderType.SQLLiteDBOASIS);
        missing.Should().Throw<ArgumentException>();
        var options = new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [source.Id] = Guid.Empty });
        plan.Conditions(options, Guid.Empty, ProviderType.SQLLiteDBOASIS).Conditions.Single().ExpectedRevisionId.Should().Be(Guid.Empty);
        Action extra = () => plan.Conditions(new CosmicWriteOptions(Guid.NewGuid(),
            new Dictionary<Guid, Guid?> { [Guid.NewGuid()] = null }), Guid.Empty, ProviderType.SQLLiteDBOASIS);
        extra.Should().Throw<ArgumentException>();
    }
}

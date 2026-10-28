using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    // These cases cross the manager/router boundary. Planning-only tests cannot
    // prove that the selected obligations reach the atomic provider transaction.
    [Theory]
    [InlineData(false, "node")]
    [InlineData(true, "node")]
    [InlineData(false, "avatar")]
    [InlineData(true, "avatar")]
    [InlineData(false, "request")]
    [InlineData(true, "request")]
    [InlineData(false, "explicit")]
    [InlineData(true, "explicit")]
    public async Task OwnedSaveSubmitsEffectiveReplicationWithOriginalWriteIntent(bool batch, string scope)
    {
        await Task.Run(async () =>
        {
            var fixture = Fixture(true, conditional: true, atomicReplication: true);
            foreach (var type in new[] { ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS })
            {
                var target = new Mock<IOASISStorageProvider>();
                target.As<ICommittedHolonReplicaStorageProvider>();
                target.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(type));
                target.SetupGet(value => value.ProviderCategory).Returns(new EnumValue<ProviderCategory>(ProviderCategory.Storage));
                fixture.Providers.RegisterProvider(target.Object);
            }

            fixture.Providers.PublishRoutingOverrides(new ProviderRoutingOverrides(
                autoReplicationEnabled: true, replicationProviders: new[] { ProviderType.MongoDBOASIS })).IsError.Should().BeFalse();

            if (scope != "node")
                OASISRequestContext.AvatarRoutingOverrides = new ProviderRoutingOverrides(
                    replicationProviders: new[] { ProviderType.Neo4jOASIS });
            if (scope == "request")
                OASISRequestContext.RequestRoutingOverrides = new ProviderRoutingOverrides(
                    replicationProviders: new[] { ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS });

            var explicitEnrollment = scope == "explicit"
                ? new HolonReplicationEnrollment("explicit-source", new[] { "explicit-target" }) : null;
            var actor = Guid.NewGuid();
            var operation = Guid.NewGuid();
            var root = new Holon { Name = "pending native write" };
            ConditionalHolonWriteRequest? submitted = null;
            HolonReplicationEnrollment? enrolled = null;

            fixture.Provider.As<IAtomicHolonReplicationStorageProvider>()
                .Setup(value => value.ExecuteReplicatingHolonWriteAsync(
                    It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<HolonReplicationEnrollment>(), It.IsAny<CancellationToken>()))
                .Callback<ConditionalHolonWriteRequest, HolonReplicationEnrollment, CancellationToken>((request, targets, _) =>
                { submitted = request; enrolled = targets; })
                .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>
                { IsError = true, ErrorCode = "CONTROLLED_ATOMIC_REJECTION", Message = "No transaction committed." });

            var options = new CosmicWriteOptions(operation, expectedRevisions: null, replicationEnrollment: explicitEnrollment);
            var error = batch
                ? (await fixture.Manager.SaveOwnedHolonsAsync(new[] { root }, actor, options)).ErrorCode
                : (await fixture.Manager.SaveOwnedHolonAsync(root, actor, options)).ErrorCode;

            error.Should().Be("CONTROLLED_ATOMIC_REJECTION");
            submitted.Should().NotBeNull();
            submitted!.Write.OperationId.Should().Be(operation);
            submitted.Write.ActorId.Should().Be(actor);
            submitted.Write.ReplicationEnrollment.Should().BeSameAs(enrolled);
            enrolled!.SourceId.Should().Be(explicitEnrollment?.SourceId ?? HolonReplicationRoutes.SourceId(ProviderType.SQLLiteDBOASIS));
            var expectedTargets = scope switch
            {
                "explicit" => new[] { "explicit-target" },
                "avatar" => new[] { HolonReplicationRoutes.TargetId(ProviderType.Neo4jOASIS) },
                "request" => new[] { HolonReplicationRoutes.TargetId(ProviderType.MongoDBOASIS), HolonReplicationRoutes.TargetId(ProviderType.Neo4jOASIS) },
                _ => new[] { HolonReplicationRoutes.TargetId(ProviderType.MongoDBOASIS) }
            };
            enrolled.TargetIds.Should().BeEquivalentTo(expectedTargets);

            // Rejection cannot downgrade to plain persistence or acknowledge the
            // generated identity/revision into the caller's still-pending object.
            root.Id.Should().BeEmpty();
            root.Name.Should().Be("pending native write");
            HolonChangeTracking.HasChanges(root).Should().BeTrue();
            fixture.Provider.Invocations.Count(call => call.Method.Name == "ExecuteReplicatingHolonWriteAsync").Should().Be(1);
            fixture.Provider.Invocations.Should().NotContain(call => call.Method.Name.StartsWith("SaveHolon") || call.Method.Name.StartsWith("ExecuteConditionalHolonWrite"));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedUnchangedSaveDoesNotSubmitAtomicReplication(bool batch)
    {
        await Task.Run(async () =>
        {
            var fixture = Fixture(true, conditional: true, atomicReplication: true);
            var actor = Guid.NewGuid();
            var root = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = actor, Name = "unchanged" };
            CosmicConcurrency.SetProviderRevisionId(root, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            root.AcceptChanges();
            fixture.Provider.Setup(value => value.LoadHolonAsync(root.Id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(root) { IsLoaded = true });
            var options = new CosmicWriteOptions(Guid.NewGuid(), expectedRevisions: null, replicationEnrollment:
                new HolonReplicationEnrollment("source", new[] { "target" }));

            if (batch)
            {
                var result = await fixture.Manager.SaveOwnedHolonsAsync(new[] { root }, actor, options);
                result.IsError.Should().BeFalse(result.Message);
                result.IsSaved.Should().BeFalse();
            }
            else
            {
                var result = await fixture.Manager.SaveOwnedHolonAsync(root, actor, options);
                result.IsError.Should().BeFalse(result.Message);
                result.IsSaved.Should().BeFalse();
            }

            fixture.Provider.Invocations.Should().NotContain(call => call.Method.Name.Contains("Write") || call.Method.Name.StartsWith("SaveHolon"));
            HolonChangeTracking.HasChanges(root).Should().BeFalse();
        });
    }
}

using System;
using System.Threading.Tasks;
using Moq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests
{
    /// <summary>V2 owned writes enrol replication from the effective routing, only to providers able to apply replicas.</summary>
    public class HolonReplicationRoutesTests
    {
        private static IOASISStorageProvider Provider(ProviderType type, bool atomicSource, bool replicaTarget, bool boundOutbox = true, HolonReplicationDestinationBinding known = null, bool offline = false, bool catalogueError = false, bool withCatalogue = true)
        {
            var storage = new Mock<IOASISStorageProvider>();
            storage.SetupAllProperties();
            storage.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(type));
            storage.SetupGet(value => value.ProviderCategory).Returns(new EnumValue<ProviderCategory>(ProviderCategory.Storage));
            if (atomicSource)
            {
                storage.As<IBoundAtomicHolonReplicationStorageProvider>();
                if (boundOutbox)
                {
                    var outbox = new Mock<IHolonReplicationRouteInventoryOutbox>();
                    if (withCatalogue)
                        outbox.As<IHolonReplicationDestinationCatalogue>()
                            .Setup(value => value.ReadDestinationBindingAsync(It.IsAny<ProviderType>(), It.IsAny<System.Threading.CancellationToken>()))
                            .ReturnsAsync((ProviderType target, System.Threading.CancellationToken token) => catalogueError
                                ? new NextGenSoftware.OASIS.Common.OASISResult<HolonReplicationDestinationBinding> { IsError = true, ErrorCode = "CORRUPT_APPROVAL" }
                                : new NextGenSoftware.OASIS.Common.OASISResult<HolonReplicationDestinationBinding>(known));
                    storage.As<IBoundHolonReplicationOutboxSource>()
                        .Setup(value => value.GetBoundHolonReplicationOutbox(It.IsAny<CosmicStorageIdentity>()))
                        .Returns(new NextGenSoftware.OASIS.Common.OASISResult<IHolonReplicationRouteInventoryOutbox>(outbox.Object));
                }
            }
            if (replicaTarget) storage.As<IBoundCommittedHolonReplicaStorageProvider>();
            if (atomicSource || replicaTarget)
                storage.As<ICosmicStorageIdentityProvider>().Setup(value => value.CaptureStorageIdentityAsync(It.IsAny<System.Threading.CancellationToken>()))
                    .ReturnsAsync(new NextGenSoftware.OASIS.Common.OASISResult<CosmicStorageIdentity>(Identity(type)));
            if (offline)
                storage.As<ICosmicStorageIdentityProvider>()
                    .Setup(value => value.CaptureStorageIdentityAsync(It.IsAny<System.Threading.CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Controlled destination metadata outage."));
            storage.Object.IsProviderActivated = true;
            return storage.Object;
        }

        private static CosmicStorageIdentity Identity(ProviderType type) => new(type, new string('A',64), "controlled-store-" + type);
        private static string Target(ProviderType source, ProviderType target) =>
            new HolonReplicationRouteBinding(Identity(source),Identity(target),HolonReplicationRoutes.DefaultPolicyBinding).TargetId;

        private static ProviderManager Node(bool replication, params ProviderType[] replicationList)
        {
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
            var manager = new ProviderManager(null, dna);
            manager.RegisterProvider(Provider(ProviderType.MongoDBOASIS, atomicSource: true, replicaTarget: true));
            manager.RegisterProvider(Provider(ProviderType.SQLLiteDBOASIS, atomicSource: true, replicaTarget: true));
            manager.RegisterProvider(Provider(ProviderType.Neo4jOASIS, atomicSource: true, replicaTarget: true));
            manager.RegisterProvider(Provider(ProviderType.IPFSOASIS, atomicSource: false, replicaTarget: false));
            Assert.False(manager.PublishRoutingOverrides(new ProviderRoutingOverrides(true, replication, true,
                replicationList, replicationList, replicationList)).IsError);
            return manager;
        }

        [Fact]
        public async Task ColdEnrollmentUsesApprovedDestinationWithoutAnIdentityProbeAndUsesCurrentPolicy()
        {
            var manager = new ProviderManager(null, new OASISDNA());
            var route = new HolonReplicationRouteBinding(Identity(ProviderType.MongoDBOASIS),
                Identity(ProviderType.SQLLiteDBOASIS), "old-policy-provenance");
            var approval = new HolonReplicationDestinationBinding(route, Guid.NewGuid(), Guid.NewGuid());
            manager.RegisterProvider(Provider(ProviderType.MongoDBOASIS, true, true, known: approval));
            var target = Provider(ProviderType.SQLLiteDBOASIS, false, true, offline: true);
            manager.RegisterProvider(target);
            Assert.False(manager.PublishRoutingOverrides(new ProviderRoutingOverrides(autoReplicationEnabled: true,
                replicationProviders: new[] { ProviderType.SQLLiteDBOASIS })).IsError);
            HolonReplicationRoutes.UseAuthorizationBinding(manager, () => "current-policy");

            var planned = await HolonReplicationRoutes.PlanAsync(manager, ProviderType.MongoDBOASIS);

            var actual = Assert.Single(planned.NativeRoutes);
            Assert.Equal(route.TargetStorage.BindingFingerprint, actual.TargetStorage.BindingFingerprint);
            Assert.Equal("current-policy", actual.PolicyBinding);
            Assert.NotEqual(route.TargetId, actual.TargetId);
            Mock.Get(target).As<ICosmicStorageIdentityProvider>().Verify(value =>
                value.CaptureStorageIdentityAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task CatalogueFailureOrMissingCapabilityCannotBecomePermissionToDiscover(bool error, bool capability)
        {
            var manager = new ProviderManager(null, new OASISDNA());
            manager.RegisterProvider(Provider(ProviderType.MongoDBOASIS, true, true, catalogueError: error, withCatalogue: capability));
            var target = Provider(ProviderType.SQLLiteDBOASIS, false, true);
            manager.RegisterProvider(target);
            Assert.False(manager.PublishRoutingOverrides(new ProviderRoutingOverrides(autoReplicationEnabled: true,
                replicationProviders: new[] { ProviderType.SQLLiteDBOASIS })).IsError);

            await Assert.ThrowsAsync<InvalidOperationException>(() => HolonReplicationRoutes.PlanAsync(manager, ProviderType.MongoDBOASIS));
            Mock.Get(target).As<ICosmicStorageIdentityProvider>().Verify(value =>
                value.CaptureStorageIdentityAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ApprovalFromAnotherNativeScopeRejectsBeforeDestinationDiscovery(bool wrongSource)
        {
            var source = wrongSource
                ? new CosmicStorageIdentity(ProviderType.MongoDBOASIS, new string('C', 64), Guid.NewGuid())
                : Identity(ProviderType.MongoDBOASIS);
            var targetIdentity = Identity(wrongSource ? ProviderType.SQLLiteDBOASIS : ProviderType.Neo4jOASIS);
            var route = new HolonReplicationRouteBinding(source, targetIdentity, "policy");
            var manager = new ProviderManager(null, new OASISDNA());
            manager.RegisterProvider(Provider(ProviderType.MongoDBOASIS, true, true,
                known: new HolonReplicationDestinationBinding(route, Guid.NewGuid(), Guid.NewGuid())));
            var target = Provider(ProviderType.SQLLiteDBOASIS, false, true);
            manager.RegisterProvider(target);
            Assert.False(manager.PublishRoutingOverrides(new ProviderRoutingOverrides(autoReplicationEnabled: true,
                replicationProviders: new[] { ProviderType.SQLLiteDBOASIS })).IsError);

            await Assert.ThrowsAsync<InvalidOperationException>(() => HolonReplicationRoutes.PlanAsync(manager, ProviderType.MongoDBOASIS));
            Mock.Get(target).As<ICosmicStorageIdentityProvider>().Verify(value =>
                value.CaptureStorageIdentityAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Fact]
        public void EnrolsEverySelectedBoundReplicationProvider()
        {
            var manager = Node(true, ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS);

            var enrollment = HolonReplicationRoutes.Plan(manager, ProviderType.Neo4jOASIS);

            Assert.Equal(new HolonReplicationRouteBinding(Identity(ProviderType.Neo4jOASIS),Identity(ProviderType.MongoDBOASIS),HolonReplicationRoutes.DefaultPolicyBinding).SourceId, enrollment.SourceId);
            Assert.Equal(new[] { Target(ProviderType.Neo4jOASIS,ProviderType.MongoDBOASIS), Target(ProviderType.Neo4jOASIS,ProviderType.SQLLiteDBOASIS) },
                enrollment.TargetIds);
        }

        [Fact]
        public void NothingIsEnrolledWhenReplicationIsOffOrHasNoOtherTarget()
        {
            Assert.Null(HolonReplicationRoutes.Plan(Node(false, ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS), ProviderType.MongoDBOASIS));
            Assert.Null(HolonReplicationRoutes.Plan(Node(true, ProviderType.MongoDBOASIS), ProviderType.MongoDBOASIS));
            Assert.Throws<InvalidOperationException>(() => HolonReplicationRoutes.Plan(Node(true, ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS), ProviderType.IPFSOASIS));
        }

        [Fact]
        public void BoundAtomicSourceWithoutBoundOutboxCannotEnrolAutomaticDelivery()
        {
            var manager = new ProviderManager(null, new OASISDNA());
            manager.RegisterProvider(Provider(ProviderType.MongoDBOASIS, true, true, boundOutbox: false));
            manager.RegisterProvider(Provider(ProviderType.SQLLiteDBOASIS, true, true));
            Assert.False(manager.PublishRoutingOverrides(new ProviderRoutingOverrides(
                autoReplicationEnabled: true, replicationProviders: new[] { ProviderType.SQLLiteDBOASIS })).IsError);
            Assert.Throws<InvalidOperationException>(() => HolonReplicationRoutes.Plan(manager, ProviderType.MongoDBOASIS));
        }

        [Fact]
        public void SelectedUnboundTargetRejectsEnrollmentInsteadOfSilentlyDroppingAnObligation()
        {
            var manager = Node(true, ProviderType.MongoDBOASIS, ProviderType.IPFSOASIS);
            Assert.Throws<InvalidOperationException>(() => HolonReplicationRoutes.Plan(manager, ProviderType.MongoDBOASIS));
        }

        [Fact]
        public async Task TheAvatarsReplicationListDecidesTheTargets()
        {
            var manager = Node(true, ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS);
            var enrollment = await Task.Run(() =>
            {
                OASISRequestContext.AvatarRoutingOverrides = new ProviderRoutingOverrides(
                    replicationProviders: new[] { ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS });
                return HolonReplicationRoutes.Plan(manager, ProviderType.MongoDBOASIS);
            });
            Assert.Equal(new[] { Target(ProviderType.MongoDBOASIS,ProviderType.Neo4jOASIS) }, enrollment.TargetIds);
        }
    }
}

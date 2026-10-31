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
        private static IOASISStorageProvider Provider(ProviderType type, bool atomicSource, bool replicaTarget)
        {
            var storage = new Mock<IOASISStorageProvider>();
            storage.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(type));
            storage.SetupGet(value => value.ProviderCategory).Returns(new EnumValue<ProviderCategory>(ProviderCategory.Storage));
            if (atomicSource) storage.As<IAtomicHolonReplicationStorageProvider>();
            if (replicaTarget) storage.As<ICommittedHolonReplicaStorageProvider>();
            return storage.Object;
        }

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
        public void EnrolsEveryOtherCapableReplicationProvider()
        {
            var manager = Node(true, ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS, ProviderType.IPFSOASIS);

            var enrollment = HolonReplicationRoutes.Plan(manager, ProviderType.Neo4jOASIS);

            Assert.Equal(HolonReplicationRoutes.SourceId(ProviderType.Neo4jOASIS), enrollment.SourceId);
            Assert.Equal(new[] { HolonReplicationRoutes.TargetId(ProviderType.MongoDBOASIS), HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS) },
                enrollment.TargetIds);
        }

        [Fact]
        public void NothingIsEnrolledWhenReplicationIsOffOrHasNoOtherTarget()
        {
            Assert.Null(HolonReplicationRoutes.Plan(Node(false, ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS), ProviderType.MongoDBOASIS));
            Assert.Null(HolonReplicationRoutes.Plan(Node(true, ProviderType.MongoDBOASIS), ProviderType.MongoDBOASIS));
            Assert.Null(HolonReplicationRoutes.Plan(Node(true, ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS), ProviderType.IPFSOASIS));
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
            Assert.Equal(new[] { HolonReplicationRoutes.TargetId(ProviderType.Neo4jOASIS) }, enrollment.TargetIds);
        }
    }
}

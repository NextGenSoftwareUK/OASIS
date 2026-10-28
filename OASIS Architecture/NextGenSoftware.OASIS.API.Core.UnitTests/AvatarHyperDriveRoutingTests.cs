using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests
{
    /// <summary>
    /// Per-avatar HyperDrive routing: request overrides win over the avatar's saved settings, which win over the node's
    /// DNA defaults; each request sees only its own values; publishing node defaults never absorbs one avatar's view.
    /// </summary>
    public class AvatarHyperDriveRoutingTests
    {
        private static readonly ProviderType[] Node = { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS, ProviderType.SolanaOASIS };

        private static ProviderManager NodeManager()
        {
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
            var manager = new ProviderManager(null, dna);
            Assert.False(manager.PublishRoutingOverrides(new ProviderRoutingOverrides(true, false, true, Node, Node, Node)).IsError);
            return manager;
        }

        private static ProviderType[] Types(IEnumerable<NextGenSoftware.Utilities.EnumValue<ProviderType>> values) => values.Select(v => v.Value).ToArray();

        private static Task<T> InRequest<T>(ProviderRoutingOverrides avatar, ProviderRoutingOverrides request, Func<T> read) => Task.Run(() =>
        {
            OASISRequestContext.AvatarRoutingOverrides = avatar;
            OASISRequestContext.RequestRoutingOverrides = request;
            return read();
        });

        [Fact]
        public async Task RequestOverAvatarOverNodeDefaults()
        {
            var manager = NodeManager();
            var avatar = new ProviderRoutingOverrides(autoReplicationEnabled: true,
                failoverProviders: new[] { ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS });
            var request = new ProviderRoutingOverrides(autoReplicationEnabled: false);

            var avatarOnly = await InRequest(avatar, null, () => (manager.IsAutoReplicationEnabled, Types(manager.GetProviderAutoFailOverList())));
            Assert.True(avatarOnly.IsAutoReplicationEnabled);
            Assert.Equal(new[] { ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS }, avatarOnly.Item2);

            var withRequest = await InRequest(avatar, request, () => (manager.IsAutoReplicationEnabled, Types(manager.GetProviderAutoFailOverList())));
            Assert.False(withRequest.IsAutoReplicationEnabled);
            Assert.Equal(new[] { ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS }, withRequest.Item2);

            var none = await InRequest(null, null, () => (manager.IsAutoReplicationEnabled, Types(manager.GetProvidersThatAreAutoReplicating())));
            Assert.False(none.IsAutoReplicationEnabled);
            Assert.Equal(Node, none.Item2);
        }

        [Fact]
        public async Task ConcurrentRequestsSeeOnlyTheirOwnAvatarsRouting()
        {
            var manager = NodeManager();
            var gate = new TaskCompletionSource();
            Task<ProviderType[]> Read(ProviderType only) => Task.Run(async () =>
            {
                OASISRequestContext.AvatarRoutingOverrides = new ProviderRoutingOverrides(loadBalancingProviders: new[] { only });
                await gate.Task;
                return Types(manager.GetProviderAutoLoadBalanceList());
            });
            var a = Read(ProviderType.MongoDBOASIS);
            var b = Read(ProviderType.Neo4jOASIS);
            gate.SetResult();
            Assert.Equal(new[] { ProviderType.MongoDBOASIS }, await a);
            Assert.Equal(new[] { ProviderType.Neo4jOASIS }, await b);
        }

        [Fact]
        public async Task PublishingNodeDefaultsIgnoresTheCallersAvatarSettings()
        {
            var manager = NodeManager();
            await InRequest(new ProviderRoutingOverrides(autoFailoverEnabled: false, failoverProviders: new[] { ProviderType.Neo4jOASIS }), null,
                () => manager.PublishRoutingOverrides(new ProviderRoutingOverrides(autoLoadBalancingEnabled: false)));

            var node = await InRequest(null, null, () => (manager.IsAutoFailOverEnabled, manager.IsAutoLoadBalanceEnabled, Types(manager.GetProviderAutoFailOverList())));
            Assert.True(node.IsAutoFailOverEnabled);
            Assert.False(node.IsAutoLoadBalanceEnabled);
            Assert.Equal(Node, node.Item3);
        }

        [Fact]
        public async Task RoutingScopesStartFromTheAvatarViewAndThenIgnoreTheAmbientContext()
        {
            var manager = NodeManager();
            var scope = await InRequest(new ProviderRoutingOverrides(failoverProviders: new[] { ProviderType.Neo4jOASIS }), null,
                () => manager.CreateRoutingScope(new ProviderRoutingOverrides(autoReplicationEnabled: true)).Result);

            var seen = await InRequest(new ProviderRoutingOverrides(failoverProviders: new[] { ProviderType.MongoDBOASIS }, autoReplicationEnabled: false), null,
                () => (Types(scope.GetProviderAutoFailOverList()), scope.IsAutoReplicationEnabled));
            Assert.Equal(new[] { ProviderType.Neo4jOASIS }, seen.Item1);
            Assert.True(seen.IsAutoReplicationEnabled);
        }

        [Fact]
        public async Task ProviderFaultsApplyOnlyToTheAvatarThatSetThem()
        {
            ProviderFaultGate.Enable();
            Guid visitor = Guid.NewGuid(), other = Guid.NewGuid();
            ProviderFaultGate.Fault(visitor, ProviderType.Neo4jOASIS, TimeSpan.FromMinutes(1));
            try
            {
                Task<bool> Check(Guid? avatar) => Task.Run(() => { OASISRequestContext.CurrentAvatarId = avatar; return ProviderFaultGate.IsFaulted(ProviderType.Neo4jOASIS); });
                Assert.True(await Check(visitor));
                Assert.False(await Check(other));
                Assert.False(await Check(null));
                Assert.Equal(1, ProviderFaultGate.GetRefusals(visitor)[ProviderType.Neo4jOASIS]);
                Assert.Empty(ProviderFaultGate.GetFaulted(other));
                Assert.Throws<ArgumentException>(() => ProviderFaultGate.Fault(Guid.Empty, ProviderType.Neo4jOASIS, TimeSpan.FromSeconds(5)));
            }
            finally { ProviderFaultGate.Restore(visitor, ProviderType.Neo4jOASIS); }
        }

        [Fact]
        public void SavedSettingsParseAndValidate()
        {
            var normalized = AvatarHyperDriveSettings.Normalize(new Dictionary<string, object>
            {
                [AvatarHyperDriveSettings.AutoFailoverEnabled] = "false",
                [AvatarHyperDriveSettings.AutoReplicationProviders] = "[\"MongoDBOASIS\", \"Neo4jOASIS\"]",
                [AvatarHyperDriveSettings.LoadBalancingProviders] = "DEFAULT",
                ["theme"] = "dark"
            }, provider => Node.Contains(provider));
            Assert.False(normalized.IsError, normalized.Message);
            Assert.Equal(false, normalized.Result[AvatarHyperDriveSettings.AutoFailoverEnabled]);
            Assert.Equal("MongoDBOASIS, Neo4jOASIS", normalized.Result[AvatarHyperDriveSettings.AutoReplicationProviders]);
            Assert.Null(normalized.Result[AvatarHyperDriveSettings.LoadBalancingProviders]);
            Assert.Equal("dark", normalized.Result["theme"]);

            var parsed = AvatarHyperDriveSettings.FromSettings(new Dictionary<string, object>
            {
                [AvatarHyperDriveSettings.AutoFailoverEnabled] = false,
                [AvatarHyperDriveSettings.AutoReplicationProviders] = "MongoDBOASIS, Neo4jOASIS"
            });
            Assert.False(parsed.Result.AutoFailoverEnabled.Value);
            Assert.Null(parsed.Result.AutoLoadBalancingEnabled);
            Assert.Equal(new[] { ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS }, parsed.Result.ReplicationProviders);

            Assert.Null(AvatarHyperDriveSettings.FromSettings(new Dictionary<string, object> { ["theme"] = "dark" }).Result);
        }

        [Fact]
        public void InvalidSettingsAreRefusedNotIgnored()
        {
            Assert.Equal("AVATAR_HYPERDRIVE_SETTINGS_INVALID", AvatarHyperDriveSettings.Normalize(new Dictionary<string, object>
            { [AvatarHyperDriveSettings.AutoFailoverProviders] = "EthereumOASIS" }, provider => Node.Contains(provider)).ErrorCode);
            Assert.True(AvatarHyperDriveSettings.Normalize(new Dictionary<string, object>
            { [AvatarHyperDriveSettings.AutoReplicationEnabled] = "sometimes" }, _ => true).IsError);
            Assert.True(AvatarHyperDriveSettings.FromSettings(new Dictionary<string, object>
            { [AvatarHyperDriveSettings.AutoFailoverProviders] = "NotAProvider" }).IsError);
            Assert.True(AvatarHyperDriveSettings.FromSettings(new Dictionary<string, object>
            { [AvatarHyperDriveSettings.AutoFailoverProviders] = "MongoDBOASIS, MongoDBOASIS" }).IsError);
        }
    }
}

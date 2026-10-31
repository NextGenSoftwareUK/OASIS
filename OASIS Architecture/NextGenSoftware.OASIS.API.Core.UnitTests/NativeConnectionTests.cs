using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests
{
    /// <summary>Caller-supplied native connections: the settings bag, the address guard, and how a connection separates imported holons.</summary>
    [Collection("NativeConnectionGuard")]
    public class NativeConnectionTests
    {
        private static readonly Guid Owner = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

        [Fact]
        public void SettingsAreCaseInsensitiveRequiredOrOptionalAndTypoChecked()
        {
            var connection = new NativeConnection { ["URI"] = "mongodb://db", ["database"] = " " };

            Assert.Equal("mongodb://db", connection.Require("uri"));
            Assert.Null(connection.Optional("database"));
            Assert.Throws<NativeConnectionRefusedException>(() => connection.Require("database"));
            connection.RequireOnly("uri", "database");
            var typo = Assert.Throws<NativeConnectionRefusedException>(() => new NativeConnection { ["urii"] = "x" }.RequireOnly("uri", "database"));
            Assert.Contains("urii", typo.Message);
        }

        [Fact]
        public void RedactRemovesSecretsFromMessages()
        {
            var connection = new NativeConnection { ["password"] = "hunter22", ["host"] = "db.example" };
            var message = connection.Redact("auth failed for hunter22 on db.example", "password");
            Assert.Equal("auth failed for *** on db.example", message);
            Assert.Equal("no secret here", connection.Redact("no secret here", "password"));
        }

        [Theory]
        [InlineData("8.8.8.8", true)]
        [InlineData("1.1.1.1", true)]
        [InlineData("2606:4700:4700::1111", true)]
        [InlineData("127.0.0.1", false)]
        [InlineData("10.1.2.3", false)]
        [InlineData("172.16.0.1", false)]
        [InlineData("172.31.255.255", false)]
        [InlineData("172.32.0.1", true)]
        [InlineData("192.168.1.1", false)]
        [InlineData("169.254.169.254", false)]
        [InlineData("100.64.0.1", false)]
        [InlineData("0.0.0.0", false)]
        [InlineData("224.0.0.1", false)]
        [InlineData("::1", false)]
        [InlineData("fe80::1", false)]
        [InlineData("fd00::1", false)]
        [InlineData("::ffff:10.0.0.1", false)]
        public void OnlyPublicAddressesPass(string address, bool expected) =>
            Assert.Equal(expected, NativeConnectionGuard.IsPublicAddress(IPAddress.Parse(address)));

        [Theory]
        [InlineData("localhost")]
        [InlineData("neo4j.railway.internal")]
        [InlineData("db.local")]
        [InlineData("intranet")]
        [InlineData("127.0.0.1")]
        [InlineData("[::1]")]
        [InlineData("169.254.169.254")]
        [InlineData("")]
        public async Task InternalAndLoopbackHostsAreRefusedBeforeAnyLookup(string host)
        {
            var lookups = 0;
            var original = NativeConnectionGuard.Resolver;
            NativeConnectionGuard.Resolver = (_, _) => { lookups++; return Task.FromResult(new[] { IPAddress.Parse("8.8.8.8") }); };
            try
            {
                await Assert.ThrowsAsync<NativeConnectionRefusedException>(() => NativeConnectionGuard.EnsurePublicHostAsync(host));
                Assert.Equal(0, lookups);
            }
            finally { NativeConnectionGuard.Resolver = original; }
        }

        [Fact]
        public async Task NamesMustResolveEntirelyToPublicAddresses()
        {
            var original = NativeConnectionGuard.Resolver;
            try
            {
                NativeConnectionGuard.Resolver = (_, _) => Task.FromResult(new[] { IPAddress.Parse("8.8.8.8") });
                await NativeConnectionGuard.EnsurePublicHostAsync("db.example.com");

                // One private answer is enough to refuse the name (DNS pointing a public name at the internal network).
                NativeConnectionGuard.Resolver = (_, _) => Task.FromResult(new[] { IPAddress.Parse("8.8.8.8"), IPAddress.Parse("10.0.0.5") });
                await Assert.ThrowsAsync<NativeConnectionRefusedException>(() => NativeConnectionGuard.EnsurePublicHostAsync("db.example.com"));

                NativeConnectionGuard.Resolver = (_, _) => Task.FromResult(Array.Empty<IPAddress>());
                await Assert.ThrowsAsync<NativeConnectionRefusedException>(() => NativeConnectionGuard.EnsurePublicHostAsync("db.example.com"));

                NativeConnectionGuard.Resolver = (_, _) => throw new System.Net.Sockets.SocketException();
                await Assert.ThrowsAsync<NativeConnectionRefusedException>(() => NativeConnectionGuard.EnsurePublicHostAsync("db.example.com"));
            }
            finally { NativeConnectionGuard.Resolver = original; }
        }


        [Fact]
        public async Task AllowPrivateSkipsTheChecksAndDefaultsToOff()
        {
            Assert.False(NativeConnectionGuard.AllowPrivate);
            try
            {
                NativeConnectionGuard.AllowPrivateOverride = true;
                await NativeConnectionGuard.EnsurePublicHostAsync("localhost");
                await NativeConnectionGuard.EnsurePublicHostAsync("10.0.0.5");
                await NativeConnectionGuard.EnsurePublicHostAsync("neo4j.railway.internal");
            }
            finally { NativeConnectionGuard.AllowPrivateOverride = null; }
            await Assert.ThrowsAsync<NativeConnectionRefusedException>(() => NativeConnectionGuard.EnsurePublicHostAsync("localhost"));
        }
        [Fact]
        public void UnsupportedReadersRefuseAConnectionInsteadOfIgnoringIt()
        {
            NativeConnectionGuard.RefuseUnsupported(null, "X");
            var refused = Assert.Throws<NativeConnectionRefusedException>(() => NativeConnectionGuard.RefuseUnsupported(new NativeConnection { ["a"] = "b" }, "Solana"));
            Assert.Contains("Solana", refused.Message);
        }

        [Fact]
        public void ConnectionIdentityChangesTheHolonIdButNotTheConfiguredOnes()
        {
            var configured = NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.MongoDBOASIS, "people", "1");
            var sameAsBefore = NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.MongoDBOASIS, "people", "1", null);
            var mine = NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.MongoDBOASIS, "people", "1", "mongodb://a/shop");
            var theirs = NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.MongoDBOASIS, "people", "1", "mongodb://b/shop");

            Assert.Equal(configured, sameAsBefore);
            Assert.NotEqual(configured, mine);
            Assert.NotEqual(mine, theirs);
            Assert.Equal(mine, NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.MongoDBOASIS, "people", "1", "mongodb://a/shop"));
        }

        private sealed class Reader : INativeDataImportProvider
        {
            public NativeConnection Seen;
            public string Identity = "fake://db.example/shop";

            public ProviderType NativeDataProviderType => ProviderType.MongoDBOASIS;

            public string DescribeNativeConnection(NativeConnection connection) =>
                connection == null ? null : connection.ContainsKey("bad") ? throw new NativeConnectionRefusedException("bad connection") : Identity;

            public Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(NativeConnection connection = null, CancellationToken cancellationToken = default)
            {
                Seen = connection;
                return Task.FromResult(new OASISResult<IReadOnlyList<NativeDataSource>>(new[] { new NativeDataSource("people", "collection", 1) }));
            }

            public Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize,
                NativeConnection connection = null, CancellationToken cancellationToken = default)
            {
                Seen = connection;
                return Task.FromResult(new OASISResult<NativeRecordPage>(new NativeRecordPage(new[]
                { new NativeRecord("people", "1", new Dictionary<string, object> { ["name"] = "Ada" }) }, null)));
            }
        }

        private static NativeDataImportManager Manager(Dictionary<Guid, IHolon> store) => new NativeDataImportManager(
            (id, _) => Task.FromResult(new OASISResult<IHolon>(store.TryGetValue(id, out var found) ? found : null)),
            (holon, _, _) => { store[holon.Id] = holon; return Task.FromResult(new OASISResult<IHolon>(holon) { IsSaved = true, HasAnyHolonsChanged = true }); });

        [Fact]
        public async Task ImportFromAConnectionIsKeptApartFromTheConfiguredStoreAndUpdatesOnReimport()
        {
            var store = new Dictionary<Guid, IHolon>();
            var reader = new Reader();
            var manager = Manager(store);
            var connection = new NativeConnection { ["host"] = "db.example" };

            await manager.ImportSourceAsync(reader, "people", Owner);
            var configured = await manager.ImportSourceAsync(reader, "people", Owner);
            Assert.Equal(1, configured.Result.HolonsUpdated);
            Assert.Single(store);

            var first = await manager.ImportSourceAsync(reader, "people", Owner, connection: connection);
            Assert.False(first.IsError, first.Message);
            Assert.Equal(1, first.Result.HolonsCreated);
            Assert.Same(connection, reader.Seen);
            Assert.Equal(2, store.Count);
            Assert.Equal("fake://db.example/shop", store.Values.Single(h => h.MetaData.ContainsKey(NativeRecordHolonMapper.ConnectionMetaDataKey))
                .MetaData[NativeRecordHolonMapper.ConnectionMetaDataKey]);

            var again = await manager.ImportSourceAsync(reader, "people", Owner, connection: connection);
            Assert.Equal(0, again.Result.HolonsCreated);
            Assert.Equal(1, again.Result.HolonsUpdated);
            Assert.Equal(2, store.Count);

            reader.Identity = "fake://other.example/shop";
            var other = await manager.ImportSourceAsync(reader, "people", Owner, connection: connection);
            Assert.Equal(1, other.Result.HolonsCreated);
            Assert.Equal(3, store.Count);
        }

        [Fact]
        public async Task RefusedOrUndescribableConnectionsStopTheImportBeforeReading()
        {
            var reader = new Reader();
            var manager = Manager(new Dictionary<Guid, IHolon>());

            var refused = await manager.ImportSourceAsync(reader, "people", Owner, connection: new NativeConnection { ["bad"] = "1" });
            Assert.Equal("NATIVE_IMPORT_CONNECTION_REFUSED", refused.ErrorCode);
            Assert.Null(reader.Seen);

            reader.Identity = null;
            var undescribed = await manager.ImportSourceAsync(reader, "people", Owner, connection: new NativeConnection { ["host"] = "x" });
            Assert.Equal("NATIVE_IMPORT_CONNECTION_REFUSED", undescribed.ErrorCode);
            Assert.Null(reader.Seen);
        }

        [Fact]
        public async Task ListingAndImportAllCarryTheConnection()
        {
            var reader = new Reader();
            var manager = Manager(new Dictionary<Guid, IHolon>());
            var connection = new NativeConnection { ["host"] = "db.example" };

            await manager.ListSourcesAsync(reader, connection);
            Assert.Same(connection, reader.Seen);

            reader.Seen = null;
            var all = await manager.ImportAllAsync(reader, Owner, connection: connection);
            Assert.False(all.IsError, all.Message);
            Assert.Same(connection, reader.Seen);
            Assert.Equal(1, all.Result.Single().HolonsCreated);
        }
    }

    [CollectionDefinition("NativeConnectionGuard", DisableParallelization = true)]
    public class NativeConnectionGuardCollection { }
}

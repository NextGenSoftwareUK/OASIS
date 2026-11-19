using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;
using MongoNative = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteNative = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NeoNative = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    // Each observer inherits the shipping provider and awaits its actual reader.
    // The real result object, native clients, writes and profile capabilities stay intact.
    private sealed class PortraitNativeReadBarrier
    {
        private readonly TaskCompletionSource<OASISResult<IAvatarDetail>> _captured =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Guid _id;
        private int _armed;
        private int _claimed;
        public Task<OASISResult<IAvatarDetail>> Captured => _captured.Task;
        public void Arm(Guid id) { Assert.NotEqual(Guid.Empty, id); _id = id; Volatile.Write(ref _armed, 1); }
        public void Release() => _released.TrySetResult(true);
        public async Task<OASISResult<IAvatarDetail>> AfterNativeReadAsync(Guid id, Task<OASISResult<IAvatarDetail>> actual)
        {
            var result = await actual.ConfigureAwait(false);
            if (Volatile.Read(ref _armed) == 1 && _id == id && Interlocked.CompareExchange(ref _claimed, 1, 0) == 0)
            {
                _captured.TrySetResult(result);
                await _released.Task.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            }
            return result;
        }
    }

    private sealed class PortraitReadGatedMongo(string connection, string database, PortraitNativeReadBarrier barrier)
        : MongoNative(connection, database)
    {
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) =>
            barrier.AfterNativeReadAsync(id, base.LoadAvatarDetailAsync(id, version));
    }
    private sealed class PortraitReadGatedSqlite(string connection, PortraitNativeReadBarrier barrier) : SqliteNative(connection)
    {
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) =>
            barrier.AfterNativeReadAsync(id, base.LoadAvatarDetailAsync(id, version));
    }
    private sealed class PortraitReadGatedNeo(string endpoint, string user, string password, PortraitNativeReadBarrier barrier)
        : NeoNative(endpoint, user, password)
    {
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) =>
            barrier.AfterNativeReadAsync(id, base.LoadAvatarDetailAsync(id, version));
    }

    [Theory]
    [InlineData("MongoDBOASIS")]
    [InlineData("SQLLiteDBOASIS")]
    [InlineData("Neo4jOASIS")]
    public async Task PortraitNativeReadWriteOverlapPreservesNewerProfile(string providerName)
    {
        RequireOwnedFixtures();
        var id = Guid.NewGuid();
        var barrier = new PortraitNativeReadBarrier();
        IOASISStorageProvider? provider = null, peer = null;
        IAsyncDisposable? asyncOwner = null;
        IDisposable? syncOwner = null;
        Task<OASISResult<bool>>? upload = null;
        const string image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl2nXcAAAAASUVORK5CYII=";
        try
        {
            switch (providerName)
            {
                case "MongoDBOASIS":
                    var mongo = new MongoStore(barrier); asyncOwner = mongo;
                    await mongo.Activate(); provider = mongo.Provider; peer = mongo.CreatePeerProvider(); break;
                case "SQLLiteDBOASIS":
                    var sqlite = new SqliteStore(barrier); syncOwner = sqlite; provider = sqlite.Provider;
                    var active = await provider.ActivateProviderAsync(); Assert.False(active.IsError, active.Message);
                    peer = sqlite.CreatePeerProvider(); break;
                case "Neo4jOASIS":
                    var neo = new BoltStore(barrier); asyncOwner = neo;
                    await neo.Activate(id); provider = neo.Provider; peer = neo.CreatePeerProvider(); break;
                default: throw new ArgumentOutOfRangeException(nameof(providerName));
            }
            var peerActive = await peer.ActivateProviderAsync(); Assert.False(peerActive.IsError, peerActive.Message);
            Assert.NotSame(provider, peer);
            var before = await SeedRoutingStores([provider], id);
            var originalPortrait = (await peer.LoadAvatarDetailAsync(id)).Result.Portrait;
            var routing = Routing([provider], false, HyperDriveModes.V2);
            var sdk = new AvatarManager(null, routing.OASISDNA, routing);
            var peerRouting = Routing([peer], false, HyperDriveModes.V2);
            var peerSdk = new AvatarManager(null, peerRouting.OASISDNA, peerRouting);
            barrier.Arm(id);
            upload = sdk.UploadAvatarPortraitAsync(id, null!, null!, image);
            var arrived = await Task.WhenAny(barrier.Captured, upload).WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Same(barrier.Captured, arrived);
            var captured = await barrier.Captured;
            Assert.False(captured.IsError, captured.Message); Assert.NotNull(captured.Result);
            Assert.Equal(before.FirstName, captured.Result.FirstName);
            var newerName = "Newer " + providerName;
            var edited = await peerSdk.ApplyAvatarProfileAsync(id,
                Edit(id, before, (AvatarProfileProperty.FirstName, newerName)), peer.ProviderType.Value);
            Assert.False(edited.IsError, edited.Message); Assert.True(edited.IsSaved);
            var committedDetail = await peer.LoadAvatarDetailAsync(id);
            Assert.False(committedDetail.IsError, committedDetail.Message);
            Assert.Equal(newerName, committedDetail.Result.FirstName);
            Console.WriteLine("Portrait overlap ordering verified: " + providerName +
                "; actual native read completed before independent native profile commit, portrait save still paused.");
            barrier.Release();
            var result = await upload;
            var account = await peer.LoadAvatarAsync(id);
            var detail = await peer.LoadAvatarDetailAsync(id);
            Assert.False(account.IsError, account.Message); Assert.False(detail.IsError, detail.Message);
            Assert.Equal(newerName, account.Result.FirstName);
            Assert.Equal(newerName, detail.Result.FirstName);
            Assert.Equal(before.LastName, detail.Result.LastName);
            Assert.Equal(before.Username, detail.Result.Username);
            Assert.Equal(before.Email, detail.Result.Email);
            Assert.Equal(42, detail.Result.XP); Assert.Equal("stored-hash", account.Result.Password);
            if (result.IsError)
            {
                Assert.Equal("AVATAR_PROFILE_REVISION_CONFLICT", result.ErrorCode);
                Assert.False(result.IsSaved); Assert.False(result.Result);
                Assert.Equal(originalPortrait, detail.Result.Portrait);
            }
            else
            {
                Assert.True(result.IsSaved); Assert.True(result.Result);
                Assert.Equal(image, detail.Result.Portrait);
            }
            Console.WriteLine("Portrait native overlap preserved newer profile: " + providerName +
                "; real independent clients, successful field-safe save or explicit revision conflict, no retry.");
        }
        finally
        {
            barrier.Release();
            try { if (upload != null) await upload; }
            finally
            {
                try
                {
                    if (peer != null)
                    {
                        try
                        {
                            var stopped = await peer.DeActivateProviderAsync();
                            Assert.False(stopped.IsError, stopped.Message); Assert.True(stopped.Result);
                        }
                        finally { (peer as IDisposable)?.Dispose(); }
                    }
                }
                finally
                {
                    if (asyncOwner != null) await asyncOwner.DisposeAsync();
                    syncOwner?.Dispose();
                    Console.WriteLine("Owned portrait overlap clients/data cleanup verified: " + providerName);
                }
            }
        }
    }
}

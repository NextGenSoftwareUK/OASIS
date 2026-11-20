using System.Reflection;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
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
    private sealed class PreferenceNativeReadBarrier
    {
        private readonly TaskCompletionSource<OASISResult<IAvatar>> _captured = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Guid _id;
        private int _armed, _claimed;
        public Task<OASISResult<IAvatar>> Captured => _captured.Task;
        public void Arm(Guid id) { Assert.NotEqual(Guid.Empty,id); _id=id; Volatile.Write(ref _armed,1); }
        public void Release() => _release.TrySetResult(true);
        public async Task<OASISResult<IAvatar>> AfterNativeReadAsync(Guid id, Task<OASISResult<IAvatar>> actual)
        {
            var value=await actual.ConfigureAwait(false);
            if(Volatile.Read(ref _armed)==1 && id==_id && Interlocked.CompareExchange(ref _claimed,1,0)==0)
            {
                _captured.TrySetResult(value);
                await _release.Task.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            }
            return value;
        }
    }
    private sealed class PreferenceReadGatedMongo(string connection,string database,PreferenceNativeReadBarrier barrier)
        : MongoNative(connection,database)
    {
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id,int version=0) =>
            barrier.AfterNativeReadAsync(id,base.LoadAvatarAsync(id,version));
    }
    private sealed class PreferenceReadGatedSqlite(string connection,PreferenceNativeReadBarrier barrier) : SqliteNative(connection)
    {
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id,int version=0) =>
            barrier.AfterNativeReadAsync(id,base.LoadAvatarAsync(id,version));
    }
    private sealed class PreferenceReadGatedNeo(string endpoint,string user,string password,PreferenceNativeReadBarrier barrier)
        : NeoNative(endpoint,user,password)
    {
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id,int version=0) =>
            barrier.AfterNativeReadAsync(id,base.LoadAvatarAsync(id,version));
    }

    private static string? StoredPrivacy(IAvatar avatar) =>
        (string?)NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.CaptureRuntimeToken(
            avatar.MetaData ?? new Dictionary<string,object>())["preferences"]?["privacy"]?["profileVisibility"];

    [Theory]
    [InlineData("MongoDBOASIS")]
    [InlineData("SQLLiteDBOASIS")]
    [InlineData("Neo4jOASIS")]
    public async Task PreferenceNativeReadWriteOverlapPreservesNewerAccount(string providerName)
    {
        RequireOwnedFixtures();
        var id=Guid.NewGuid();var barrier=new PreferenceNativeReadBarrier();
        IOASISStorageProvider? owner=null,reader=null;
        IAsyncDisposable? asyncOwner=null;IDisposable? syncOwner=null;
        Task<OASISResult<bool>>? saving=null;
        Func<Task<string?>>? nativeFirstName=null;
        Func<Task<string?>>? nativePrivacy=null;
        try
        {
            switch(providerName)
            {
                case "MongoDBOASIS":
                    var mongo=new MongoStore();asyncOwner=mongo;await mongo.Activate();owner=mongo.Provider;
                    reader=mongo.CreatePreferencePeerProvider(barrier);
                    nativeFirstName=async () => (await mongo.ReadNative(id)).FirstName;break;
                case "SQLLiteDBOASIS":
                    var sqlite=new SqliteStore();syncOwner=sqlite;owner=sqlite.Provider;
                    var active=await owner.ActivateProviderAsync();Assert.False(active.IsError,active.Message);
                    reader=sqlite.CreatePreferencePeerProvider(barrier);nativeFirstName=()=>sqlite.ReadFirstNameNativeAsync(id);
                    nativePrivacy=async () => (string?)JObject.Parse(await sqlite.ReadMetadataJsonNativeAsync(id))
                        ["preferences"]?["privacy"]?["profileVisibility"];break;
                case "Neo4jOASIS":
                    var neo=new BoltStore();asyncOwner=neo;await neo.Activate(id);owner=neo.Provider;
                    reader=neo.CreatePreferencePeerProvider(barrier);nativeFirstName=()=>neo.ReadFirstNameNativeAsync(id);break;
                default:throw new ArgumentOutOfRangeException(nameof(providerName));
            }
            Assert.NotSame(owner,reader);
            var activation=await reader.ActivateProviderAsync();Assert.False(activation.IsError,activation.Message);
            // This path legitimately processes credentials during a full save.
            // Seed a real shipping hash so credential preservation is meaningful.
            var storedPassword=PasswordEncryptionHelper.HashPassword("owned-preference-test-password",null);
            var before=await SeedRoutingStores([owner],id,password:storedPassword);
            var original=await owner.LoadAvatarAsync(id);Assert.False(original.IsError,original.Message);
            var originalPrivacy=StoredPrivacy(original.Result);
            var routing=Routing([reader],false,HyperDriveModes.V2);
            var settings=(SettingsManager)Activator.CreateInstance(typeof(SettingsManager),
                BindingFlags.Instance|BindingFlags.NonPublic,null,[null,routing.OASISDNA,routing],null)!;
            barrier.Arm(id);
            saving=settings.UpdatePrivacySettingsAsync(id,new Dictionary<string,object>{{"profileVisibility","private"}});
            var arrived=await Task.WhenAny(barrier.Captured,saving).WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Same(barrier.Captured,arrived);
            var captured=await barrier.Captured;Assert.False(captured.IsError,captured.Message);
            Assert.Equal(before.FirstName,captured.Result.FirstName);
            var peerRouting=Routing([owner],false,HyperDriveModes.V2);
            var peer=new AvatarManager(null,peerRouting.OASISDNA,peerRouting);
            var newer="Newer preference account "+providerName;
            var changed=await peer.ApplyAvatarProfileAsync(id,Edit(id,before,(AvatarProfileProperty.FirstName,newer)),owner.ProviderType.Value);
            Assert.False(changed.IsError,changed.Message);Assert.True(changed.IsSaved);
            Assert.Equal(newer,await nativeFirstName());
            Console.WriteLine("Preference overlap ordering verified: "+providerName+"; actual avatar read paused, independent profile commit persisted before preference save resumes.");
            barrier.Release();var result=await saving;
            Assert.Equal(newer,await nativeFirstName());
            var account=await owner.LoadAvatarAsync(id);Assert.False(account.IsError,account.Message);
            Assert.Equal(newer,account.Result.FirstName);
            Assert.Equal(before.LastName,account.Result.LastName);Assert.Equal(before.Username,account.Result.Username);
            Assert.Equal(before.Email,account.Result.Email);Assert.Equal(storedPassword,account.Result.Password);
            var detail=await owner.LoadAvatarDetailAsync(id);Assert.False(detail.IsError,detail.Message);Assert.Equal(42,detail.Result.XP);
            if(result.IsError)
            {
                Assert.False(result.Result);Assert.False(result.IsSaved);
                Assert.False(string.IsNullOrWhiteSpace(result.ErrorCode));
                Assert.Contains("CONFLICT",result.ErrorCode,StringComparison.Ordinal);
                Assert.Equal(originalPrivacy,StoredPrivacy(account.Result));
                var fresh=await settings.UpdatePrivacySettingsAsync(id,new Dictionary<string,object>{{"profileVisibility","private"}});
                Assert.False(fresh.IsError,fresh.Message);Assert.True(fresh.Result);Assert.True(fresh.IsSaved);
                Assert.Equal(newer,await nativeFirstName());
                var persisted=await owner.LoadAvatarAsync(id);Assert.False(persisted.IsError,persisted.Message);
                Assert.Equal(storedPassword,persisted.Result.Password);
                Assert.Equal("private",StoredPrivacy(persisted.Result));
                if(nativePrivacy!=null)Assert.Equal("private",await nativePrivacy());
                var privacy=await settings.GetPrivacySettingsAsync(id);Assert.False(privacy.IsError,privacy.Message);
                Assert.Equal("private",privacy.Result["profileVisibility"]?.ToString());
                Console.WriteLine("Preference explicit fresh action after conflict verified: "+providerName);
            }
            else
            {
                Assert.True(result.Result);Assert.True(result.IsSaved);
                Assert.Equal("private",StoredPrivacy(account.Result));
            }
            Console.WriteLine("Preference native overlap preserved newer account: "+providerName+"; protected profile/password/XP retained, native preference save or typed conflict acknowledged.");
        }
        finally
        {
            barrier.Release();
            try { if(saving!=null)await saving; }
            finally
            {
                try
                {
                    if(reader!=null)
                    {
                        try { await reader.DeActivateProviderAsync(); }
                        finally { if(reader is IDisposable disposable)disposable.Dispose(); }
                    }
                }
                finally
                {
                    try { if(asyncOwner!=null)await asyncOwner.DisposeAsync(); }
                    finally { syncOwner?.Dispose(); }
                    Console.WriteLine("Owned preference overlap clients/data cleanup verified: "+providerName);
                }
            }
        }
    }
}

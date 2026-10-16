using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;
public sealed partial class RealProviderRoutingTests
{
    // Test adapter performs actual disk I/O; it is NOT a live Vercel/cloud SDK certification.
    private sealed class OwnedDiskKeys:IKeyValueBackend
    {
        private readonly string _directory;
        public string FailurePrefix;public int Attempts;public readonly List<string> Acknowledged=new();
        public string DeleteFailureKey;public readonly List<string> DeletedKeys=new();
        public readonly IOException Cause=new("Owned failed later key write after earlier disk acknowledgement.");
        public OwnedDiskKeys(string directory) {_directory=directory;Directory.CreateDirectory(directory);}
        private string PathFor(string key)=>Path.Combine(_directory,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)))+".json");
        public async Task<string> GetAsync(string key,CancellationToken token=default)
        {
            var path=PathFor(key);if(!File.Exists(path)) return null;
            var row=System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(await File.ReadAllTextAsync(path,token));
            if(row["key"]!=key) throw new InvalidDataException("Owned disk key mismatch.");return row["value"];
        }
        public async Task PutAsync(string key,string value,CancellationToken token=default)
        {
            Attempts++;if(FailurePrefix!=null && key.StartsWith(FailurePrefix,StringComparison.Ordinal)) throw Cause;
            await File.WriteAllTextAsync(PathFor(key),System.Text.Json.JsonSerializer.Serialize(new Dictionary<string,string> {{"key",key},{"value",value}}),token);Acknowledged.Add(key);
        }
        public Task DeleteAsync(string key,CancellationToken token=default) {
            if(key==DeleteFailureKey)throw Cause;
            File.Delete(PathFor(key));DeletedKeys.Add(key);return Task.CompletedTask;}
        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken token=default)
        {
            var keys=new List<string>();foreach(var path in Directory.GetFiles(_directory,"*.json")) {
                var row=System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(await File.ReadAllTextAsync(path,token));
                if(row["key"].StartsWith(prefix,StringComparison.Ordinal)) keys.Add(row["key"]);
            }return keys;
        }
        public Task VerifyAsync(CancellationToken token=default)
        {
            token.ThrowIfCancellationRequested();
            if(!Directory.Exists(_directory)) throw new DirectoryNotFoundException(_directory);
            _=Directory.GetFiles(_directory,"*.json");return Task.CompletedTask;
        }
    }
    private sealed class OwnedDiskProvider:KeyValueStorageProviderBase
    {
        public OwnedDiskProvider(IKeyValueBackend backend):base(backend)
        {ProviderName="Owned disk key-value test adapter";ProviderType=new EnumValue<ProviderType>(Core.Enums.ProviderType.VercelKVOASIS);ProviderCategory=new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocal);}
    }

    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task MissingPrimaryDeleteNeverDeletesStaleSecondary(bool synchronous,bool avatar)
    {
        Directory.CreateDirectory(_root);var primary=new OwnedDiskProvider(new OwnedDiskKeys(FilesPath));
        using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await primary.ActivateProviderAsync()).IsError.Should().BeFalse();(await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        try
        {
            var id=Guid.NewGuid();
            if(avatar)(await sqlite.SaveAvatarAsync(new Avatar {Id=id,Username="owned-stale-delete",Email="stale@example.invalid",AvatarType=new EnumValue<AvatarType>(AvatarType.User),IsActive=true})).IsError.Should().BeFalse();
            else (await sqlite.SaveHolonAsync(new Holon {Id=id,Name="owned-stale-delete"},saveChildren:false)).IsError.Should().BeFalse();
            var manager=CreateManager(autoFailover:true,autoReplication:true);manager.RegisterProvider(primary);manager.RegisterProvider(sqlite);
            var types=new[] {new EnumValue<ProviderType>(ProviderType.VercelKVOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
            manager.SetAndReplaceAutoFailOverListForProviders(types).IsError.Should().BeFalse();manager.SetAndReplaceAutoReplicationListForProviders(types).IsError.Should().BeFalse();
            var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests??0;
            var router=new OASISHyperDrive(manager);var request=new StorageOperationRequest {Operation=avatar ? "DeleteAvatar":"DeleteHolon",AvatarId=id,HolonId=id,SoftDelete=false,PreferredProvider=ProviderType.VercelKVOASIS};
            void Missing<T>(Common.OASISResult<T> result)
            {result.IsDeleted.Should().BeFalse();result.ErrorCode.Should().Be(Common.OASISResultErrorCodes.RecordNotFound);}
            _preserveFixture=true;
            if(avatar)Missing(synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request));
            else Missing(synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request));
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests??0).Should().Be(before);
            if(avatar)(await sqlite.LoadAvatarAsync(id)).Result.Id.Should().Be(id);else(await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Id.Should().Be(id);
            _preserveFixture=false;
        }
        finally{await primary.DeActivateProviderAsync();}
    }

    [Theory]
    [InlineData(false,0)] [InlineData(true,0)] [InlineData(false,1)] [InlineData(true,1)] [InlineData(false,2)] [InlineData(true,2)]
    public async Task AcknowledgedPartialDeletesNeverFailOverOrReplicate(bool synchronous,int kind)
    {
        Directory.CreateDirectory(_root);var backend=new OwnedDiskKeys(FilesPath);var primary=new OwnedDiskProvider(backend) {IsVersionControlEnabled=true};
        using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await primary.ActivateProviderAsync()).IsError.Should().BeFalse();(await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id=Guid.NewGuid();
        try
        {
            foreach(var store in new IOASISStorageProvider[] {primary,sqlite})
            {
                var profile=await store.SaveAvatarAsync(new Avatar {Id=id,Username="owned-delete",Email="owned-delete@example.invalid",IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User)});
                profile.IsError.Should().BeFalse(profile.Message);
                (await store.SaveAvatarDetailAsync(new AvatarDetail {Id=id,IsActive=true,XP=10})).IsError.Should().BeFalse();
                (await store.SaveHolonAsync(new Holon {Id=id,ParentHolonId=Guid.NewGuid(),Name="owned-delete"},saveChildren:false)).IsError.Should().BeFalse();
            }
            // Ensure detail hard deletion has an acknowledged historical step before its root failure.
            (await primary.SaveAvatarDetailAsync(new AvatarDetail {Id=id,IsActive=true,XP=20})).IsError.Should().BeFalse();
            backend.DeleteFailureKey="oasis/"+(kind==0 ? "avatar/":kind==1 ? "avatardetail/":"holon/")+id.ToString("N");
            backend.DeletedKeys.Clear();
            var manager=CreateManager(autoFailover:true,autoReplication:true);manager.RegisterProvider(primary);manager.RegisterProvider(sqlite);
            var types=new[] {new EnumValue<ProviderType>(ProviderType.VercelKVOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
            manager.SetAndReplaceAutoFailOverListForProviders(types).IsError.Should().BeFalse();manager.SetAndReplaceAutoReplicationListForProviders(types).IsError.Should().BeFalse();
            var router=new OASISHyperDrive(manager);var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests??0;
            var request=new StorageOperationRequest {Operation=kind==0 ? "DeleteAvatar":kind==1 ? "DeleteAvatarDetail":"DeleteHolon",AvatarId=id,HolonId=id,SoftDelete=false,PreferredProvider=ProviderType.VercelKVOASIS};
            _preserveFixture=true;
            KeyValuePartialMutationException Pending<T>(Common.OASISResult<T> result)
            {result.IsError.Should().BeTrue();result.IsDeleted.Should().BeFalse();return result.Exception.Should().BeOfType<KeyValuePartialMutationException>().Which;}
            var marker=kind==2 ? Pending(synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request))
                :Pending(synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request));
            marker.InnerException.Should().BeSameAs(backend.Cause);marker.AcknowledgedKeys.Should().BeEquivalentTo(backend.DeletedKeys);backend.DeletedKeys.Should().NotBeEmpty();
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests??0).Should().Be(before);
            (await backend.GetAsync(backend.DeleteFailureKey)).Should().NotBeNullOrEmpty();
            foreach(var key in backend.DeletedKeys)(await backend.GetAsync(key)).Should().BeNull();
            (await sqlite.LoadAvatarAsync(id)).Result.Id.Should().Be(id);(await sqlite.LoadAvatarDetailAsync(id)).Result.XP.Should().Be(10);
            (await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Id.Should().Be(id);
            _preserveFixture=false;
        }
        finally {await primary.DeActivateProviderAsync();}
    }

    [Theory]
    [InlineData(false,0)] [InlineData(true,0)] [InlineData(false,1)] [InlineData(true,1)] [InlineData(false,2)] [InlineData(true,2)]
    public async Task AcknowledgedPartialKeysNeverFailOverOrReplicate(bool synchronous,int kind)
    {
        Directory.CreateDirectory(_root);var backend=new OwnedDiskKeys(FilesPath);var primary=new OwnedDiskProvider(backend) {IsVersionControlEnabled=true};
        using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await primary.ActivateProviderAsync()).IsError.Should().BeFalse();(await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        try
        {
            var manager=CreateManager(autoFailover:true,autoReplication:true);manager.RegisterProvider(primary);manager.RegisterProvider(sqlite);
            var types=new[] {new EnumValue<ProviderType>(ProviderType.VercelKVOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
            manager.SetAndReplaceAutoFailOverListForProviders(types).IsError.Should().BeFalse();manager.SetAndReplaceAutoReplicationListForProviders(types).IsError.Should().BeFalse();
            var router=new OASISHyperDrive(manager);var profile=new Avatar {Username="owned-partial"};
            var detail=new AvatarDetail {Id=Guid.NewGuid(),XP=10};var holon=new Holon {ParentHolonId=Guid.NewGuid()};
            if(kind==1) (await primary.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
            backend.Attempts=0;backend.Acknowledged.Clear();detail.XP=20;
            backend.FailurePrefix=kind==0 ? "oasis/avatar-username/":kind==1 ? "oasis/avatardetail/":"oasis/holon-parent/";
            var beforePayload=kind==0 ? OasisJson.Serialize(profile):kind==1 ? OasisJson.Serialize(detail):OasisJson.Serialize(holon);
            var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            var request=new StorageOperationRequest {Operation=kind==0 ? "SaveAvatar":kind==1 ? "SaveAvatarDetail":"SaveHolon",Payload=kind==0 ? profile:kind==1 ? detail:holon,PreferredProvider=ProviderType.VercelKVOASIS,SaveChildren=false};
            _preserveFixture=true;
            KeyValuePartialMutationException Pending<T>(Common.OASISResult<T> r) {r.IsError.Should().BeTrue();r.IsSaved.Should().BeFalse();r.Result.Should().BeNull();return r.Exception.Should().BeOfType<KeyValuePartialMutationException>().Which;}
            var marker=kind==0 ? Pending(synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request))
                :kind==1 ? Pending(synchronous ? router.RouteRequest<IAvatarDetail>(request):await router.RouteRequestAsync<IAvatarDetail>(request))
                :Pending(synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request));
            marker.InnerException.Should().BeSameAs(backend.Cause);backend.Attempts.Should().Be(2);backend.Acknowledged.Should().HaveCount(1);marker.AcknowledgedKeys.Should().Equal(backend.Acknowledged);
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
            (kind==0 ? OasisJson.Serialize(profile):kind==1 ? OasisJson.Serialize(detail):OasisJson.Serialize(holon)).Should().Be(beforePayload);
            var committed=await backend.GetAsync(marker.AcknowledgedKeys.Single());committed.Should().NotBeNullOrEmpty();
            if(kind==0) {var stored=OasisJson.Deserialize<IAvatar>(committed);stored.Id.Should().NotBeEmpty();stored.Username.Should().Be(profile.Username);
                marker.AcknowledgedKeys.Single().Should().Be("oasis/avatar/"+stored.Id.ToString("N"));}
            else if(kind==1) {OasisJson.Deserialize<IAvatarDetail>(committed).XP.Should().Be(10);
                OasisJson.Deserialize<IAvatarDetail>(await backend.GetAsync("oasis/avatardetail/"+detail.Id.ToString("N"))).XP.Should().Be(10);}
            else {var stored=OasisJson.Deserialize<IHolon>(committed);stored.Id.Should().NotBeEmpty();stored.ParentHolonId.Should().Be(holon.ParentHolonId);
                marker.AcknowledgedKeys.Single().Should().Be("oasis/holon/"+stored.Id.ToString("N"));}
            (await sqlite.LoadAllAvatarsAsync()).Result.Should().BeEmpty();(await sqlite.LoadAllAvatarDetailsAsync()).Result.Should().BeEmpty();(await sqlite.LoadAllHolonsAsync(loadChildren:false)).Result.Should().BeEmpty();
            _preserveFixture=false;
        }
        finally {await primary.DeActivateProviderAsync();}
    }
}

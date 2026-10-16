using System.Diagnostics;
using System.Reflection;
using Ipfs;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using SqliteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
[Trait("Category","NativeIPFS")]
public sealed class IPFSSqlitePolicyTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(true,false)]
    [InlineData(false,true)] [InlineData(true,true)]
    public async Task RealPriorityStoresReplicateBalanceFailOverAndRejectCorruption(bool synchronous,bool ipfsPrimary)
    {
        const string endpoint="http://127.0.0.1:18547";
        if(Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC")!=endpoint) throw new InvalidOperationException("Existing owned IPFS runner required.");
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null && !File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/Run-ReusableIpfsTests.ps1"))) directory=directory.Parent;
        if(directory==null) throw new InvalidOperationException("Existing checkout required.");
        var work=Path.Combine(directory.FullName,"TestResults","IPFSCurrent");
        var path=Path.Combine(work,"owned-dna.json");var dbPath=Path.Combine(work,"v2-routing.db");
        if(Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH")!)!=path || File.Exists(path) || File.Exists(dbPath))
            throw new InvalidOperationException("Existing/unexpected fixed fixture requires inspection; no reset.");
        var resolve=typeof(OASISDNAManager).GetMethod("ResolveEffectiveOASISDNAPath",BindingFlags.Static|BindingFlags.NonPublic)!;
        Assert.Equal(path,(string)resolve.Invoke(null,new object[]{path})!);
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,SubscriptionConfig=new SubscriptionConfig {PlanType="Gold"},
            StorageProviders=new StorageProviderSettings {IPFSOASIS=new IPFSOASISSettings {ConnectionString=endpoint}}}};
        var ipfs=new IPFSOASIS(dna,path);
        using var sqlite=new SqliteProvider($"Data Source={dbPath};Pooling=False");
        var cids=new HashSet<string>(StringComparer.Ordinal);var verified=false;
        var types=new[]{new EnumValue<ProviderType>(ProviderType.IPFSOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        void Root(){if(!string.IsNullOrEmpty(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress)) cids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);}
        void Record(IHolonBase record){cids.Add(record.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);Root();}
        ProviderManager Manager(IOASISStorageProvider sql)
        {
            var manager=new ProviderManager(null,dna);manager.RegisterProvider(ipfs);manager.RegisterProvider(sql);return manager;
        }
        async Task<OASISResult<T>> Route<T>(OASISHyperDrive router,StorageOperationRequest request,LoadBalancingStrategy strategy=LoadBalancingStrategy.Auto)
            => synchronous ? router.RouteRequest<T>(request,strategy):await router.RouteRequestAsync<T>(request,strategy);
        try
        {
            Assert.False((await ipfs.ActivateProviderAsync()).IsError);Assert.False((await sqlite.ActivateProviderAsync()).IsError);
            var marker=Guid.NewGuid();
            var seeded=await ipfs.SaveHolonAsync(new Holon {Id=marker,Name="native-ipfs-marker"},saveChildren:false);
            Assert.False(seeded.IsError,seeded.Message);Record(seeded.Result);
            Assert.False((await sqlite.SaveHolonAsync(new Holon {Id=marker,Name="native-sqlite-marker"},saveChildren:false)).IsError);
            var manager=Manager(sqlite);manager.IsAutoReplicationEnabled=true;
            Assert.False(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            var router=new OASISHyperDrive(manager);var primary=ipfsPrimary ? ProviderType.IPFSOASIS:ProviderType.SQLLiteDBOASIS;
            var id=Guid.NewGuid();
            var avatar=new Avatar {Id=id,Username="owned-policy-avatar",Email="policy@example.invalid",FirstName="complete replicated profile",
                AvatarType=new EnumValue<AvatarType>(AvatarType.User),IsActive=true};
            var savedAvatar=await Route<IAvatar>(router,new StorageOperationRequest {Operation="SaveAvatar",Payload=avatar,PreferredProvider=primary});
            Assert.False(savedAvatar.IsError,savedAvatar.Message);Assert.False(savedAvatar.IsWarning,savedAvatar.Message);
            var nativeAvatar=await ipfs.LoadAvatarAsync(id);Assert.False(nativeAvatar.IsError,nativeAvatar.Message);Record(nativeAvatar.Result);
            Assert.Equal(avatar.FirstName,(await sqlite.LoadAvatarAsync(id)).Result.FirstName);
            Assert.Equal(avatar.Email,nativeAvatar.Result.Email);
            var detail=new AvatarDetail {Id=id,Username=avatar.Username,XP=321,Karma=29,IsActive=true};
            detail.Inventory.Add(new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem {Id=Guid.NewGuid(),Name="owned policy seed",Quantity=4});
            var savedDetail=await Route<IAvatarDetail>(router,new StorageOperationRequest {Operation="SaveAvatarDetail",Payload=detail,PreferredProvider=primary});
            Assert.False(savedDetail.IsError,savedDetail.Message);Assert.False(savedDetail.IsWarning,savedDetail.Message);
            var nativeDetail=await ipfs.LoadAvatarDetailAsync(id);Assert.False(nativeDetail.IsError,nativeDetail.Message);Record(nativeDetail.Result);
            Assert.Equal(321,nativeDetail.Result.XP);Assert.Equal(29,nativeDetail.Result.Karma);
            Assert.Equal(detail.Inventory[0].Id,Assert.Single(nativeDetail.Result.Inventory).Id);
            Assert.Equal(detail.Inventory[0].Id,Assert.Single((await sqlite.LoadAvatarDetailAsync(id)).Result.Inventory).Id);
            var savedHolon=await Route<IHolon>(router,new StorageOperationRequest {Operation="SaveHolon",SaveChildren=false,
                Payload=new Holon {Id=id,Name="complete replicated holon"},PreferredProvider=primary});
            Assert.False(savedHolon.IsError,savedHolon.Message);Assert.False(savedHolon.IsWarning,savedHolon.Message);
            var nativeHolon=await ipfs.LoadHolonAsync(id,loadChildren:false);Assert.False(nativeHolon.IsError,nativeHolon.Message);Record(nativeHolon.Result);
            Assert.Equal(nativeHolon.Result.Name,(await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Name);

            var balancedManager=Manager(sqlite);balancedManager.IsAutoLoadBalanceEnabled=true;balancedManager.IsAutoReplicationEnabled=true;
            Assert.False(balancedManager.SetAndReplaceAutoLoadBalanceListForProviders(types).IsError);
            Assert.False(balancedManager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            var clock=Stopwatch.StartNew();Assert.False((await ipfs.LoadHolonAsync(marker,loadChildren:false)).IsError);clock.Stop();var ipfsMs=clock.Elapsed.TotalMilliseconds;
            clock.Restart();Assert.False((await sqlite.LoadHolonAsync(marker,loadChildren:false)).IsError);clock.Stop();var sqliteMs=clock.Elapsed.TotalMilliseconds;
            balancedManager.PerformanceMonitor.RecordRequest(ProviderType.IPFSOASIS,true,ipfsMs);
            balancedManager.PerformanceMonitor.RecordRequest(ProviderType.SQLLiteDBOASIS,true,sqliteMs);
            var rootBefore=dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress;
            var selected=await Route<IHolon>(new OASISHyperDrive(balancedManager),new StorageOperationRequest {Operation="LoadHolon",HolonId=marker,LoadChildren=false},LoadBalancingStrategy.Performance);
            Assert.False(selected.IsError,selected.Message);
            var fastest=ipfsMs<=sqliteMs ? ProviderType.IPFSOASIS:ProviderType.SQLLiteDBOASIS;
            Assert.Equal(fastest,balancedManager.LastProviderSelectionDiagnostic.SelectedProvider);
            Assert.Equal(fastest==ProviderType.IPFSOASIS ? "native-ipfs-marker":"native-sqlite-marker",selected.Result.Name);
            Assert.Equal(rootBefore,dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);

            var failingManager=Manager(sqlite);failingManager.IsAutoFailOverEnabled=true;failingManager.IsAutoReplicationEnabled=true;
            Assert.False(failingManager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            var healthyClient=ipfs.IPFSClient;
            try
            {
                ipfs.IPFSClient=new Ipfs.Http.IpfsClient("http://127.0.0.1:18549");
                var fallback=await Route<IHolon>(new OASISHyperDrive(failingManager),new StorageOperationRequest {
                    Operation="LoadHolon",HolonId=marker,LoadChildren=false,PreferredProvider=ProviderType.IPFSOASIS});
                Assert.False(fallback.IsError,fallback.Message);Assert.Equal("native-sqlite-marker",fallback.Result.Name);
                Assert.Equal(ProviderType.SQLLiteDBOASIS,failingManager.LastFailoverDiagnostic.SelectedProvider);
            }
            finally {ipfs.IPFSClient=healthyClient;}
            var missingPath=Path.Combine(work,"unavailable-routing.db");Assert.False(File.Exists(missingPath));
            using(var unavailable=new SqliteProvider($"Data Source={missingPath};Mode=ReadOnly;Pooling=False"))
            {
                var reverse=Manager(unavailable);reverse.IsAutoFailOverEnabled=true;
                Assert.False(reverse.SetAndReplaceAutoFailOverListForProviders(types.Reverse()).IsError);
                var fallback=await Route<IHolon>(new OASISHyperDrive(reverse),new StorageOperationRequest {
                    Operation="LoadHolon",HolonId=marker,LoadChildren=false,PreferredProvider=ProviderType.SQLLiteDBOASIS});
                Assert.False(fallback.IsError,fallback.Message);Assert.Equal("native-ipfs-marker",fallback.Result.Name);
                Assert.Equal(ProviderType.IPFSOASIS,reverse.LastFailoverDiagnostic.SelectedProvider);
            }
            Assert.False(File.Exists(missingPath));

            var corrupt=await ipfs.IPFSClient.FileSystem.AddTextAsync("owned invalid encrypted index");cids.Add(corrupt.Id.ToString());
            var corruptionManager=Manager(sqlite);corruptionManager.IsAutoFailOverEnabled=true;
            Assert.False(corruptionManager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            var beforeSecondary=corruptionManager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            try
            {
                dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress=corrupt.Id.ToString();
                var rejected=await Route<IHolon>(new OASISHyperDrive(corruptionManager),new StorageOperationRequest {
                    Operation="LoadHolon",HolonId=marker,LoadChildren=false,PreferredProvider=ProviderType.IPFSOASIS});
                Assert.True(rejected.IsError);Assert.Null(rejected.Result);Assert.IsType<InvalidDataException>(rejected.Exception);
                Assert.Equal(beforeSecondary,corruptionManager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            }
            finally {dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress=rootBefore;}
            Assert.Equal("native-ipfs-marker",(await ipfs.LoadHolonAsync(marker,loadChildren:false)).Result.Name);
            Assert.Equal("native-sqlite-marker",(await sqlite.LoadHolonAsync(marker,loadChildren:false)).Result.Name);
            verified=true;
        }
        finally
        {
            await sqlite.DeActivateProviderAsync();sqlite.Dispose();await ipfs.DeActivateProviderAsync();
            OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;
            if(verified)
            {
                var client=new Ipfs.Http.IpfsClient(endpoint);
                foreach(var cid in cids) Assert.Contains((await client.Pin.RemoveAsync((Cid)cid)).Select(value=>value.ToString()),value=>value==cid);
                File.Delete(path);File.Delete(dbPath);
            }
        }
    }
}

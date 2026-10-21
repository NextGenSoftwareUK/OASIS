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
        var path=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH")!);
        var work=Path.GetDirectoryName(path)!;var dbPath=Path.Combine(work,"v2-routing.db");
        if(Path.GetFileName(path)!="owned-dna.json" || !Directory.Exists(work) || File.Exists(path) || File.Exists(dbPath))
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
            var credential="owned-policy-"+id;var walletId=Guid.NewGuid();
            var credentialOperations=new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"};
            StorageOperationRequest CredentialRequest(string operation)=>new() {Operation=operation,VerificationToken=credential,ResetToken=credential,
                RefreshToken=credential,PublicKey=credential,PrivateKey=credential};
            var avatar=new Avatar {Id=id,Username="owned-policy-avatar",Email="policy@example.invalid",FirstName="complete replicated profile",
                AvatarType=new EnumValue<AvatarType>(AvatarType.User),IsActive=true,VerificationToken=credential,ResetToken=credential,
                RefreshTokens=new() {new RefreshToken {Token=credential}},ProviderWallets=new() {{ProviderType.SolanaOASIS,
                    new() {new ProviderWallet {Id=walletId,PublicKey=credential,PrivateKey=credential}}}}};
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
            foreach(var store in new IOASISStorageProvider[] {ipfs,sqlite})
            {
                var selectors=new[] {
                    synchronous ? store.LoadAvatarByVerificationToken(credential):await store.LoadAvatarByVerificationTokenAsync(credential),
                    synchronous ? store.LoadAvatarByResetToken(credential):await store.LoadAvatarByResetTokenAsync(credential),
                    synchronous ? store.LoadAvatarByRefreshToken(credential):await store.LoadAvatarByRefreshTokenAsync(credential),
                    synchronous ? store.LoadAvatarByPublicKey(credential):await store.LoadAvatarByPublicKeyAsync(credential),
                    synchronous ? store.LoadAvatarByPrivateKey(credential):await store.LoadAvatarByPrivateKeyAsync(credential)};
                foreach(var read in selectors) {Assert.False(read.IsError,read.Message);Assert.Equal(id,read.Result.Id);
                    Assert.Equal(walletId,Assert.Single(read.Result.ProviderWallets[ProviderType.SolanaOASIS]).Id);}
            }
            var credentialManager=Manager(sqlite);credentialManager.IsAutoLoadBalanceEnabled=true;
            Assert.False(credentialManager.SetAndReplaceAutoLoadBalanceListForProviders(types).IsError);
            var credentialRouter=new OASISHyperDrive(credentialManager);
            foreach(var operation in credentialOperations)
            {
                var before=types.Select(type=>credentialManager.PerformanceMonitor.GetMetrics(type.Value)?.TotalRequests??0).ToArray();
                for(var index=0;index<2;index++)
                {
                    var read=await Route<IAvatar>(credentialRouter,CredentialRequest(operation),LoadBalancingStrategy.RoundRobin);
                    Assert.False(read.IsError,read.Message);Assert.Equal(id,read.Result.Id);
                }
                for(var index=0;index<2;index++) Assert.Equal(before[index]+1,credentialManager.PerformanceMonitor.GetMetrics(types[index].Value).TotalRequests);
            }
            var savedHolon=await Route<IHolon>(router,new StorageOperationRequest {Operation="SaveHolon",SaveChildren=false,
                Payload=new Holon {Id=id,Name="complete replicated holon"},PreferredProvider=primary});
            Assert.False(savedHolon.IsError,savedHolon.Message);Assert.False(savedHolon.IsWarning,savedHolon.Message);
            var nativeHolon=await ipfs.LoadHolonAsync(id,loadChildren:false);Assert.False(nativeHolon.IsError,nativeHolon.Message);Record(nativeHolon.Result);
            Assert.Equal(nativeHolon.Result.Name,(await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Name);

            var importRoot=new Holon {Name="owned routed import root",IsActive=true};
            var importChild=new Holon {Name="owned routed import child",IsActive=true};importRoot.Children.Add(importChild);
            var imported=await Route<bool>(router,new StorageOperationRequest {Operation="Import",Payload=new IHolon[]{importRoot},PreferredProvider=primary});
            Assert.False(imported.IsError,imported.Message);Assert.False(imported.IsWarning,imported.Message);Assert.True(imported.Result);
            Assert.NotEqual(Guid.Empty,importRoot.Id);Assert.NotEqual(Guid.Empty,importChild.Id);Assert.Equal(importRoot.Id,importChild.ParentHolonId);
            foreach(var item in new[]{importRoot,importChild})
            {
                var ipfsRow=await ipfs.LoadHolonAsync(item.Id,loadChildren:false);Assert.False(ipfsRow.IsError,ipfsRow.Message);Record(ipfsRow.Result);
                var sqliteRow=await sqlite.LoadHolonAsync(item.Id,loadChildren:false);Assert.False(sqliteRow.IsError,sqliteRow.Message);
                Assert.Equal(item.Name,ipfsRow.Result.Name);Assert.Equal(item.Name,sqliteRow.Result.Name);
                Assert.Equal(item.ParentHolonId,ipfsRow.Result.ParentHolonId);Assert.Equal(item.ParentHolonId,sqliteRow.Result.ParentHolonId);
            }
            Assert.Equal(importChild.Id,Assert.Single((await ipfs.LoadHolonAsync(importRoot.Id,loadChildrenFromProvider:true)).Result.Children).Id);
            Assert.Equal(importChild.Id,Assert.Single((await sqlite.LoadHolonAsync(importRoot.Id,loadChildrenFromProvider:true)).Result.Children).Id);

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
                typeof(IPFSOASIS).GetField("_ipfsClient",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(ipfs,new Ipfs.Http.IpfsClient("http://127.0.0.1:18549"));
                var fallback=await Route<IHolon>(new OASISHyperDrive(failingManager),new StorageOperationRequest {
                    Operation="LoadHolon",HolonId=marker,LoadChildren=false,PreferredProvider=ProviderType.IPFSOASIS});
                Assert.False(fallback.IsError,fallback.Message);Assert.Equal("native-sqlite-marker",fallback.Result.Name);
                Assert.Equal(ProviderType.SQLLiteDBOASIS,failingManager.LastFailoverDiagnostic.SelectedProvider);
                foreach(var operation in credentialOperations)
                {
                    // Fresh contexts require each selector to encounter actual SDK failure, not an already-open circuit.
                    var credentialFailover=Manager(sqlite);credentialFailover.IsAutoFailOverEnabled=true;
                    Assert.False(credentialFailover.SetAndReplaceAutoFailOverListForProviders(types).IsError);
                    var request=CredentialRequest(operation);request.PreferredProvider=ProviderType.IPFSOASIS;
                    var read=await Route<IAvatar>(new OASISHyperDrive(credentialFailover),request);
                    Assert.False(read.IsError,read.Message);Assert.Equal(id,read.Result.Id);
                    Assert.Equal(1L,credentialFailover.PerformanceMonitor.GetMetrics(ProviderType.IPFSOASIS).TotalRequests);
                    Assert.Equal(1L,credentialFailover.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS).TotalRequests);
                }
            }
            finally {typeof(IPFSOASIS).GetField("_ipfsClient",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(ipfs,healthyClient);}
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

            var protect=typeof(IPFSOASIS).GetMethod("ProtectIndex",BindingFlags.Instance|BindingFlags.NonPublic)!;
            foreach(var invalidPayload in new[]{"owned invalid encrypted index",(string)protect.Invoke(ipfs,new object[]{"{invalid stored json"})!})
            {
            var corrupt=await ipfs.IPFSClient.FileSystem.AddTextAsync(invalidPayload);cids.Add(corrupt.Id.ToString());
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
            }
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

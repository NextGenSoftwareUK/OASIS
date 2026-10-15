using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Ipfs;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

public class IPFSNativeStorageTests
{
    [Fact]
    public async Task RealOfflineDaemonRetainsEncryptedSameGuidRecordsAndIndex()
    {
        const string endpoint="http://127.0.0.1:18547";
        if(Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC")!=endpoint) throw new InvalidOperationException("Owned reusable IPFS runner required.");
        var path=Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH") ?? throw new InvalidOperationException("Owned fixture path required.");
        if(Path.GetFileName(path)!="owned-dna.json" || File.Exists(path)) throw new InvalidOperationException("Existing or unexpected fixture requires inspection.");
        var resolve=typeof(OASISDNAManager).GetMethod("ResolveEffectiveOASISDNAPath",BindingFlags.Static|BindingFlags.NonPublic)!;
        Assert.Equal(Path.GetFullPath(path),(string)resolve.Invoke(null,new object[] {path})!);
        var priorDna=OASISDNAManager.OASISDNA; var priorPath=OASISDNAManager.OASISDNAPath;
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {StorageProviders=new StorageProviderSettings {
            IPFSOASIS=new IPFSOASISSettings {ConnectionString=endpoint}}}};
        var provider=new IPFSOASIS(dna,path);
        var ownedCids=new HashSet<string>(StringComparer.Ordinal);
        var verified=false;
        try
        {
            var activated=await provider.ActivateProviderAsync(); Assert.False(activated.IsError,activated.Message);
            var id=Guid.NewGuid();
            var avatar=new Avatar {Id=id,Username="owned-native-avatar",Email="owned@example.invalid",Password="synthetic-not-a-user-secret"};
            var detail=new AvatarDetail {Id=id,Username=avatar.Username,Email=avatar.Email,XP=321,Karma=29};
            var inventoryId=Guid.NewGuid();
            detail.Inventory.Add(new InventoryItem {Id=inventoryId,Name="owned oak seed",Quantity=5,GameSource="Our World"});
            var holon=new Holon {Id=id,Name="owned-native-holon",HolonType=HolonType.Holon};
            avatar.ProviderUniqueStorageKey=null!; avatar.PreviousVersionProviderUniqueStorageKey=null!;
            detail.ProviderUniqueStorageKey=null!; detail.PreviousVersionProviderUniqueStorageKey=null!;
            holon.ProviderUniqueStorageKey=null!; holon.PreviousVersionProviderUniqueStorageKey=null!;
            await provider.SaveAvatarToFile(avatar);
            ownedCids.Add(avatar.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            await provider.SaveAvatarDetailToFile(detail);
            ownedCids.Add(detail.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            await provider.SaveHolonToFile(holon);
            ownedCids.Add(holon.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            var profile=await provider.LoadAvatarTemplateAsync(row=>row.Id==id);
            var stats=await provider.LoadAvatarDetailTemplateAsync(row=>row.Id==id);
            var stored=await provider.LoadHolonTemplateAsync(row=>row.Id==id);
            Assert.Equal(avatar.Email,profile.Result.Email); Assert.Equal(avatar.Password,profile.Result.Password);
            Assert.Equal(detail.XP,stats.Result.XP); Assert.Equal(detail.Karma,stats.Result.Karma); Assert.Equal(holon.Name,stored.Result.Name);
            var item=Assert.IsType<InventoryItem>(Assert.Single(stats.Result.Inventory));
            Assert.Equal(inventoryId,item.Id); Assert.Equal(5,item.Quantity); Assert.Equal("Our World",item.GameSource);
            Assert.Single((await provider.LoadAllAvatarsAsync()).Result); Assert.Single((await provider.LoadAllAvatarDetailsAsync()).Result);
            Assert.Single((await provider.LoadAllHolonsAsync(HolonType.All)).Result);
            var child=new Holon {Id=Guid.NewGuid(),ParentHolonId=id,Name="owned-native-child",HolonType=HolonType.Holon};
            await provider.SaveHolonToFile(child);
            ownedCids.Add(child.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            var parentKey=holon.ProviderUniqueStorageKey[ProviderType.IPFSOASIS];
            Assert.Equal(child.Id,Assert.Single((await provider.LoadHolonsForParentAsync(parentKey,loadChildren:false)).Result).Id);
            Assert.Equal(child.Id,Assert.Single(provider.LoadHolonsForParent(parentKey,loadChildren:false).Result).Id);
            Assert.Empty((await provider.LoadHolonsForParentAsync(child.ProviderUniqueStorageKey[ProviderType.IPFSOASIS],loadChildren:false)).Result);
            Assert.Throws<NotSupportedException>(()=>provider.LoadHolonsForParent(parentKey,version:1));
            var grandchild=new Holon {Id=Guid.NewGuid(),ParentHolonId=child.Id,Name="owned-native-grandchild",HolonType=HolonType.Holon};
            await provider.SaveHolonToFile(grandchild);
            ownedCids.Add(grandchild.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            var graph=Assert.Single((await provider.LoadHolonsForParentAsync(parentKey,loadChildrenFromProvider:true)).Result);
            Assert.Equal(grandchild.Id,Assert.Single(graph.Children).Id);
            Assert.Empty(Assert.Single((await provider.LoadHolonsForParentAsync(parentKey,loadChildren:false,loadChildrenFromProvider:true)).Result).Children);
            Assert.Empty(Assert.Single((await provider.LoadHolonsForParentAsync(parentKey,maxChildDepth:1,curentChildDepth:1,loadChildrenFromProvider:true)).Result).Children);
            Assert.Throws<ArgumentOutOfRangeException>(()=>provider.LoadHolonsForParent(parentKey,maxChildDepth:-1));
            var loadedRoot=await provider.LoadHolonAsync(id,loadChildrenFromProvider:true);
            Assert.False(loadedRoot.IsError,loadedRoot.Message);
            Assert.Equal(grandchild.Id,Assert.Single(Assert.Single(loadedRoot.Result.Children).Children).Id);
            var shallow=provider.LoadHolon(parentKey,recursive:false,loadChildrenFromProvider:true);
            Assert.False(shallow.IsError,shallow.Message);
            Assert.Empty(Assert.Single(shallow.Result.Children).Children);
            Assert.Empty(provider.LoadHolon(id,loadChildren:false,loadChildrenFromProvider:true).Result.Children);
            var historical=provider.LoadHolon(parentKey,version:1);
            Assert.True(historical.IsError); Assert.IsType<NotSupportedException>(historical.Exception);
            var missingId=Guid.NewGuid();
            var missing=await provider.LoadHolonAsync(missingId,loadChildren:false);
            Assert.True(missing.IsError); Assert.Null(missing.Result);
            Assert.Null(missing.Exception); Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);
            var missingAvatar=await provider.LoadAvatarAsync(missingId);
            var missingDetail=provider.LoadAvatarDetail(missingId);
            Assert.True(missingAvatar.IsError); Assert.Null(missingAvatar.Exception);
            Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missingAvatar.ErrorCode);
            Assert.True(missingDetail.IsError); Assert.Null(missingDetail.Exception);
            Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missingDetail.ErrorCode);
            Assert.IsType<NotSupportedException>(provider.LoadAvatar(id,version:1).Exception);
            Assert.IsType<NotSupportedException>(provider.LoadAvatarDetailByUsername(detail.Username,version:1).Exception);
            Assert.IsType<NotSupportedException>(provider.LoadAvatarByProviderKey(avatar.ProviderUniqueStorageKey[ProviderType.IPFSOASIS],version:1).Exception);
            var snapshot=await provider.LoadLookupToJson();
            var snapshotReader=typeof(IPFSOASIS).GetMethod("ReadIndexSnapshotAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Holon));
            var currentRoot=dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress;
            try
            {
                // A captured index is authoritative even if the configured root changes afterward.
                dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress="not-a-cid";
                var read=(Task<List<Holon>>)snapshotReader.Invoke(provider,new object[] {snapshot,"holon",new Func<HolonResume,bool>(row=>row.ParentHolonId==child.Id)})!;
                Assert.Equal(grandchild.Id,Assert.Single(await read).Id);
            }
            finally {dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress=currentRoot;}
            var local=new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:Path.Combine(Path.GetDirectoryName(path)!,"routing-files"));
            Assert.False((await local.ActivateProviderAsync()).IsError);
            try
            {
                Assert.False((await local.SaveHolonAsync(new Holon {Id=id,Name="distinct-local-route"},saveChildren:false)).IsError);
                dna.OASIS.HyperDriveMode=HyperDriveModes.V2;
                dna.OASIS.SubscriptionConfig=new SubscriptionConfig {PlanType="Gold"};
                var manager=new ProviderManager(null,dna) {IsAutoLoadBalanceEnabled=true,IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false};
                manager.RegisterProvider(provider); manager.RegisterProvider(local);
                manager.SetAndReplaceAutoLoadBalanceListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.IPFSOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)});
                var router=new OASISHyperDrive(manager);
                var counts=new Dictionary<string,int>();
                for(var index=0;index<6;index++)
                {
                    var request=new StorageOperationRequest {Operation="LoadHolon",HolonId=id};
                    var routed=index%2==0 ? router.RouteRequest<IHolon>(request,LoadBalancingStrategy.RoundRobin)
                        : await router.RouteRequestAsync<IHolon>(request,LoadBalancingStrategy.RoundRobin);
                    Assert.False(routed.IsError,routed.Message); Assert.NotNull(routed.Result);
                    var name=routed.Result.Name; counts[name]=counts.GetValueOrDefault(name)+1;
                }
                Assert.Equal(3,counts[holon.Name]); Assert.Equal(3,counts["distinct-local-route"]);
                manager.IsAutoLoadBalanceEnabled=false; manager.IsAutoFailOverEnabled=true;
                manager.SetAndReplaceAutoFailOverListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.IPFSOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)});
                Assert.False(manager.SetAndActivateCurrentStorageProvider(provider).IsError);
                var healthyClient=provider.IPFSClient;
                try
                {
                    provider.IPFSClient=new Ipfs.Http.IpfsClient("http://127.0.0.1:18549");
                    foreach(var asynchronous in new[] {false,true})
                    {
                        var request=new StorageOperationRequest {Operation="LoadHolon",HolonId=id,PreferredProvider=ProviderType.IPFSOASIS};
                        var routed=asynchronous ? await router.RouteRequestAsync<IHolon>(request) : router.RouteRequest<IHolon>(request);
                        Assert.False(routed.IsError,routed.Message); Assert.Equal("distinct-local-route",routed.Result.Name);
                        Assert.Equal(ProviderType.LocalFileOASIS,manager.LastFailoverDiagnostic.SelectedProvider);
                    }
                }
                finally {provider.IPFSClient=healthyClient;}
                Assert.False((await local.SaveHolonAsync(new Holon {Id=missingId,Name="must-not-return-stale-local-copy"},saveChildren:false)).IsError);
                var localBefore=manager.PerformanceMonitor.GetMetrics(ProviderType.LocalFileOASIS).TotalRequests;
                foreach(var asynchronous in new[] {false,true})
                {
                    var request=new StorageOperationRequest {Operation="LoadHolon",HolonId=missingId,PreferredProvider=ProviderType.IPFSOASIS};
                    var routed=asynchronous ? await router.RouteRequestAsync<IHolon>(request) : router.RouteRequest<IHolon>(request);
                    Assert.False(routed.IsError,routed.Message); Assert.Null(routed.Result);
                    Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,routed.ErrorCode);
                }
                Assert.Equal(localBefore,manager.PerformanceMonitor.GetMetrics(ProviderType.LocalFileOASIS).TotalRequests);
                Assert.False((await local.DeleteHolonAsync(missingId)).IsError);
                manager.IsAutoFailOverEnabled=false; manager.IsAutoReplicationEnabled=true;
                Assert.False(manager.SetAndReplaceAutoReplicationListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.IPFSOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)}).IsError);
                foreach(var primary in new[] {ProviderType.IPFSOASIS,ProviderType.LocalFileOASIS})
                {
                    var changed=new Holon {Id=id,Name="replicated-from-"+primary};
                    var request=new StorageOperationRequest {Operation="SaveHolon",Payload=changed,SaveChildren=false,PreferredProvider=primary};
                    var saved=primary==ProviderType.IPFSOASIS ? await router.RouteRequestAsync<IHolon>(request) : router.RouteRequest<IHolon>(request);
                    Assert.False(saved.IsError,saved.Message); Assert.False(saved.IsWarning,saved.Message);
                    var native=await provider.LoadHolonAsync(id,loadChildren:false);
                    Assert.False(native.IsError,native.Message); Assert.Equal(changed.Name,native.Result.Name);
                    Assert.Equal(changed.Name,(await local.LoadHolonAsync(id,loadChildren:false)).Result.Name);
                    ownedCids.Add(native.Result.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
                    ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                }
                Assert.False((await local.DeleteHolonAsync(id)).IsError);
            }
            finally {await local.DeActivateProviderAsync();}
            foreach(var cid in new[] {avatar.ProviderUniqueStorageKey[ProviderType.IPFSOASIS],detail.ProviderUniqueStorageKey[ProviderType.IPFSOASIS],holon.ProviderUniqueStorageKey[ProviderType.IPFSOASIS],dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress})
            {
                var raw=await provider.IPFSClient.FileSystem.ReadAllTextAsync((Cid)cid);
                Assert.StartsWith("oasis-aes256gcm-gzip-v2:",raw); Assert.DoesNotContain(avatar.Email,raw); Assert.DoesNotContain(avatar.Password,raw); Assert.DoesNotContain(id.ToString(),raw);
            }
            await VerifyPartialPublicationAsync(provider,dna,Path.GetDirectoryName(path)!,ownedCids);
            var exactTombstone=await VerifySqliteTombstoneReplication(provider,dna,id,Path.GetDirectoryName(path)!,ownedCids);
            var deleted=await provider.ApplyAvatarTombstoneAsync(exactTombstone);
            Assert.False(deleted.IsError,deleted.Message); Assert.True(deleted.Result);
            var tombstonedAvatar=(await provider.LoadAvatarAsync(id)).Result;
            var tombstonedDetail=(await provider.LoadAvatarDetailAsync(id)).Result;
            Assert.False(tombstonedAvatar.IsActive); Assert.False(tombstonedDetail.IsActive);
            Assert.Equal(tombstonedAvatar.DeletedDate,tombstonedDetail.DeletedDate);
            Assert.Equal(DateTimeKind.Utc,tombstonedAvatar.DeletedDate.Kind);
            Assert.Equal(exactTombstone.DeletedAtUtc,tombstonedAvatar.DeletedDate);
            Assert.Equal(exactTombstone.DeletedByAvatarId,tombstonedAvatar.DeletedByAvatarId);
            Assert.Equal(exactTombstone.DeletedByAvatarId,tombstonedDetail.DeletedByAvatarId);
            Assert.Equal(321,tombstonedDetail.XP); Assert.Equal(29,tombstonedDetail.Karma);
            Assert.Equal(inventoryId,Assert.Single(tombstonedDetail.Inventory).Id);
            ownedCids.Add(tombstonedAvatar.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
            ownedCids.Add(tombstonedDetail.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
            ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            var committedRoot=dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress;
            Assert.False(provider.ApplyAvatarTombstone(exactTombstone).IsError);
            Assert.False(provider.DeleteAvatarByEmail(avatar.Email.ToUpperInvariant()).IsError);
            Assert.True((await provider.ApplyAvatarTombstoneAsync(new AvatarTombstone(id,exactTombstone.DeletedAtUtc.AddTicks(1),exactTombstone.DeletedByAvatarId))).IsError);
            Assert.True((await provider.ApplyAvatarTombstoneAsync(new AvatarTombstone(Guid.NewGuid(),DateTime.UtcNow))).IsError);
            Assert.False((await provider.DeleteAvatarByUsernameAsync(avatar.Username)).IsError);
            Assert.Equal(committedRoot,dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            Assert.True((await provider.DeleteAvatarByEmailAsync(" ")).IsError);
            Assert.Equal(committedRoot,dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            Assert.False((await provider.DeleteAvatarAsync(id,softDelete:false)).IsError);
            ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            Assert.True((await provider.LoadAvatarAsync(id)).IsError);
            Assert.True((await provider.LoadAvatarDetailAsync(id)).IsError);
            Assert.False((await provider.LoadHolonAsync(id,loadChildren:false)).IsError);
            await provider.DeActivateProviderAsync();
            var version=await new Ipfs.Http.IpfsClient(endpoint).VersionAsync(); Assert.NotEmpty(version);
            verified=true;
        }
        finally
        {
            var cleaned=false;
            try
            {
                if(verified)
                {
                    var client=new Ipfs.Http.IpfsClient(endpoint);
                    foreach(var cid in ownedCids) Assert.Contains((await client.Pin.RemoveAsync((Cid)cid)).Select(value=>value.ToString()),value=>value==cid);
                    cleaned=true;
                }
            }
            finally
            {
                await provider.DeActivateProviderAsync();
                OASISDNAManager.OASISDNA=priorDna; OASISDNAManager.OASISDNAPath=priorPath;
                if(cleaned && File.Exists(path)) File.Delete(path);
            }
        }
    }
    private static async Task VerifyPartialPublicationAsync(IPFSOASIS ipfs,OASISDNA dna,string directory,HashSet<string> ownedCids)
    {
        var id=Guid.NewGuid();
        var avatar=new Avatar {Id=id,Username="partial-native-"+id,Email=id+"@example.invalid",IsActive=true};
        var detail=new AvatarDetail {Id=id,Username=avatar.Username,IsActive=true,XP=123};
        await ipfs.SaveAvatarToFile(avatar);
        ownedCids.Add(avatar.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
        await ipfs.SaveAvatarDetailToFile(detail);
        ownedCids.Add(detail.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
        var root=dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress;
        var local=new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:Path.Combine(directory,"routing-files"));
        Assert.False((await local.ActivateProviderAsync()).IsError);
        try
        {
            Assert.False((await local.SaveAvatarAsync(new Avatar {Id=id,Username="untouched-backup",IsActive=true})).IsError);
            var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
            manager.RegisterProvider(ipfs); manager.RegisterProvider(local);
            Assert.False(manager.SetAndReplaceAutoFailOverListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.IPFSOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)}).IsError);
            var keyField=typeof(IPFSOASIS).GetField("_storageKeyResolver",BindingFlags.Instance|BindingFlags.NonPublic)!;
            var prior=keyField.GetValue(ipfs); var calls=0;
            var cause=new System.IO.IOException("owned index encryption failure after both native acknowledgements");
            keyField.SetValue(ipfs,new Func<byte[]>(()=>++calls==6 ? throw cause:Convert.FromBase64String(Environment.GetEnvironmentVariable("OASIS_IPFS_STORAGE_ENCRYPTION_KEY")!)));
            NextGenSoftware.OASIS.Common.OASISResult<bool> failed;
            try
            {
                failed=await new OASISHyperDrive(manager).RouteRequestAsync<bool>(new StorageOperationRequest {
                    Operation="DeleteAvatar",AvatarId=id,SoftDelete=true,PreferredProvider=ProviderType.IPFSOASIS});
            }
            finally {keyField.SetValue(ipfs,prior);}
            Assert.True(failed.IsError); Assert.Equal(6,calls);
            var publication=Assert.IsType<IPFSRecordPublicationException>(failed.Exception);
            Assert.Same(cause,publication.InnerException); Assert.Equal(2,publication.AcknowledgedRecordCids.Count);
            Assert.Equal(2,publication.AcknowledgedRecordCids.Distinct().Count());
            foreach(var cid in publication.AcknowledgedRecordCids)
            {
                Assert.StartsWith("oasis-aes256gcm-gzip-v2:",await ipfs.IPFSClient.FileSystem.ReadAllTextAsync((Cid)cid));
                ownedCids.Add(cid);
            }
            Assert.Equal(root,dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            Assert.True((await ipfs.LoadAvatarAsync(id)).Result.IsActive);
            Assert.True((await ipfs.LoadAvatarDetailAsync(id)).Result.IsActive);
            Assert.True((await local.LoadAvatarAsync(id)).Result.IsActive);
            Assert.Null(manager.PerformanceMonitor.GetMetrics(ProviderType.LocalFileOASIS));
            Assert.False((await local.DeleteAvatarAsync(id,softDelete:false)).IsError);
        }
        finally {await local.DeActivateProviderAsync();}
    }

    private static async Task<AvatarTombstone> VerifySqliteTombstoneReplication(IPFSOASIS ipfs,OASISDNA dna,Guid originalId,string directory,HashSet<string> ownedCids)
    {
        var path=Path.Combine(directory,"tombstone-routing.db");
        if(File.Exists(path)) throw new InvalidOperationException("Retained SQLite fixture requires inspection; no reset.");
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var verified=false;
        try
        {
            Assert.False((await sqlite.ActivateProviderAsync()).IsError);
            AvatarTombstone original=null!;
            foreach(var primaryProvider in new[] {ProviderType.SQLLiteDBOASIS,ProviderType.IPFSOASIS})
            foreach(var synchronous in new[] {false,true})
            {
                var id=primaryProvider==ProviderType.SQLLiteDBOASIS && !synchronous ? originalId:Guid.NewGuid();
                if(id!=originalId)
                {
                    var nativeAvatar=new Avatar {Id=id,Username="native-"+id,Email=id+"@example.invalid",IsActive=true};
                    await ipfs.SaveAvatarToFile(nativeAvatar);
                    ownedCids.Add(nativeAvatar.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                    var nativeDetail=new AvatarDetail {Id=id,Username=nativeAvatar.Username,XP=321,Karma=29,IsActive=true};
                    await ipfs.SaveAvatarDetailToFile(nativeDetail);
                    ownedCids.Add(nativeDetail.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                }
                var saved=await sqlite.SaveAvatarAsync(new Avatar {Id=id,Username="owned-sqlite-primary",Email="sqlite@example.invalid",IsActive=true,
                    AvatarType=new EnumValue<AvatarType>(AvatarType.User),DeletedByAvatarId=Guid.NewGuid()});
                Assert.False(saved.IsError,saved.Message);
                var savedDetail=await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="owned-sqlite-primary",XP=999,Karma=77,IsActive=true});
                Assert.False(savedDetail.IsError,savedDetail.Message);
                dna.OASIS.HyperDriveMode=HyperDriveModes.V2;
                dna.OASIS.SubscriptionConfig=new SubscriptionConfig {PlanType="Gold"};
                var manager=new ProviderManager(null,dna) {IsAutoReplicationEnabled=true,IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false};
                manager.RegisterProvider(ipfs); manager.RegisterProvider(sqlite);
                Assert.False(manager.SetAndReplaceAutoReplicationListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.IPFSOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError);
                var router=new OASISHyperDrive(manager);
                var request=new StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=true,PreferredProvider=primaryProvider};
                var deleted=synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request);
                Assert.False(deleted.IsError,deleted.Message); Assert.False(deleted.IsWarning,deleted.Message); Assert.True(deleted.Result);
                var primary=(await sqlite.LoadAvatarAsync(id)).Result;
                Assert.Equal(DateTimeKind.Utc,primary.DeletedDate.Kind);
                Assert.Equal(DateTimeKind.Utc,(await sqlite.LoadAvatarDetailAsync(id)).Result.DeletedDate.Kind);
                var replica=(await ipfs.LoadAvatarAsync(id)).Result;
                var detail=(await ipfs.LoadAvatarDetailAsync(id)).Result;
                Assert.Equal(primary.DeletedDate,replica.DeletedDate); Assert.Equal(primary.DeletedByAvatarId,replica.DeletedByAvatarId);
                Assert.Equal(primary.DeletedDate,detail.DeletedDate); Assert.False(replica.IsActive); Assert.False(detail.IsActive);
                Assert.Equal(321,detail.XP); Assert.Equal(29,detail.Karma);
                Assert.Equal(999,(await sqlite.LoadAvatarDetailAsync(id)).Result.XP);
                Assert.True(NextGenSoftware.OASIS.API.Core.Helpers.AvatarTombstoneEvidence.TryGet(deleted,out var committed));
                Assert.Equal(DateTimeKind.Utc,committed.DeletedAtUtc.Kind);
                Assert.Equal(committed.DeletedAtUtc,replica.DeletedDate);
                Assert.Equal(committed.DeletedByAvatarId,replica.DeletedByAvatarId);
                ownedCids.Add(replica.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
                ownedCids.Add(detail.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
                ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                var repeatRoot=dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress;
                Assert.False(ipfs.ApplyAvatarTombstone(committed).IsError);
                Assert.Equal(repeatRoot,dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                Assert.False(sqlite.ApplyAvatarTombstone(committed).IsError);
                if(id==originalId) original=committed;
            }
            verified=true;
            return original;
        }
        finally
        {
            await sqlite.DeActivateProviderAsync(); sqlite.Dispose();
            if(verified && File.Exists(path)) File.Delete(path);
        }
    }
}

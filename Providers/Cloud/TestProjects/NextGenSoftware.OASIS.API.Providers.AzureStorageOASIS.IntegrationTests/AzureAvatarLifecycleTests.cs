using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    private static readonly FieldInfo ServiceField=typeof(AzureStorageOASIS).GetField("_serviceClient",BindingFlags.Instance|BindingFlags.NonPublic)!;
    private BlobClient ProfileBlob(Guid id)=>_service.GetBlobContainerClient(_containers[1]).GetBlobClient(id.ToString());
    private BlobClient DetailBlob(Guid id)=>_service.GetBlobContainerClient(_containers[2]).GetBlobClient(id.ToString());
    private async Task<(Avatar Profile,AvatarDetail Detail)> Pair(Guid id)
        =>(OasisJson.Deserialize<Avatar>((await ProfileBlob(id).DownloadContentAsync()).Value.Content.ToString()),
            OasisJson.Deserialize<AvatarDetail>((await DetailBlob(id).DownloadContentAsync()).Value.Content.ToString()));
    private async Task Seed(Guid id,Guid actor)
    {
        Assert.IsFalse((await _provider.SaveAvatarAsync(new Avatar {Id=id,Username="owned-pair-"+id.ToString("N"),IsActive=true,
            FirstName="azure-profile",DeletedByAvatarId=actor})).IsError);
        var detail=new AvatarDetail {Id=id,IsActive=true,Username="owned-pair-"+id.ToString("N"),XP=321,Karma=29};
        detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="azure-inventory"});
        Assert.IsFalse((await _provider.SaveAvatarDetailAsync(detail)).IsError);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task PairedLifecycleAndIncomingTombstonePreserveData(bool synchronous)
    {
        var id=Guid.NewGuid();await Seed(id,Guid.NewGuid());
        var removed=synchronous ? _provider.DeleteAvatar(id):await _provider.DeleteAvatarAsync(id);
        Assert.IsFalse(removed.IsError,removed.Message);Assert.IsTrue(removed.IsSaved);
        Assert.IsTrue(AvatarTombstoneEvidence.TryGet(removed,out var committed));
        var pair=await Pair(id);Assert.IsFalse(pair.Profile.IsActive);Assert.IsFalse(pair.Detail.IsActive);
        Assert.AreEqual(committed.DeletedAtUtc,pair.Detail.DeletedDate);Assert.AreEqual(committed.DeletedByAvatarId,pair.Detail.DeletedByAvatarId);
        Assert.AreEqual("azure-profile",pair.Profile.FirstName);Assert.AreEqual(321L,pair.Detail.XP);Assert.AreEqual("azure-inventory",pair.Detail.Inventory.Single().Name);
        var profileBeforeRepeat=(await ProfileBlob(id).DownloadContentAsync()).Value;var detailBeforeRepeat=(await DetailBlob(id).DownloadContentAsync()).Value;
        Assert.IsFalse((await _provider.DeleteAvatarAsync(id)).IsError);Assert.AreEqual(committed.DeletedAtUtc,(await Pair(id)).Profile.DeletedDate);
        await AssertUnchangedPair(id,profileBeforeRepeat,detailBeforeRepeat);
        Assert.IsFalse((await _provider.DeleteAvatarAsync(id,false)).IsError);
        Assert.IsFalse((await ProfileBlob(id).ExistsAsync()).Value);Assert.IsFalse((await DetailBlob(id).ExistsAsync()).Value);
        var target=Guid.NewGuid();await Seed(target,Guid.Empty);
        var tombstone=new AvatarTombstone(target,new DateTime(2026,10,16,0,0,0,DateTimeKind.Utc),Guid.NewGuid());
        var capability=(IAvatarTombstoneStorageProvider)_provider;
        BlobDownloadResult previousProfile=null,previousDetail=null;
        foreach(var repeat in new[] {false,true})
        {
            var applied=synchronous ? capability.ApplyAvatarTombstone(tombstone):await capability.ApplyAvatarTombstoneAsync(tombstone);
            Assert.IsFalse(applied.IsError,applied.Message);pair=await Pair(target);
            Assert.AreEqual(tombstone.DeletedAtUtc,pair.Profile.DeletedDate);Assert.AreEqual(tombstone.DeletedAtUtc,pair.Detail.DeletedDate);
            Assert.AreEqual(tombstone.DeletedByAvatarId,pair.Profile.DeletedByAvatarId);Assert.AreEqual(tombstone.DeletedByAvatarId,pair.Detail.DeletedByAvatarId);
            Assert.AreEqual(321L,pair.Detail.XP);
            if(repeat)await AssertUnchangedPair(target,previousProfile,previousDetail);
            previousProfile=(await ProfileBlob(target).DownloadContentAsync()).Value;previousDetail=(await DetailBlob(target).DownloadContentAsync()).Value;
        }
        var conflict=await capability.ApplyAvatarTombstoneAsync(new AvatarTombstone(target,tombstone.DeletedAtUtc.AddSeconds(1),tombstone.DeletedByAvatarId));
        Assert.IsTrue(conflict.IsError);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,conflict.ErrorCode);
    }

    private async Task AssertUnchangedPair(Guid id,BlobDownloadResult profile,BlobDownloadResult detail)
    {
        var actualProfile=(await ProfileBlob(id).DownloadContentAsync()).Value;var actualDetail=(await DetailBlob(id).DownloadContentAsync()).Value;
        Assert.AreEqual(profile.Content.ToString(),actualProfile.Content.ToString());Assert.AreEqual(profile.Details.ETag,actualProfile.Details.ETag);
        Assert.AreEqual(detail.Content.ToString(),actualDetail.Content.ToString());Assert.AreEqual(detail.Details.ETag,actualDetail.Details.ETag);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ExistingProfileTombstoneStillCompletesActiveDetail(bool synchronous)
    {
        RequireLive();_provider.IsVersionControlEnabled=true;var id=Guid.NewGuid();await Seed(id,Guid.NewGuid());
        var blob=ProfileBlob(id);var original=(await blob.DownloadContentAsync()).Value;
        var profile=OasisJson.Deserialize<Avatar>(original.Content.ToString());profile.IsActive=false;profile.DeletedDate=DateTime.UtcNow;
        await blob.UploadAsync(BinaryData.FromString(OasisJson.Serialize(profile)),new BlobUploadOptions{Conditions=new BlobRequestConditions{IfMatch=original.Details.ETag}});
        var appliedProfile=(await blob.DownloadContentAsync()).Value;
        var detailBefore=(await DetailBlob(id).DownloadContentAsync()).Value;
        var result=synchronous?_provider.DeleteAvatar(id):await _provider.DeleteAvatarAsync(id);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(AvatarTombstoneEvidence.TryGet(result,out var tombstone));
        var finalProfile=(await blob.DownloadContentAsync()).Value;Assert.AreEqual(appliedProfile.Content.ToString(),finalProfile.Content.ToString());Assert.AreEqual(appliedProfile.Details.ETag,finalProfile.Details.ETag);
        var finalDetail=(await DetailBlob(id).DownloadContentAsync()).Value;Assert.AreNotEqual(detailBefore.Details.ETag,finalDetail.Details.ETag);
        var detail=OasisJson.DeserializeStored<AvatarDetail>(finalDetail.Content.ToString());Assert.IsFalse(detail.IsActive);Assert.AreEqual(tombstone.DeletedAtUtc,detail.DeletedDate);Assert.AreEqual(profile.DeletedByAvatarId,detail.DeletedByAvatarId);Assert.AreEqual(321L,detail.XP);
        Assert.IsFalse((await _provider.DeleteAvatarAsync(id)).IsError);await AssertUnchangedPair(id,finalProfile,finalDetail);
    }

    private sealed class SplitService : BlobServiceClient
    {
        private readonly BlobServiceClient _healthy,_bad;private readonly string _container;
        public SplitService(BlobServiceClient healthy,string container):base("UseDevelopmentStorage=true")
        {
            _healthy=healthy;_container=container;var options=new BlobClientOptions();options.Retry.MaxRetries=0;options.Retry.NetworkTimeout=TimeSpan.FromSeconds(1);
            _bad=new BlobServiceClient(new Uri("http://127.0.0.1:10003/devstoreaccount1"),options);
        }
        public override BlobContainerClient GetBlobContainerClient(string name)
            =>name==_container ? new SplitContainer(_healthy.GetBlobContainerClient(name),_bad.GetBlobContainerClient(name)):_healthy.GetBlobContainerClient(name);
    }
    private sealed class SplitContainer : BlobContainerClient
    {
        private readonly BlobContainerClient _healthy,_bad;
        public SplitContainer(BlobContainerClient healthy,BlobContainerClient bad):base("UseDevelopmentStorage=true",healthy.Name) {_healthy=healthy;_bad=bad;}
        public override BlobClient GetBlobClient(string name)=>new SplitBlob(_healthy.GetBlobClient(name),_bad.GetBlobClient(name));
    }
    private sealed class SplitBlob : BlobClient
    {
        private readonly BlobClient _healthy,_bad;
        public SplitBlob(BlobClient healthy,BlobClient bad):base("UseDevelopmentStorage=true",healthy.BlobContainerName,healthy.Name) {_healthy=healthy;_bad=bad;}
        public override Task<Response<BlobDownloadResult>> DownloadContentAsync(CancellationToken cancellationToken=default)=>_healthy.DownloadContentAsync(cancellationToken);
        public override Task<Response<BlobContentInfo>> UploadAsync(BinaryData content,BlobUploadOptions options,CancellationToken cancellationToken=default)=>_bad.UploadAsync(content,options,cancellationToken);
        public override Task<Response> DeleteAsync(DeleteSnapshotsOption snapshotsOption=DeleteSnapshotsOption.None,BlobRequestConditions conditions=null,CancellationToken cancellationToken=default)=>_bad.DeleteAsync(snapshotsOption,conditions,cancellationToken);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task V2LifecycleReplicatesAcrossAzureAndSqlite(bool synchronous)=>Routing(synchronous,false);
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task V2PartialLifecycleCannotFailOverOrReplicate(bool synchronous)=>Routing(synchronous,true);

    private async Task Routing(bool synchronous,bool failSecondMutation)
    {
        var path=Path.GetFullPath("TestResults/AzureRoutingFiles/lifecycle.db");
        if(File.Exists(path)) Assert.Fail("Retained Azure lifecycle SQLite evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoReplicationEnabled=true,IsAutoFailOverEnabled=failSecondMutation};
            manager.RegisterProvider(_provider);manager.RegisterProvider(sqlite);
            var types=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AzureStorageOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            foreach(var primary in failSecondMutation ? types.Take(1):types)
            foreach(var soft in new[] {true,false})
            {
                var id=Guid.NewGuid();var actor=Guid.NewGuid();await Seed(id,actor);
                Assert.IsFalse((await sqlite.SaveAvatarAsync(new Avatar {Id=id,Username="owned-sqlite",FirstName="sqlite-profile",IsActive=true,DeletedByAvatarId=actor,
                    AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
                var local=new AvatarDetail {Id=id,Username="owned-sqlite",IsActive=true,XP=777};local.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="sqlite-inventory"});
                Assert.IsFalse((await sqlite.SaveAvatarDetailAsync(local)).IsError);
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                if(failSecondMutation) ServiceField.SetValue(_provider,new SplitService(healthy,soft ? _containers[2]:_containers[1]));
                NextGenSoftware.OASIS.Common.OASISResult<bool> result;
                try
                {
                    var request=new Core.StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=soft,PreferredProvider=primary.Value};
                    result=synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request);
                }
                finally {ServiceField.SetValue(_provider,healthy);}
                if(failSecondMutation)
                {
                    Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsFalse(AvatarTombstoneEvidence.TryGet(result,out _));
                    Assert.IsInstanceOfType(result.Exception,typeof(AzureAvatarLifecycleException));
                    var partial=(AzureAvatarLifecycleException)result.Exception;Assert.AreEqual(id,partial.AvatarId);Assert.AreEqual(1,partial.AcknowledgedBlobPaths.Count);
                    Assert.AreEqual((soft ? _containers[1]:_containers[2])+"/"+id,partial.AcknowledgedBlobPaths[0]);
                    Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
                    Assert.IsTrue((await sqlite.LoadAvatarAsync(id)).Result.IsActive);Assert.IsTrue((await sqlite.LoadAvatarDetailAsync(id)).Result.IsActive);
                    if(soft) {var state=await Pair(id);Assert.IsFalse(state.Profile.IsActive);Assert.IsTrue(state.Detail.IsActive);Assert.AreEqual(321L,state.Detail.XP);}
                    else {Assert.IsTrue(OasisJson.Deserialize<Avatar>((await ProfileBlob(id).DownloadContentAsync()).Value.Content.ToString()).IsActive);Assert.IsFalse((await DetailBlob(id).ExistsAsync()).Value);}
                }
                else
                {
                    Assert.IsFalse(result.IsError,result.Message);Assert.IsFalse(result.IsWarning,result.Message);
                    if(soft)
                    {
                        Assert.IsTrue(AvatarTombstoneEvidence.TryGet(result,out var committed));var state=await Pair(id);
                        var lp=(await sqlite.LoadAvatarAsync(id)).Result;var ld=(await sqlite.LoadAvatarDetailAsync(id)).Result;
                        Assert.IsFalse(state.Profile.IsActive);Assert.IsFalse(state.Detail.IsActive);Assert.IsFalse(lp.IsActive);Assert.IsFalse(ld.IsActive);
                        Assert.AreEqual(committed.DeletedAtUtc,state.Detail.DeletedDate);Assert.AreEqual(committed.DeletedAtUtc,ld.DeletedDate);
                        Assert.AreEqual(actor,state.Detail.DeletedByAvatarId);Assert.AreEqual(actor,ld.DeletedByAvatarId);
                        Assert.AreEqual("azure-profile",state.Profile.FirstName);Assert.AreEqual("sqlite-profile",lp.FirstName);
                        Assert.AreEqual(321L,state.Detail.XP);Assert.AreEqual(777L,ld.XP);
                        Assert.AreEqual("azure-inventory",state.Detail.Inventory.Single().Name);Assert.AreEqual("sqlite-inventory",ld.Inventory.Single().Name);
                    }
                    else {Assert.IsFalse((await ProfileBlob(id).ExistsAsync()).Value);Assert.IsFalse((await DetailBlob(id).ExistsAsync()).Value);Assert.IsNull((await sqlite.LoadAvatarAsync(id)).Result);Assert.IsNull((await sqlite.LoadAvatarDetailAsync(id)).Result);}
                }
            }
            verified=true;
        }
        finally
        {
            ServiceField.SetValue(_provider,healthy);await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }
}

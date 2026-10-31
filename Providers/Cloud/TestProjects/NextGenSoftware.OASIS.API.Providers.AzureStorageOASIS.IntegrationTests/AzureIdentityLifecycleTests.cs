using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeIdentityHardDeleteResolvesTombstoneWithoutExposingItPublicly(bool email,bool synchronous)
    {
        RequireLive();_provider.IsVersionControlEnabled=true;var id=Guid.NewGuid();
        var source=new Avatar{Id=id,Username="owned-old-owner",Email="owned-old-owner@example.invalid",IsActive=true};
        Assert.IsFalse((await _provider.SaveAvatarAsync(source)).IsError);
        Assert.IsFalse((await _provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=source.Username,Email=source.Email,IsActive=true,Karma=123,XP=456})).IsError);
        var soft=await _provider.DeleteAvatarAsync(id);Assert.IsFalse(soft.IsError,soft.Exception?.ToString());
        var hidden=await _provider.LoadAvatarAsync(id);Assert.IsTrue(hidden.IsError);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,hidden.ErrorCode);
        var list=await _provider.LoadAllAvatarsAsync();Assert.IsFalse(list.IsError,list.Exception?.ToString());Assert.AreEqual(0,list.Result.Count());
        var publicIdentity=email?await _provider.LoadAvatarByEmailAsync(source.Email):await _provider.LoadAvatarByUsernameAsync(source.Username);
        Assert.IsTrue(publicIdentity.IsError);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,publicIdentity.ErrorCode);
        var old=await _provider.LoadAvatarAsync(id,1);Assert.IsFalse(old.IsError,old.Exception?.ToString());Assert.AreEqual(source.Username,old.Result.Username);Assert.IsTrue(old.Result.IsActive);
        var deleted=email?synchronous?_provider.DeleteAvatarByEmail(source.Email,false):await _provider.DeleteAvatarByEmailAsync(source.Email,false)
            :synchronous?_provider.DeleteAvatarByUsername(source.Username,false):await _provider.DeleteAvatarByUsernameAsync(source.Username,false);
        Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());Assert.IsTrue(deleted.IsDeleted);Assert.IsTrue(deleted.Result);
        Assert.IsFalse((await ProfileBlob(id).ExistsAsync()).Value);Assert.IsFalse((await DetailBlob(id).ExistsAsync()).Value);
    }

    private sealed class IdentityReassignService(BlobServiceClient healthy,BlobClient target,bool detail=false):BlobServiceClient
    {
        public BlobClient Target=>target;
        public bool IsDetail=>detail;
        public int ProfileReads,OtherReads,Writes,Deletes;
        public string? WinnerJson;public ETag WinnerTag;
        public override BlobContainerClient GetBlobContainerClient(string name)=>new IdentityReassignContainer(healthy.GetBlobContainerClient(name),this);
    }
    private sealed class IdentityReassignContainer:BlobContainerClient
    {
        private readonly BlobContainerClient healthy;private readonly IdentityReassignService owner;
        public IdentityReassignContainer(BlobContainerClient healthy,IdentityReassignService owner):base("UseDevelopmentStorage=true",healthy.Name){this.healthy=healthy;this.owner=owner;}
        public override BlobClient GetBlobClient(string name)=>new IdentityReassignBlob(healthy.GetBlobClient(name),owner);
    }
    private sealed class IdentityReassignBlob(BlobClient healthy,IdentityReassignService owner):BlobClient
    {
        public override string Name=>healthy.Name;
        public override string BlobContainerName=>healthy.BlobContainerName;
        public override async Task<Response<BlobDownloadResult>> DownloadContentAsync(CancellationToken cancellationToken=default)
        {
            var observed=await healthy.DownloadContentAsync(cancellationToken);
            if(Name==owner.Target.Name&&BlobContainerName==owner.Target.BlobContainerName){
                if(++owner.ProfileReads==1){
                    if(owner.IsDetail){
                        var reassigned=OasisJson.DeserializeStored<AvatarDetail>(observed.Value.Content.ToString());
                        reassigned.Username="reassigned-owner";reassigned.Email="reassigned-owner@example.invalid";
                        await WriteHistory(healthy,reassigned,true);
                    }else{
                        var reassigned=OasisJson.DeserializeStored<Avatar>(observed.Value.Content.ToString());
                        reassigned.Username="reassigned-owner";reassigned.Email="reassigned-owner@example.invalid";
                        await WriteHistory(healthy,reassigned,true);
                    }
                    var actual=(await healthy.DownloadContentAsync(cancellationToken)).Value;owner.WinnerJson=actual.Content.ToString();owner.WinnerTag=actual.Details.ETag;
                }
            }else owner.OtherReads++;
            // Unique identity lookup sees its ORIGINAL native snapshot; lifecycle must
            // recheck that identity against the current tagged canonical observation.
            return observed;
        }
        public override Task<Response<BlobContentInfo>> UploadAsync(BinaryData content,BlobUploadOptions options,CancellationToken cancellationToken=default)
        {owner.Writes++;return healthy.UploadAsync(content,options,cancellationToken);}
        public override Task<Response> DeleteAsync(DeleteSnapshotsOption snapshotsOption=DeleteSnapshotsOption.None,BlobRequestConditions? conditions=null,CancellationToken cancellationToken=default)
        {owner.Deletes++;return healthy.DeleteAsync(snapshotsOption,conditions,cancellationToken);}
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task NativeIdentityDeletionRefusesReassignmentBeforeAnyMutation(bool email,bool softDelete,bool synchronous)
    {
        RequireLive();_provider.IsVersionControlEnabled=true;var id=Guid.NewGuid();
        var original=new Avatar{Id=id,Username="owned-original",Email="owned-original@example.invalid",IsActive=true};
        Assert.IsFalse((await _provider.SaveAvatarAsync(original)).IsError);
        Assert.IsFalse((await _provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=original.Username,Email=original.Email,IsActive=true,Karma=123,XP=456})).IsError);
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;var profile=ProfileBlob(id);var detail=DetailBlob(id);
        var detailBefore=(await detail.DownloadContentAsync()).Value;var race=new IdentityReassignService(healthy,profile);
        try{
            ServiceField.SetValue(_provider,race);
            var result=email?synchronous?_provider.DeleteAvatarByEmail(original.Email,softDelete):await _provider.DeleteAvatarByEmailAsync(original.Email,softDelete)
                :synchronous?_provider.DeleteAvatarByUsername(original.Username,softDelete):await _provider.DeleteAvatarByUsernameAsync(original.Username,softDelete);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);
            Assert.IsInstanceOfType(result.Exception,typeof(StorageLifecycleConflictException));Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
            Assert.AreEqual(2,race.ProfileReads);Assert.AreEqual(0,race.OtherReads);Assert.AreEqual(0,race.Writes);Assert.AreEqual(0,race.Deletes);
        }finally{ServiceField.SetValue(_provider,healthy);}
        var winner=(await profile.DownloadContentAsync()).Value;Assert.AreEqual(race.WinnerJson,winner.Content.ToString());Assert.AreEqual(race.WinnerTag,winner.Details.ETag);
        var unchanged=(await detail.DownloadContentAsync()).Value;Assert.AreEqual(detailBefore.Content.ToString(),unchanged.Content.ToString());Assert.AreEqual(detailBefore.Details.ETag,unchanged.Details.ETag);
        var current=OasisJson.DeserializeStored<Avatar>(winner.Content.ToString());Assert.AreEqual("reassigned-owner",current.Username);Assert.IsTrue(current.IsActive);Assert.AreEqual(2,current.Version);Assert.AreEqual(DateTime.MinValue,current.DeletedDate);
        var old=await _provider.LoadAvatarAsync(id,1);Assert.IsFalse(old.IsError,old.Exception?.ToString());Assert.AreEqual(original.Username,old.Result.Username);
        await detail.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=unchanged.Details.ETag});
        await profile.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=winner.Details.ETag});
        Assert.IsFalse((await detail.ExistsAsync()).Value);Assert.IsFalse((await profile.ExistsAsync()).Value);
    }
}

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    private async Task<OASISResult<bool>> DeleteDetailRoute(int route,AvatarDetail row,bool soft)
    {
        IOASISStorageProvider contract=_provider;
        return route switch{
            0=>await contract.DeleteAvatarDetailAsync(row.Id,soft),1=>contract.DeleteAvatarDetail(row.Id,soft),
            2=>await contract.DeleteAvatarDetailByEmailAsync(row.Email,soft),3=>contract.DeleteAvatarDetailByEmail(row.Email,soft),
            4=>await contract.DeleteAvatarDetailByUsernameAsync(row.Username,soft),_=>contract.DeleteAvatarDetailByUsername(row.Username,soft)};
    }
    private async Task<AvatarDetail> SeedStandaloneDetail()
    {
        RequireLive();_provider.IsVersionControlEnabled=true;var id=Guid.NewGuid();
        Assert.IsFalse((await _provider.SaveAvatarAsync(new Avatar{Id=id,Username="untouched-avatar",Description="avatar-must-not-change",IsActive=true})).IsError);
        var row=new AvatarDetail{Id=id,Username="owned-detail",Email="owned-detail@example.invalid",Description="original-detail",IsActive=true,Karma=123,XP=456};
        Assert.IsFalse((await _provider.SaveAvatarDetailAsync(row)).IsError);return row;
    }

    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(3,false)][DataRow(4,false)][DataRow(5,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)][DataRow(3,true)][DataRow(4,true)][DataRow(5,true)]
    public async Task NativeStandaloneDetailRoutesPreserveAvatarAndHistory(int route,bool hard)
    {
        var row=await SeedStandaloneDetail();var avatar=ProfileBlob(row.Id);var detail=DetailBlob(row.Id);
        var avatarBefore=(await avatar.DownloadContentAsync()).Value;
        var deleted=await DeleteDetailRoute(route,row,!hard);Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());Assert.IsTrue(deleted.Result);Assert.IsTrue(deleted.IsDeleted);
        if(!hard){
            var actual=(await detail.DownloadContentAsync()).Value;var current=OasisJson.DeserializeStored<AvatarDetail>(actual.Content.ToString());
            Assert.IsFalse(current.IsActive);Assert.AreNotEqual(DateTime.MinValue,current.DeletedDate);Assert.AreEqual(2,current.Version);Assert.AreEqual(123L,current.Karma);Assert.AreEqual(456L,current.XP);
            var old=await _provider.LoadAvatarDetailAsync(row.Id,1);Assert.IsFalse(old.IsError,old.Exception?.ToString());Assert.AreEqual("original-detail",old.Result.Description);Assert.AreEqual(row.VersionId,old.Result.VersionId);
            var hidden=await _provider.LoadAvatarDetailAsync(row.Id);Assert.IsTrue(hidden.IsError);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,hidden.ErrorCode);
            var repeat=await DeleteDetailRoute(route,row,true);Assert.IsFalse(repeat.IsError,repeat.Exception?.ToString());Assert.IsTrue(repeat.IsDeleted);
            var unchanged=(await detail.DownloadContentAsync()).Value;Assert.AreEqual(actual.Content.ToString(),unchanged.Content.ToString());Assert.AreEqual(actual.Details.ETag,unchanged.Details.ETag);
            var removed=await DeleteDetailRoute(route,row,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsTrue(removed.IsDeleted);
        }
        Assert.IsFalse((await detail.ExistsAsync()).Value);
        var avatarAfter=(await avatar.DownloadContentAsync()).Value;Assert.AreEqual(avatarBefore.Content.ToString(),avatarAfter.Content.ToString());Assert.AreEqual(avatarBefore.Details.ETag,avatarAfter.Details.ETag);
        await avatar.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=avatarAfter.Details.ETag});Assert.IsFalse((await avatar.ExistsAsync()).Value);
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task NativeDetailLifecycleFencesRealRevisionRaceOrLostAcknowledgement(bool lost,bool soft,bool synchronous)
    {
        var row=await SeedStandaloneDetail();var avatar=ProfileBlob(row.Id);var detail=DetailBlob(row.Id);var avatarBefore=(await avatar.DownloadContentAsync()).Value;
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;var cause=new IOException("Owned loss after actual native detail mutation.");
        Func<Task> change=lost?()=>Task.FromException(cause):async()=>{await WriteHistory(detail,new AvatarDetail{Id=row.Id,Username=row.Username,Email=row.Email,Description="concurrent-winner",Karma=999,XP=888,IsActive=true},true);};
        var fault=new CallerEditService(healthy,()=>{},lost,change);
        try{
            ServiceField.SetValue(_provider,fault);var result=await DeleteDetailRoute(synchronous?1:0,row,soft);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);
            Assert.AreEqual(soft?1:0,fault.Writes);Assert.AreEqual(soft?0:1,fault.Deletes);
            if(lost){Assert.IsInstanceOfType(result.Exception,typeof(AzureBlobMutationReconciliationException));Assert.AreSame(cause,result.Exception.InnerException);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));}
            else{Assert.IsInstanceOfType(result.Exception,typeof(RequestFailedException));Assert.AreEqual(412,((RequestFailedException)result.Exception).Status);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));}
        }finally{ServiceField.SetValue(_provider,healthy);}
        var avatarAfter=(await avatar.DownloadContentAsync()).Value;Assert.AreEqual(avatarBefore.Content.ToString(),avatarAfter.Content.ToString());Assert.AreEqual(avatarBefore.Details.ETag,avatarAfter.Details.ETag);
        if(lost&&!soft)Assert.IsFalse((await detail.ExistsAsync()).Value);
        else{
            var actual=(await detail.DownloadContentAsync()).Value;var current=OasisJson.DeserializeStored<AvatarDetail>(actual.Content.ToString());Assert.AreEqual(2,current.Version);
            if(lost){Assert.IsFalse(current.IsActive);Assert.AreNotEqual(DateTime.MinValue,current.DeletedDate);Assert.AreEqual(123L,current.Karma);}
            else{Assert.IsTrue(current.IsActive);Assert.AreEqual(DateTime.MinValue,current.DeletedDate);Assert.AreEqual("concurrent-winner",current.Description);Assert.AreEqual(999L,current.Karma);}
            var old=await _provider.LoadAvatarDetailAsync(row.Id,1);Assert.IsFalse(old.IsError,old.Exception?.ToString());Assert.AreEqual(row.VersionId,old.Result.VersionId);
            await detail.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=actual.Details.ETag});Assert.IsFalse((await detail.ExistsAsync()).Value);
        }
        await avatar.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=avatarAfter.Details.ETag});Assert.IsFalse((await avatar.ExistsAsync()).Value);
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task NativeStandaloneDetailIdentityReassignmentRefusesMutation(bool email,bool soft,bool synchronous)
    {
        var row=await SeedStandaloneDetail();var avatar=ProfileBlob(row.Id);var detail=DetailBlob(row.Id);var avatarBefore=(await avatar.DownloadContentAsync()).Value;
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;var race=new IdentityReassignService(healthy,detail,true);
        try{
            ServiceField.SetValue(_provider,race);var result=await DeleteDetailRoute(email?synchronous?3:2:synchronous?5:4,row,soft);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.IsInstanceOfType(result.Exception,typeof(StorageLifecycleConflictException));Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
            Assert.AreEqual(2,race.ProfileReads);Assert.AreEqual(0,race.OtherReads);Assert.AreEqual(0,race.Writes);Assert.AreEqual(0,race.Deletes);
        }finally{ServiceField.SetValue(_provider,healthy);}
        var winner=(await detail.DownloadContentAsync()).Value;Assert.AreEqual(race.WinnerJson,winner.Content.ToString());Assert.AreEqual(race.WinnerTag,winner.Details.ETag);
        var current=OasisJson.DeserializeStored<AvatarDetail>(winner.Content.ToString());Assert.IsTrue(current.IsActive);Assert.AreEqual("reassigned-owner",current.Username);Assert.AreEqual(2,current.Version);
        var avatarAfter=(await avatar.DownloadContentAsync()).Value;Assert.AreEqual(avatarBefore.Content.ToString(),avatarAfter.Content.ToString());Assert.AreEqual(avatarBefore.Details.ETag,avatarAfter.Details.ETag);
        await detail.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=winner.Details.ETag});Assert.IsFalse((await detail.ExistsAsync()).Value);
        await avatar.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=avatarAfter.Details.ETag});Assert.IsFalse((await avatar.ExistsAsync()).Value);
    }
}

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    private sealed class CallerEditService(BlobServiceClient healthy,Action edit,bool afterWrite):BlobServiceClient
    {
        public int Writes;private bool edited;
        public void Edit(){if(!edited){edited=true;edit();}}
        public override BlobContainerClient GetBlobContainerClient(string name)=>new CallerEditContainer(healthy.GetBlobContainerClient(name),this,afterWrite);
    }
    private sealed class CallerEditContainer(BlobContainerClient healthy,CallerEditService owner,bool afterWrite):BlobContainerClient
    {public override BlobClient GetBlobClient(string name)=>new CallerEditBlob(healthy.GetBlobClient(name),owner,afterWrite);}
    private sealed class CallerEditBlob(BlobClient healthy,CallerEditService owner,bool afterWrite):BlobClient
    {
        public override string Name=>healthy.Name;
        public override string BlobContainerName=>healthy.BlobContainerName;
        public override async Task<Response<BlobDownloadResult>> DownloadContentAsync(CancellationToken cancellationToken=default)
        {try{return await healthy.DownloadContentAsync(cancellationToken);}finally{if(!afterWrite)owner.Edit();}}
        public override async Task<Response<BlobContentInfo>> UploadAsync(BinaryData content,BlobUploadOptions options,CancellationToken cancellationToken=default)
        {owner.Writes++;var response=await healthy.UploadAsync(content,options,cancellationToken);if(afterWrite)owner.Edit();return response;}
    }

    [DataTestMethod]
    [DataRow(0,false,false)][DataRow(1,false,false)][DataRow(2,false,false)]
    [DataRow(0,true,false)][DataRow(1,true,false)][DataRow(2,true,false)]
    [DataRow(0,false,true)][DataRow(1,false,true)][DataRow(2,false,true)]
    [DataRow(0,true,true)][DataRow(1,true,true)][DataRow(2,true,true)]
    public async Task NativePublicSaveRetainsCallerEditsAndActualBlobOutcome(int kind,bool synchronous,bool afterWrite)
    {
        RequireLive();_provider.IsVersionControlEnabled=true;
        IHolonBase source=kind==0?new Holon():kind==1?new Avatar():new AvatarDetail();
        source.Id=Guid.NewGuid();source.Version=1;source.VersionId=Guid.NewGuid();source.Description="owned-native-proposal";var originalVersion=source.VersionId;
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;
        var blob=healthy.GetBlobContainerClient(_containers[kind]).GetBlobClient(source.Id.ToString());Assert.IsFalse((await blob.ExistsAsync()).Value);
        var edited=new CallerEditService(healthy,()=>source.Version=777,afterWrite);
        async Task<(bool Error,bool Saved,object? Result,Exception? Cause)> Save(){
            if(kind==0){var result=synchronous?_provider.SaveHolon((IHolon)source,false):await _provider.SaveHolonAsync((IHolon)source,false);return(result.IsError,result.IsSaved,result.Result,result.Exception);}
            if(kind==1){var result=synchronous?_provider.SaveAvatar((IAvatar)source):await _provider.SaveAvatarAsync((IAvatar)source);return(result.IsError,result.IsSaved,result.Result,result.Exception);}
            var detail=synchronous?_provider.SaveAvatarDetail((IAvatarDetail)source):await _provider.SaveAvatarDetailAsync((IAvatarDetail)source);return(detail.IsError,detail.IsSaved,detail.Result,detail.Exception);
        }
        try{
            ServiceField.SetValue(_provider,edited);var result=await Save();
            Assert.IsTrue(result.Error);Assert.IsFalse(result.Saved);Assert.IsNull(result.Result);
            Assert.AreEqual(777,source.Version);Assert.AreEqual(originalVersion,source.VersionId);Assert.AreEqual(afterWrite?1:0,edited.Writes);
            Assert.IsFalse(source.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.AzureStorageOASIS));
            if(afterWrite)Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Cause));else Assert.IsInstanceOfType(result.Cause,typeof(InvalidOperationException));
        }finally{ServiceField.SetValue(_provider,healthy);}
        if(!afterWrite){Assert.IsFalse((await blob.ExistsAsync()).Value);return;}
        var actual=(await blob.DownloadContentAsync()).Value;
        IHolonBase canonical=kind==0?OasisJson.DeserializeStored<Holon>(actual.Content.ToString()):kind==1?OasisJson.DeserializeStored<Avatar>(actual.Content.ToString()):OasisJson.DeserializeStored<AvatarDetail>(actual.Content.ToString());
        Assert.AreEqual(source.Id,canonical.Id);Assert.AreEqual(1,canonical.Version);Assert.AreEqual("owned-native-proposal",canonical.Description);
        await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=actual.Details.ETag});Assert.IsFalse((await blob.ExistsAsync()).Value);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeGraphDoesNotOverwriteEditedOriginalOrSubmitLaterEditedChild(bool synchronous,bool editChild)
    {
        RequireLive();_provider.IsVersionControlEnabled=true;
        var child=new Holon{Id=Guid.NewGuid()};var root=new Holon{Id=Guid.NewGuid(),Description="owned-native-root",Children=new List<IHolon>{child}};
        var changed=editChild?child:root;var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;
        var edited=new CallerEditService(healthy,()=>changed.Version=777,true);
        try{
            ServiceField.SetValue(_provider,edited);var result=synchronous?_provider.SaveHolons(new[]{root}):await _provider.SaveHolonsAsync(new[]{root});
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
            Assert.AreEqual(777,changed.Version);Assert.AreEqual(1,edited.Writes);
            if(editChild)CollectionAssert.AreEqual(new[]{root.Id},((AzureBlobPartialMutationException)result.Exception).AcknowledgedRecordIds.ToArray());
        }finally{ServiceField.SetValue(_provider,healthy);}
        var container=healthy.GetBlobContainerClient(_containers[0]);Assert.IsFalse((await container.GetBlobClient(child.Id.ToString()).ExistsAsync()).Value);
        var blob=container.GetBlobClient(root.Id.ToString());var actual=(await blob.DownloadContentAsync()).Value;
        var canonical=OasisJson.DeserializeStored<Holon>(actual.Content.ToString());Assert.AreEqual(1,canonical.Version);Assert.AreEqual(root.Id,canonical.Id);Assert.AreEqual("owned-native-root",canonical.Description);
        await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=actual.Details.ETag});Assert.IsFalse((await blob.ExistsAsync()).Value);
    }
}

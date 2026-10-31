using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(0,false)] [DataRow(0,true)] [DataRow(1,false)] [DataRow(1,true)] [DataRow(2,false)] [DataRow(2,true)]
    public async Task HistoryUpdateLostAckRetainsCallerVersionAndReconcilesActualArchiveWithoutReplay(int kind,bool synchronous)
    {
        RequireLive();_provider.IsVersionControlEnabled=true;
        var profile=new Avatar {Id=Guid.NewGuid(),Description="owned-before",Username="owned-history-loss",IsActive=true};
        var detail=new AvatarDetail {Id=Guid.NewGuid(),Description="owned-before",Username="owned-history-loss",IsActive=true,XP=321};
        var holon=new Holon {Id=Guid.NewGuid(),Description="owned-before",IsActive=true};
        IHolonBase source=kind==0 ? profile:kind==1 ? detail:holon;
        async Task<(Exception Cause,bool Error,bool Saved)> Save() {
            if(kind==0) {var r=synchronous ? _provider.SaveAvatar(profile):await _provider.SaveAvatarAsync(profile);return(r.Exception,r.IsError,r.IsSaved);}
            if(kind==1) {var r=synchronous ? _provider.SaveAvatarDetail(detail):await _provider.SaveAvatarDetailAsync(detail);return(r.Exception,r.IsError,r.IsSaved);}
            var h=synchronous ? _provider.SaveHolon(holon,false):await _provider.SaveHolonAsync(holon,false);return(h.Exception,h.IsError,h.IsSaved);
        }
        var seeded=await Save();Assert.IsFalse(seeded.Error);Assert.IsTrue(seeded.Saved);Assert.AreEqual(1,source.Version);
        var originalVersion=source.VersionId;var originalPrevious=source.PreviousVersionId;
        var originalKey=source.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS];
        source.Description="owned-after";
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;
        var fault=new LostUploadService {Healthy=healthy};
        try {
            ServiceField.SetValue(_provider,fault);
            var failed=await Save();Assert.IsTrue(failed.Error);Assert.IsFalse(failed.Saved);
            Assert.IsInstanceOfType(failed.Cause,typeof(AzureBlobMutationReconciliationException));
            Assert.AreSame(fault.Cause,failed.Cause.GetBaseException());Assert.IsTrue(NativeMutationReconciliation.IsRequired(failed.Cause));
            Assert.AreEqual(1,fault.Commits);Assert.AreEqual(1,source.Version);Assert.AreEqual(originalVersion,source.VersionId);
            Assert.AreEqual(originalPrevious,source.PreviousVersionId);
            Assert.AreEqual(originalKey,source.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS]);
        }
        finally {ServiceField.SetValue(_provider,healthy);}
        var blob=healthy.GetBlobContainerClient(fault.Container).GetBlobClient(fault.Blob);
        var readback=(await blob.DownloadContentAsync()).Value;
        var document=JObject.Parse(readback.Content.ToString());
        var current=await new EmbeddedRecordHistory().ReadAsync(source.Id,document,0,OasisJson.DeserializeStored<Holon>);
        var previous=await new EmbeddedRecordHistory().ReadAsync(source.Id,document,1,OasisJson.DeserializeStored<Holon>);
        Assert.AreEqual(2,current.Version);Assert.AreEqual("owned-after",current.Description);
        Assert.AreEqual(originalVersion,current.PreviousVersionId);Assert.AreNotEqual(originalVersion,current.VersionId);
        Assert.AreEqual(1,previous.Version);Assert.AreEqual("owned-before",previous.Description);Assert.AreEqual(originalVersion,previous.VersionId);
        await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions {IfMatch=readback.Details.ETag});
        Assert.IsFalse((await blob.ExistsAsync()).Value);
    }
}

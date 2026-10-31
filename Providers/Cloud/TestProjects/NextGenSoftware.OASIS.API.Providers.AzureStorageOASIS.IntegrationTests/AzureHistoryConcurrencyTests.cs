using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    private sealed class HistoryRaceService : BlobServiceClient
    {
        public required BlobServiceClient Healthy;
        public required Holon Winner;
        public int Writes;
        public HistoryRaceService():base("UseDevelopmentStorage=true") { }
        public override BlobContainerClient GetBlobContainerClient(string name)=>new HistoryRaceContainer(Healthy.GetBlobContainerClient(name),this);
    }
    private sealed class HistoryRaceContainer : BlobContainerClient
    {
        private readonly BlobContainerClient _healthy;private readonly HistoryRaceService _owner;
        public HistoryRaceContainer(BlobContainerClient healthy,HistoryRaceService owner):base("UseDevelopmentStorage=true",healthy.Name){_healthy=healthy;_owner=owner;}
        public override BlobClient GetBlobClient(string name)=>new HistoryRaceBlob(_healthy.GetBlobClient(name),_owner);
    }
    private sealed class HistoryRaceBlob : BlobClient
    {
        private readonly BlobClient _healthy;private readonly HistoryRaceService _owner;
        public HistoryRaceBlob(BlobClient healthy,HistoryRaceService owner):base("UseDevelopmentStorage=true",healthy.BlobContainerName,healthy.Name){_healthy=healthy;_owner=owner;}
        public override async Task<Response<BlobDownloadResult>> DownloadContentAsync(CancellationToken cancellationToken=default)
        {
            Response<BlobDownloadResult> previous;
            try { previous=await _healthy.DownloadContentAsync(cancellationToken); }
            catch(RequestFailedException absent) when(absent.Status==404 && absent.ErrorCode=="BlobNotFound") {
                await WriteHistory(_healthy,_owner.Winner,true);
                throw;
            }
            await WriteHistory(_healthy,_owner.Winner,true);
            return previous;
        }
        public override Task<Response<BlobContentInfo>> UploadAsync(BinaryData content,BlobUploadOptions options,CancellationToken cancellationToken=default)
        {_owner.Writes++;return _healthy.UploadAsync(content,options,cancellationToken);}
    }

    [DataTestMethod] [DataRow(false,false)] [DataRow(false,true)] [DataRow(true,false)] [DataRow(true,true)]
    public async Task PublicConditionalHistoryRejectsStaleOrRacingCreateWithoutReplacingWinner(bool existing,bool synchronous)
    {
        RequireLive();_provider.IsVersionControlEnabled=true;
        var id=Guid.NewGuid();var original=new Holon {Id=id,Name="owned-original",IsActive=true};
        if(existing) Assert.IsFalse((await _provider.SaveHolonAsync(original,false)).IsError);
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;
        var race=new HistoryRaceService {Healthy=healthy,Winner=new Holon {Id=id,Name="owned-winner",IsActive=true}};
        var loser=new Holon {Id=id,Name="owned-loser",IsActive=true};
        Core.Interfaces.IHolon? resultRow;
        try {
            ServiceField.SetValue(_provider,race);
            var result=synchronous ? _provider.SaveHolon(loser,false):await _provider.SaveHolonAsync(loser,false);
            Assert.IsTrue(result.IsError,result.Message);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
            Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
            Assert.IsInstanceOfType(result.Exception,typeof(RequestFailedException));
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreEqual(1,race.Writes);
            Assert.AreEqual(0,loser.Version);Assert.AreEqual(Guid.Empty,loser.VersionId);
        }
        finally {ServiceField.SetValue(_provider,healthy);}
        var read=await _provider.LoadHolonAsync(id,loadChildren:false);
        Assert.IsFalse(read.IsError,read.Message);resultRow=read.Result;
        Assert.AreEqual("owned-winner",resultRow.Name);Assert.AreEqual(existing?2:1,resultRow.Version);
        if(existing) {
            var old=await _provider.LoadHolonAsync(id,loadChildren:false,version:1);
            Assert.IsFalse(old.IsError,old.Message);Assert.AreEqual("owned-original",old.Result.Name);
            Assert.AreEqual(original.VersionId,old.Result.VersionId);
        }
        Assert.IsFalse((await _provider.DeleteHolonAsync(id)).IsError);
        Assert.IsFalse((await healthy.GetBlobContainerClient(_containers[0]).GetBlobClient(id.ToString()).ExistsAsync()).Value);
    }
}

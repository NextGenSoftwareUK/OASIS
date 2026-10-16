using System;
using System.Threading;
using System.Threading.Tasks;
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
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeHolonDeleteReturnsAcknowledgedOriginalAndStructuredAbsence(bool synchronous,bool customKey)
    {
        var value=new Holon {Id=Guid.NewGuid(),Name="owned-original",IsActive=true,ParentHolonId=Guid.NewGuid()};
        if(customKey) value.CustomKey="owned-custom-"+value.Id;
        value.MetaData["retained-field"]="retained-value";
        Assert.IsFalse((await _provider.SaveHolonAsync(value,saveChildren:false)).IsError);
        var key=customKey ? value.CustomKey:value.Id.ToString();var blob=_service.GetBlobContainerClient(_containers[0]).GetBlobClient(key);
        try
        {
            var removed=customKey ? synchronous ? _provider.DeleteHolon(key):await _provider.DeleteHolonAsync(key)
                :synchronous ? _provider.DeleteHolon(value.Id):await _provider.DeleteHolonAsync(value.Id);
            Assert.IsFalse(removed.IsError,removed.Message);Assert.IsTrue(removed.IsSaved);Assert.IsTrue(removed.IsDeleted);
            Assert.AreEqual(value.Id,removed.Result.Id);Assert.AreEqual(value.Name,removed.Result.Name);
            Assert.AreEqual(value.ParentHolonId,removed.Result.ParentHolonId);Assert.AreEqual("retained-value",removed.Result.MetaData["retained-field"]);
            Assert.IsFalse((await blob.ExistsAsync()).Value);
            var missing=customKey ? synchronous ? _provider.DeleteHolon(key):await _provider.DeleteHolonAsync(key)
                :synchronous ? _provider.DeleteHolon(value.Id):await _provider.DeleteHolonAsync(value.Id);
            Assert.IsTrue(missing.IsError);Assert.IsFalse(missing.IsSaved);Assert.IsNull(missing.Result);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);
        }
        finally {await blob.DeleteIfExistsAsync();}
    }

    private sealed class HolonRaceService : BlobServiceClient
    {
        public required BlobServiceClient Healthy;public required string Target;public int Reads,Deletes;
        public HolonRaceService():base("UseDevelopmentStorage=true") { }
        public override BlobContainerClient GetBlobContainerClient(string name)=>new HolonRaceContainer(Healthy.GetBlobContainerClient(name),this);
    }
    private sealed class HolonRaceContainer : BlobContainerClient
    {
        private readonly BlobContainerClient _healthy;private readonly HolonRaceService _owner;
        public HolonRaceContainer(BlobContainerClient healthy,HolonRaceService owner):base("UseDevelopmentStorage=true",healthy.Name) {_healthy=healthy;_owner=owner;}
        public override BlobClient GetBlobClient(string name)=>name==_owner.Target ? new HolonRaceBlob(_healthy.GetBlobClient(name),_owner):_healthy.GetBlobClient(name);
    }
    private sealed class HolonRaceBlob : BlobClient
    {
        private readonly BlobClient _healthy;private readonly HolonRaceService _owner;
        public HolonRaceBlob(BlobClient healthy,HolonRaceService owner):base("UseDevelopmentStorage=true",healthy.BlobContainerName,healthy.Name) {_healthy=healthy;_owner=owner;}
        public override async Task<Response<BlobDownloadResult>> DownloadContentAsync(CancellationToken cancellationToken=default)
        {
            var observed=await _healthy.DownloadContentAsync(cancellationToken);
            if(Interlocked.Increment(ref _owner.Reads)==1)
            {
                var newer=OasisJson.Deserialize<Holon>(observed.Value.Content.ToString());newer.Name="owned-newer";
                await _healthy.UploadAsync(BinaryData.FromString(OasisJson.Serialize(newer)),overwrite:true,cancellationToken);
            }
            return observed;
        }
        public override Task<Response> DeleteAsync(DeleteSnapshotsOption snapshotsOption=DeleteSnapshotsOption.None,BlobRequestConditions conditions=null,CancellationToken cancellationToken=default)
        {_owner.Deletes++;return _healthy.DeleteAsync(snapshotsOption,conditions,cancellationToken);}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeConcurrentHolonUpdatePreventsStaleDeletion(bool synchronous)
    {
        var value=new Holon {Id=Guid.NewGuid(),Name="owned-original",IsActive=true};
        Assert.IsFalse((await _provider.SaveHolonAsync(value,saveChildren:false)).IsError);
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;
        var race=new HolonRaceService {Healthy=healthy,Target=value.Id.ToString()};
        var blob=healthy.GetBlobContainerClient(_containers[0]).GetBlobClient(value.Id.ToString());
        try
        {
            ServiceField.SetValue(_provider,race);
            var result=synchronous ? _provider.DeleteHolon(value.Id):await _provider.DeleteHolonAsync(value.Id);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
            Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
            Assert.IsInstanceOfType(result.Exception,typeof(RequestFailedException));Assert.AreEqual(412,((RequestFailedException)result.Exception).Status);
            Assert.AreEqual(1,race.Reads);Assert.AreEqual(1,race.Deletes);
            var native=OasisJson.Deserialize<Holon>((await blob.DownloadContentAsync()).Value.Content.ToString());
            Assert.AreEqual(value.Id,native.Id);Assert.AreEqual("owned-newer",native.Name);
        }
        finally {ServiceField.SetValue(_provider,healthy);await blob.DeleteIfExistsAsync();}
    }
}

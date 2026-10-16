using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
    private sealed class LostUploadService : BlobServiceClient
    {
        public required BlobServiceClient Healthy;
        public readonly IOException Cause=new("Controlled loss after real SDK blob upload acknowledgement.");
        public string Container="",Blob="";public int Commits;
        public int FailOnCommit=1;
        public LostUploadService():base("UseDevelopmentStorage=true") { }
        public override BlobContainerClient GetBlobContainerClient(string name)=>new LostUploadContainer(Healthy.GetBlobContainerClient(name),this);
    }
    private sealed class LostUploadContainer : BlobContainerClient
    {
        private readonly BlobContainerClient _healthy;private readonly LostUploadService _owner;
        public LostUploadContainer(BlobContainerClient healthy,LostUploadService owner):base("UseDevelopmentStorage=true",healthy.Name) {_healthy=healthy;_owner=owner;}
        public override BlobClient GetBlobClient(string name)=>new LostUploadBlob(_healthy.GetBlobClient(name),_owner);
    }
    private sealed class LostUploadBlob : BlobClient
    {
        private readonly BlobClient _healthy;private readonly LostUploadService _owner;
        public LostUploadBlob(BlobClient healthy,LostUploadService owner):base("UseDevelopmentStorage=true",healthy.BlobContainerName,healthy.Name) {_healthy=healthy;_owner=owner;}
        private Response<BlobContentInfo> Lose(Response<BlobContentInfo> response)
        {
            Assert.AreEqual(201,response.GetRawResponse().Status);_owner.Commits++;_owner.Container=_healthy.BlobContainerName;_owner.Blob=_healthy.Name;
            if(_owner.Commits==_owner.FailOnCommit) throw _owner.Cause;
            return response;
        }
        public override async Task<Response<BlobContentInfo>> UploadAsync(Stream content,bool overwrite=false,CancellationToken cancellationToken=default)
            =>Lose(await _healthy.UploadAsync(content,overwrite,cancellationToken));
        public override async Task<Response<BlobContentInfo>> UploadAsync(BinaryData content,bool overwrite=false,CancellationToken cancellationToken=default)
            =>Lose(await _healthy.UploadAsync(content,overwrite,cancellationToken));
    }

    [DataTestMethod]
    [DataRow(0,false)] [DataRow(0,true)] [DataRow(1,false)] [DataRow(1,true)] [DataRow(2,false)] [DataRow(2,true)]
    public async Task RealCommittedUploadWithLostAcknowledgementDoesNotPublishCallerIdentity(int kind,bool synchronous)
    {
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;
        var fault=new LostUploadService {Healthy=healthy};
        var profile=new Avatar {Username="owned-upload-profile",IsActive=true};
        var detail=new AvatarDetail {Username="owned-upload-detail",IsActive=true,XP=321};
        var holon=new Holon {Name="owned-upload-holon",IsActive=true};
        Exception cause;bool error,saved;
        try
        {
            ServiceField.SetValue(_provider,fault);
            if(kind==0)
            {
                var result=synchronous ? _provider.SaveAvatar(profile):await _provider.SaveAvatarAsync(profile);
                cause=result.Exception;error=result.IsError;saved=result.IsSaved;
            }
            else if(kind==1)
            {
                var result=synchronous ? _provider.SaveAvatarDetail(detail):await _provider.SaveAvatarDetailAsync(detail);
                cause=result.Exception;error=result.IsError;saved=result.IsSaved;
            }
            else
            {
                var result=synchronous ? _provider.SaveHolon(holon,saveChildren:false):await _provider.SaveHolonAsync(holon,saveChildren:false);
                cause=result.Exception;error=result.IsError;saved=result.IsSaved;
            }
            Assert.IsTrue(error);Assert.IsFalse(saved);Assert.AreEqual(1,fault.Commits);
            Assert.IsInstanceOfType(cause,typeof(AzureBlobMutationReconciliationException));Assert.AreSame(fault.Cause,cause.GetBaseException());
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(cause));
            var evidence=(AzureBlobMutationReconciliationException)cause;
            Assert.AreEqual(fault.Container,evidence.ContainerName);Assert.AreEqual(fault.Blob,evidence.BlobName);
            Assert.IsTrue(Guid.TryParse(evidence.BlobName,out var attempted));Assert.AreNotEqual(Guid.Empty,attempted);
            var json=(await healthy.GetBlobContainerClient(fault.Container).GetBlobClient(fault.Blob).DownloadContentAsync()).Value.Content.ToString();
            var persisted=kind==0 ? OasisJson.Deserialize<Avatar>(json).Id:kind==1 ? OasisJson.Deserialize<AvatarDetail>(json).Id:OasisJson.Deserialize<Holon>(json).Id;
            Assert.AreEqual(attempted,persisted);
            Assert.AreEqual(Guid.Empty,profile.Id);Assert.AreEqual(Guid.Empty,detail.Id);Assert.AreEqual(Guid.Empty,holon.Id);
            Assert.IsFalse(profile.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.AzureStorageOASIS));
            Assert.IsFalse(detail.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.AzureStorageOASIS));
            Assert.IsFalse(holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.AzureStorageOASIS));
        }
        finally
        {
            ServiceField.SetValue(_provider,healthy);
            if(fault.Commits==1) await healthy.GetBlobContainerClient(fault.Container).GetBlobClient(fault.Blob).DeleteAsync();
        }
    }
}

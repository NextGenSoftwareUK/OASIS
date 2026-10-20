using System.Reflection;
using System.IO;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests;

[TestClass]
public class AzureConditionalHistoryWriterTests
{
    private sealed class SdkResponse : Response
    {
        public override int Status => 201;
        public override string ReasonPhrase => "Created";
        public override Stream? ContentStream { get; set; }
        public override string ClientRequestId { get; set; } = "controlled";
        public override void Dispose() { }
        protected override bool ContainsHeader(string name) => false;
        protected override IEnumerable<HttpHeader> EnumerateHeaders() => Array.Empty<HttpHeader>();
        protected override bool TryGetHeader(string name, out string value) { value = ""; return false; }
        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values) { values = Array.Empty<string>(); return false; }
    }

    private sealed class ObservedBlob : BlobClient
    {
        public Holon? Existing;
        public IHolonBase? ExistingRecord;
        public string ReadEtag = "original-revision";
        public string AckEtag = "committed-revision";
        public Exception? ReadFailure;
        public Exception? WriteFailure;
        public int Reads,Writes,Deletes;
        public string RecordName="owned-record";
        public BlobUploadOptions? Options;
        public BinaryData? Payload;
        public Action? OnRead,OnWrite;
        public override string Name => RecordName;
        public override string BlobContainerName => "owned-container";
        public override Task<Response<BlobDownloadResult>> DownloadContentAsync(CancellationToken cancellationToken = default)
        {
            Reads++;
            OnRead?.Invoke();
            if (ReadFailure != null) throw ReadFailure;
            if (Existing == null&&ExistingRecord==null) throw new RequestFailedException(404, "Absent", "BlobNotFound", null);
            return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobDownloadResult(
                BinaryData.FromString(OasisJson.Serialize(ExistingRecord??Existing)), BlobsModelFactory.BlobDownloadDetails(eTag: new ETag(ReadEtag))), new SdkResponse()));
        }
        public override Task<Response<BlobContentInfo>> UploadAsync(BinaryData content, BlobUploadOptions options, CancellationToken cancellationToken = default)
        {
            Writes++; Payload = content; Options = options;
            if (WriteFailure != null) throw WriteFailure;
            OnWrite?.Invoke();
            return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobContentInfo(new ETag(AckEtag), DateTimeOffset.UtcNow, Array.Empty<byte>(), null, null, null, 0), new SdkResponse()));
        }
        public override Task<Response> DeleteAsync(DeleteSnapshotsOption snapshotsOption=DeleteSnapshotsOption.None,BlobRequestConditions? conditions=null,CancellationToken cancellationToken=default)
        {Deletes++;return Task.FromResult<Response>(new SdkResponse());}
    }

    private sealed class Container(BlobClient blob):BlobContainerClient
    {
        public override BlobClient GetBlobClient(string name)=>blob;
        public override AsyncPageable<BlobItem> GetBlobsAsync(BlobTraits traits=BlobTraits.None,BlobStates states=BlobStates.None,string? prefix=null,CancellationToken cancellationToken=default)
            =>AsyncPageable<BlobItem>.FromPages(new[]{Page<BlobItem>.FromValues(new[]{BlobsModelFactory.BlobItem(name:blob.Name)},null,new SdkResponse())});
    }

    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(3,false)][DataRow(4,false)][DataRow(5,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)][DataRow(3,true)][DataRow(4,true)][DataRow(5,true)]
    public async Task StandaloneDetailDeletionUsesObservedRevisionWithoutAvatarAccess(int route,bool hard)
    {
        var row=new AvatarDetail{Id=Guid.NewGuid(),Username="owned-detail",Email="owned-detail@example.invalid",IsActive=true,Version=1,Karma=123,XP=456};
        var blob=new ObservedBlob{ExistingRecord=row,RecordName=row.Id.ToString()};var service=new Service(blob,"oasis-avatar-details");
        var provider=new AzureStorageOASIS("UseDevelopmentStorage=true"){IsVersionControlEnabled=true};
        typeof(AzureStorageOASIS).GetField("_serviceClient",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,service);IOASISStorageProvider contract=provider;
        var result=route switch{
            0=>await contract.DeleteAvatarDetailAsync(row.Id,!hard),1=>contract.DeleteAvatarDetail(row.Id,!hard),
            2=>await contract.DeleteAvatarDetailByEmailAsync(row.Email,!hard),3=>contract.DeleteAvatarDetailByEmail(row.Email,!hard),
            4=>await contract.DeleteAvatarDetailByUsernameAsync(row.Username,!hard),_=>contract.DeleteAvatarDetailByUsername(row.Username,!hard)};
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.Result);Assert.IsTrue(result.IsDeleted);Assert.AreEqual(0,service.MissingReads);
        Assert.AreEqual(route<2?1:2,blob.Reads);Assert.AreEqual(hard?0:1,blob.Writes);Assert.AreEqual(hard?1:0,blob.Deletes);
        if(!hard){
            Assert.AreEqual(new ETag(blob.ReadEtag),blob.Options!.Conditions.IfMatch);
            var actual=OasisJson.DeserializeStored<AvatarDetail>(blob.Payload!.ToString());Assert.IsFalse(actual.IsActive);Assert.AreNotEqual(DateTime.MinValue,actual.DeletedDate);Assert.AreEqual(123L,actual.Karma);Assert.AreEqual(456L,actual.XP);
        }
    }
    private sealed class MissingBlob(Action read):BlobClient
    {public override Task<Response<BlobDownloadResult>> DownloadContentAsync(CancellationToken cancellationToken=default){read();throw new RequestFailedException(404,"Absent","BlobNotFound",null);}}
    private sealed class Service(BlobClient blob,string? onlyContainer=null):BlobServiceClient
    {
        public int MissingReads;
        public override BlobContainerClient GetBlobContainerClient(string name)=>new Container(onlyContainer==null||name==onlyContainer?blob:new MissingBlob(()=>MissingReads++));
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task IdentityHardDeletionCanResolveExistingAvatarTombstone(bool email,bool synchronous)
    {
        var row=new Avatar{Id=Guid.NewGuid(),Username="owned-tombstone",Email="owned-tombstone@example.invalid",IsActive=false,DeletedDate=DateTime.UtcNow};
        var blob=new ObservedBlob{ExistingRecord=row,RecordName=row.Id.ToString()};var service=new Service(blob,"oasis-avatars");
        var provider=new AzureStorageOASIS("UseDevelopmentStorage=true");typeof(AzureStorageOASIS).GetField("_serviceClient",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,service);
        var result=email?synchronous?provider.DeleteAvatarByEmail(row.Email,false):await provider.DeleteAvatarByEmailAsync(row.Email,false)
            :synchronous?provider.DeleteAvatarByUsername(row.Username,false):await provider.DeleteAvatarByUsernameAsync(row.Username,false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.Result);Assert.IsTrue(result.IsDeleted);Assert.AreEqual(1,blob.Deletes);Assert.AreEqual(0,blob.Writes);
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task IdentityDeletionRefusesReassignedCanonicalAvatar(bool email,bool softDelete,bool synchronous)
    {
        var original=new Avatar{Id=Guid.NewGuid(),Username="owned-original",Email="owned-original@example.invalid"};
        var changed=new Avatar{Id=original.Id,Username="different-owner",Email="different-owner@example.invalid",IsActive=true};
        var blob=new ObservedBlob{RecordName=original.Id.ToString()};blob.OnRead=()=>blob.ExistingRecord=blob.Reads==1?original:changed;
        var service=new Service(blob,"oasis-avatars");var provider=new AzureStorageOASIS("UseDevelopmentStorage=true");
        typeof(AzureStorageOASIS).GetField("_serviceClient",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,service);
        var result=email?synchronous?provider.DeleteAvatarByEmail(original.Email,softDelete):await provider.DeleteAvatarByEmailAsync(original.Email,softDelete)
            :synchronous?provider.DeleteAvatarByUsername(original.Username,softDelete):await provider.DeleteAvatarByUsernameAsync(original.Username,softDelete);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);
        Assert.IsInstanceOfType(result.Exception,typeof(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException));
        Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
        Assert.AreEqual(0,service.MissingReads);Assert.AreEqual(0,blob.Writes);Assert.AreEqual(0,blob.Deletes);
    }
    private static AzureStorageOASIS PublicProvider(BlobClient blob)
    {
        var provider=new AzureStorageOASIS("UseDevelopmentStorage=true"){IsVersionControlEnabled=true};
        typeof(AzureStorageOASIS).GetField("_serviceClient",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,new Service(blob));return provider;
    }

    [DataTestMethod]
    [DataRow(0,false,false)][DataRow(1,false,false)][DataRow(2,false,false)]
    [DataRow(0,true,false)][DataRow(1,true,false)][DataRow(2,true,false)]
    [DataRow(0,false,true)][DataRow(1,false,true)][DataRow(2,false,true)]
    [DataRow(0,true,true)][DataRow(1,true,true)][DataRow(2,true,true)]
    public async Task PublicSaveNeverOverwritesNewerCallerState(int kind,bool synchronous,bool afterWrite)
    {
        IHolonBase source=kind==0?new Holon():kind==1?new Avatar():new AvatarDetail();source.Version=1;source.VersionId=Guid.NewGuid();var beforeVersionId=source.VersionId;
        var blob=new ObservedBlob();Action edit=()=>source.Version=777;if(afterWrite)blob.OnWrite=edit;else blob.OnRead=edit;
        var provider=PublicProvider(blob);
        async Task<(bool Error,bool Saved,object? Result,Exception? Cause)> Save(){
            if(kind==0){var result=synchronous?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false);return(result.IsError,result.IsSaved,result.Result,result.Exception);}
            if(kind==1){var result=synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);return(result.IsError,result.IsSaved,result.Result,result.Exception);}
            var detail=synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);return(detail.IsError,detail.IsSaved,detail.Result,detail.Exception);
        }
        var actual=await Save();Assert.IsTrue(actual.Error);Assert.IsFalse(actual.Saved);Assert.IsNull(actual.Result);
        Assert.AreEqual(777,source.Version);Assert.AreEqual(beforeVersionId,source.VersionId);Assert.AreEqual(Guid.Empty,source.Id);
        Assert.IsFalse(source.ProviderUniqueStorageKey.ContainsKey(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.AzureStorageOASIS));
        Assert.AreEqual(afterWrite?1:0,blob.Writes);
        if(afterWrite)Assert.IsInstanceOfType(actual.Cause,typeof(AzureBlobMutationReconciliationException));else Assert.IsInstanceOfType(actual.Cause,typeof(InvalidOperationException));
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task GraphAcknowledgementDoesNotPublishOverEditedOriginal(bool synchronous,bool editChild)
    {
        var child=new Holon();var root=new Holon{Children=new List<IHolon>{child}};var edited=editChild?child:root;
        var blob=new ObservedBlob{OnWrite=()=>edited.Version=777};var provider=PublicProvider(blob);
        var result=synchronous?provider.SaveHolons(new[]{root}):await provider.SaveHolonsAsync(new[]{root});
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(777,edited.Version);Assert.AreEqual(1,blob.Writes);
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
        Assert.AreEqual(Guid.Empty,child.Id);if(editChild)Assert.AreNotEqual(Guid.Empty,root.Id);else Assert.AreEqual(Guid.Empty,root.Id);
    }

    private static Task<Holon> Write(BlobClient blob, Holon record)
        => (Task<Holon>)typeof(AzureStorageOASIS).GetMethod("WriteVersionedBlobAsync", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(Holon)).Invoke(null, new object?[] { blob, record, true, null })!;

    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task ExactlyOneConditionalWritePublishesOnlyDetachedAcknowledgedVersion(bool existing)
    {
        var record = new Holon { Id = Guid.NewGuid() };
        var blob = new ObservedBlob { Existing = existing ? new Holon { Id = record.Id, Version = 4, VersionId = Guid.NewGuid() } : null };
        var committed = await Write(blob, record);
        Assert.AreEqual(1, blob.Writes);
        Assert.AreEqual(existing ? 5 : 1, committed.Version);
        Assert.AreEqual(0, record.Version);
        Assert.AreEqual(Guid.Empty, record.VersionId);
        Assert.AreEqual(existing ? new ETag(blob.ReadEtag) : (ETag?)null, blob.Options!.Conditions.IfMatch);
        Assert.AreEqual(existing ? (ETag?)null : ETag.All, blob.Options.Conditions.IfNoneMatch);
    }

    [TestMethod]
    public async Task ConditionalRejectionPreservesExactCauseAndDoesNotRetry()
    {
        var cause = new RequestFailedException(412, "Revision changed", "ConditionNotMet", null);
        var blob = new ObservedBlob { WriteFailure = cause };
        var failure = await Assert.ThrowsExceptionAsync<RequestFailedException>(() => Write(blob, new Holon { Id = Guid.NewGuid() }));
        Assert.AreSame(cause, failure); Assert.AreEqual(1, blob.Writes);
    }

    [TestMethod]
    public async Task LostAcknowledgementIsFencedWithoutRetryOrCallerPublication()
    {
        var cause = new IOException("Controlled transport loss");
        var record = new Holon { Id = Guid.NewGuid() };
        var blob = new ObservedBlob { WriteFailure = cause };
        var failure = await Assert.ThrowsExceptionAsync<AzureBlobMutationReconciliationException>(() => Write(blob, record));
        Assert.AreSame(cause, failure.InnerException); Assert.AreEqual(1, blob.Writes);
        Assert.AreEqual(0, record.Version); Assert.AreEqual(Guid.Empty, record.VersionId);
    }

    [TestMethod]
    public async Task MissingAcknowledgementRevisionIsNotSuccess()
    {
        var blob = new ObservedBlob { AckEtag = "" };
        var failure = await Assert.ThrowsExceptionAsync<AzureBlobMutationReconciliationException>(() => Write(blob, new Holon { Id = Guid.NewGuid() }));
        Assert.IsInstanceOfType(failure.InnerException, typeof(InvalidDataException)); Assert.AreEqual(1, blob.Writes);
    }

    [TestMethod]
    public async Task ReadFailureOtherThanExactBlobAbsenceNeverBecomesCreate()
    {
        var cause = new RequestFailedException(404, "Container absent", "ContainerNotFound", null);
        var blob = new ObservedBlob { ReadFailure = cause };
        var failure = await Assert.ThrowsExceptionAsync<RequestFailedException>(() => Write(blob, new Holon { Id = Guid.NewGuid() }));
        Assert.AreSame(cause, failure); Assert.AreEqual(0, blob.Writes);
    }

    [TestMethod]
    public async Task MissingObservedRevisionRefusesWrite()
    {
        var record = new Holon { Id = Guid.NewGuid() };
        var blob = new ObservedBlob { Existing = record, ReadEtag = "" };
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => Write(blob, record));
        Assert.AreEqual(0, blob.Writes);
    }
}

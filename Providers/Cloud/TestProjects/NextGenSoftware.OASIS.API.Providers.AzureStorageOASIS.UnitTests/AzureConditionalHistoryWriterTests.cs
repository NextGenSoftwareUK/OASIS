using System.Reflection;
using System.IO;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
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
        public string ReadEtag = "original-revision";
        public string AckEtag = "committed-revision";
        public Exception? ReadFailure;
        public Exception? WriteFailure;
        public int Writes;
        public BlobUploadOptions? Options;
        public BinaryData? Payload;
        public override string Name => "owned-record";
        public override string BlobContainerName => "owned-container";
        public override Task<Response<BlobDownloadResult>> DownloadContentAsync(CancellationToken cancellationToken = default)
        {
            if (ReadFailure != null) throw ReadFailure;
            if (Existing == null) throw new RequestFailedException(404, "Absent", "BlobNotFound", null);
            return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobDownloadResult(
                BinaryData.FromString(OasisJson.Serialize(Existing)), BlobsModelFactory.BlobDownloadDetails(eTag: new ETag(ReadEtag))), new SdkResponse()));
        }
        public override Task<Response<BlobContentInfo>> UploadAsync(BinaryData content, BlobUploadOptions options, CancellationToken cancellationToken = default)
        {
            Writes++; Payload = content; Options = options;
            if (WriteFailure != null) throw WriteFailure;
            return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobContentInfo(new ETag(AckEtag), DateTimeOffset.UtcNow, Array.Empty<byte>(), null, null, null, 0), new SdkResponse()));
        }
    }

    private static Task<Holon> Write(BlobClient blob, Holon record)
        => (Task<Holon>)typeof(AzureStorageOASIS).GetMethod("WriteVersionedBlobAsync", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(Holon)).Invoke(null, new object[] { blob, record, true })!;

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.S3;
using Backend = NextGenSoftware.OASIS.Providers.Shared.S3.S3ObjectBackend;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

// Deterministic SDK-boundary controls, not native/cloud acceptance.
[TestClass]
public class SharedS3TransportTests
{
    private sealed class Client : AmazonS3Client
    {
        public Client(int retry = 0) : base(new AnonymousAWSCredentials(), new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.USEast1, MaxErrorRetry = retry }) { }
        public Exception Error;
        public string IfMatch, IfNoneMatch, Key, Prefix;
        public bool BadChecksum, BadStatus;
        public int Calls;
        public GetObjectResponse Get = new() { HttpStatusCode = HttpStatusCode.OK, ETag = "opaque", ContentLength = 5, ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes("value")) };
        public readonly Queue<ListObjectsV2Response> Pages = new();
        private void Called() { Calls++; if (Error != null) throw Error; }
        public override Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request, CancellationToken token = default)
        { Called(); Key = request.Key; return Task.FromResult(Get); }
        public override Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken token = default)
        {
            Called(); IfMatch = request.IfMatch; IfNoneMatch = request.IfNoneMatch; Key = request.Key;
            return Task.FromResult(new PutObjectResponse { HttpStatusCode = BadStatus ? HttpStatusCode.Accepted : HttpStatusCode.OK,
                ETag = "opaque", ChecksumSHA256 = BadChecksum ? "bad" : request.ChecksumSHA256 });
        }
        public override Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken token = default)
        { Called(); IfMatch = request.IfMatch; Key = request.Key; return Task.FromResult(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent }); }
        public override Task<ListObjectsV2Response> ListObjectsV2Async(ListObjectsV2Request request, CancellationToken token = default)
        { Called(); Prefix = request.Prefix; return Task.FromResult(Pages.Dequeue()); }
        public override Task<HeadBucketResponse> HeadBucketAsync(HeadBucketRequest request, CancellationToken token = default)
        { Called(); return Task.FromResult(new HeadBucketResponse { HttpStatusCode = HttpStatusCode.OK }); }
    }
    private static AmazonS3Exception Error(HttpStatusCode status, string code) => new(code) { StatusCode = status, ErrorCode = code };

    [TestMethod]
    public void Automatic_mutation_retries_are_rejected()
    {
        using var client = new Client(1);
        Assert.ThrowsException<ArgumentException>(() => new Backend(client, "bucket"));
        Assert.AreEqual(0, client.Calls);
    }
    [TestMethod]
    public async Task Observation_binds_value_and_revision_to_one_get()
    {
        using var client = new Client();
        var result = await new Backend(client, "bucket", "scope/").ReadObservedAsync("key");
        Assert.AreEqual("value", result.Value); Assert.AreEqual("opaque", result.Revision);
        Assert.AreEqual("scope/key", client.Key); Assert.AreEqual(1, client.Calls);
    }
    [TestMethod]
    public async Task Missing_key_is_absent_but_missing_bucket_errors()
    {
        using var client = new Client { Error = Error(HttpStatusCode.NotFound, "NoSuchKey") };
        var backend = new Backend(client, "bucket"); Assert.IsNull(await backend.ReadObservedAsync("key"));
        client.Error = Error(HttpStatusCode.NotFound, "NoSuchBucket");
        Assert.AreSame(client.Error, await Assert.ThrowsExceptionAsync<AmazonS3Exception>(() => backend.ReadObservedAsync("key")));
    }
    [TestMethod]
    public async Task Invalid_utf8_is_not_decoded_as_replacement_text()
    {
        using var client = new Client(); client.Get.ResponseStream = new MemoryStream(new byte[] { 0xff });
        await Assert.ThrowsExceptionAsync<DecoderFallbackException>(() => new Backend(client, "bucket").GetAsync("key"));
    }
    [TestMethod]
    public async Task Partial_get_cannot_publish_a_record()
    {
        using var client = new Client(); client.Get.ContentRange = "bytes 0-4/100";
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => new Backend(client, "bucket").GetAsync("key"));
    }
    [TestMethod]
    public async Task Body_length_mismatch_cannot_publish_a_record()
    {
        using var client = new Client(); client.Get.ContentLength = 100;
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => new Backend(client, "bucket").GetAsync("key"));
    }
    [TestMethod]
    public async Task Missing_observed_revision_is_corruption()
    {
        using var client = new Client(); client.Get.ETag = "";
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => new Backend(client, "bucket").GetAsync("key"));
    }
    [TestMethod]
    public async Task Preconditions_are_sent_to_native_sdk_without_read_then_write()
    {
        using var client = new Client(); var backend = new Backend(client, "bucket", "scope/");
        await backend.CreateOnlyAsync("key", "value"); Assert.AreEqual("*", client.IfNoneMatch); Assert.IsNull(client.IfMatch);
        await backend.ReplaceUnchangedAsync("key", "value2", "revision"); Assert.AreEqual("revision", client.IfMatch); Assert.IsNull(client.IfNoneMatch);
        await backend.DeleteUnchangedAsync("key", "revision2"); Assert.AreEqual("revision2", client.IfMatch);
        Assert.AreEqual(3, client.Calls); Assert.AreEqual("scope/key", client.Key);
    }
    [DataTestMethod]
    [DataRow(412, "PreconditionFailed")]
    [DataRow(409, "ConditionalRequestConflict")]
    [DataRow(404, "NoSuchKey")]
    public async Task Known_conditional_rejections_are_conflicts(int status, string code)
    {
        using var client = new Client { Error = Error((HttpStatusCode)status, code) }; var backend = new Backend(client, "bucket");
        var result = await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(() => backend.ReplaceUnchangedAsync("key", "value", "revision"));
        Assert.AreSame(client.Error, result.Data["NativeCause"]); Assert.AreEqual(1, client.Calls);
    }
    [TestMethod]
    public async Task Permission_failure_remains_original_error()
    {
        using var client = new Client { Error = Error(HttpStatusCode.Forbidden, "AccessDenied") };
        Assert.AreSame(client.Error, await Assert.ThrowsExceptionAsync<AmazonS3Exception>(() => new Backend(client, "bucket").PutAsync("key", "value")));
    }
    [TestMethod]
    public async Task Transport_loss_requires_reconciliation_without_replay()
    {
        using var client = new Client { Error = new IOException("response lost") };
        var error = await Assert.ThrowsExceptionAsync<S3MutationReconciliationException>(() => new Backend(client, "bucket", "scope/").PutAsync("key", "value"));
        Assert.AreSame(client.Error, error.InnerException); Assert.AreEqual("scope/key", error.ObjectName);
        Assert.AreEqual("bucket", error.BucketName);
        Assert.AreEqual(string.Empty, error.OriginalTransactionId); Assert.AreEqual(1, client.Calls);
    }
    [TestMethod]
    public async Task Delete_transport_loss_also_requires_reconciliation()
    {
        using var client = new Client { Error = new IOException("response lost") };
        var error = await Assert.ThrowsExceptionAsync<S3MutationReconciliationException>(() => new Backend(client, "bucket").DeleteUnchangedAsync("key", "revision"));
        Assert.AreSame(client.Error, error.InnerException); Assert.AreEqual(1, client.Calls);
    }
    [TestMethod]
    public async Task Conditional_delete_disappearance_is_a_conflict()
    {
        using var client = new Client { Error = Error(HttpStatusCode.NotFound, "NoSuchKey") };
        await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(() => new Backend(client, "bucket").DeleteUnchangedAsync("key", "revision"));
        Assert.AreEqual(1, client.Calls);
    }
    [TestMethod]
    public async Task Unconditional_delete_of_absent_key_succeeds()
    {
        using var client = new Client { Error = Error(HttpStatusCode.NotFound, "NoSuchKey") };
        await new Backend(client, "bucket").DeleteAsync("key"); Assert.AreEqual(1, client.Calls);
    }
    [DataTestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public async Task Malformed_write_acknowledgement_is_uncertain(bool checksum, bool status)
    {
        using var client = new Client { BadChecksum = checksum, BadStatus = status };
        await Assert.ThrowsExceptionAsync<S3MutationReconciliationException>(() => new Backend(client, "bucket").PutAsync("key", "value"));
        Assert.AreEqual(1, client.Calls);
    }
    [TestMethod]
    public async Task Precancelled_mutation_does_not_call_sdk()
    {
        using var client = new Client(); using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => new Backend(client, "bucket").PutAsync("key", "value", cancel.Token));
        Assert.AreEqual(0, client.Calls);
    }
    [TestMethod]
    public async Task Listing_strips_only_owned_prefix_and_follows_pages()
    {
        using var client = new Client();
        client.Pages.Enqueue(new() { HttpStatusCode = HttpStatusCode.OK, IsTruncated = true, NextContinuationToken = "next", S3Objects = new() { new() { Key = "scope/prefix/a" } } });
        client.Pages.Enqueue(new() { HttpStatusCode = HttpStatusCode.OK, S3Objects = new() { new() { Key = "scope/prefix/b" } } });
        var result = await new Backend(client, "bucket", "scope/").ListKeysAsync("prefix/");
        CollectionAssert.AreEqual(new[] { "prefix/a", "prefix/b" }, new List<string>(result));
        Assert.AreEqual("scope/prefix/", client.Prefix); Assert.AreEqual(2, client.Calls);
    }
    [TestMethod]
    public async Task Out_of_scope_listing_is_not_published()
    {
        using var client = new Client();
        client.Pages.Enqueue(new() { HttpStatusCode = HttpStatusCode.OK, S3Objects = new() { new() { Key = "other/key" } } });
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => new Backend(client, "bucket", "scope/").ListKeysAsync("prefix/"));
    }
    [TestMethod]
    public async Task Duplicate_listing_is_rejected()
    {
        using var client = new Client();
        client.Pages.Enqueue(new() { HttpStatusCode = HttpStatusCode.OK, S3Objects = new() { new() { Key = "key" }, new() { Key = "key" } } });
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => new Backend(client, "bucket").ListKeysAsync(""));
    }
    [TestMethod]
    public async Task Repeated_continuation_is_rejected_before_replay()
    {
        using var client = new Client();
        client.Pages.Enqueue(new() { HttpStatusCode = HttpStatusCode.OK, IsTruncated = true, NextContinuationToken = "same", S3Objects = new() });
        client.Pages.Enqueue(new() { HttpStatusCode = HttpStatusCode.OK, IsTruncated = true, NextContinuationToken = "same", S3Objects = new() });
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => new Backend(client, "bucket").ListKeysAsync(""));
        Assert.AreEqual(2, client.Calls);
    }
    [TestMethod]
    public async Task Verification_is_one_read_only_head()
    {
        using var client = new Client(); await new Backend(client, "bucket").VerifyAsync(); Assert.AreEqual(1, client.Calls);
    }
}

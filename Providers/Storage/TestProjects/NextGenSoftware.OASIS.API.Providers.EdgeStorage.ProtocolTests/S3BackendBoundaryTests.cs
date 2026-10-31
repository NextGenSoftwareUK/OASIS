using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.TigrisOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

// SDK boundary tests, not hosted AWS acceptance: no network requests or resources.
[TestClass]
public class S3BackendBoundaryTests
{
    private sealed class Client : AmazonS3Client
    {
        public Client() : base(new AnonymousAWSCredentials(), new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.USEast1 }) { }
        public readonly Queue<ListObjectsV2Response> Pages = new();
        public readonly List<string> Tokens = new();
        public AmazonS3Exception ReadError;
        public int Heads;
        public override Task<GetObjectResponse> GetObjectAsync(string bucket, string key, CancellationToken token = default)
            => Task.FromException<GetObjectResponse>(ReadError);
        public override Task<ListObjectsV2Response> ListObjectsV2Async(ListObjectsV2Request request, CancellationToken token = default)
        {
            Tokens.Add(request.ContinuationToken);
            return Task.FromResult(Pages.Dequeue());
        }
        public override Task<HeadBucketResponse> HeadBucketAsync(HeadBucketRequest request, CancellationToken token = default)
        {
            Assert.AreEqual("existing-bucket", request.BucketName);
            Heads++;
            return Task.FromResult(new HeadBucketResponse { HttpStatusCode = HttpStatusCode.OK });
        }
    }

    [DataTestMethod]
    [DataRow("NoSuchBucket")]
    [DataRow("AccessDenied")]
    [DataRow("NotFound")]
    public async Task Non_key_404_errors_are_not_absence(string code)
    {
        using var client = new Client { ReadError = new AmazonS3Exception(code) { ErrorCode = code, StatusCode = HttpStatusCode.NotFound } };
        var error = await Assert.ThrowsExceptionAsync<AmazonS3Exception>(() => new S3ObjectBackend(client, "existing-bucket").GetAsync("key"));
        Assert.AreSame(client.ReadError, error);
    }

    [TestMethod]
    public async Task Only_explicit_missing_key_is_absence()
    {
        using var client = new Client { ReadError = new AmazonS3Exception("missing") { ErrorCode = "NoSuchKey", StatusCode = HttpStatusCode.NotFound } };
        Assert.IsNull(await new S3ObjectBackend(client, "existing-bucket").GetAsync("key"));
    }

    [TestMethod]
    public async Task Verification_uses_read_only_bucket_metadata()
    {
        using var client = new Client();
        await new S3ObjectBackend(client, "existing-bucket").VerifyAsync();
        Assert.AreEqual(1, client.Heads);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task Truncated_page_requires_continuation_token(string next)
    {
        using var client = new Client();
        client.Pages.Enqueue(new ListObjectsV2Response { IsTruncated = true, NextContinuationToken = next });
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => new S3ObjectBackend(client, "existing-bucket").ListKeysAsync("prefix"));
        Assert.AreEqual(1, client.Tokens.Count);
    }

    [TestMethod]
    public async Task Repeated_token_is_rejected_without_a_third_request()
    {
        using var client = new Client();
        client.Pages.Enqueue(new ListObjectsV2Response { IsTruncated = true, NextContinuationToken = "same" });
        client.Pages.Enqueue(new ListObjectsV2Response { IsTruncated = true, NextContinuationToken = "same" });
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => new S3ObjectBackend(client, "existing-bucket").ListKeysAsync("prefix"));
        Assert.AreEqual(2, client.Tokens.Count);
    }

    [TestMethod]
    public async Task Pages_are_followed_to_completion()
    {
        using var client = new Client();
        client.Pages.Enqueue(new ListObjectsV2Response { IsTruncated = true, NextContinuationToken = "next", S3Objects = new() { new S3Object { Key = "prefix/a" } } });
        client.Pages.Enqueue(new ListObjectsV2Response { IsTruncated = false, S3Objects = new() { new S3Object { Key = "prefix/b" } } });
        CollectionAssert.AreEqual(new[] { "prefix/a", "prefix/b" }, new List<string>(await new S3ObjectBackend(client, "existing-bucket").ListKeysAsync("prefix")));
        CollectionAssert.AreEqual(new string[] { null, "next" }, client.Tokens);
    }
}

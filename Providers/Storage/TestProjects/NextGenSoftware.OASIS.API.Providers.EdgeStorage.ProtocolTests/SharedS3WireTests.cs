using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Backend = NextGenSoftware.OASIS.Providers.Shared.S3.S3ObjectBackend;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

// Actual official SDK signing/serialization against controlled HTTP, not AWS acceptance.
[TestClass]
public class SharedS3WireTests
{
    [TestMethod]
    public async Task Conditional_operations_are_sigv4_signed_and_send_native_headers()
    {
        var puts = 0; var gets = 0; var deletes = 0; var heads = 0;
        var checksum = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("value")));
        var revision = "\"" + Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("value"))).ToLowerInvariant() + "\"";
        using var service = new FakeService((request, body) =>
        {
            Assert.IsTrue(request.Headers.GetValues("Authorization").Single().StartsWith("AWS4-HMAC-SHA256 ", StringComparison.Ordinal));
            Assert.AreEqual("s3.us-east-1.amazonaws.com", request.RequestUri.Host);
            if (request.Method == HttpMethod.Head)
            {
                heads++; Assert.AreEqual("/test-bucket", request.RequestUri.AbsolutePath.TrimEnd('/'));
                return FakeService.Status(HttpStatusCode.OK);
            }
            Assert.AreEqual("/test-bucket/scope/key", request.RequestUri.AbsolutePath);
            if (request.Method == HttpMethod.Put)
            {
                puts++;
                if (puts == 1) Assert.AreEqual("*", request.Headers.GetValues("If-None-Match").Single());
                else Assert.AreEqual(revision, request.Headers.GetValues("If-Match").Single());
                Assert.AreEqual(checksum, request.Headers.GetValues("x-amz-checksum-sha256").Single());
                var response = FakeService.Status(HttpStatusCode.OK);
                response.Headers.TryAddWithoutValidation("ETag", revision);
                response.Headers.TryAddWithoutValidation("x-amz-checksum-sha256", checksum);
                return response;
            }
            if (request.Method == HttpMethod.Get)
            {
                gets++; var response = FakeService.Text("value");
                response.Headers.TryAddWithoutValidation("ETag", revision);
                response.Content.Headers.ContentLength = 5;
                return response;
            }
            Assert.AreEqual(HttpMethod.Delete, request.Method);
            deletes++; Assert.AreEqual(revision, request.Headers.GetValues("If-Match").Single());
            return FakeService.Status(HttpStatusCode.NoContent);
        });
        using var client = new AmazonS3Client(new BasicAWSCredentials("synthetic-key", "synthetic-secret"), new AmazonS3Config
        {
            ServiceURL = "https://s3.us-east-1.amazonaws.com", AuthenticationRegion = "us-east-1", ForcePathStyle = true,
            MaxErrorRetry = 0, HttpClientFactory = new HandlerHttpClientFactory(service)
        });
        var backend = new Backend(client, "test-bucket", "scope/");
        await backend.VerifyAsync();
        await backend.CreateOnlyAsync("key", "value");
        var observation = await backend.ReadObservedAsync("key");
        Assert.AreEqual("value", observation.Value); Assert.AreEqual(revision, observation.Revision);
        await backend.ReplaceUnchangedAsync("key", "value", observation.Revision);
        await backend.DeleteUnchangedAsync("key", observation.Revision);
        Assert.AreEqual(1, heads); Assert.AreEqual(2, puts); Assert.AreEqual(1, gets); Assert.AreEqual(1, deletes);
    }
}

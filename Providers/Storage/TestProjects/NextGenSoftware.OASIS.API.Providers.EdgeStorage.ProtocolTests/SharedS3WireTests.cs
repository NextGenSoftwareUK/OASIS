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
    [DataTestMethod]
    [DataRow("../outside")][DataRow("a/../outside")][DataRow("a\\outside")][DataRow("a?b#c")]
    public async Task Object_names_remain_exact_data_under_configured_prefix(string key)
    {
        var calls=0;
        using var service=new FakeService((request,bytes)=>{
            calls++;Assert.AreEqual(HttpMethod.Get,request.Method);
            Assert.IsTrue(request.Headers.GetValues("Authorization").Single().StartsWith("AWS4-HMAC-SHA256 ",StringComparison.Ordinal));
            Assert.AreEqual("/test-bucket/scope/"+key,Uri.UnescapeDataString(request.RequestUri.AbsolutePath));
            Assert.AreEqual("",request.RequestUri.Query);Assert.AreEqual("",request.RequestUri.Fragment);
            var response=FakeService.Text("value");response.Headers.TryAddWithoutValidation("ETag","\""+Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("value"))).ToLowerInvariant()+"\"");return response;
        });
        using var client=new AmazonS3Client(new BasicAWSCredentials("synthetic-key","synthetic-secret"),new AmazonS3Config{
            ServiceURL="https://s3.us-east-1.amazonaws.com",AuthenticationRegion="us-east-1",ForcePathStyle=true,MaxErrorRetry=0,
            HttpClientFactory=new HandlerHttpClientFactory(service)});
        Assert.AreEqual("value",await new Backend(client,"test-bucket","scope/").GetAsync(key));Assert.AreEqual(1,calls);
    }
    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,true,false)][DataRow(true,false,false)][DataRow(true,true,false)]
    [DataRow(false,false,true)][DataRow(false,true,true)][DataRow(true,false,true)][DataRow(true,true,true)]
    public async Task Full_object_checksum_is_requested_and_corrupt_bytes_are_not_published(bool observed,bool valid,bool requiredOnly)
    {
        const string body="tree 🌳";var calls=0;var requested=false;
        using var service=new FakeService((request,bytes)=>{
            calls++;Assert.AreEqual(HttpMethod.Get,request.Method);
            requested=request.Headers.TryGetValues("x-amz-checksum-mode",out var modes)&&modes.Single()=="ENABLED";
            var response=FakeService.Text(body);response.Headers.TryAddWithoutValidation("ETag","\"opaque-revision\"");
            // Model the service contract: checksum metadata is requested, not assumed present.
            if(requested){
                response.Headers.TryAddWithoutValidation("x-amz-checksum-type","FULL_OBJECT");
                response.Headers.TryAddWithoutValidation("x-amz-checksum-sha256",Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(valid?body:"different bytes"))));
            }
            return response;
        });
        using var client=new AmazonS3Client(new BasicAWSCredentials("synthetic-key","synthetic-secret"),new AmazonS3Config{
            ServiceURL="https://s3.us-east-1.amazonaws.com",AuthenticationRegion="us-east-1",ForcePathStyle=true,MaxErrorRetry=0,
            ResponseChecksumValidation=requiredOnly?ResponseChecksumValidation.WHEN_REQUIRED:ResponseChecksumValidation.WHEN_SUPPORTED,
            HttpClientFactory=new HandlerHttpClientFactory(service)});
        var backend=new Backend(client,"test-bucket");Exception failure=null;string result=null;
        try{result=observed?(await backend.ReadObservedAsync("key"))?.Value:await backend.GetAsync("key");}catch(Exception cause){failure=cause;}
        Assert.IsTrue(requested,"Whole-record SDK reads must request the uploaded native checksum.");Assert.AreEqual(1,calls,"No automatic retry or second HEAD may establish read integrity.");
        if(valid){Assert.IsNull(failure,failure?.ToString());Assert.AreEqual(body,result);}
        else{Assert.IsNotNull(failure,"Corrupt full-object bytes must not become a successful observation.");Assert.IsNull(result);}
    }

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

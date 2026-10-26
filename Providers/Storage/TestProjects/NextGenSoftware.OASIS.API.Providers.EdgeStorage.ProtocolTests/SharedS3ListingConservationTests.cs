using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Backend=NextGenSoftware.OASIS.Providers.Shared.S3.S3ObjectBackend;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

[TestClass]
public class SharedS3ListingConservationTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task Public_avatar_collection_cannot_report_false_absence_from_a_contradictory_page(bool synchronous)
    {
        var calls=0;
        using var service=new FakeService((request,body)=>{
            calls++;
            Assert.IsTrue(request.RequestUri.Query.Contains("list-type=2",StringComparison.Ordinal));
            Assert.IsTrue(request.Headers.GetValues("Authorization").Single().StartsWith("AWS4-HMAC-SHA256 ",StringComparison.Ordinal));
            return FakeService.Text("<ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><Name>test-bucket</Name><KeyCount>1</KeyCount><IsTruncated>false</IsTruncated></ListBucketResult>");
        });
        using var client=new AmazonS3Client(new BasicAWSCredentials("synthetic-key","synthetic-secret"),new AmazonS3Config{
            ServiceURL="https://s3.us-east-1.amazonaws.com",AuthenticationRegion="us-east-1",ForcePathStyle=true,MaxErrorRetry=0,
            HttpClientFactory=new HandlerHttpClientFactory(service)});
        var key=Enumerable.Range(1,32).Select(value=>(byte)value).ToArray();
        try{
            using var provider=new NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.AWSS3OASIS(client,"test-bucket",()=>key.ToArray(),"scope/");
            var result=synchronous?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync();
            Assert.IsTrue(result.IsError,"A claimed object with no payload cannot become successful absence.");
            Assert.IsNull(result.Result);
            Assert.AreEqual(0,result.LoadedCount);
            Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
            Assert.AreEqual(1,calls,"No object read, retry or guessed empty-page continuation is permitted.");
        }finally{CryptographicOperations.ZeroMemory(key);}
    }

    [DataTestMethod]
    [DataRow("count-missing",false)][DataRow("count-missing",true)]
    [DataRow("match-one",false)][DataRow("match-one",true)]
    [DataRow("match-zero",false)][DataRow("match-zero",true)]
    [DataRow("zero-with-object",false)][DataRow("zero-with-object",true)]
    [DataRow("positive-without-object",false)][DataRow("positive-without-object",true)]
    [DataRow("too-high",false)][DataRow("too-high",true)]
    [DataRow("negative",false)][DataRow("negative",true)]
    [DataRow("grouped-no-count",false)][DataRow("grouped-no-count",true)]
    [DataRow("grouped-matching-count",false)][DataRow("grouped-matching-count",true)]
    public async Task Declared_page_count_and_ungrouped_request_cannot_publish_missing_objects(string mode,bool priorPage)
    {
        var calls=0;
        var valid=mode is "count-missing" or "match-one" or "match-zero";
        var empty=mode is "match-zero" or "positive-without-object" or "negative" or "grouped-no-count" or "grouped-matching-count";
        using var service=new FakeService((request,body)=>{
            calls++;
            Assert.IsTrue(request.Headers.GetValues("Authorization").Single().StartsWith("AWS4-HMAC-SHA256 ",StringComparison.Ordinal));
            Assert.IsFalse(request.RequestUri.Query.Contains("delimiter=",StringComparison.Ordinal),"Backend requested every object, not grouped folders.");
            var first=priorPage&&calls==1;
            if(calls==2)StringAssert.Contains(request.RequestUri.Query,"continuation-token=next");
            var flag=first?"<IsTruncated>true</IsTruncated><NextContinuationToken>next</NextContinuationToken>":"<IsTruncated>false</IsTruncated>";
            var count=first?"<KeyCount>1</KeyCount>":mode switch {
                "count-missing" or "grouped-no-count"=>"",
                "match-zero" or "zero-with-object"=>"<KeyCount>0</KeyCount>",
                "too-high"=>"<KeyCount>2</KeyCount>","negative"=>"<KeyCount>-1</KeyCount>",
                _=>"<KeyCount>1</KeyCount>"};
            var contents=first||!empty?$"<Contents><Key>scope/key{calls}</Key><Size>1</Size></Contents>":"";
            var groups=!first&&mode.StartsWith("grouped-",StringComparison.Ordinal)?"<CommonPrefixes><Prefix>scope/hidden/</Prefix></CommonPrefixes>":"";
            return FakeService.Text($"<ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><Name>test-bucket</Name><Prefix>scope/</Prefix>{flag}{count}{contents}{groups}</ListBucketResult>");
        });
        using var client=new AmazonS3Client(new BasicAWSCredentials("synthetic-key","synthetic-secret"),new AmazonS3Config{
            ServiceURL="https://s3.us-east-1.amazonaws.com",AuthenticationRegion="us-east-1",ForcePathStyle=true,MaxErrorRetry=0,
            HttpClientFactory=new HandlerHttpClientFactory(service)});
        var backend=new Backend(client,"test-bucket","scope/");
        if(!valid)await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>backend.ListKeysAsync(""));
        else{
            var result=await backend.ListKeysAsync("");
            var expected=priorPage?(empty?new[]{"key1"}:new[]{"key1","key2"}):(empty?Array.Empty<string>():new[]{"key1"});
            CollectionAssert.AreEqual(expected,result.ToArray());
        }
        Assert.AreEqual(priorPage?2:1,calls,"Inconsistent page metadata must not cause retries, guessed pagination or partial publication.");
    }
}

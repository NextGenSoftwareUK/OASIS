using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Backend=NextGenSoftware.OASIS.Providers.Shared.S3.S3ObjectBackend;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

// Real pinned SDK XML decoding/signing over controlled HTTP, not hosted acceptance.
[TestClass]
public class SharedS3ListingCompletionTests
{
    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,true,false)][DataRow(true,false,false)][DataRow(true,true,false)]
    [DataRow(false,false,true)][DataRow(false,true,true)][DataRow(true,false,true)][DataRow(true,true,true)]
    public async Task Complete_page_cannot_also_offer_a_next_cursor(bool priorPage,bool empty,bool contradictory)
    {
        var calls=0;
        using var service=new FakeService((request,body)=>{
            calls++;
            Assert.IsTrue(request.Headers.GetValues("Authorization").Single().StartsWith("AWS4-HMAC-SHA256 ",StringComparison.Ordinal));
            var first=priorPage&&calls==1;
            if(calls==2)StringAssert.Contains(request.RequestUri.Query,"continuation-token=next");
            var flag=first?"<IsTruncated>true</IsTruncated><NextContinuationToken>next</NextContinuationToken>"
                :"<IsTruncated>false</IsTruncated>"+(contradictory?"<NextContinuationToken>unexpected-more-results</NextContinuationToken>":"");
            var contents=empty?"":$"<Contents><Key>scope/key{calls}</Key><Size>1</Size></Contents>";
            return FakeService.Text($"<ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><Name>test-bucket</Name><Prefix>scope/</Prefix>{flag}{contents}</ListBucketResult>");
        });
        using var client=new AmazonS3Client(new BasicAWSCredentials("synthetic-key","synthetic-secret"),new AmazonS3Config{
            ServiceURL="https://s3.us-east-1.amazonaws.com",AuthenticationRegion="us-east-1",ForcePathStyle=true,MaxErrorRetry=0,
            HttpClientFactory=new HandlerHttpClientFactory(service)});
        var backend=new Backend(client,"test-bucket","scope/");
        if(contradictory)await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>backend.ListKeysAsync(""));
        else{
            var keys=await backend.ListKeysAsync("");
            CollectionAssert.AreEqual(empty?Array.Empty<string>():priorPage?new[]{"key1","key2"}:new[]{"key1"},keys.ToArray());
        }
        Assert.AreEqual(priorPage?2:1,calls,"Contradictory completion must not trigger a guessed retry or cursor follow-up.");
    }

    [DataTestMethod]
    [DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public async Task Missing_completion_flag_cannot_become_a_complete_listing(int kind)
    {
        var calls=0;var twoPages=kind is 1 or 3 or 5;var empty=kind is 2 or 3 or 4;
        var valid=kind>=4;
        using var service=new FakeService((request,body)=>{
            calls++;
            Assert.IsTrue(request.Headers.GetValues("Authorization").Single().StartsWith("AWS4-HMAC-SHA256 ",StringComparison.Ordinal));
            Assert.IsTrue(request.RequestUri.Query.Contains("list-type=2",StringComparison.Ordinal));
            var first=twoPages&&calls==1;
            var flag=first?"<IsTruncated>true</IsTruncated><NextContinuationToken>next</NextContinuationToken>":valid?"<IsTruncated>false</IsTruncated>":"";
            var contents=empty?"":$"<Contents><Key>scope/key{calls}</Key><Size>1</Size></Contents>";
            return FakeService.Text($"<ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><Name>test-bucket</Name><Prefix>scope/</Prefix>{flag}{contents}</ListBucketResult>");
        });
        using var client=new AmazonS3Client(new BasicAWSCredentials("synthetic-key","synthetic-secret"),new AmazonS3Config{
            ServiceURL="https://s3.us-east-1.amazonaws.com",AuthenticationRegion="us-east-1",ForcePathStyle=true,MaxErrorRetry=0,
            HttpClientFactory=new HandlerHttpClientFactory(service)});
        var backend=new Backend(client,"test-bucket","scope/");
        if(!valid)await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>backend.ListKeysAsync(""));
        else{
            var result=await backend.ListKeysAsync("");
            CollectionAssert.AreEqual(empty?Array.Empty<string>():new[]{"key1","key2"},new List<string>(result));
        }
        Assert.AreEqual(twoPages?2:1,calls,"No retry or follow-up request may infer omitted completion metadata.");
    }
}

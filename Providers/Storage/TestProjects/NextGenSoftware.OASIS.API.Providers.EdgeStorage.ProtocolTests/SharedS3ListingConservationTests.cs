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
    [DataTestMethod]
    [DataRow("omitted",false)][DataRow("omitted",true)]
    [DataRow("matching",false)][DataRow("matching",true)]
    [DataRow("encoded-matching",false)][DataRow("encoded-matching",true)]
    [DataRow("encoded-percent",false)][DataRow("encoded-percent",true)]
    [DataRow("literal-plus",false)][DataRow("literal-plus",true)]
    [DataRow("bad-percent",false)][DataRow("bad-percent",true)]
    [DataRow("bad-utf8",false)][DataRow("bad-utf8",true)]
    [DataRow("unknown-encoding",false)][DataRow("unknown-encoding",true)]
    [DataRow("encoded-duplicate",false)][DataRow("encoded-duplicate",true)]
    [DataRow("foreign-prefix",false)][DataRow("foreign-prefix",true)]
    [DataRow("empty-prefix",false)][DataRow("empty-prefix",true)]
    [DataRow("foreign-token",false)][DataRow("foreign-token",true)]
    [DataRow("unexpected-start",false)][DataRow("unexpected-start",true)]
    [DataRow("unexpected-delimiter",false)][DataRow("unexpected-delimiter",true)]
    public async Task Completed_listing_must_match_its_requested_selection_and_cursor(string mode,bool priorPage)
    {
        var calls=0;var valid=mode is "omitted" or "matching" or "encoded-matching" or "encoded-percent" or "literal-plus";
        var encoded=mode.StartsWith("encoded-",StringComparison.Ordinal)||mode is "literal-plus" or "bad-percent" or "bad-utf8";
        var logical=mode=="encoded-percent"?"avatars%2F/":mode=="literal-plus"?"avatars+/":"avatars/";
        var physical="scope/"+logical;
        string Encoded(string value)=>Uri.EscapeDataString(value).Replace("%2B",mode=="literal-plus"?"+":"%2B",StringComparison.Ordinal);
        using var service=new FakeService((request,body)=>{
            calls++;var first=priorPage && calls==1;
            Assert.IsTrue(request.RequestUri.Query.Contains("list-type=2",StringComparison.Ordinal));
            if(calls==2)Assert.IsTrue(request.RequestUri.Query.Contains("continuation-token=next",StringComparison.Ordinal));
            var prefix=encoded?$"<Prefix>{Encoded(physical)}</Prefix>"
                :first||mode=="matching"?"<Prefix>scope/avatars/</Prefix>"
                :mode=="foreign-prefix"?"<Prefix>scope/other/</Prefix>":mode=="empty-prefix"?"<Prefix></Prefix>":"";
            if(!first && mode=="bad-percent")prefix="<Prefix>scope%2Favatars%2</Prefix>";
            if(!first && mode=="bad-utf8")prefix="<Prefix>scope%2Favatars%FF%2F</Prefix>";
            var encoding=encoded?"<EncodingType>url</EncodingType>":!first&&mode=="unknown-encoding"?"<EncodingType>gzip</EncodingType>":"";
            var token=!first&&mode=="foreign-token"?"<ContinuationToken>foreign</ContinuationToken>"
                :calls==2&&mode=="matching"?"<ContinuationToken>next</ContinuationToken>":"";
            var start=!first&&mode=="unexpected-start"?"<StartAfter>scope/avatars/skip</StartAfter>":"";
            var delimiter=!first&&mode=="unexpected-delimiter"?"<Delimiter>/</Delimiter>":"";
            var contents=first?$"<Contents><Key>{(encoded?Encoded(physical+"key"):physical+"key")}</Key><Size>1</Size></Contents>":"";
            var count=first?1:0;
            if(!first && mode=="encoded-duplicate"){
                var alias="<Contents><Key>scope%2favatars%2fkey</Key><Size>1</Size></Contents>";
                contents=priorPage?alias:alias+"<Contents><Key>%73cope%2Favatars%2Fkey</Key><Size>1</Size></Contents>";
                count=priorPage?1:2;
            }
            var flag=first?"<IsTruncated>true</IsTruncated><NextContinuationToken>next</NextContinuationToken>":"<IsTruncated>false</IsTruncated>";
            return FakeService.Text($"<ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><Name>test-bucket</Name>{prefix}{encoding}{token}{start}{delimiter}{flag}<KeyCount>{count}</KeyCount>{contents}</ListBucketResult>");
        });
        using var client=new AmazonS3Client(new BasicAWSCredentials("synthetic-key","synthetic-secret"),new AmazonS3Config{
            ServiceURL="https://s3.us-east-1.amazonaws.com",AuthenticationRegion="us-east-1",ForcePathStyle=true,MaxErrorRetry=0,
            HttpClientFactory=new HandlerHttpClientFactory(service)});
        var backend=new Backend(client,"test-bucket","scope/");
        if(!valid)await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>backend.ListKeysAsync(logical));
        else{
            var result=await backend.ListKeysAsync(logical);
            CollectionAssert.AreEqual(priorPage?new[]{logical+"key"}:Array.Empty<string>(),result.ToArray());
        }
        Assert.AreEqual(priorPage?2:1,calls,"A mismatched page cannot be retried or published as complete.");
    }

    [DataTestMethod][DataRow("count",false)][DataRow("count",true)]
    [DataRow("prefix",false)][DataRow("prefix",true)][DataRow("start",false)][DataRow("start",true)]
    public async Task Public_avatar_collection_cannot_report_false_absence_from_a_contradictory_page(string mode,bool synchronous)
    {
        var calls=0;
        using var service=new FakeService((request,body)=>{
            calls++;
            Assert.IsTrue(request.RequestUri.Query.Contains("list-type=2",StringComparison.Ordinal));
            Assert.IsTrue(request.Headers.GetValues("Authorization").Single().StartsWith("AWS4-HMAC-SHA256 ",StringComparison.Ordinal));
            var scope=mode=="prefix"?"<Prefix>scope/foreign/</Prefix>":mode=="start"?"<StartAfter>scope/skipped</StartAfter>":"";
            return FakeService.Text($"<ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><Name>test-bucket</Name><KeyCount>{(mode=="count"?1:0)}</KeyCount>{scope}<IsTruncated>false</IsTruncated></ListBucketResult>");
        });
        using var client=new AmazonS3Client(new BasicAWSCredentials("synthetic-key","synthetic-secret"),new AmazonS3Config{
            ServiceURL="https://s3.us-east-1.amazonaws.com",AuthenticationRegion="us-east-1",ForcePathStyle=true,MaxErrorRetry=0,
            HttpClientFactory=new HandlerHttpClientFactory(service)});
        var key=Enumerable.Range(1,32).Select(value=>(byte)value).ToArray();
        try{
            using var provider=new NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.AWSS3OASIS(client,"test-bucket",()=>key.ToArray(),"scope/");
            var result=synchronous?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync();
            Assert.IsTrue(result.IsError,"A contradictory page cannot become successful absence.");
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

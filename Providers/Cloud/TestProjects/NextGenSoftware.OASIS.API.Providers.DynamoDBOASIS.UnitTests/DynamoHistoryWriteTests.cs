using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoHistoryWriteTests
{
    [TestMethod]
    public Task UnversionedStagingPreservesExactMetadata()=>Use(new HistoryClient(),async provider=>{
        var value=1.0000000000000000000000000001m;
        var row=new Holon{Id=Guid.NewGuid(),MetaData=new(){["precise"]=value}};
        var staged=await Stage(provider,row,null);
        using var json=System.Text.Json.JsonDocument.Parse(staged.ToString());
        Assert.AreEqual(value,json.RootElement.GetProperty("MetaData").GetProperty("precise").GetDecimal());
        Assert.AreEqual(0,row.Version);Assert.AreEqual(value,row.MetaData["precise"]);
    });

    private static Task<JObject> Stage(DynamoDBOASIS provider,Holon row,JObject? previous)
        =>(Task<JObject>)typeof(DynamoDBOASIS).GetMethod("StageRecordAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.MakeGenericMethod(typeof(Holon)).Invoke(provider,new object?[]{row,previous})!;

    [TestMethod]
    public Task DisabledHistoryStillPreservesExistingArchive()=>Use(new HistoryClient(),async provider=>{
        provider.IsVersionControlEnabled=true;
        var row=new Holon{Id=Guid.NewGuid(),Name="before"};
        var first=await Stage(provider,row,null);var original=first.ToString();
        provider.IsVersionControlEnabled=false;row.Name="deleted";row.DeletedDate=DateTime.UtcNow;
        var second=await Stage(provider,row,first);
        Assert.AreEqual(2,second["Version"]!.Value<int>());
        Assert.AreEqual("before",second[EmbeddedRecordHistory.PropertyName]!["records"]![0]!["Name"]!.Value<string>());
        Assert.AreEqual(original,first.ToString());Assert.AreEqual(0,row.Version);
    });

    [TestMethod]
    public Task EnablingHistoryArchivesLegacyCanonicalRecordWithoutNativeId()=>Use(new HistoryClient(),async provider=>{
        var row=new Holon{Id=Guid.NewGuid(),Name="legacy",Version=3};var previous=JObject.Parse(OasisJson.Serialize(row));
        Assert.IsNull(previous["id"]);provider.IsVersionControlEnabled=true;row.Name="next";
        var staged=await Stage(provider,row,previous);
        Assert.AreEqual(4,staged["Version"]!.Value<int>());
        Assert.AreEqual(3,staged[EmbeddedRecordHistory.PropertyName]!["records"]![0]!["Version"]!.Value<int>());
        Assert.IsNull(previous["id"]);Assert.AreEqual(3,row.Version);
    });

    [TestMethod]
    public Task DisabledHistoryDoesNotInventVersionsForUnversionedRecords()=>Use(new HistoryClient(),async provider=>{
        var row=new Holon{Id=Guid.NewGuid(),Version=7};
        var staged=await Stage(provider,row,JObject.Parse(OasisJson.Serialize(row)));
        Assert.IsNull(staged[EmbeddedRecordHistory.PropertyName]);Assert.AreEqual(7,staged["Version"]!.Value<int>());
    });
    private sealed class HistoryClient:AmazonDynamoDBClient
    {
        public Dictionary<string,AttributeValue>? Stored;public int Writes,Reads;public string Mode="normal";
        public readonly IOException Cause=new("owned lost acknowledgement");
        public Action? DuringRead;
        public HistoryClient(int retries=0):base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",UseHttp=true,MaxErrorRetry=retries}){}
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
        {Reads++;Assert.IsTrue(request.ConsistentRead);DuringRead?.Invoke();return Task.FromResult(new GetItemResponse{HttpStatusCode=HttpStatusCode.OK,Item=Stored??new()});}
        public override Task<PutItemResponse> PutItemAsync(PutItemRequest request,CancellationToken cancellationToken=default)
        {
            Writes++;Assert.AreEqual("OasisHolons",request.TableName);Assert.AreEqual(ReturnValue.ALL_OLD,request.ReturnValues);
            if(Stored==null)Assert.AreEqual("attribute_not_exists(#id)",request.ConditionExpression);
            else{Assert.AreEqual("#data = :observed",request.ConditionExpression);Assert.AreEqual(Stored["data_json"].S,request.ExpressionAttributeValues[":observed"].S);}
            if(Mode=="conflict")throw new ConditionalCheckFailedException("owned race");
            var old=Stored;Stored=request.Item;
            if(Mode=="lost")throw Cause;
            return Task.FromResult(new PutItemResponse{HttpStatusCode=HttpStatusCode.OK,Attributes=old??new()});
        }
    }
    private static PutItemRequest Request(Holon row)=>new(){TableName="OasisHolons",Item=new(){
        ["id"]=new(){S=row.Id.ToString()},["parentHolonId"]=new(){S=row.ParentHolonId.ToString()},
        ["holon_type"]=new(){N=((int)row.HolonType).ToString()},["is_deleted"]=new(){BOOL=row.IsDeleted},["data_json"]=new(){S=OasisJson.Serialize(row)}}};
    private static Task<Holon> Write(DynamoDBOASIS provider,PutItemRequest request)
        =>(Task<Holon>)typeof(DynamoDBOASIS).GetMethod("WriteRecordAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.MakeGenericMethod(typeof(Holon)).Invoke(provider,new object?[]{request,true,(Action)(()=>{}),null})!;
    private static async Task Use(HistoryClient client,Func<DynamoDBOASIS,Task> test)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;field.SetValue(provider,client);
        try{await test(provider);}finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();client.Dispose();}
    }
    [TestMethod]
    public Task CreateAndReplaceRetainHistoryWithoutMutatingProposal()=>Use(new HistoryClient(),async provider=>{
        var row=new Holon{Id=Guid.NewGuid(),Name="first"};var firstRequest=Request(row);var original=firstRequest.Item["data_json"].S;
        var first=await Write(provider,firstRequest);Assert.AreEqual(1,first.Version);Assert.AreEqual(0,row.Version);Assert.AreEqual(original,firstRequest.Item["data_json"].S);
        row.Name="second";var next=await Write(provider,Request(row));Assert.AreEqual(2,next.Version);Assert.AreEqual(first.VersionId,next.PreviousVersionId);Assert.AreEqual(0,row.Version);
    });
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ConflictIsKnownNoncommitWhileLostAckIsFenced(bool lost)
    {
        var client=new HistoryClient{Mode=lost?"lost":"conflict"};
        await Use(client,async provider=>{
            var row=new Holon{Id=Guid.NewGuid()};
            if(lost){var error=await Assert.ThrowsExceptionAsync<DynamoDBMutationReconciliationException>(()=>Write(provider,Request(row)));Assert.AreSame(client.Cause,error.InnerException);Assert.IsNotNull(client.Stored);}
            else{await Assert.ThrowsExceptionAsync<ConditionalCheckFailedException>(()=>Write(provider,Request(row)));Assert.IsNull(client.Stored);}
            Assert.AreEqual(1,client.Writes);Assert.AreEqual(0,row.Version);
        });
    }
    [TestMethod]
    public async Task TableAndPayloadAreFrozenBeforeSnapshotRead()
    {
        var client=new HistoryClient();await Use(client,async provider=>{
            var row=new Holon{Id=Guid.NewGuid(),Name="original"};var proposal=Request(row);
            client.DuringRead=()=>{proposal.TableName="OasisAvatarDetails";row.Name="mutated";proposal.Item["data_json"].S=OasisJson.Serialize(row);};
            var stored=await Write(provider,proposal);Assert.AreEqual("original",stored.Name);Assert.AreEqual("OasisAvatarDetails",proposal.TableName);
        });
    }
    [TestMethod]
    public async Task RetryCapableClientIsRejectedBeforeReadOrWrite()
    {
        var client=new HistoryClient(1);await Use(client,async provider=>{
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>Write(provider,Request(new Holon{Id=Guid.NewGuid()})));
            Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Writes);
        });
    }
}

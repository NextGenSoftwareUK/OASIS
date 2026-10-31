using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoCreateAcknowledgementTests
{
    private sealed class SignedService(int shape):HttpMessageHandler
    {
        public int Reads,Creates;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Assert.AreEqual("dynamodb.eu-west-1.amazonaws.com",request.RequestUri!.Host);
            Assert.IsTrue(request.Headers.GetValues("Authorization").Single().StartsWith("AWS4-HMAC-SHA256 ",StringComparison.Ordinal));
            using var body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));var table=body.RootElement.GetProperty("TableName").GetString();
            var create=request.Headers.GetValues("X-Amz-Target").Single()=="DynamoDB_20120810.CreateTable";
            string json;var status=HttpStatusCode.OK;
            if(create){Creates++;Assert.AreEqual("OasisAvatars",table);Assert.AreEqual("PAY_PER_REQUEST",body.RootElement.GetProperty("BillingMode").GetString());
                json=shape==0?"{}":JsonSerializer.Serialize(new{TableDescription=new{TableName=shape==1?"foreign-table":table,TableStatus=shape==2?"DELETING":shape==3?"CREATING":"ACTIVE"}});
            }else{Assert.AreEqual("DynamoDB_20120810.DescribeTable",request.Headers.GetValues("X-Amz-Target").Single());Reads++;
                if(Reads==1){status=HttpStatusCode.BadRequest;json="{\"__type\":\"com.amazonaws.dynamodb.v20120810#ResourceNotFoundException\",\"message\":\"Synthetic absent table\"}";}
                else json=JsonSerializer.Serialize(new{Table=new{TableName=table,TableStatus="ACTIVE",KeySchema=new[]{new{AttributeName="id",KeyType="HASH"}},AttributeDefinitions=new[]{new{AttributeName="id",AttributeType="S"}}}});
            }
            var response=new HttpResponseMessage(status){Content=new StringContent(json,Encoding.UTF8,"application/x-amz-json-1.0")};response.Headers.TryAddWithoutValidation("x-amzn-RequestId","synthetic-create-ack");return response;
        }
    }
    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)][DataRow(4,false)][DataRow(4,true)]
    public async Task SignedOfficialSdkCreationResponseMustIdentifyItsRequestedTable(int shape,bool sync)
    {
        using var service=new SignedService(shape);using var client=new AmazonDynamoDBClient(new BasicAWSCredentials("synthetic-key","synthetic-secret"),new AmazonDynamoDBConfig{RegionEndpoint=Amazon.RegionEndpoint.EUWest1,MaxErrorRetry=0,HttpClientFactory=new HandlerHttpClientFactory(service)});
        var provider=new DynamoDBOASIS("owned","owned","eu-west-1");var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;field.SetValue(provider,client);
        try{var result=sync?provider.ActivateProvider():await provider.ActivateProviderAsync();Assert.AreEqual(shape<3,result.IsError,result.Exception?.ToString());Assert.AreEqual(shape>=3,provider.IsProviderActivated);Assert.AreEqual(1,service.Creates);Assert.AreEqual(shape<3?1:10,service.Reads);if(shape<3)Assert.IsInstanceOfType<InvalidDataException>(result.Exception);}
        finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
    private sealed class Probe(int shape):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",MaxErrorRetry=0})
    {
        public int Reads,Creates;
        public override Task<DescribeTableResponse> DescribeTableAsync(string name,CancellationToken token=default)
        {
            Reads++;if(Reads==1)throw new ResourceNotFoundException("Synthetic absent first table");
            return Task.FromResult(new DescribeTableResponse{HttpStatusCode=HttpStatusCode.OK,Table=new(){TableName=name,TableStatus=TableStatus.ACTIVE,KeySchema=new(){new(){AttributeName="id",KeyType=KeyType.HASH}},AttributeDefinitions=new(){new(){AttributeName="id",AttributeType=ScalarAttributeType.S}}}});
        }
        public override Task<CreateTableResponse> CreateTableAsync(CreateTableRequest request,CancellationToken token=default)
        {
            Creates++;Assert.AreEqual("OasisAvatars",request.TableName);Assert.AreEqual(BillingMode.PAY_PER_REQUEST,request.BillingMode);
            if(shape==0)return Task.FromResult<CreateTableResponse>(null!);
            return Task.FromResult(new CreateTableResponse{HttpStatusCode=shape switch{1=>0,2=>HttpStatusCode.Accepted,3=>HttpStatusCode.InternalServerError,_=>HttpStatusCode.OK},
                TableDescription=shape==4?null:new(){TableName=shape==5?"foreign-table":request.TableName,TableStatus=shape==6?TableStatus.DELETING:shape==7?TableStatus.CREATING:TableStatus.ACTIVE}});
        }
    }
    public static IEnumerable<object[]> Cases(){foreach(var shape in Enumerable.Range(0,9))foreach(var sync in new[]{false,true})yield return new object[]{shape,sync};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task CreationRequiresItsOwnCompletedIdentityBoundAcknowledgement(int shape,bool sync)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var probe=new Probe(shape);field.SetValue(provider,probe);
        try{
            var result=sync?provider.ActivateProvider():await provider.ActivateProviderAsync();
            Assert.AreEqual(shape<7,result.IsError);Assert.AreEqual(shape>=7,provider.IsProviderActivated);Assert.AreEqual(shape>=7,result.Result);
            Assert.AreEqual(1,probe.Creates);Assert.AreEqual(shape<7?1:10,probe.Reads,"Malformed creation evidence cannot be repaired by a later describe.");
            if(shape<7)Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}

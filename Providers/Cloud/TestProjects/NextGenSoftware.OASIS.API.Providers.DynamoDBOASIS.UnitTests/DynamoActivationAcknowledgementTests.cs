using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoActivationAcknowledgementTests
{
    private sealed class Probe(int kind):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",MaxErrorRetry=0})
    {
        public int Reads,Creates;
        public override Task<DescribeTableResponse> DescribeTableAsync(string name,CancellationToken token=default)
        {
            Reads++;var bad=kind<8&&Reads==(kind<4?1:2);var shape=kind%4;
            return Task.FromResult(new DescribeTableResponse{
                HttpStatusCode=bad?(HttpStatusCode)(shape switch{0=>0,1=>202,2=>500,_=>200}):HttpStatusCode.OK,
                Table=bad&&shape==3?null:new(){TableName=name,TableStatus=TableStatus.ACTIVE,
                    KeySchema=new(){new(){AttributeName="id",KeyType=KeyType.HASH}},
                    AttributeDefinitions=new(){new(){AttributeName="id",AttributeType=ScalarAttributeType.S}}}});
        }
        public override Task<CreateTableResponse> CreateTableAsync(CreateTableRequest request,CancellationToken token=default)
        {Creates++;throw new InvalidOperationException("Malformed observation is not evidence that a table is absent.");}
    }
    public static IEnumerable<object[]> Cases(){foreach(var kind in Enumerable.Range(0,9))foreach(var sync in new[]{false,true})yield return new object[]{kind,sync};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task ActivationRequiresCompletedTableObservationWithoutProvisioningOnMalformedResponse(int kind,bool sync)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var probe=new Probe(kind);field.SetValue(provider,probe);
        try{
            var result=sync?provider.ActivateProvider():await provider.ActivateProviderAsync();
            Assert.AreEqual(kind<8,result.IsError);Assert.AreEqual(kind==8,result.Result);Assert.AreEqual(kind==8,provider.IsProviderActivated);
            Assert.AreEqual(0,probe.Creates);
            Assert.AreEqual(kind<4?1:kind<8?2:10,probe.Reads,"Malformed observation must stop before another native operation.");
            if(kind<8)Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}

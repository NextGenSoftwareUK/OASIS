using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoCapacityUsageTests
{
    private static IReadOnlyList<NativeResourceUsageObservation> Capture(ConsumedCapacity? capacity,string? request="sdk-original")
    {
        var method=typeof(DynamoDBOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoCapacityUsage")!
            .GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static)!;
        try{return (IReadOnlyList<NativeResourceUsageObservation>)method.Invoke(null,new object?[]{"AWSOASIS","logical-original",request,"eu-west-1","OasisHolons",capacity,new DateTime(2026,10,24,0,0,0,DateTimeKind.Utc)})!;}
        catch(TargetInvocationException cause) when(cause.InnerException!=null){ExceptionDispatchInfo.Capture(cause.InnerException).Throw();throw;}
    }
    private sealed class Service(string? capacity):HttpMessageHandler
    {
        internal int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;Assert.AreEqual(HttpMethod.Post,request.Method);Assert.AreEqual("dynamodb.eu-west-1.amazonaws.com",request.RequestUri!.Host);
            Assert.IsTrue(request.Headers.GetValues("Authorization").Single().StartsWith("AWS4-HMAC-SHA256 ",StringComparison.Ordinal));
            StringAssert.Contains(await request.Content!.ReadAsStringAsync(token),"\"ReturnConsumedCapacity\":\"TOTAL\"");
            var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"Item\":{}"+(capacity==null?"":",\"ConsumedCapacity\":"+capacity)+"}",Encoding.UTF8,"application/x-amz-json-1.0")};
            response.Headers.TryAddWithoutValidation("x-amzn-RequestId","sdk-original");return response;
        }
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
    public async Task OfficialSdkWirePreservesOmittedZeroAndFractionalCapacity(int mode)
    {
        var payload=mode switch{0=>null,1=>"{}",2=>"{\"TableName\":\"OasisHolons\",\"CapacityUnits\":0}",_=>"{\"TableName\":\"OasisHolons\",\"CapacityUnits\":0.5}"};
        using var service=new Service(payload);using var client=new AmazonDynamoDBClient(new BasicAWSCredentials("synthetic-key","synthetic-secret"),new AmazonDynamoDBConfig{
            RegionEndpoint=Amazon.RegionEndpoint.EUWest1,MaxErrorRetry=0,HttpClientFactory=new HandlerHttpClientFactory(service)});
        var response=await client.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S="controlled"}}},ReturnConsumedCapacity=ReturnConsumedCapacity.TOTAL});
        var row=Capture(response.ConsumedCapacity,response.ResponseMetadata.RequestId).Single();Assert.AreEqual(1,service.Calls);
        Assert.AreEqual(mode<2?(decimal?)null:mode==2?0m:0.5m,row.Quantity);
        Assert.AreEqual("sdk-original",row.NativeRequestId);Assert.AreEqual("eu-west-1/table/OasisHolons",row.ResourceScope);Assert.AreEqual("Total",row.View);
        Assert.AreEqual("capacity-units",row.Unit);Assert.AreEqual("logical-original",row.ProviderRequestId);
    }
    [TestMethod]
    public void TotalsAndTableIndexViewsRemainSeparateWithoutSumming()
    {
        var capacity=new ConsumedCapacity{TableName="OasisHolons",CapacityUnits=3,ReadCapacityUnits=2,WriteCapacityUnits=1,
            Table=new(){CapacityUnits=2},GlobalSecondaryIndexes=new(){{"name-index",new(){WriteCapacityUnits=1}}},
            LocalSecondaryIndexes=new(){{"local-index",new(){ReadCapacityUnits=0}}}};
        var rows=Capture(capacity);Assert.AreEqual(6,rows.Count);Assert.AreEqual(3,rows.Count(row=>row.View=="Total"));
        Assert.AreEqual(0m,rows.Single(row=>row.ResourceScope.EndsWith("/lsi/local-index",StringComparison.Ordinal)).Quantity);
        Assert.AreEqual(1m,rows.Single(row=>row.ResourceScope.EndsWith("/gsi/name-index",StringComparison.Ordinal)).Quantity);
        Assert.IsFalse(rows.Any(row=>row.Quantity==6m));
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)]
    public void InvalidOrUnrepresentableNativeNumbersCannotProduceEvidence(int mode)
    {
        var value=mode switch{0=>double.NaN,1=>double.PositiveInfinity,2=>-1d,3=>1e-30,_=>double.MaxValue};
        Assert.ThrowsException<InvalidDataException>(()=>Capture(new(){TableName="OasisHolons",CapacityUnits=value}));
    }
    [TestMethod]
    public void WrongTableCannotSubstituteResourceIdentity()
        =>Assert.ThrowsException<InvalidDataException>(()=>Capture(new(){TableName="other",CapacityUnits=1}));
    [TestMethod]
    public void MissingNativeIdentityRemainsIncompleteInsteadOfBeingInvented()
    {
        var row=Capture(new(){TableName="OasisHolons",ReadCapacityUnits=0},null).Single();
        Assert.AreEqual(0m,row.Quantity);Assert.IsNull(row.NativeRequestId);
        Assert.ThrowsException<UsageProtocolException>(()=>row.Validate("AWSOASIS","logical-original",requireComplete:true));
    }
}

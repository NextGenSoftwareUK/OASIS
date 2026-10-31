using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class DynamoCapacityNativeTests
{
    public TestContext TestContext {get;set;}=null!;
    [TestMethod]
    public async Task RetainedServiceReportsCapacityForRepeatedTableConditionChecks()
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        using var client=new AmazonDynamoDBClient(new BasicAWSCredentials("ownedhistorytest","ownedhistorytest"),
            new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",MaxErrorRetry=0});
        // Condition checks only: no row is created, modified or deleted.
        var request=new TransactWriteItemsRequest{ReturnConsumedCapacity=ReturnConsumedCapacity.INDEXES};
        foreach(var id in new[]{Guid.NewGuid(),Guid.NewGuid()})request.TransactItems.Add(new(){ConditionCheck=new(){
            TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString("D")}}},ConditionExpression="attribute_not_exists(id)"}});
        var response=await client.TransactWriteItemsAsync(request);
        Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);
        Assert.IsFalse(string.IsNullOrWhiteSpace(response.ResponseMetadata.RequestId));
        Assert.IsTrue(response.ConsumedCapacity.Count>0);
        var capture=typeof(DynamoDBOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoCapacityUsage")!
            .GetMethod("CaptureTransaction",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;
        var observations=(IReadOnlyList<NextGenSoftware.OASIS.API.Core.Services.Subscriptions.NativeResourceUsageObservation>)capture.Invoke(null,
            new object[]{"DynamoDBOASIS","native-capacity-observation",response.ResponseMetadata.RequestId,"us-east-1",new[]{"OasisHolons","OasisHolons"},response.ConsumedCapacity,DateTime.UtcNow})!;
        Assert.AreEqual(1,observations.Count(row=>row.Unit=="capacity-units"&&row.View=="Total"));
        Assert.AreEqual((decimal)response.ConsumedCapacity.Single().CapacityUnits,observations.Single(row=>row.Unit=="capacity-units"&&row.View=="Total").Quantity);
        TestContext.WriteLine("Native capacity entries: "+response.ConsumedCapacity.Count);
        foreach(var entry in response.ConsumedCapacity){
            Assert.AreEqual("OasisHolons",entry.TableName);
            TestContext.WriteLine($"Table={entry.TableName}; total={entry.CapacityUnits:R}; read={entry.ReadCapacityUnits:R}; write={entry.WriteCapacityUnits:R}");
        }
    }
}

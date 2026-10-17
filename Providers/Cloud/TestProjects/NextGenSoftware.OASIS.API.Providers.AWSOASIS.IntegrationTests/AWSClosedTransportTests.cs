using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

[TestClass]
public class AWSClosedTransportTests
{
    [TestMethod]
    public async Task SDKClosedPortRetainsOriginalTransportCause()
    {
        using var client=new AmazonDynamoDBClient(new BasicAWSCredentials("owned-key","owned-secret"),
            new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",UseHttp=true,MaxErrorRetry=0,Timeout=TimeSpan.FromSeconds(2)});
        Exception? failure=null;
        try{await client.ScanAsync(new ScanRequest{TableName="Avatar"});}
        catch(Exception cause){failure=cause;}
        Assert.IsNotNull(failure,"Closed owned outage port must not accept a query.");
        Console.WriteLine(failure.ToString());
        var provider=new AWSOASIS("us-east-1","owned-key","owned-secret","http://127.0.0.1:18001");
        Assert.IsTrue(provider.IsProviderTransportFailure(failure));
        Assert.IsFalse(provider.IsProviderTransportFailure(new OperationCanceledException()));
        Assert.IsFalse(provider.IsProviderTransportFailure(new InvalidDataException()));
    }
}

using Azure.Storage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

[TestClass]
public class AzureClosedTransportTests
{
    [TestMethod]
    public async Task SDKClosedEndpointRetainsDeadlineCause()
    {
        var options=new BlobClientOptions();options.Retry.MaxRetries=0;options.Retry.NetworkTimeout=TimeSpan.FromSeconds(2);
        var client=new BlobServiceClient(new Uri("http://127.0.0.1:10003/owned"),options);
        Exception? failure=null;
        try{await foreach(var container in client.GetBlobContainersAsync()){Assert.Fail("Closed owned endpoint returned a container.");}}
        catch(Exception cause){failure=cause;}
        Assert.IsNotNull(failure);Console.WriteLine(failure.ToString());
        var provider=new AzureStorageOASIS("UseDevelopmentStorage=true");
        Assert.IsTrue(provider.IsProviderTransportFailure(failure));
        Assert.IsFalse(provider.IsProviderTransportFailure(new OperationCanceledException()));
        Assert.IsFalse(provider.IsProviderTransportFailure(new OperationCanceledException("caller",new CancellationToken(true))));
        Assert.IsFalse(provider.IsProviderTransportFailure(new InvalidDataException()));
    }
}

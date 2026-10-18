using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosConditionalReplacementTests
{
    [DataTestMethod][DataRow("changed")][DataRow("no-etag")][DataRow("etag-race")][DataRow("read-failure")]
    public async Task ConditionalMutationRejectsStaleOrUnverifiableSnapshot(string mode)
    {
        var expected=new Avatar{Id=Guid.NewGuid(),FirstName="original"};
        var current=JObject.Parse(OasisJson.Serialize((object)expected));current["id"]=expected.Id.ToString();
        if(mode!="no-etag")current["_etag"]="owned-native-etag";
        if(mode=="changed")current["FirstName"]="competitor";
        var read=new ResponseMessage(HttpStatusCode.OK){Content=new MemoryStream(Encoding.UTF8.GetBytes(current.ToString()))};
        var rejected=new ResponseMessage(HttpStatusCode.PreconditionFailed);
        var container=new Mock<Container>();
        container.Setup(c=>c.ReadItemStreamAsync(expected.Id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(read);
        var cause=new IOException("owned conditional pre-read failure");
        if(mode=="read-failure")container.Setup(c=>c.ReadItemStreamAsync(expected.Id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>())).ThrowsAsync(cause);
        container.Setup(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),expected.Id.ToString(),It.IsAny<PartitionKey>(),It.Is<ItemRequestOptions>(o=>o.IfMatchEtag=="owned-native-etag"),It.IsAny<CancellationToken>())).ReturnsAsync(rejected);
        var client=new CosmosDbClient(container.Object);
        Task Write()=>client.ReplaceUnchangedDocumentAsync(expected.Id.ToString(),expected,new Avatar{Id=expected.Id,FirstName="replacement"},null);
        if(mode=="read-failure")Assert.AreSame(cause,await Assert.ThrowsExceptionAsync<IOException>(Write));
        else if(mode=="changed")await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(Write);
        else if(mode=="no-etag")await Assert.ThrowsExceptionAsync<InvalidDataException>(Write);
        else
        {
            var failure=await Assert.ThrowsExceptionAsync<CosmosException>(Write);
            Assert.AreEqual(HttpStatusCode.PreconditionFailed,failure.StatusCode);Assert.IsFalse(NativeMutationReconciliation.IsRequired(failure));
        }
        container.Verify(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()),mode=="etag-race"?Times.Once():Times.Never());
    }
}

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
public sealed class CosmosConditionalDeleteTests
{
    [DataTestMethod][DataRow("success")][DataRow("changed")][DataRow("no-etag")][DataRow("conflict")][DataRow("lost")][DataRow("bad-ack")]
    public async Task ConditionalDeleteRequiresOriginalPayloadAndNativeRevision(string mode)
    {
        var expected=new AvatarDetail{Id=Guid.NewGuid(),Username="owned-original"};
        var json=JObject.Parse(OasisJson.Serialize((object)expected));json["id"]=expected.Id.ToString();
        if(mode!="no-etag")json["_etag"]="owned-revision";
        if(mode=="changed")json["Username"]="other";
        var container=new Mock<Container>();
        container.Setup(c=>c.ReadItemStreamAsync(expected.Id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResponseMessage(HttpStatusCode.OK){Content=new MemoryStream(Encoding.UTF8.GetBytes(json.ToString()))});
        var delete=container.Setup(c=>c.DeleteItemStreamAsync(expected.Id.ToString(),It.IsAny<PartitionKey>(),It.Is<ItemRequestOptions>(options=>options.IfMatchEtag=="owned-revision"),It.IsAny<CancellationToken>()));
        if(mode=="lost")delete.ThrowsAsync(new IOException("owned lost acknowledgement"));
        else delete.ReturnsAsync(new ResponseMessage(mode=="conflict"?HttpStatusCode.PreconditionFailed:mode=="bad-ack"?HttpStatusCode.OK:HttpStatusCode.NoContent));
        var client=new CosmosDbClient(container.Object);
        Task Run()=>client.DeleteUnchangedDocumentAsync(expected.Id.ToString(),expected,new PartitionKey(expected.Id.ToString()));
        if(mode=="success")await Run();
        else if(mode=="changed")await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(Run);
        else if(mode=="no-etag")await Assert.ThrowsExceptionAsync<InvalidDataException>(Run);
        else if(mode=="conflict"){var error=await Assert.ThrowsExceptionAsync<CosmosException>(Run);Assert.AreEqual(HttpStatusCode.PreconditionFailed,error.StatusCode);}
        else Assert.IsTrue(NativeMutationReconciliation.IsRequired(await Assert.ThrowsExceptionAsync<CosmosMutationReconciliationException>(Run)));
        container.Verify(c=>c.DeleteItemStreamAsync(It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()),mode is "changed" or "no-etag"?Times.Never():Times.Once());
    }
}

using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Moq;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

internal static class CosmosSdkTestRecords
{
    public static void Existing(Mock<Container> container,Guid id)
    {
        var document=JObject.Parse(OasisJson.Serialize(new {Id=id}));document["id"]=id.ToString();document["_etag"]="owned-preread-etag";
        container.Setup(c=>c.ReadItemStreamAsync(id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Returns(()=>Task.FromResult(new ResponseMessage(HttpStatusCode.OK){Content=new MemoryStream(Encoding.UTF8.GetBytes(document.ToString()))}));
    }
}

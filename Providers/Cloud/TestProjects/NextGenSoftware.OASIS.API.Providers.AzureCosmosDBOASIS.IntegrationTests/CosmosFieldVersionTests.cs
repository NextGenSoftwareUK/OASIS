using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [TestMethod]
    public async Task NativeFieldVersionUsesCanonicalStoredVersionNotLowercaseProperty()
    {
        var id=Guid.NewGuid(); var marker="owned-version-"+id.ToString("N");
        var saved=await _provider.SaveAvatarAsync(new Avatar{Id=id,Username=marker,IsActive=true,Version=7});
        Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
        var native=new CosmosDbClient(_client.GetContainer(DatabaseName,"avatarItems"));
        var original=JObject.Parse(await native.ReadDocumentAsync(id.ToString(),new PartitionKey(id.ToString())));
        Assert.AreEqual(id.ToString(),original["id"]!.Value<string>());
        Assert.AreEqual(marker,original["Username"]!.Value<string>());
        Assert.AreEqual(7,original["Version"]!.Value<int>());
        string? selected=null,wrong=null,current=null;
        try {
            selected=native.ReadDocumentByField("Username",marker,7);
            wrong=native.ReadDocumentByField("Username",marker,8);
            current=native.ReadDocumentByField("Username",marker,0);
        } finally {
            // Only this acknowledged and independently inspected original fixture; no container-wide deletion.
            await native.DeleteDocumentAsync(id.ToString(),new PartitionKey(id.ToString()));
        }
        Assert.IsNotNull(selected); Assert.AreEqual(id.ToString(),JObject.Parse(selected)["id"]!.Value<string>());
        Assert.IsNull(wrong); Assert.IsNotNull(current);
    }
}

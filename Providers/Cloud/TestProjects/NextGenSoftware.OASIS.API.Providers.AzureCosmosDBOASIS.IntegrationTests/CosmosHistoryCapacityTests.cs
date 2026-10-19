using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [TestMethod]
    public async Task NativeHistoryCapacityRejectionPreservesAcknowledgedRecordWithoutTrimming()
    {
        _provider.IsVersionControlEnabled=true;
        var root=new Holon{Id=Guid.NewGuid(),IsActive=true,Name="owned-history-capacity",Description=new string('a',1_100_000)};
        var saved=await _provider.SaveHolonAsync(root,saveChildren:false);
        Assert.IsFalse(saved.IsError,saved.Message);
        var versionId=root.VersionId;
        var native=new CosmosDbClient(_client.GetContainer(DatabaseName,"holonItems"));
        var inspected=false;
        try
        {
            var before=JObject.Parse(await native.ReadDocumentAsync(root.Id.ToString(),new PartitionKey(root.Id.ToString())));
            root.Description=new string('b',1_100_000);
            var rejected=await _provider.SaveHolonAsync(root,saveChildren:false);
            var after=JObject.Parse(await native.ReadDocumentAsync(root.Id.ToString(),new PartitionKey(root.Id.ToString())));
            // Independently inspect the actual original before removing only this owned fixture.
            Assert.IsTrue(JToken.DeepEquals(before,after),"Rejected replacement must not change the native item or its archive.");
            await native.DeleteDocumentAsync(root.Id.ToString(),new PartitionKey(root.Id.ToString()));
            inspected=true;
            Assert.IsTrue(rejected.IsError,"An archive exceeding the native item limit must fail, never trim history.");
            Assert.IsFalse(rejected.IsSaved);
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(rejected.Exception),"Native capacity rejection is definite, not an unknown commit.");
            Assert.AreEqual(1,root.Version);Assert.AreEqual(versionId,root.VersionId);
            Assert.AreEqual(new string('a',1_100_000),after["Description"]!.Value<string>());
        }
        finally {if(!inspected)_ownsDatabase=false;}
    }
}

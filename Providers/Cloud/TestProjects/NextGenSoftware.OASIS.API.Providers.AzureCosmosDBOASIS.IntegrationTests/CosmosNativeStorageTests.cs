using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public class CosmosNativeStorageTests
{
    [TestMethod]
    public async Task RetainedEmulatorPersistsProfilesInventoryAndGraphThroughOfficialSdk()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_COSMOSDB_NATIVE"));
        const string endpoint="https://127.0.0.1:8081";
        // Public emulator-only key documented by Microsoft; never a hosted-account credential.
        const string key="C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        const string databaseName="oasis-cosmos-native";
        using var client=new CosmosClient(endpoint,key,new CosmosClientOptions {ConnectionMode=ConnectionMode.Gateway,
            RequestTimeout=TimeSpan.FromSeconds(10),MaxRetryAttemptsOnRateLimitedRequests=0,
            HttpClientFactory=()=>new HttpClient(new HttpClientHandler {ServerCertificateCustomValidationCallback=HttpClientHandler.DangerousAcceptAnyServerCertificateValidator})});
        var names=new List<string> {"avatarItems","avatarDetailItems","holonItems"};
        var database=(await client.CreateDatabaseIfNotExistsAsync(databaseName)).Database;
        foreach(var name in names)
        {
            var container=(await database.CreateContainerIfNotExistsAsync(name,"/id")).Container;
            using var query=container.GetItemQueryIterator<int>("SELECT VALUE COUNT(1) FROM c");
            Assert.AreEqual(0,(await query.ReadNextAsync()).Single(),"Refusing retained/nonempty native test data.");
        }
        var provider=new AzureCosmosDBOASIS(new Uri(endpoint),key,databaseName,names);
        var avatarId=Guid.NewGuid();var holonId=Guid.NewGuid();var childId=Guid.NewGuid();
        try
        {
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);Assert.IsTrue(active.Result);
            var missingId=Guid.NewGuid().ToString();
            var nativeReader=new Infrastructure.CosmosDbClient(database.GetContainer("avatarItems"));
            Assert.IsNull(await nativeReader.ReadDocumentAsync(missingId,new PartitionKey(missingId)));
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarAsync(Guid.Parse(missingId))).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarDetailAsync(Guid.Parse(missingId))).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,provider.LoadHolon(missingId,loadChildren:false).ErrorCode);
            var avatar=new Avatar {Id=avatarId,IsActive=true,Username="owned-cosmos",Email="owned-cosmos@example.invalid",FirstName="Owned",
                MetaData=new Dictionary<string,object> {{"owned-field","owned-value"}}};
            var saved=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Message);
            var read=await provider.LoadAvatarAsync(avatarId);Assert.IsFalse(read.IsError,read.Message);
            Assert.AreEqual("Owned",read.Result.FirstName);Assert.AreEqual("owned-value",read.Result.MetaData["owned-field"].ToString());
            Assert.AreEqual(avatarId,(await provider.LoadAvatarByEmailAsync(avatar.Email)).Result.Id);
            var detail=new AvatarDetail {Id=avatarId,IsActive=true,Username=avatar.Username,Email=avatar.Email,XP=321,Karma=29};
            detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="owned native seed"});
            var detailSaved=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(detailSaved.IsError,detailSaved.Message);
            var detailRead=await provider.LoadAvatarDetailAsync(avatarId);Assert.IsFalse(detailRead.IsError,detailRead.Message);
            Assert.AreEqual(321,detailRead.Result.XP);Assert.AreEqual(29L,detailRead.Result.Karma);
            Assert.AreEqual(detail.Inventory[0].Id,detailRead.Result.Inventory.Single().Id);
            var child=new Holon {Id=childId,IsActive=true,Name="owned child"};
            var root=new Holon {Id=holonId,IsActive=true,Name="owned root",Children=new List<IHolon> {child},
                MetaData=new Dictionary<string,object> {{"owned-field","owned-value"}}};
            var graph=await provider.SaveHolonAsync(root);Assert.IsFalse(graph.IsError,graph.Message);
            var childRead=await provider.LoadHolonAsync(childId,loadChildren:false);Assert.IsFalse(childRead.IsError,childRead.Message);
            Assert.AreEqual(holonId,childRead.Result.ParentHolonId);
        }
        finally
        {
            foreach(var pair in new[] {("avatarItems",avatarId),("avatarDetailItems",avatarId),("holonItems",holonId),("holonItems",childId)})
            {
                using var deleted=await database.GetContainer(pair.Item1).DeleteItemStreamAsync(pair.Item2.ToString(),new PartitionKey(pair.Item2.ToString()));
                if(!deleted.IsSuccessStatusCode && deleted.StatusCode!=System.Net.HttpStatusCode.NotFound) deleted.EnsureSuccessStatusCode();
            }
            await provider.DeActivateProviderAsync();
        }
    }
}

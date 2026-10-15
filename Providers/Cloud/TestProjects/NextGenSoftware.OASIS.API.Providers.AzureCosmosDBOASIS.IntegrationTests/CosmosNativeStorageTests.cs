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
            var factoryField=typeof(AzureCosmosDBOASIS).GetField("dbClientFactory",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            var retainedFactory=factoryField.GetValue(provider);
            foreach(var activation in await Task.WhenAll(provider.ActivateProviderAsync(),provider.ActivateProviderAsync()))
            {Assert.IsFalse(activation.IsError,activation.Message);Assert.IsTrue(activation.Result);}
            Assert.AreSame(retainedFactory,factoryField.GetValue(provider));
            var missingId=Guid.NewGuid().ToString();
            var nativeReader=new Infrastructure.CosmosDbClient(database.GetContainer("avatarItems"));
            Assert.IsNull(await nativeReader.ReadDocumentAsync(missingId,new PartitionKey(missingId)));
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarAsync(Guid.Parse(missingId))).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarDetailAsync(Guid.Parse(missingId))).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,provider.LoadHolon(missingId,loadChildren:false).ErrorCode);
            var avatar=new Avatar {Id=avatarId,IsActive=true,Username="owned-cosmos",Email="owned-cosmos@example.invalid",FirstName="Owned",
                MetaData=new Dictionary<string,object> {{"owned-field","owned-value"}}};
            Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
            var saved=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Message);
            Assert.IsTrue(saved.IsSaved);Assert.IsTrue(provider.IsProviderActivated);
            var read=await provider.LoadAvatarAsync(avatarId);Assert.IsFalse(read.IsError,read.Message);
            Assert.AreEqual("Owned",read.Result.FirstName);Assert.AreEqual("owned-value",read.Result.MetaData["owned-field"].ToString());
            Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);Assert.IsFalse(provider.IsProviderActivated);
            var reloaded=await provider.LoadAvatarAsync(avatarId);Assert.IsFalse(reloaded.IsError,reloaded.Message);Assert.AreEqual("Owned",reloaded.Result.FirstName);
            Assert.AreNotSame(retainedFactory,factoryField.GetValue(provider));Assert.IsTrue(provider.IsProviderActivated);
            Assert.AreEqual(avatarId,(await provider.LoadAvatarByEmailAsync(avatar.Email)).Result.Id);
            var detail=new AvatarDetail {Id=avatarId,IsActive=true,Username=avatar.Username,Email=avatar.Email,XP=321,Karma=29};
            detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="owned native seed"});
            Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
            var detailSaved=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(detailSaved.IsError,detailSaved.Message);
            Assert.IsTrue(detailSaved.IsSaved);Assert.IsTrue(provider.IsProviderActivated);
            var detailRead=await provider.LoadAvatarDetailAsync(avatarId);Assert.IsFalse(detailRead.IsError,detailRead.Message);
            Assert.AreEqual(321,detailRead.Result.XP);Assert.AreEqual(29L,detailRead.Result.Karma);
            Assert.AreEqual(detail.Inventory[0].Id,detailRead.Result.Inventory.Single().Id);
            await VerifyV2RoutingAsync(provider,avatar,detail,holonId);
            var child=new Holon {Id=childId,IsActive=true,Name="owned child"};
            var root=new Holon {Id=holonId,IsActive=true,Name="owned root",Children=new List<IHolon> {child},
                MetaData=new Dictionary<string,object> {{"owned-field","owned-value"}}};
            Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
            var graph=await provider.SaveHolonAsync(root);Assert.IsFalse(graph.IsError,graph.Message);
            Assert.IsTrue(graph.IsSaved);Assert.IsTrue(provider.IsProviderActivated);
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
    private static async Task VerifyV2RoutingAsync(AzureCosmosDBOASIS cosmos,Avatar avatar,AvatarDetail detail,Guid holonId)
    {
        var path=Path.GetFullPath("TestResults/CosmosDBRouting/cosmos-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained SQLite routing evidence exists; refusing reset.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await sqlite.SaveAvatarAsync(new Avatar {Id=avatar.Id,IsActive=true,Username=avatar.Username,Email=avatar.Email,
                AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
            Assert.IsFalse((await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=detail.Id,IsActive=true,Username=detail.Username,XP=999,Karma=999})).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings(),
                SubscriptionConfig=new NextGenSoftware.OASIS.API.DNA.SubscriptionConfig {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoLoadBalanceEnabled=true,IsAutoReplicationEnabled=false,IsAutoFailOverEnabled=false};
            manager.RegisterProvider(cosmos);manager.RegisterProvider(sqlite);
            var providers=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AzureCosmosDBOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var counts=new Dictionary<long,int>();
            for(var i=0;i<6;i++)
            {
                var request=new Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=detail.Id};
                var read=i%2==0 ? router.RouteRequest<IAvatarDetail>(request,Core.Enums.LoadBalancingStrategy.RoundRobin)
                    :await router.RouteRequestAsync<IAvatarDetail>(request,Core.Enums.LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(read.IsError,read.Message);counts[read.Result.Karma]=counts.GetValueOrDefault(read.Result.Karma)+1;
            }
            Assert.AreEqual(3,counts[29]);Assert.AreEqual(3,counts[999]);
            foreach(var strategy in Enum.GetValues<Core.Enums.LoadBalancingStrategy>())
            foreach(var asynchronous in new[] {false,true})
            {
                var beforeCosmos=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.AzureCosmosDBOASIS).TotalRequests;
                var beforeSqlite=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests;
                var request=new Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=detail.Id};
                var read=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request,strategy):router.RouteRequest<IAvatarDetail>(request,strategy);
                Assert.IsFalse(read.IsError,read.Message);
                var choice=manager.LastProviderSelectionDiagnostic;Assert.AreEqual(strategy,choice.RequestedStrategy);Assert.AreEqual(2,choice.Candidates.Count);
                var selected=choice.SelectedProvider==Core.Enums.ProviderType.AzureCosmosDBOASIS;
                Assert.AreEqual(selected ? 29L:999L,read.Result.Karma);
                Assert.AreEqual(beforeCosmos+(selected ? 1:0),manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.AzureCosmosDBOASIS).TotalRequests);
                Assert.AreEqual(beforeSqlite+(selected ? 0:1),manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
            }
            manager.IsAutoLoadBalanceEnabled=false;manager.IsAutoReplicationEnabled=true;
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError);
            var amount=400;
            foreach(var primary in providers)
            foreach(var asynchronous in new[] {false,true})
            {
                var value=new AvatarDetail {Id=detail.Id,IsActive=true,Username=detail.Username,Email=detail.Email,XP=321,Karma=++amount};
                value.Inventory.Add(detail.Inventory[0]);
                var request=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=value,PreferredProvider=primary.Value};
                var saved=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request):router.RouteRequest<IAvatarDetail>(request);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);
                foreach(var store in new IOASISStorageProvider[] {cosmos,sqlite})
                {
                    var read=await store.LoadAvatarDetailAsync(detail.Id);Assert.IsFalse(read.IsError,read.Message);
                    Assert.AreEqual(value.Karma,read.Result.Karma);Assert.AreEqual(321,read.Result.XP);Assert.AreEqual(detail.Inventory[0].Id,read.Result.Inventory.Single().Id);
                }
                var profile=new Avatar {Id=avatar.Id,IsActive=true,Username=avatar.Username,Email=avatar.Email,FirstName="routed profile",
                    AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User),
                    MetaData=new Dictionary<string,object> {{"routed-field","profile-value"}}};
                var profileRequest=new Core.StorageOperationRequest {Operation="SaveAvatar",Payload=profile,PreferredProvider=primary.Value};
                var profileSaved=asynchronous ? await router.RouteRequestAsync<IAvatar>(profileRequest):router.RouteRequest<IAvatar>(profileRequest);
                Assert.IsFalse(profileSaved.IsError,profileSaved.Message);Assert.IsFalse(profileSaved.IsWarning,profileSaved.Message);
                var holon=new Holon {Id=holonId,IsActive=true,Name="routed holon",ParentHolonId=avatar.Id,CreatedByAvatarId=avatar.Id,
                    MetaData=new Dictionary<string,object> {{"routed-field","holon-value"}}};
                var holonRequest=new Core.StorageOperationRequest {Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=primary.Value};
                var holonSaved=asynchronous ? await router.RouteRequestAsync<IHolon>(holonRequest):router.RouteRequest<IHolon>(holonRequest);
                Assert.IsFalse(holonSaved.IsError,holonSaved.Message);Assert.IsFalse(holonSaved.IsWarning,holonSaved.Message);
                foreach(var store in new IOASISStorageProvider[] {cosmos,sqlite})
                {
                    var profileRead=await store.LoadAvatarAsync(avatar.Id);Assert.IsFalse(profileRead.IsError,profileRead.Message);
                    Assert.AreEqual("routed profile",profileRead.Result.FirstName);Assert.AreEqual("profile-value",profileRead.Result.MetaData["routed-field"].ToString());
                    var holonRead=await store.LoadHolonAsync(holonId,loadChildren:false);Assert.IsFalse(holonRead.IsError,holonRead.Message);
                    Assert.AreEqual(avatar.Id,holonRead.Result.ParentHolonId);Assert.AreEqual(avatar.Id,holonRead.Result.CreatedByAvatarId);
                    Assert.AreEqual("holon-value",holonRead.Result.MetaData["routed-field"].ToString());
                }
            }
            verified=true;
        }
        finally {await sqlite.DeActivateProviderAsync();sqlite.Dispose();if(verified && File.Exists(path)) File.Delete(path);}
    }
}

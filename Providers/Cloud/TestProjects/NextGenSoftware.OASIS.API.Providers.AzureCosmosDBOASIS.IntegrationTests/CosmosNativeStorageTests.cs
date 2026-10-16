using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class CosmosNativeStorageTests
{
    private const string EmulatorKey="C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    [TestMethod]
    public async Task RetainedEmulatorPersistsProfilesInventoryAndGraphThroughOfficialSdk()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_COSMOSDB_NATIVE"));
        const string endpoint="https://127.0.0.1:8081";
        // Public emulator-only key documented by Microsoft; never a hosted-account credential.
        const string key=EmulatorKey;
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
                MetaData=new Dictionary<string,object> {{"owned-field","owned-value"},{"Latitude",-33.0009d},{"Longitude",151d}}};
            Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
            var saved=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Message);
            Assert.AreEqual(avatarId,provider.GetAvatarsNearMe(-33000000,151000000,200).Result.Single().Id);
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
            var root=new Holon {Id=holonId,IsActive=true,Name="owned root",CreatedByAvatarId=avatarId,Children=new List<IHolon> {child},
                MetaData=new Dictionary<string,object> {{"owned-field","owned-value"},{"Latitude",-33.0009d},{"Longitude",151d}}};
            Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
            var graph=await provider.SaveHolonAsync(root);Assert.IsFalse(graph.IsError,graph.Message);
            Assert.IsTrue(graph.IsSaved);Assert.IsTrue(provider.IsProviderActivated);
            var childRead=await provider.LoadHolonAsync(childId,loadChildren:false);Assert.IsFalse(childRead.IsError,childRead.Message);
            Assert.AreEqual(holonId,childRead.Result.ParentHolonId);
            var rootRead=await provider.LoadHolonAsync(holonId,loadChildrenFromProvider:true);
            Assert.IsFalse(rootRead.IsError,rootRead.Message);Assert.AreEqual(childId,rootRead.Result.Children.Single().Id);
            var collection=provider.LoadAllHolons(loadChildrenFromProvider:true);
            Assert.IsFalse(collection.IsError,collection.Message);Assert.AreEqual(2,collection.Result.Count());
            Assert.AreEqual(childId,collection.Result.Single(row=>row.Id==holonId).Children.Single().Id);
            var parentRead=await provider.LoadHolonsForParentAsync(holonId);
            Assert.IsFalse(parentRead.IsError,parentRead.Message);Assert.IsTrue(parentRead.IsLoaded);
            Assert.AreEqual(childId,parentRead.Result.Single().Id);
            var emptyParent=provider.LoadHolonsForParent(Guid.NewGuid().ToString());
            Assert.IsFalse(emptyParent.IsError,emptyParent.Message);Assert.IsTrue(emptyParent.IsLoaded);Assert.IsFalse(emptyParent.Result.Any());
            var pairs=new Dictionary<string,string> {{"owned-field","owned-value"},{"missing","no"}};
            var metadata=await provider.LoadHolonsByMetaDataAsync(pairs,Core.Enums.MetaKeyValuePairMatchMode.Any,loadChildrenFromProvider:true);
            Assert.IsFalse(metadata.IsError,metadata.Message);Assert.IsTrue(metadata.IsLoaded);
            Assert.AreEqual(holonId,metadata.Result.Single().Id);Assert.AreEqual(childId,metadata.Result.Single().Children.Single().Id);
            var allMetadata=provider.LoadHolonsByMetaData(pairs,Core.Enums.MetaKeyValuePairMatchMode.All);
            Assert.IsFalse(allMetadata.IsError,allMetadata.Message);Assert.IsFalse(allMetadata.Result.Any());
            var search=await provider.SearchAsync(new Core.Objects.Search.SearchParams {SearchOnlyForCurrentAvatar=false,
                FilterByMetaData=new Dictionary<string,string> {{"owned-field","owned-value"}},
                SearchGroups=new() {new Core.Objects.Search.SearchTextGroup {SearchQuery="owned root",SearchHolons=true}}});
            Assert.IsFalse(search.IsError,search.Message);Assert.AreEqual(1,search.Result.NumberOfResults);
            Assert.AreEqual(holonId,search.Result.SearchResultHolons.Single().Id);Assert.AreEqual(0,search.Result.SearchResultAvatars.Count);
            var exported=await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username.ToUpperInvariant());
            Assert.AreEqual(avatarId,(await provider.LoadAvatarByUsernameAsync(avatar.Username.ToUpperInvariant())).Result.Id);
            Assert.AreEqual(avatarId,provider.LoadAvatarByEmail(avatar.Email.ToUpperInvariant()).Result.Id);
            Assert.IsFalse(exported.IsError,exported.Message);Assert.IsTrue(exported.IsLoaded);
            Assert.AreEqual(holonId,exported.Result.Single().Id);Assert.AreEqual(childId,exported.Result.Single().Children.Single().Id);
            Assert.AreEqual(holonId,provider.ExportAllDataForAvatarByEmail(avatar.Email.ToUpperInvariant()).Result.Single().Id);
            Assert.AreEqual(2,(await provider.ExportAllAsync()).Result.Count());
            var nearby=provider.GetHolonsNearMe(-33000000,151000000,200,Core.Enums.HolonType.All);
            Assert.IsFalse(nearby.IsError,nearby.Message);Assert.IsTrue(nearby.IsLoaded);Assert.AreEqual(holonId,nearby.Result.Single().Id);
            Assert.IsFalse(provider.GetHolonsNearMe(-33000000,151000000,50,Core.Enums.HolonType.All).Result.Any());
            var depthLimited=await provider.LoadAllHolonsAsync(maxChildDepth:1,curentChildDepth:1,loadChildrenFromProvider:true);
            Assert.IsFalse(depthLimited.IsError,depthLimited.Message);
            Assert.IsFalse(depthLimited.Result.Single(row=>row.Id==holonId).Children.Any());
            childRead.Result.Name="replaced child";
            var repository=new Infrastructure.HolonRepository((Infrastructure.ICosmosDbClientFactory)factoryField.GetValue(provider)!);
            await repository.UpdateAsync(childRead.Result);
            var replaced=await provider.LoadHolonAsync(childId,loadChildren:false);
            Assert.IsFalse(replaced.IsError,replaced.Message);Assert.AreEqual("replaced child",replaced.Result.Name);
            Assert.AreEqual(holonId,replaced.Result.ParentHolonId);
            var deleted=await provider.DeleteHolonAsync(childId);
            Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.IsSaved);Assert.AreEqual(childId,deleted.Result.Id);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,provider.LoadHolon(childId).ErrorCode);
            var softDeleted=await provider.DeleteAvatarByEmailAsync(avatar.Email.ToUpperInvariant());
            Assert.IsFalse(softDeleted.IsError,softDeleted.Message);Assert.IsTrue(softDeleted.Result);Assert.IsTrue(softDeleted.IsSaved);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,provider.LoadAvatar(avatarId).ErrorCode);
            var avatarStore=new Infrastructure.AvatarRepository((Infrastructure.ICosmosDbClientFactory)factoryField.GetValue(provider)!);
            var tombstone=await avatarStore.GetByIdAsync(avatarId.ToString());
            Assert.IsFalse(tombstone.IsActive);Assert.AreEqual(DateTimeKind.Utc,tombstone.DeletedDate.Kind);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarDetailAsync(avatarId)).ErrorCode);
            var detailStore=new Infrastructure.AvatarDetailRepository((Infrastructure.ICosmosDbClientFactory)factoryField.GetValue(provider)!);
            var detailTombstone=await detailStore.GetByIdAsync(avatarId.ToString());
            Assert.IsFalse(detailTombstone.IsActive);Assert.AreEqual(tombstone.DeletedDate,detailTombstone.DeletedDate);
            Assert.AreEqual(321L,detailTombstone.XP);Assert.AreEqual(404L,detailTombstone.Karma);Assert.AreEqual(detail.Inventory.Single().Name,detailTombstone.Inventory.Single().Name);
            var repeated=provider.DeleteAvatar(avatarId);Assert.IsFalse(repeated.IsError,repeated.Message);Assert.IsTrue(repeated.Result);Assert.IsFalse(repeated.IsSaved);
            var hardDeleted=await provider.DeleteAvatarAsync(avatarId,softDelete:false);
            Assert.IsFalse(hardDeleted.IsError,hardDeleted.Message);Assert.IsTrue(hardDeleted.Result);Assert.IsTrue(hardDeleted.IsSaved);
            Assert.IsNull(await detailStore.GetByIdAsync(avatarId.ToString()));
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
            await VerifyFailureRoutingAsync(cosmos,sqlite,manager,router,avatar.Id,holonId);
            verified=true;
        }
        finally {await sqlite.DeActivateProviderAsync();sqlite.Dispose();if(verified && File.Exists(path)) File.Delete(path);}
    }
    private sealed class DeadlineFactory(CosmosClient client) : Infrastructure.ICosmosDbClientFactory
    {
        public Infrastructure.ICosmosDbClient GetClient(string name)
            =>new DeadlineClient(new Infrastructure.CosmosDbClient(client.GetContainer("oasis-cosmos-native",name)));
    }

    private sealed class LifecycleFaultFactory(Infrastructure.ICosmosDbClient healthy,Infrastructure.ICosmosDbClient failed)
        : Infrastructure.ICosmosDbClientFactory,Infrastructure.ICosmosDbClient
    {
        public Infrastructure.ICosmosDbClient GetClient(string name)=>this;
        public Task<string> ReadDocumentAsync(string id,PartitionKey? key,CancellationToken token=default)=>healthy.ReadDocumentAsync(id,key,token);
        public string ReadDocumentByField(string field,string value,int version=0)=>healthy.ReadDocumentByField(field,value,version);
        public List<string> ReadAllDocuments()=>healthy.ReadAllDocuments();
        public Task<string> CreateDocumentAsync(object doc,PartitionKey? key,CancellationToken token=default)=>failed.CreateDocumentAsync(doc,key,token);
        public Task<string> UpsertDocumentAsync(object doc,PartitionKey? key,CancellationToken token=default)=>failed.UpsertDocumentAsync(doc,key,token);
        public Task ReplaceDocumentAsync(string id,object doc,PartitionKey? key,CancellationToken token=default)=>failed.ReplaceDocumentAsync(id,doc,key,token);
        public Task DeleteDocumentAsync(string id,PartitionKey? key,CancellationToken token=default)=>failed.DeleteDocumentAsync(id,key,token);
    }

    private sealed class DeadlineClient(Infrastructure.ICosmosDbClient inner) : Infrastructure.ICosmosDbClient
    {
        private static async Task<T> Bound<T>(Func<CancellationToken,Task<T>> action,CancellationToken token)
        {using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(TimeSpan.FromSeconds(2));return await action(deadline.Token);}
        public Task<string> ReadDocumentAsync(string id,PartitionKey? key,CancellationToken token=default)=>Bound(t=>inner.ReadDocumentAsync(id,key,t),token);
        public string ReadDocumentByField(string field,string value,int version=0)=>inner.ReadDocumentByField(field,value,version);
        public List<string> ReadAllDocuments()=>inner.ReadAllDocuments();
        public Task<string> CreateDocumentAsync(object doc,PartitionKey? key,CancellationToken token=default)=>Bound(t=>inner.CreateDocumentAsync(doc,key,t),token);
        public Task<string> UpsertDocumentAsync(object doc,PartitionKey? key,CancellationToken token=default)=>Bound(t=>inner.UpsertDocumentAsync(doc,key,t),token);
        public async Task ReplaceDocumentAsync(string id,object doc,PartitionKey? key,CancellationToken token=default)
            =>await Bound(async t=>{await inner.ReplaceDocumentAsync(id,doc,key,t);return true;},token);
        public async Task DeleteDocumentAsync(string id,PartitionKey? key,CancellationToken token=default)
            =>await Bound(async t=>{await inner.DeleteDocumentAsync(id,key,t);return true;},token);
    }

    private static async Task VerifyFailureRoutingAsync(AzureCosmosDBOASIS cosmos,IOASISStorageProvider sqlite,
        Core.Managers.ProviderManager manager,Core.Managers.OASISHyperDrive.OASISHyperDrive router,Guid avatarId,Guid holonId)
    {
        manager.IsAutoReplicationEnabled=false;manager.IsAutoFailOverEnabled=true;
        Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {
            new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AzureCosmosDBOASIS),
            new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)}).IsError);
        var stale=Guid.NewGuid();
        Assert.IsFalse((await sqlite.SaveAvatarAsync(new Avatar {Id=stale,IsActive=true,Username="stale-"+stale,
            AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
        Assert.IsFalse((await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=stale,IsActive=true,XP=999})).IsError);
        Assert.IsFalse((await sqlite.SaveHolonAsync(new Holon {Id=stale,IsActive=true},saveChildren:false)).IsError);
        foreach(var asynchronous in new[] {false,true})
        {
            var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests;
            var a=new Core.StorageOperationRequest {Operation="LoadAvatar",AvatarId=stale,PreferredProvider=Core.Enums.ProviderType.AzureCosmosDBOASIS};
            var d=new Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=stale,PreferredProvider=Core.Enums.ProviderType.AzureCosmosDBOASIS};
            var h=new Core.StorageOperationRequest {Operation="LoadHolon",HolonId=stale,LoadChildren=false,PreferredProvider=Core.Enums.ProviderType.AzureCosmosDBOASIS};
            var ar=asynchronous ? await router.RouteRequestAsync<IAvatar>(a):router.RouteRequest<IAvatar>(a);
            var dr=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(d):router.RouteRequest<IAvatarDetail>(d);
            var hr=asynchronous ? await router.RouteRequestAsync<IHolon>(h):router.RouteRequest<IHolon>(h);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,ar.ErrorCode);Assert.IsNull(ar.Result);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,dr.ErrorCode);Assert.IsNull(dr.Result);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,hr.ErrorCode);Assert.IsNull(hr.Result);
            Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
        }
        using var failedSdk=new CosmosClient("https://127.0.0.1:8082",EmulatorKey,new CosmosClientOptions {ConnectionMode=ConnectionMode.Gateway,
            LimitToEndpoint=true,RequestTimeout=TimeSpan.FromSeconds(2),MaxRetryAttemptsOnRateLimitedRequests=0,
            HttpClientFactory=()=>new HttpClient(new HttpClientHandler {ServerCertificateCustomValidationCallback=HttpClientHandler.DangerousAcceptAnyServerCertificateValidator})});
        var factory=new DeadlineFactory(failedSdk);
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var originals=new Dictionary<System.Reflection.FieldInfo,object>();
        foreach(var pair in new Dictionary<string,object> {{"avatarRepository",new Infrastructure.AvatarRepository(factory)},
            {"avatarDetailRepository",new Infrastructure.AvatarDetailRepository(factory)},{"holonRepository",new Infrastructure.HolonRepository(factory)}})
        {var field=typeof(AzureCosmosDBOASIS).GetField(pair.Key,flags)!;originals[field]=field.GetValue(cosmos)!;field.SetValue(cosmos,pair.Value);}
        try
        {
            foreach(var asynchronous in new[] {false,true})
            {
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests;
                var a=new Core.StorageOperationRequest {Operation="LoadAvatar",AvatarId=avatarId,PreferredProvider=Core.Enums.ProviderType.AzureCosmosDBOASIS};
                var d=new Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=avatarId,PreferredProvider=Core.Enums.ProviderType.AzureCosmosDBOASIS};
                var h=new Core.StorageOperationRequest {Operation="LoadHolon",HolonId=holonId,LoadChildren=false,PreferredProvider=Core.Enums.ProviderType.AzureCosmosDBOASIS};
                var ar=asynchronous ? await router.RouteRequestAsync<IAvatar>(a):router.RouteRequest<IAvatar>(a);
                Assert.IsFalse(ar.IsError,ar.Message);Assert.AreEqual("routed profile",ar.Result.FirstName);
                var dr=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(d):router.RouteRequest<IAvatarDetail>(d);
                Assert.IsFalse(dr.IsError,dr.Message);Assert.AreEqual(404L,dr.Result.Karma);
                var hr=asynchronous ? await router.RouteRequestAsync<IHolon>(h):router.RouteRequest<IHolon>(h);
                Assert.IsFalse(hr.IsError,hr.Message);Assert.AreEqual(holonId,hr.Result.Id);
                Assert.AreEqual(Core.Enums.ProviderType.SQLLiteDBOASIS,manager.LastFailoverDiagnostic.SelectedProvider);
                Assert.AreEqual(before+3,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
                var failedId=Guid.NewGuid();
                var failed=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=failedId,IsActive=true,XP=888},PreferredProvider=Core.Enums.ProviderType.AzureCosmosDBOASIS};
                var written=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(failed):router.RouteRequest<IAvatarDetail>(failed);
                Assert.IsTrue(written.IsError);Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(written.Exception));
                Assert.AreEqual(before+3,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
                Assert.IsNull((await sqlite.LoadAvatarDetailAsync(failedId)).Result);
            }
        }
        finally {foreach(var pair in originals) pair.Key.SetValue(cosmos,pair.Value);manager.IsAutoFailOverEnabled=false;}
        var avatarField=typeof(AzureCosmosDBOASIS).GetField("avatarRepository",flags)!;
        var detailField=typeof(AzureCosmosDBOASIS).GetField("avatarDetailRepository",flags)!;
        var healthyAvatar=(Interfaces.IAvatarRepository)originals[avatarField];
        var healthyDetail=(Interfaces.IAvatarDetailRepository)originals[detailField];
        var baseline=await healthyAvatar.GetByIdAsync(avatarId.ToString());
        var healthyFactory=(Infrastructure.ICosmosDbClientFactory)typeof(AzureCosmosDBOASIS).GetField("dbClientFactory",flags)!.GetValue(cosmos)!;
        var fault=new LifecycleFaultFactory(healthyFactory.GetClient("avatarDetailItems"),factory.GetClient("avatarDetailItems"));
        detailField.SetValue(cosmos,new Infrastructure.AvatarDetailRepository(fault));
        manager.IsAutoFailOverEnabled=true;
        try
        {
            foreach(var asynchronous in new[] {false,true})
            {
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests;
                var request=new Core.StorageOperationRequest {Operation="DeleteAvatar",AvatarId=avatarId,SoftDelete=true,PreferredProvider=Core.Enums.ProviderType.AzureCosmosDBOASIS};
                var deleted=asynchronous ? await router.RouteRequestAsync<bool>(request):router.RouteRequest<bool>(request);
                Assert.IsTrue(deleted.IsError);Assert.IsFalse(deleted.Result);
                var evidence=deleted.Exception as CosmosAvatarLifecycleException;Assert.IsNotNull(evidence);
                Assert.AreEqual(avatarId,evidence.AvatarId);CollectionAssert.AreEqual(new[] {"avatarItems"},evidence.AcknowledgedContainers.ToArray());
                Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(evidence.InnerException));
                Assert.IsFalse((await healthyAvatar.GetByIdAsync(avatarId.ToString())).IsActive);
                Assert.IsTrue((await healthyDetail.GetByIdAsync(avatarId.ToString())).IsActive);
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
                Assert.IsTrue((await sqlite.LoadAvatarAsync(avatarId)).Result.IsActive);
                Assert.IsTrue((await sqlite.LoadAvatarDetailAsync(avatarId)).Result.IsActive);
                await healthyAvatar.UpsertAsync(baseline);
            }
        }
        finally
        {
            detailField.SetValue(cosmos,healthyDetail);manager.IsAutoFailOverEnabled=false;
            await healthyAvatar.UpsertAsync(baseline);
        }
    }
}

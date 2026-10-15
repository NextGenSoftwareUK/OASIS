using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

[TestClass]
public class AWSOASISIntegrationTests
{
    [TestMethod]
    public async Task ActualV2RoundRobinAndBidirectionalReplication()
    {
        var directory=Path.GetFullPath("TestResults/AWSRoutingFiles");
        if(Directory.Exists(directory) && Directory.EnumerateFiles(directory,"*",SearchOption.AllDirectories).Any())
            Assert.Fail("Owned routing fixture is not empty; refusing to overwrite existing evidence.");
        var local=new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:directory);
        var id=Guid.NewGuid();
        Assert.IsFalse((await local.ActivateProviderAsync()).IsError);
        try {
            Assert.IsFalse((await _provider.SaveHolonAsync(new Holon {Id=id,Name="native-aws"},saveChildren:false)).IsError);
            Assert.IsFalse((await local.SaveHolonAsync(new Holon {Id=id,Name="native-local"},saveChildren:false)).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings(),
                SubscriptionConfig=new NextGenSoftware.OASIS.API.DNA.SubscriptionConfig {PlanType="Gold"}}};
            var manager=new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna) {
                IsAutoLoadBalanceEnabled=true,IsAutoReplicationEnabled=false,IsAutoFailOverEnabled=false};
            manager.RegisterProvider(_provider); manager.RegisterProvider(local);
            var providers=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AWSOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.LocalFileOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError);
            var router=new NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var counts=new Dictionary<string,int>();
            for(int i=0;i<6;i++) {
                var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {Operation="LoadHolon",HolonId=id};
                var read=i%2==0 ? router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request,Core.Enums.LoadBalancingStrategy.RoundRobin)
                    : await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request,Core.Enums.LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(read.IsError,read.Message);
                counts[read.Result.Name]=counts.GetValueOrDefault(read.Result.Name)+1;
            }
            Assert.AreEqual(3,counts["native-aws"]); Assert.AreEqual(3,counts["native-local"]);
            manager.IsAutoLoadBalanceEnabled=false; manager.IsAutoReplicationEnabled=true;
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError);
            foreach(var primary in providers)
            foreach(var asynchronous in new[] {false,true}) {
                var value=new Holon {Id=id,Name="replicated-"+primary.Value+"-"+asynchronous};
                var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {
                        Operation="SaveHolon",Payload=value,SaveChildren=false,PreferredProvider=primary.Value};
                var saved=asynchronous ? await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request)
                    :router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request);
                Assert.IsFalse(saved.IsError,saved.Message); Assert.IsFalse(saved.IsWarning,saved.Message);
                Assert.AreEqual(value.Name,(await _provider.LoadHolonAsync(id,loadChildren:false)).Result.Name);
                Assert.AreEqual(value.Name,(await local.LoadHolonAsync(id,loadChildren:false)).Result.Name);
            }
            manager.IsAutoReplicationEnabled=false; manager.IsAutoFailOverEnabled=true;
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
            var activated=manager.SetAndActivateCurrentStorageProvider(_provider);
            Assert.IsFalse(activated.IsError,activated.Message);
            var field=typeof(NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoDBOASIS)
                .GetField("_client",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            var healthy=field.GetValue(_provider);
            using var unreachable=new Amazon.DynamoDBv2.AmazonDynamoDBClient(
                new Amazon.Runtime.BasicAWSCredentials("owned-key","owned-secret"),new Amazon.DynamoDBv2.AmazonDynamoDBConfig {
                    ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",UseHttp=true,MaxErrorRetry=0,
                    Timeout=TimeSpan.FromSeconds(2)});
            try {
                field.SetValue(_provider,unreachable);
                foreach(var asynchronous in new[] {false,true}) {
                    var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests;
                    var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {
                        Operation="LoadHolon",HolonId=id,PreferredProvider=Core.Enums.ProviderType.AWSOASIS};
                    var read=asynchronous ? await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request)
                        :router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request);
                    Assert.IsFalse(read.IsError,read.Message);
                    Assert.AreEqual(id,read.Result.Id);
                    Assert.AreEqual((await local.LoadHolonAsync(id,loadChildren:false)).Result.Name,read.Result.Name);
                    Assert.AreEqual(Core.Enums.ProviderType.LocalFileOASIS,manager.LastFailoverDiagnostic.SelectedProvider);
                    Assert.AreEqual(before+1,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests);
                }
                var attemptedId=Guid.NewGuid();
                var secondaryBefore=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests;
                var failedWrite=await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(
                    new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {Operation="SaveHolon",SaveChildren=false,
                        Payload=new Holon {Id=attemptedId,Name="must-not-replay"},PreferredProvider=Core.Enums.ProviderType.AWSOASIS});
                Assert.IsTrue(failedWrite.IsError);
                Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failedWrite.Exception));
                Assert.AreEqual(secondaryBefore,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests);
                Assert.IsNull((await local.LoadHolonAsync(attemptedId,loadChildren:false)).Result);
            }
            finally {field.SetValue(_provider,healthy);}
            // Healthy primary absence is authoritative; a distinct stale secondary is not a failover target.
            var missingId=Guid.NewGuid();
            try {
                Assert.IsFalse((await local.SaveHolonAsync(new Holon {Id=missingId,Name="stale-secondary"},saveChildren:false)).IsError);
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests;
                foreach(var asynchronous in new[] {false,true}) {
                    var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {
                        Operation="LoadHolon",HolonId=missingId,PreferredProvider=Core.Enums.ProviderType.AWSOASIS};
                    var read=asynchronous ? await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request)
                        :router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request);
                    Assert.IsNull(read.Result);
                    Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,read.ErrorCode);
                }
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests);
                var avatar=await _provider.LoadAvatarAsync(missingId);
                var detail=await _provider.LoadAvatarDetailAsync(missingId);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,avatar.ErrorCode);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,detail.ErrorCode);
            }
            finally {await local.DeleteHolonAsync(missingId);}
        }
        finally {await local.DeleteHolonAsync(id); await local.DeActivateProviderAsync();}
    }

    private AWSOASIS _provider = null!;

    [TestInitialize]
    public async Task Setup()
    {
        string endpoint = Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL") ?? "http://127.0.0.1:8000";
        _provider = new AWSOASIS("us-east-1", "fakeMyKeyId", "fakeSecretAccessKey", endpoint);
        var activated = await _provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        Assert.IsTrue(_provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task PopulatedAvatarDetailRoundTrip_UsesOfficialDynamoDbApi()
    {
        var detail=new AvatarDetail {Id=Guid.NewGuid(),Username=$"owned-{Guid.NewGuid():N}",XP=321,Karma=29};
        var item=new InventoryItem {Id=Guid.NewGuid(),Name="owned seed"};
        detail.Inventory.Add(item);
        var saved=await _provider.SaveAvatarDetailAsync(detail);
        Assert.IsFalse(saved.IsError,saved.Message);
        var loaded=await _provider.LoadAvatarDetailAsync(detail.Id);
        Assert.IsFalse(loaded.IsError,loaded.Message);
        Assert.AreEqual(item.Id,loaded.Result.Inventory.Single().Id);
        Assert.AreEqual(item.Name,loaded.Result.Inventory.Single().Name);
        Assert.AreEqual(321,loaded.Result.XP); Assert.AreEqual(29L,loaded.Result.Karma);
        Assert.IsNotNull(loaded.Result.Stats); Assert.IsNotNull(loaded.Result.Skills);
    }

    [TestMethod]
    public async Task DuplicateAvatarUsernameIsTerminalAndDeleteDoesNotMutate()
    {
        var username=$"duplicate-{Guid.NewGuid():N}";
        var first=new Avatar {Id=Guid.NewGuid(),Username=username,Email=$"{Guid.NewGuid():N}@example.invalid"};
        var second=new Avatar {Id=Guid.NewGuid(),Username=username,Email=$"{Guid.NewGuid():N}@example.invalid"};
        Assert.IsFalse((await _provider.SaveAvatarAsync(first)).IsError);
        Assert.IsFalse((await _provider.SaveAvatarAsync(second)).IsError);
        var lookup=await _provider.LoadAvatarByUsernameAsync(username);
        Assert.IsTrue(lookup.IsError);
        Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,lookup.ErrorCode);
        var exported=await _provider.ExportAllDataForAvatarByUsernameAsync(username);
        Assert.IsTrue(exported.IsError);
        Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,exported.ErrorCode);
        var deleted=await _provider.DeleteAvatarByUsernameAsync(username);
        Assert.IsTrue(deleted.IsError);
        Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,deleted.ErrorCode);
        Assert.IsFalse((await _provider.LoadAvatarAsync(first.Id)).IsError);
        Assert.IsFalse((await _provider.LoadAvatarAsync(second.Id)).IsError);
        var email=$"duplicate-{Guid.NewGuid():N}@example.invalid";
        Assert.IsFalse((await _provider.SaveAvatarDetailAsync(new AvatarDetail {Id=first.Id,Username=username,Email=email})).IsError);
        Assert.IsFalse((await _provider.SaveAvatarDetailAsync(new AvatarDetail {Id=second.Id,Username=username,Email=email})).IsError);
        var byName=await _provider.LoadAvatarDetailByUsernameAsync(username);
        var byEmail=_provider.LoadAvatarDetailByEmail(email);
        Assert.IsTrue(byName.IsError);Assert.IsTrue(byEmail.IsError);
        Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,byName.ErrorCode);
        Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,byEmail.ErrorCode);
    }

    [TestMethod]
    public async Task AvatarRoundTrip_UsesOfficialDynamoDbApi()
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = $"aws-{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@example.test" };

        var saved = await _provider.SaveAvatarAsync(avatar);
        Assert.IsFalse((await _provider.DeActivateProviderAsync()).IsError);
        var reactivated=await _provider.ActivateProviderAsync();
        Assert.IsFalse(reactivated.IsError,reactivated.Message);
        var loaded = await _provider.LoadAvatarAsync(avatar.Id);

        Assert.IsFalse(saved.IsError, saved.Message);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(avatar.Username, loaded.Result.Username);
        Assert.AreEqual(avatar.Id.ToString(), loaded.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.AWSOASIS]);
    }

    [TestMethod]
    public async Task HolonRoundTripAndSearch_UseOfficialDynamoDbApi()
    {
        string marker = $"aws-holon-{Guid.NewGuid():N}";
        var holon = new Holon { Id = Guid.NewGuid(), Name = marker, Description = "AWS SDK integration evidence" };

        var saved = await _provider.SaveHolonAsync(holon);
        var loaded = await _provider.LoadHolonAsync(holon.Id);
        var search = await _provider.SearchAsync(new SearchParams
        {
            SearchOnlyForCurrentAvatar=false,
            SearchGroups = [new SearchTextGroup { SearchQuery = marker }]
        });

        Assert.IsFalse(saved.IsError, saved.Message);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(marker, loaded.Result.Name);
        Assert.IsFalse(search.IsError, search.Message);
        Assert.IsTrue(search.Result.SearchResultHolons.Any(x => x.Id == holon.Id));
        Assert.AreEqual(holon.Id.ToString(), loaded.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.AWSOASIS]);
        // Six ~190KB records exceed DynamoDB's 1MB query response page while staying under its per-item limit.
        var parent=Guid.NewGuid();
        var children=Enumerable.Range(0,6).Select(i=>new Holon {
            Id=Guid.NewGuid(),ParentHolonId=parent,Name="owned-page-"+i,Description=new string('x',190000)}).ToArray();
        foreach(var child in children) {
            var written=await _provider.SaveHolonAsync(child,saveChildren:false);
            Assert.IsFalse(written.IsError,written.Message);
        }
        var allPages=await _provider.LoadHolonsForParentAsync(parent,loadChildren:false);
        Assert.IsFalse(allPages.IsError,allPages.Message);
        CollectionAssert.AreEquivalent(children.Select(c=>c.Id).ToArray(),allPages.Result.Select(c=>c.Id).ToArray());
        var grandchild=new Holon {Id=Guid.NewGuid(),ParentHolonId=children[0].Id,Name="owned-grandchild"};
        Assert.IsFalse((await _provider.SaveHolonAsync(grandchild,saveChildren:false)).IsError);
        var graph=await _provider.LoadHolonsForParentAsync(parent,loadChildrenFromProvider:true);
        Assert.IsFalse(graph.IsError,graph.Message);
        Assert.AreEqual(grandchild.Id,graph.Result.Single(row=>row.Id==children[0].Id).Children.Single().Id);
        var greatGrandchild=new Holon {Id=Guid.NewGuid(),ParentHolonId=grandchild.Id,Name="owned-great-grandchild"};
        Assert.IsFalse((await _provider.SaveHolonAsync(greatGrandchild,saveChildren:false)).IsError);
        var single=await _provider.LoadHolonAsync(children[0].Id,loadChildrenFromProvider:true);
        Assert.IsFalse(single.IsError,single.Message);
        Assert.AreEqual(greatGrandchild.Id,single.Result.Children.Single().Children.Single().Id);
        var nonRecursive=_provider.LoadHolon(children[0].Id.ToString(),recursive:false,loadChildrenFromProvider:true);
        Assert.IsFalse(nonRecursive.IsError,nonRecursive.Message);
        Assert.AreEqual(grandchild.Id,nonRecursive.Result.Children.Single().Id);
        Assert.AreEqual(0,nonRecursive.Result.Children.Single().Children.Count);
        var noChildren=await _provider.LoadHolonAsync(children[0].Id,loadChildren:false,loadChildrenFromProvider:true);
        Assert.AreEqual(0,noChildren.Result.Children.Count);
        Assert.IsInstanceOfType(_provider.LoadHolon(children[0].Id,version:1).Exception,typeof(NotSupportedException));
        children[0].MetaData=new Dictionary<string,object> {{"owned-marker",marker},{"amount",1.5m}};
        Assert.IsFalse((await _provider.SaveHolonAsync(children[0],saveChildren:false)).IsError);
        var metadata=await _provider.LoadHolonsByMetaDataAsync("owned-marker",marker,loadChildrenFromProvider:true);
        Assert.IsFalse(metadata.IsError,metadata.Message);
        Assert.AreEqual(children[0].Id,metadata.Result.Single().Id);
        Assert.AreEqual(grandchild.Id,metadata.Result.Single().Children.Single().Id);
        var invalid=_provider.LoadHolonsByMetaData("owned-marker",marker,version:1);
        Assert.IsTrue(invalid.IsError);Assert.IsInstanceOfType(invalid.Exception,typeof(NotSupportedException));
        var filtered=await _provider.SearchAsync(new SearchParams {SearchOnlyForCurrentAvatar=false,FilterByMetaData=new Dictionary<string,string> {{"owned-marker",marker}}});
        Assert.IsFalse(filtered.IsError,filtered.Message);
        Assert.AreEqual(children[0].Id,filtered.Result.SearchResultHolons.Single().Id);
        Assert.AreEqual(1,filtered.Result.NumberOfResults);
        Assert.AreEqual(grandchild.Id,filtered.Result.SearchResultHolons.Single().Children.Single().Id);
        var badSearch=await _provider.SearchAsync(new SearchParams {SearchOnlyForCurrentAvatar=false},version:1);
        Assert.IsTrue(badSearch.IsError);Assert.IsInstanceOfType(badSearch.Exception,typeof(NotSupportedException));
        var owner=Guid.NewGuid();children[0].CreatedByAvatarId=owner;
        Assert.IsFalse((await _provider.SaveHolonAsync(children[0],saveChildren:false)).IsError);
        var owned=await _provider.ExportAllDataForAvatarByIdAsync(owner);
        Assert.IsFalse(owned.IsError,owned.Message);
        Assert.AreEqual(children[0].Id,owned.Result.Single().Id);
        Assert.AreEqual(grandchild.Id,owned.Result.Single().Children.Single().Id);
        Assert.IsInstanceOfType(_provider.ExportAll(version:1).Exception,typeof(NotSupportedException));
        Assert.IsTrue(_provider.ExportAllDataForAvatarById(Guid.Empty).IsError);
        var importedRows=new[] {new Holon {Id=Guid.NewGuid(),Name="owned-import-one"},new Holon {Id=Guid.NewGuid(),Name="owned-import-two"}};
        var imported=await _provider.ImportAsync(importedRows);
        Assert.IsFalse(imported.IsError,imported.Message);Assert.IsTrue(imported.Result);Assert.IsTrue(imported.IsSaved);
        foreach(var row in importedRows) Assert.AreEqual(row.Name,(await _provider.LoadHolonAsync(row.Id,loadChildren:false)).Result.Name);
        var truncated=_provider.LoadHolonsForParent(parent,loadChildrenFromProvider:true,maxChildDepth:1,curentChildDepth:1);
        Assert.IsFalse(truncated.IsError,truncated.Message);
        Assert.IsTrue(truncated.Result.All(row=>row.Children.Count==0));
        Assert.IsTrue((await _provider.LoadAllHolonsAsync(maxChildDepth:-1)).IsError);
        Assert.IsInstanceOfType(_provider.LoadHolonsForParent(parent,version:1).Exception,typeof(NotSupportedException));
    }
}

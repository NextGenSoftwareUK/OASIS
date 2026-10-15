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
        if(Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            Assert.Fail("Owned routing fixture is not empty; refusing to overwrite existing evidence.");
        var local=new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:directory);
        var id=Guid.NewGuid();
        Assert.IsFalse((await local.ActivateProviderAsync()).IsError);
        try {
            Assert.IsFalse((await _provider.SaveHolonAsync(new Holon {Id=id,Name="native-aws"},saveChildren:false)).IsError);
            Assert.IsFalse((await local.SaveHolonAsync(new Holon {Id=id,Name="native-local"},saveChildren:false)).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
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
            foreach(var primary in providers) {
                var value=new Holon {Id=id,Name="replicated-"+primary.Value};
                var saved=await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(
                    new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {
                        Operation="SaveHolon",Payload=value,SaveChildren=false,PreferredProvider=primary.Value});
                Assert.IsFalse(saved.IsError,saved.Message); Assert.IsFalse(saved.IsWarning,saved.Message);
                Assert.AreEqual(value.Name,(await _provider.LoadHolonAsync(id,loadChildren:false)).Result.Name);
                Assert.AreEqual(value.Name,(await local.LoadHolonAsync(id,loadChildren:false)).Result.Name);
            }
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
        var deleted=await _provider.DeleteAvatarByUsernameAsync(username);
        Assert.IsTrue(deleted.IsError);
        Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,deleted.ErrorCode);
        Assert.IsFalse((await _provider.LoadAvatarAsync(first.Id)).IsError);
        Assert.IsFalse((await _provider.LoadAvatarAsync(second.Id)).IsError);
    }

    [TestMethod]
    public async Task AvatarRoundTrip_UsesOfficialDynamoDbApi()
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = $"aws-{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@example.test" };

        var saved = await _provider.SaveAvatarAsync(avatar);
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
            SearchGroups = [new SearchTextGroup { SearchQuery = marker }]
        });

        Assert.IsFalse(saved.IsError, saved.Message);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(marker, loaded.Result.Name);
        Assert.IsFalse(search.IsError, search.Message);
        Assert.IsTrue(search.Result.SearchResultHolons.Any(x => x.Id == holon.Id));
        Assert.AreEqual(holon.Id.ToString(), loaded.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.AWSOASIS]);
    }
}

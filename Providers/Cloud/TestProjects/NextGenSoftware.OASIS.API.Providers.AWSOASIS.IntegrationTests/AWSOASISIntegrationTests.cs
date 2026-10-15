using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

[TestClass]
public class AWSOASISIntegrationTests
{
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

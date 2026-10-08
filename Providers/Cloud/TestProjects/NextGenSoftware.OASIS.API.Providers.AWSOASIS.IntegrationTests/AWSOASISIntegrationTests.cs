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

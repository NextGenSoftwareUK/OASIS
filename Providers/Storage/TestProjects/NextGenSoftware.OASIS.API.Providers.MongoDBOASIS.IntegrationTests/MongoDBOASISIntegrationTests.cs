using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.IntegrationTests;

[TestClass]
public class MongoDBOASISIntegrationTests
{
    private MongoDBOASIS _provider = null!;

    [TestInitialize]
    public void Setup()
    {
        _provider = new MongoDBOASIS(Require("MONGODBOASIS_CONNECTIONSTRING"), Require("MONGODBOASIS_DBNAME"));
        var activated = _provider.ActivateProvider();
        Assert.IsFalse(activated.IsError, activated.Message);
        Assert.IsTrue(_provider.IsProviderActivated, "MongoDBOASIS did not remain activated.");
    }

    [TestMethod]
    public async Task SaveAndLoadAvatar_RoundTrips()
    {
        var avatar = NewAvatar();
        var saved = await _provider.SaveAvatarAsync(avatar);
        Assert.IsFalse(saved.IsError, saved.Message);
        var loaded = await _provider.LoadAvatarAsync(avatar.Id);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.IsNotNull(loaded.Result);
        Assert.AreEqual(avatar.Username, loaded.Result.Username);
        Assert.AreEqual(avatar.Email, loaded.Result.Email);
    }

    [TestMethod]
    public async Task LoadAvatarByUsername_FindsSavedAvatar()
    {
        var avatar = NewAvatar();
        var saved = await _provider.SaveAvatarAsync(avatar);
        Assert.IsFalse(saved.IsError, saved.Message);
        var loaded = await _provider.LoadAvatarByUsernameAsync(avatar.Username);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.IsNotNull(loaded.Result);
        Assert.AreEqual(avatar.Id, loaded.Result.Id);
    }

    [TestMethod]
    public async Task SaveAndLoadHolon_RoundTrips()
    {
        var holon = new Holon { Id = Guid.NewGuid(), Name = $"OASIS Mongo IT {Guid.NewGuid():N}", Description = "Portable loopback MongoDB integration round trip" };
        var saved = await _provider.SaveHolonAsync(holon);
        Assert.IsFalse(saved.IsError, saved.Message);
        var loaded = await _provider.LoadHolonAsync(holon.Id);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.IsNotNull(loaded.Result);
        Assert.AreEqual(holon.Name, loaded.Result.Name);
        Assert.AreEqual(holon.Description, loaded.Result.Description);
        var deleted = await _provider.DeleteHolonAsync(holon.Id);
        Assert.IsFalse(deleted.IsError, deleted.Message);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_provider?.IsProviderActivated == true)
            _provider.DeActivateProvider();
    }

    private static Avatar NewAvatar() => new()
    {
        Id = Guid.NewGuid(), Username = $"oasis-mongo-it-{Guid.NewGuid():N}",
        Email = $"oasis-mongo-it-{Guid.NewGuid():N}@test.local", FirstName = "Mongo", LastName = "Integration"
    };

    private static string Require(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"{name} must identify a disposable loopback MongoDB instance.");
        return value!;
    }
}

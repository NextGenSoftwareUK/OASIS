using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
public sealed class GoogleCloudOASISIntegrationTests
{
    private GoogleCloudOASIS _provider = null!;

    [TestInitialize]
    public async Task Setup()
    {
        string emulatorHost = Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")
            ?? throw new InvalidOperationException("FIRESTORE_EMULATOR_HOST must identify a running official Firestore emulator.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(emulatorHost));

        _provider = new GoogleCloudOASIS(
            projectId: Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") ?? "oasis-integration",
            enableStorage: false,
            enableFirestore: true,
            enableBigQuery: false);
        var activation = await _provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        Assert.IsTrue(activation.Result);
    }

    [TestMethod]
    public async Task FirestoreAvatarCrud_RoundTripsUsingGoogleSdk()
    {
        DateTime now = DateTime.UtcNow;
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = $"firestore-{Guid.NewGuid():N}",
            Email = $"firestore-{Guid.NewGuid():N}@integration.invalid",
            FirstName = "Google",
            LastName = "SDK",
            CreatedDate = now,
            ModifiedDate = now
        };

        var save = await _provider.SaveAvatarAsync(avatar);
        Assert.IsFalse(save.IsError, save.Message);
        Assert.AreEqual(avatar.Id.ToString(), save.Result.ProviderUniqueStorageKey[ProviderType.GoogleCloudOASIS]);

        var load = await _provider.LoadAvatarAsync(avatar.Id);
        Assert.IsFalse(load.IsError, load.Message);
        Assert.AreEqual(avatar.Username, load.Result.Username);

        var delete = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
        Assert.IsFalse(delete.IsError, delete.Message);
        Assert.IsTrue(delete.Result);
    }

    [TestMethod]
    public async Task FirestoreHolonCrud_RoundTripsUsingGoogleSdk()
    {
        DateTime now = DateTime.UtcNow;
        var holon = new Holon
        {
            Id = Guid.NewGuid(),
            Name = $"Firestore holon {Guid.NewGuid():N}",
            Description = "Official Firestore emulator integration",
            CreatedDate = now,
            ModifiedDate = now,
            DeletedDate = now,
            IsActive = true
        };

        var save = await _provider.SaveHolonAsync(holon);
        Assert.IsFalse(save.IsError, save.Message);
        Assert.AreEqual(holon.Id.ToString(), save.Result.ProviderUniqueStorageKey[ProviderType.GoogleCloudOASIS]);

        var load = await _provider.LoadHolonAsync(holon.Id);
        Assert.IsFalse(load.IsError, load.Message);
        Assert.AreEqual(holon.Name, load.Result.Name);

        var delete = await _provider.DeleteHolonAsync(holon.Id);
        Assert.IsFalse(delete.IsError, delete.Message);
        Assert.AreEqual(holon.Id, delete.Result.Id);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_provider.IsProviderActivated)
            await _provider.DeActivateProviderAsync();
    }
}

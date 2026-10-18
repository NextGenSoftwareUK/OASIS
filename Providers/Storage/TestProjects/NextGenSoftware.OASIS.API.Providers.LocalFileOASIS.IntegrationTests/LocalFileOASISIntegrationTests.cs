using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.IntegrationTests;

[TestClass]
public class LocalFileOASISIntegrationTests
{
    private string _storageDirectory = null!;
    private LocalFileOASIS _provider = null!;

    [TestInitialize]
    public void Setup()
    {
        _storageDirectory = Path.Combine(Path.GetTempPath(), "OASIS-LocalFile-tests", Guid.NewGuid().ToString("N"));
        _provider = new LocalFileOASIS(storageDirectory: _storageDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _provider.DeActivateProvider();
        if (Directory.Exists(_storageDirectory))
            Directory.Delete(_storageDirectory, recursive: true);
    }

    [TestMethod]
    public async Task AvatarCrud_PersistsToIsolatedDirectory()
    {
        var activated = await _provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);

        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = $"local-{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@example.test",
            FirstName = "Local",
            LastName = "File"
        };

        var saved = await _provider.SaveAvatarAsync(avatar);
        Assert.IsFalse(saved.IsError, saved.Message);
        var loaded = await _provider.LoadAvatarAsync(avatar.Id);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(avatar.Username, loaded.Result.Username);

        avatar.FirstName = "Updated";
        Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
        Assert.AreEqual("Updated", (await _provider.LoadAvatarAsync(avatar.Id)).Result.FirstName);
        Assert.IsTrue((await _provider.DeleteAvatarAsync(avatar.Id, softDelete: false)).Result);
        Assert.IsNull((await _provider.LoadAvatarAsync(avatar.Id)).Result);
    }

    [TestMethod]
    public async Task HolonCrud_PersistsToIsolatedDirectory()
    {
        Assert.IsFalse((await _provider.ActivateProviderAsync()).IsError);
        var holon = new Holon { Id = Guid.NewGuid(), Name = $"local-{Guid.NewGuid():N}" };

        var saved = await _provider.SaveHolonAsync(holon);
        Assert.IsFalse(saved.IsError, saved.Message);
        Assert.AreEqual(holon.Name, (await _provider.LoadHolonAsync(holon.Id)).Result.Name);

        holon.Name += "-updated";
        Assert.IsFalse((await _provider.SaveHolonAsync(holon)).IsError);
        Assert.AreEqual(holon.Name, (await _provider.LoadHolonAsync(holon.Id)).Result.Name);
        Assert.IsFalse((await _provider.DeleteHolonAsync(holon.Id)).IsError);
        Assert.IsNull((await _provider.LoadHolonAsync(holon.Id)).Result);
    }

    [TestMethod]
    public void Activation_RejectsAPathThatIsNotADirectory()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"oasis-localfile-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(filePath, "not a directory");
        try
        {
            var provider = new LocalFileOASIS(storageDirectory: filePath);
            var result = provider.ActivateProvider();
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(provider.IsProviderActivated);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}

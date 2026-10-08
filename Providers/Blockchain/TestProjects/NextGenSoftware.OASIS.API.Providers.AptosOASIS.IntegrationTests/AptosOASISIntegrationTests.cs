using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AptosOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.IntegrationTests;

[TestClass]
public class AptosOASISIntegrationTests
{
    private static string RequiredEnvironment(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new AssertFailedException($"Required real-runtime setting {name} is missing.");

    private static AptosOASIS CreateProvider() => new(
        RequiredEnvironment("OASIS_APTOS_RPC_ENDPOINT"),
        "local",
        RequiredEnvironment("OASIS_APTOS_PRIVATE_KEY"),
        RequiredEnvironment("OASIS_APTOS_CONTRACT_ADDRESS"));

    [TestMethod]
    public async Task OfficialSdk_ActivatesAgainstRealAptosLedger()
    {
        using var provider = CreateProvider();
        var result = await provider.ActivateProviderAsync();

        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.Result);
        Assert.IsTrue(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task OfficialSdk_PerformsRealHolonCreateReadUpdateDelete()
    {
        using var provider = CreateProvider();
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);

        var holon = new Holon
        {
            Id = Guid.NewGuid(),
            Name = "aptos-runtime-create",
            Description = "written through the Aptos Labs SDK"
        };

        var created = await provider.SaveHolonAsync(holon);
        Assert.IsFalse(created.IsError, created.Message);

        var loaded = await provider.LoadHolonAsync(holon.Id);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(holon.Name, loaded.Result.Name);

        holon.Name = "aptos-runtime-update";
        var updated = await provider.SaveHolonAsync(holon);
        Assert.IsFalse(updated.IsError, updated.Message);
        var reloaded = await provider.LoadHolonAsync(holon.Id);
        Assert.IsFalse(reloaded.IsError, reloaded.Message);
        Assert.AreEqual("aptos-runtime-update", reloaded.Result.Name);

        var deleted = await provider.DeleteHolonAsync(holon.Id);
        Assert.IsFalse(deleted.IsError, deleted.Message);
        Assert.AreEqual(holon.Id, deleted.Result.Id);
    }

    [TestMethod]
    public async Task OfficialSdk_RejectsUnreachableFullnode()
    {
        using var provider = new AptosOASIS(
            "http://127.0.0.1:1/v1",
            "unreachable",
            RequiredEnvironment("OASIS_APTOS_PRIVATE_KEY"),
            RequiredEnvironment("OASIS_APTOS_CONTRACT_ADDRESS"));

        var result = await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}

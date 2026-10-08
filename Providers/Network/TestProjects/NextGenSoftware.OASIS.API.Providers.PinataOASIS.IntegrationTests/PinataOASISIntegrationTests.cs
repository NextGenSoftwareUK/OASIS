using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.PinataOASIS.IntegrationTests;

[TestClass]
public class PinataOASISIntegrationTests
{
    [TestMethod]
    public async Task Activation_RejectsMissingCredentialsWithoutClaimingActive()
    {
        var provider = new PinataOASIS("https://api.pinata.cloud", "", "", "", "https://gateway.pinata.cloud");
        var result = await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task HolonRoundTrip_UsesAuthenticatedPinataApi()
    {
        string? jwt = Environment.GetEnvironmentVariable("OASIS_PINATA_JWT");
        string? apiKey = Environment.GetEnvironmentVariable("OASIS_PINATA_API_KEY");
        string? secret = Environment.GetEnvironmentVariable("OASIS_PINATA_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(jwt) && (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secret)))
            Assert.Inconclusive("Set OASIS_PINATA_JWT or OASIS_PINATA_API_KEY and OASIS_PINATA_SECRET_KEY to run the real Pinata test.");

        var provider = new PinataOASIS("https://api.pinata.cloud", apiKey ?? "", secret ?? "", jwt ?? "",
            Environment.GetEnvironmentVariable("OASIS_PINATA_GATEWAY_URL") ?? "https://gateway.pinata.cloud");
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);

        var holon = new Holon { Id = Guid.NewGuid(), Name = $"pinata-{Guid.NewGuid():N}" };
        var saved = await provider.SaveHolonAsync(holon);
        Assert.IsFalse(saved.IsError, saved.Message);
        string providerKey = saved.Result.ProviderUniqueStorageKey[ProviderType.PinataOASIS];
        Assert.IsFalse(string.IsNullOrWhiteSpace(providerKey));

        var loaded = await provider.LoadHolonAsync(providerKey);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(holon.Id, loaded.Result.Id);
        Assert.AreEqual(holon.Name, loaded.Result.Name);

        var deleted = await provider.DeleteHolonAsync(providerKey);
        Assert.IsFalse(deleted.IsError, deleted.Message);
    }
}

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
        if (Environment.GetEnvironmentVariable("OASIS_PINATA_HOSTED_WRITE_TEST") != "1")
            Assert.Inconclusive("Enable only for a dedicated Pinata test account and synthetic public records.");

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
        string? providerKey = null;
        try
        {
            var saved = await provider.SaveHolonAsync(holon, saveChildren: false);
            providerKey = saved.Result?.ProviderUniqueStorageKey[ProviderType.PinataOASIS];
            Assert.IsFalse(saved.IsError, saved.Exception?.GetType().FullName);
            Assert.IsTrue(saved.IsSaved);
            Assert.AreEqual(1, saved.SavedCount);
            Assert.IsFalse(string.IsNullOrWhiteSpace(providerKey));

            var loaded = await provider.LoadHolonAsync(holon.Id, loadChildren: false);
            Assert.IsFalse(loaded.IsError, loaded.Exception?.GetType().FullName);
            Assert.IsTrue(loaded.IsLoaded);
            Assert.AreEqual(holon.Id, loaded.Result.Id);
            Assert.AreEqual(holon.Name, loaded.Result.Name);
            Assert.AreEqual(providerKey, loaded.Result.ProviderUniqueStorageKey[ProviderType.PinataOASIS]);
        }
        finally
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(providerKey))
                {
                    var deleted = await provider.DeleteHolonAsync(providerKey);
                    Assert.IsFalse(deleted.IsError, deleted.Exception?.GetType().FullName);
                }
            }
            finally
            {
                await provider.DeActivateProviderAsync();
            }
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.RootstockOASIS.IntegrationTests;

[TestClass]
public class RootstockOASISIntegrationTests
{
    [TestMethod]
    public async Task HolonCrud_UsesValidatedRootstockChain()
    {
        string? rpc = Environment.GetEnvironmentVariable("OASIS_ROOTSTOCK_RPC_URL");
        string? key = Environment.GetEnvironmentVariable("OASIS_ROOTSTOCK_PRIVATE_KEY");
        string? contract = Environment.GetEnvironmentVariable("OASIS_ROOTSTOCK_CONTRACT_ADDRESS");
        if (string.IsNullOrWhiteSpace(rpc) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(contract))
            Assert.Inconclusive("Set the OASIS_ROOTSTOCK_* variables to run the real-runtime test.");

        var provider = new RootstockOASIS(rpc!, key!, contract!);
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);

        var holon = new Holon { Id = Guid.NewGuid(), Name = $"rootstock-{Guid.NewGuid():N}" };
        Assert.IsFalse((await provider.SaveHolonAsync(holon)).IsError);
        Assert.AreEqual(holon.Name, (await provider.LoadHolonAsync(holon.Id)).Result.Name);
        holon.Name += "-updated";
        Assert.IsFalse((await provider.SaveHolonAsync(holon)).IsError);
        Assert.AreEqual(holon.Name, (await provider.LoadHolonAsync(holon.Id)).Result.Name);
        Assert.IsFalse((await provider.DeleteHolonAsync(holon.Id)).IsError);
        Assert.IsTrue((await provider.LoadHolonAsync(holon.Id)).IsError);
    }

    [TestMethod]
    public async Task Activation_RejectsUnreachableRpc()
    {
        var provider = new RootstockOASIS("http://127.0.0.1:1",
            "0x0000000000000000000000000000000000000000000000000000000000000001",
            "0x0000000000000000000000000000000000000000");
        var result = await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}

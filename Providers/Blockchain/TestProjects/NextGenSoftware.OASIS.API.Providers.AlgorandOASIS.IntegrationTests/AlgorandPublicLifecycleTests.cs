using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS.IntegrationTests;

[TestClass]
public sealed class AlgorandPublicLifecycleTests
{
    [TestMethod]
    public async Task PublicAlgodLifecycleAndAccountRead()
    {
        const string address = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAY5HFKQ";
        var provider = new AlgorandOASIS("https://mainnet-api.algonode.cloud", "https://mainnet-idx.algonode.cloud");
        Assert.IsFalse(provider.IsProviderActivated);

        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        Assert.IsTrue(activated.Result);
        Assert.IsTrue(provider.IsProviderActivated);

        var loaded = await provider.LoadAvatarByProviderKeyAsync(address);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.IsNotNull(loaded.Result);
        Assert.AreEqual(address, loaded.Result.ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);
        var metadata = loaded.Result.ProviderMetaData[ProviderType.AlgorandOASIS];
        Assert.AreEqual(address, metadata["address"]);
        Assert.IsTrue(ulong.TryParse(metadata["balance_microALGO"], out _));

        Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsTrue(provider.ActivateProvider().Result);
        Assert.IsTrue(provider.IsProviderActivated);
        Assert.IsTrue(provider.DeActivateProvider().Result);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}

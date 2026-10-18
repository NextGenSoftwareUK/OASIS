using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class NativeProviderDispatchTests
{
    [Theory]
    [InlineData(typeof(CosmosBlockChainOASIS))]
    [InlineData(typeof(HashgraphOASIS))]
    [InlineData(typeof(SuiOASIS))]
    [InlineData(typeof(TRONOASIS))]
    public void DeclaredNativeContractsHaveConcreteDispatchWithoutCoreTokenDefaults(Type provider)
    {
        var contracts = new[] { typeof(IOASISStorageProvider), typeof(IOASISDBStorageProvider), typeof(IOASISNETProvider),
            typeof(IOASISBlockchainStorageProvider), typeof(IOASISSmartContractProvider), typeof(IOASISNFTProvider) };
        var methodCount = 0;
        foreach (var contract in contracts)
        {
            Assert.True(contract.IsAssignableFrom(provider), provider.Name + " must implement " + contract.Name);
            var map = provider.GetInterfaceMap(contract);
            Assert.Equal(map.InterfaceMethods.Length, map.TargetMethods.Length);
            foreach (var method in map.TargetMethods.Where(method => !method.IsSpecialName))
            {
                methodCount++;
                Assert.False(method.IsAbstract, provider.Name + ": " + method.Name);
                // Core's four Karma delegates are real shared business behavior. Its six
                // default token selectors return absence without consulting provider storage.
                if (method.DeclaringType == typeof(OASISStorageProviderBase))
                    Assert.Contains(method.Name, new[] { "AddKarmaToAvatar", "AddKarmaToAvatarAsync", "RemoveKarmaFromAvatar", "RemoveKarmaFromAvatarAsync" });
            }
        }
        Assert.True(methodCount > 0);
        Console.WriteLine($"DISPATCH_ONLY {provider.Name}: {contracts.Length} declared contracts, {methodCount} dispatch entries checked. Not functional SDK/API acceptance.");
    }
}

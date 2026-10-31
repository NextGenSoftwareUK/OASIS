using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Wallets;

public class NativeFeeCapabilityTests
{
    [Theory]
    [InlineData(ProviderType.SuiOASIS)] [InlineData(ProviderType.CosmosBlockChainOASIS)]
    [InlineData(ProviderType.HashgraphOASIS)] [InlineData(ProviderType.TRONOASIS)]
    public async Task FeeCapabilityRejectsMissingOriginalIdentityWithoutActivation(ProviderType type)
    {
        IOASISStorageProvider provider=type switch{
            ProviderType.SuiOASIS=>new API.Providers.SuiOASIS.SuiOASIS(),
            ProviderType.CosmosBlockChainOASIS=>new API.Providers.CosmosBlockChainOASIS.CosmosBlockChainOASIS(),
            ProviderType.HashgraphOASIS=>new API.Providers.HashgraphOASIS.HashgraphOASIS(),
            ProviderType.TRONOASIS=>new API.Providers.TRONOASIS.TRONOASIS(),
            _=>throw new ArgumentOutOfRangeException(nameof(type))
        };
        try{
            var capability=Assert.IsAssignableFrom<IOASISNativeFeeReceiptProvider>(provider);
            Assert.False(provider.IsProviderActivated);
            var result=await capability.GetNativeFeeReceiptAsync("");
            Assert.True(result.IsError);Assert.Equal(JsonValueKind.Undefined,result.Result.ValueKind);
            Assert.False(provider.IsProviderActivated);
        }finally{await provider.DeActivateProviderAsync();(provider as IDisposable)?.Dispose();}
    }
}

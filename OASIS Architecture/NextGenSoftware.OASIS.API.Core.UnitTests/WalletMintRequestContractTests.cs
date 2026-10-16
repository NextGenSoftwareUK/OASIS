using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class WalletMintRequestContractTests
{
    [Fact]
    public void ExplicitMintTargetAndRecipientSurviveJsonRoundTrip()
    {
        var request = new MintWeb3TokenRequest {
            TokenAddress="0x"+new string('1',40), ToWalletAddress="0x"+new string('2',40),
            MintedByAvatarId=Guid.NewGuid(), Amount=2.345678m };
        IMintWeb3TokenRequest restored = JsonConvert.DeserializeObject<MintWeb3TokenRequest>(JsonConvert.SerializeObject(request))!;
        Assert.Equal(request.TokenAddress, restored.TokenAddress);
        Assert.Equal(request.ToWalletAddress, restored.ToWalletAddress);
        Assert.Equal(request.MintedByAvatarId, restored.MintedByAvatarId);
        Assert.Equal(request.Amount, restored.Amount);
    }

    [Fact]
    public void OlderRequestImplementersDoNotAcquireInventedMintTargets()
    {
        IMintWeb3TokenRequest request = new ExistingMintRequest();
        Assert.Null(request.TokenAddress);
        Assert.Null(request.ToWalletAddress);
    }

    private sealed class ExistingMintRequest : MintTokenRequestBase, IMintWeb3TokenRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; } = null!;
    }
}

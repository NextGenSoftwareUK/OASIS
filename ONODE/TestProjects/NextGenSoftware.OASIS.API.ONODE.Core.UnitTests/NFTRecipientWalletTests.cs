using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

public sealed class NFTRecipientWalletTests
{
    [Fact]
    public async Task LookupUsesRecipientNotMintingAvatarAndDoesNotModifyEitherIdentity()
    {
        var request = new MintWeb4NFTRequest { MintedByAvatarId = Guid.NewGuid(), SendToAvatarAfterMintingId = Guid.NewGuid(),
            OnChainProvider = new EnumValue<ProviderType>(ProviderType.SuiOASIS) };
        var actor = request.MintedByAvatarId;
        var recipient = request.SendToAvatarAfterMintingId;
        var calls = 0;
        var address = await Resolve(request, (id, provider) => {
            calls++;
            Assert.Equal(recipient, id);
            Assert.NotEqual(actor, id);
            Assert.Equal(ProviderType.SuiOASIS, provider);
            return Task.FromResult(new OASISResult<IProviderWallet> { Result = new ProviderWallet { IsDefaultWallet = true, WalletAddress = "recipient-wallet" } });
        });
        Assert.Equal("recipient-wallet", address);
        Assert.Equal(1, calls);
        Assert.Equal(actor, request.MintedByAvatarId);
        Assert.Equal(recipient, request.SendToAvatarAfterMintingId);
    }

    [Theory]
    [InlineData(false, "wallet", false)]
    [InlineData(true, "", false)]
    [InlineData(true, "wallet", true)]
    public async Task MissingDefaultAddressOrFailedLookupNeverSelectsAnotherWallet(bool isDefault, string address, bool failed)
    {
        var request = new MintWeb4NFTRequest { SendToAvatarAfterMintingId = Guid.NewGuid(), OnChainProvider = new EnumValue<ProviderType>(ProviderType.SuiOASIS) };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Resolve(request, (_, _) => Task.FromResult(
            new OASISResult<IProviderWallet> { IsError = failed, Result = new ProviderWallet { IsDefaultWallet = isDefault, WalletAddress = address } })));
        Assert.Null(request.SendToAddressAfterMinting);
    }

    private static Task<string> Resolve(IMintWeb4NFTRequest request, Func<Guid, ProviderType, Task<OASISResult<IProviderWallet>>> lookup)
        => (Task<string>)typeof(NFTManager).GetMethod("ResolveDefaultMintRecipientAsync", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { request, lookup })!;
}

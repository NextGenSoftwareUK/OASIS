using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class WalletTokenSendRequestTests
{
    [Fact]
    public void AvatarSigningProjectionDoesNotExposePrivateKeyOnCallerOrModifyStoredWallet()
    {
        var request = new SendWeb4TokenRequest { FromTokenAddress = "0x2::sui::SUI", ToWalletAddress = "recipient", Amount = 1,
            ToAvatarEmail = "recipient@example.test", MemoText = "original memo" };
        var wallet = new NextGenSoftware.OASIS.API.Core.Objects.ProviderWallet
        { WalletAddress = "sender", PrivateKey = "test-only-private", PublicKey = "test-only-public" };
        var signed = WalletManager.CreateAvatarSignedTokenRequest(request, wallet);
        Assert.NotSame(request, signed); Assert.Null(request.OwnerPrivateKey); Assert.Null(request.FromWalletAddress);
        Assert.Equal(wallet.PrivateKey, signed.OwnerPrivateKey); Assert.Equal(wallet.PublicKey, signed.OwnerPublicKey);
        Assert.Equal(wallet.WalletAddress, signed.FromWalletAddress); Assert.Equal(request.ToAvatarEmail, signed.ToAvatarEmail);
        Assert.Equal(request.Amount, signed.Amount); Assert.Equal(request.MemoText, signed.MemoText);
        Assert.Equal("test-only-private", wallet.PrivateKey);
    }

    [Fact]
    public void ProviderRequestPreservesAssetIdentityAndSigningCredentials()
    {
        var request = new SendWeb4TokenRequest
        {
            Web3TokenId = Guid.NewGuid(), FromTokenAddress = "0x2::sui::SUI",
            FromWalletAddress = "sender", ToWalletAddress = "recipient", Amount = 0.001m,
            MemoText = "must reach provider validation", OwnerPrivateKey = "private-key",
            OwnerPublicKey = "public-key", OwnerSeedPhrase = "seed-phrase"
        };
        var projected = WalletManager.CreateProviderTokenSendRequest(request);
        Assert.Equal(request.Web3TokenId, projected.Web3TokenId);
        Assert.Equal(request.FromTokenAddress, projected.FromTokenAddress);
        Assert.Equal(request.FromWalletAddress, projected.FromWalletAddress);
        Assert.Equal(request.ToWalletAddress, projected.ToWalletAddress);
        Assert.Equal(request.Amount, projected.Amount);
        Assert.Equal(request.MemoText, projected.MemoText);
        Assert.Equal(request.OwnerPrivateKey, projected.OwnerPrivateKey);
        Assert.Equal(request.OwnerPublicKey, projected.OwnerPublicKey);
        Assert.Equal(request.OwnerSeedPhrase, projected.OwnerSeedPhrase);
        Assert.NotSame(request, projected);
    }

    [Fact]
    public void NullRequestIsNotConvertedToAValidEmptyTransfer()
    {
        Assert.Throws<ArgumentNullException>(() => WalletManager.CreateProviderTokenSendRequest(null!));
    }
}

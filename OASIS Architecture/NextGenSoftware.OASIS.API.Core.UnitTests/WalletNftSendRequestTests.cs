using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Xunit;

public sealed class WalletNftSendRequestTests
{
    [Fact]
    public void ProjectionPreservesAssetQuantityAndSigner()
    {
        var request = new SendWeb4NFTRequest
        {
            FromNFTTokenAddress = "0.0.1234/7", TokenAddress = "0.0.1234", TokenId = "7",
            FromWalletAddress = "0.0.1001", ToWalletAddress = "0.0.1002", Amount = 1,
            MemoText = "test", OwnerPrivateKey = "synthetic-key", OwnerPublicKey = "synthetic-public", OwnerSeedPhrase = "synthetic-seed"
        };
        var projected = WalletManager.CreateProviderNftSendRequest(request);
        Assert.Equal(request.FromNFTTokenAddress, projected.FromNFTTokenAddress);
        Assert.Equal(request.TokenAddress, projected.TokenAddress);
        Assert.Equal(request.TokenId, projected.TokenId);
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
    public void MissingRequestIsRejected() => Assert.Throws<ArgumentNullException>(() => WalletManager.CreateProviderNftSendRequest(null!));
}

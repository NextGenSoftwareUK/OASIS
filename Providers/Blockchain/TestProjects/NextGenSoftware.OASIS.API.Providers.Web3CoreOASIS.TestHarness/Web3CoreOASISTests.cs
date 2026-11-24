using System.Numerics;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.TestHarness;

public sealed class Web3CoreOASISTests(Web3CoreOASISFixture fixture) : IClassFixture<Web3CoreOASISFixture>
{
    private readonly Web3CoreOASISFixture _fixture = fixture;

    [Theory]
    [InlineData(1, new byte[] { 0x01 }, new byte[] { 0x01 })]
    [InlineData(2, new byte[] { 0x02 }, new byte[] { 0x02 })]
    [InlineData(3, new byte[] { 0x03 }, new byte[] { 0x03 })]
    public async Task Web3CoreOASIS_CreateAvatarAsync_ShouldCreateAvatar_Scenario(uint entityId, byte[] avatarId, byte[] info)
    {
        string transactionHash = await _fixture.Web3CoreOASIS.CreateAvatarAsync(entityId, avatarId, info);
        Assert.False(string.IsNullOrEmpty(transactionHash));
    }

    [Theory]
    [InlineData(1, new byte[] { 0x01 }, new byte[] { 0x01 })]
    [InlineData(2, new byte[] { 0x02 }, new byte[] { 0x02 })]
    [InlineData(3, new byte[] { 0x03 }, new byte[] { 0x03 })]
    public async Task Web3CoreOASIS_CreateHolonAsync_ShouldCreateHolon_Scenario(uint entityId, byte[] holonId, byte[] info)
    {
        string transactionHash = await _fixture.Web3CoreOASIS.CreateHolonAsync(entityId, holonId, info);
        Assert.False(string.IsNullOrEmpty(transactionHash));
    }

    [Theory]
    [InlineData(1, new byte[] { 0x01 }, new byte[] { 0x01 })]
    [InlineData(2, new byte[] { 0x02 }, new byte[] { 0x02 })]
    [InlineData(3, new byte[] { 0x03 }, new byte[] { 0x03 })]
    public async Task Web3CoreOASIS_CreateAvatarDetailAsync_ShouldCreateAvatarDetail_Scenario(uint entityId, byte[] avatarId, byte[] info)
    {
        string transactionHash = await _fixture.Web3CoreOASIS.CreateAvatarDetailAsync(entityId, avatarId, info);
        Assert.False(string.IsNullOrEmpty(transactionHash));
    }

    [Theory]
    [InlineData(1, new byte[] { 0x01 })]
    [InlineData(2, new byte[] { 0x02 })]
    [InlineData(3, new byte[] { 0x03 })]
    public async Task Web3CoreOASIS_UpdateAvatarAsync_ShouldUpdateAvatar_Scenario(uint entityId, byte[] info)
    {
        bool result = await _fixture.Web3CoreOASIS.UpdateAvatarAsync(entityId, info);
        Assert.True(result);
    }

    [Theory]
    [InlineData(1, new byte[] { 0x01 })]
    [InlineData(2, new byte[] { 0x02 })]
    [InlineData(3, new byte[] { 0x03 })]
    public async Task Web3CoreOASIS_UpdateAvatarDetailAsync_ShouldUpdateAvatarDetail_Scenario(uint entityId, byte[] info)
    {
        bool result = await _fixture.Web3CoreOASIS.UpdateAvatarDetailAsync(entityId, info);
        Assert.True(result);
    }

    [Theory]
    [InlineData(1, new byte[] { 0x01 })]
    [InlineData(2, new byte[] { 0x02 })]
    [InlineData(3, new byte[] { 0x03 })]
    public async Task Web3CoreOASIS_UpdateHolonAsync_ShouldUpdateHolon_Scenario(uint entityId, byte[] info)
    {
        bool result = await _fixture.Web3CoreOASIS.UpdateHolonAsync(entityId, info);
        Assert.True(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Web3CoreOASIS_DeleteAvatarAsync_ShouldDeleteAvatar_Scenario(uint entityId)
    {
        bool result = await _fixture.Web3CoreOASIS.DeleteAvatarAsync(entityId);
        Assert.True(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Web3CoreOASIS_DeleteAvatarDetailAsync_ShouldDeleteAvatarDetail_Scenario(uint entityId)
    {
        bool result = await _fixture.Web3CoreOASIS.DeleteAvatarDetailAsync(entityId);
        Assert.True(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Web3CoreOASIS_DeleteHolonAsync_ShouldDeleteHolon_Scenario(uint entityId)
    {
        bool result = await _fixture.Web3CoreOASIS.DeleteHolonAsync(entityId);
        Assert.True(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Web3CoreOASIS_GetAvatarByIdAsync_ShouldReturnAvatar_Scenario(uint entityId)
    {
        EntityOASIS? avatar = await _fixture.Web3CoreOASIS.GetAvatarByIdAsync(entityId);
        Assert.NotNull(avatar);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Web3CoreOASIS_GetAvatarDetailByIdAsync_ShouldReturnAvatarDetail_Scenario(uint entityId)
    {
        EntityOASIS? avatarDetail = await _fixture.Web3CoreOASIS.GetAvatarDetailByIdAsync(entityId);
        Assert.NotNull(avatarDetail);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Web3CoreOASIS_GetHolonByIdAsync_ShouldReturnHolon_Scenario(uint entityId)
    {
        EntityOASIS? holon = await _fixture.Web3CoreOASIS.GetHolonByIdAsync(entityId);
        Assert.NotNull(holon);
    }

    [Fact]
    public async Task GetAvatarsCountAsync_ShouldReturnAvatarsCount_Scenario()
    {
        uint count = await _fixture.Web3CoreOASIS.GetAvatarsCountAsync();
        Assert.True(count >= 0);
    }

    [Fact]
    public async Task Web3CoreOASIS_GetAvatarDetailsCountAsync_ShouldReturnAvatarDetailsCount_Scenario()
    {
        uint count = await _fixture.Web3CoreOASIS.GetAvatarDetailsCountAsync();
        Assert.True(count >= 0);
    }

    [Fact]
    public async Task Web3CoreOASIS_GetHolonsCountAsync_ShouldReturnHolonsCount_Scenario()
    {
        uint count = await _fixture.Web3CoreOASIS.GetHolonsCountAsync();
        Assert.True(count >= 0);
    }

    /// <summary>
    /// Test for minting an NFT using the MintAsync method.
    /// Verifies that the transaction hash is not empty, indicating the transaction was sent successfully.
    /// </summary>
    [Theory]
    [InlineData("0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266", "{\"name\":\"NFT1\",\"description\":\"My first NFT\"}")]
    [InlineData("0x70997970C51812dc3A010C7d01b50e0d17dc79C8", "{\"name\":\"NFT2\",\"description\":\"My second NFT\"}")]
    public async Task Web3CoreOASIS_MintAsync_ShouldMintNFT_Scenario(string toAddress, string metadataJson)
    {
        // Ensure that the test environment is running, the contract is deployed, and the required accounts exist.
        // Verify that the address provided in the test has sufficient funds to mint the NFT.
        // When running multiple mint operations, be aware that tokens may be moved or consumed, which can affect subsequent tests.

        string transactionHash = await _fixture.Web3CoreOASIS.MintAsync(toAddress, metadataJson);
        Assert.False(string.IsNullOrEmpty(transactionHash));
    }

    /// <summary>
    /// Test for sending an NFT using the SendNFTAsync method.
    /// Verifies that the transaction hash is not empty, indicating the transaction was sent successfully.
    /// </summary>
    [Theory]
    [InlineData("0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266", "0x70997970C51812dc3A010C7d01b50e0d17dc79C8", 2, "provider1", "provider2", 100, "Test memo")]
    [InlineData("0x70997970C51812dc3A010C7d01b50e0d17dc79C8", "0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266", 3, "provider2", "provider1", 200, "Another memo")]
    public async Task Web3CoreOASIS_SendNFTAsync_ShouldSendNFT_Scenario(
        string fromAddress, 
        string toAddress, 
        BigInteger tokenId, 
        string fromProviderType, 
        string toProviderType, 
        BigInteger amount, 
        string memoText)
    {
        // Ensure that the test environment is running, the contract is deployed, and the required accounts exist.
        // Verify that the 'fromAddress' has the token with the specified tokenId and sufficient balance for sending.
        // Be aware that sending NFTs may affect the availability of tokens, potentially causing tests to fail if the tokens are no longer available.

        string transactionHash = await _fixture.Web3CoreOASIS.SendNFTAsync(
            fromAddress, 
            toAddress, 
            tokenId, 
            fromProviderType, 
            toProviderType, 
            amount, 
            memoText
        );
        Assert.False(string.IsNullOrEmpty(transactionHash));
    }
}
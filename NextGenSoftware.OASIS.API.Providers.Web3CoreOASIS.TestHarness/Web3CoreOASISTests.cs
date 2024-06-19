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
}
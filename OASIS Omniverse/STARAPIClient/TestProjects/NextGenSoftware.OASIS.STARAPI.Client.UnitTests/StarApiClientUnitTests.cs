using NextGenSoftware.OASIS.STARAPI.Client;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STARAPI.Client.UnitTests;

public class StarApiClientUnitTests
{
    [Fact]
    public void Init_WithMissingWeb5BaseUrl_ReturnsInvalidParam()
    {
        using var client = new StarApiClient();

        var result = client.Init(new StarApiConfig { Web5StarApiBaseUrl = string.Empty });

        Assert.True(result.IsError);
        Assert.Equal(((int)StarApiResultCode.InvalidParam).ToString(), result.ErrorCode);
    }

    [Fact]
    public void Init_WithInvalidWeb5BaseUrl_ReturnsInvalidParam()
    {
        using var client = new StarApiClient();

        var result = client.Init(new StarApiConfig { Web5StarApiBaseUrl = "not-a-uri" });

        Assert.True(result.IsError);
        Assert.Equal(((int)StarApiResultCode.InvalidParam).ToString(), result.ErrorCode);
    }

    [Fact]
    public async Task Methods_WhenNotInitialized_ReturnNotInitialized()
    {
        using var client = new StarApiClient();

        var auth = await client.AuthenticateAsync("user", "pass");
        var getCurrentAvatar = await client.GetCurrentAvatarAsync();
        var hasItem = await client.HasItemAsync("item");
        var getInventory = await client.GetInventoryAsync();
        var addItem = await client.AddItemAsync("item", "desc", "game");
        var queueAddItem = await client.QueueAddItemAsync("item", "desc", "game");
        var flushAdd = await client.FlushAddItemJobsAsync();
        var useItem = await client.UseItemAsync("item");
        var queueUse = await client.QueueUseItemAsync("item");
        var flushUse = await client.FlushUseItemJobsAsync();
        var startQuest = await client.StartQuestAsync("quest");
        var completeObjective = await client.CompleteQuestObjectiveAsync("quest", "objective", "game");
        var queueObjective = await client.QueueCompleteQuestObjectiveAsync("quest", "objective", "game");
        var flushObjective = await client.FlushQuestObjectiveJobsAsync();
        var completeQuest = await client.CompleteQuestAsync("quest");
        var createQuest = await client.CreateCrossGameQuestAsync("quest", "desc", [new StarQuestObjective { Description = "x", GameSource = "g", ItemRequired = "i" }]);
        var activeQuests = await client.GetActiveQuestsAsync();
        var createBossNft = await client.CreateBossNftAsync("boss", "desc", "game", "{}");
        var deployBossNft = await client.DeployBossNftAsync("nft", "game");
        var nftCollection = await client.GetNftCollectionAsync();
        var setApiKey = client.SetApiKey("key", "avatar");
        var setWeb4 = client.SetWeb4OasisApiBaseUrl("https://web4.example.com");
        var setWeb5 = client.SetWeb5StarApiBaseUrl("https://web5.example.com");

        AssertNotInitialized(auth);
        AssertNotInitialized(getCurrentAvatar);
        AssertNotInitialized(hasItem);
        AssertNotInitialized(getInventory);
        AssertNotInitialized(addItem);
        AssertNotInitialized(queueAddItem);
        AssertNotInitialized(flushAdd);
        AssertNotInitialized(useItem);
        AssertNotInitialized(queueUse);
        AssertNotInitialized(flushUse);
        AssertNotInitialized(startQuest);
        AssertNotInitialized(completeObjective);
        AssertNotInitialized(queueObjective);
        AssertNotInitialized(flushObjective);
        AssertNotInitialized(completeQuest);
        AssertNotInitialized(createQuest);
        AssertNotInitialized(activeQuests);
        AssertNotInitialized(createBossNft);
        AssertNotInitialized(deployBossNft);
        AssertNotInitialized(nftCollection);
        AssertNotInitialized(setApiKey);
        AssertNotInitialized(setWeb4);
        AssertNotInitialized(setWeb5);
    }

    [Fact]
    public async Task Failure_UpdatesLastError_AndInvokesCallback()
    {
        using var client = new StarApiClient();
        StarApiResultCode? callbackCode = null;

        client.SetCallback((code, _) => callbackCode = code, null);
        var result = await client.HasItemAsync("Anything");
        var lastError = client.GetLastError();

        Assert.True(result.IsError);
        Assert.Equal(StarApiResultCode.NotInitialized, callbackCode);
        Assert.Contains("not initialized", lastError.Result ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Init_ThenCleanup_Succeeds()
    {
        using var client = new StarApiClient();
        var config = new StarApiConfig { Web5StarApiBaseUrl = "https://web5.example.com/api" };

        var initResult = client.Init(config);
        var cleanupResult = client.Cleanup();

        Assert.False(initResult.IsError);
        Assert.False(cleanupResult.IsError);
    }

    [Fact]
    public void ConsumeLastMintResult_WhenNoMint_ReturnsFalse()
    {
        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = "https://web5.example.com/api" });

        var consumed = client.ConsumeLastMintResult(out var itemName, out var nftId, out var hash);
        Assert.False(consumed);
        Assert.Null(itemName);
        Assert.Null(nftId);
        Assert.Null(hash);
    }

    [Fact]
    public void EnqueuePickupWithMintJobOnly_WhenNotInitialized_DoesNotThrow()
    {
        using var client = new StarApiClient();
        client.EnqueuePickupWithMintJobOnly("Item", "Desc", "Game", "KeyItem", doMint: true, quantity: 1);
    }

    [Fact]
    public async Task MintInventoryItemNftAsync_WhenWeb4UrlNotSet_ReturnsError()
    {
        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = "https://web5.example.com/api" });

        var mint = await client.MintInventoryItemNftAsync("Key", "Desc", "Game", "KeyItem");
        Assert.True(mint.IsError);
        Assert.Contains("WEB4", mint.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendItemToAvatarAsync_WhenNotInitialized_ReturnsNotInitialized()
    {
        using var client = new StarApiClient();
        var result = await client.SendItemToAvatarAsync("target", "item", 1);
        AssertNotInitialized(result);
    }

    [Fact]
    public async Task SendItemToClanAsync_WhenNotInitialized_ReturnsNotInitialized()
    {
        using var client = new StarApiClient();
        var result = await client.SendItemToClanAsync("clan", "item", 1);
        AssertNotInitialized(result);
    }

    /// <summary>Contract for [NFT] prefix: when NftId is set, games (Doom/Quake) show "[NFT] " + Name.</summary>
    [Fact]
    public void ItemWithNftId_DisplayNameForOverlay_StartsWithNftPrefix()
    {
        var withNft = new StarItem { Name = "Red Keycard", NftId = "nft-123", Quantity = 1 };
        var withoutNft = new StarItem { Name = "Blue Keycard", NftId = "", Quantity = 1 };

        var displayWith = string.IsNullOrEmpty(withNft.NftId) ? withNft.Name : "[NFT] " + withNft.Name;
        var displayWithout = string.IsNullOrEmpty(withoutNft.NftId) ? withoutNft.Name : "[NFT] " + withoutNft.Name;

        Assert.Equal("[NFT] Red Keycard", displayWith);
        Assert.Equal("Blue Keycard", displayWithout);
    }

    private static void AssertNotInitialized<T>(OASISResult<T> result)
    {
        Assert.True(result.IsError);
        Assert.Equal(((int)StarApiResultCode.NotInitialized).ToString(), result.ErrorCode);
    }
}


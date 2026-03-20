using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Contracts;
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
        var queueAuth = await client.QueueAuthenticateAsync("user", "pass");
        var getCurrentAvatar = await client.GetCurrentAvatarAsync();
        var queueGetAvatar = await client.QueueGetCurrentAvatarAsync();
        var hasItem = await client.HasItemAsync("item");
        var getInventory = await client.GetInventoryAsync();
        var queueGetInventory = await client.QueueGetInventoryAsync();
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
        var createQuest = await client.CreateCrossGameQuestAsync("quest", "desc", [new StarQuestObjective { Description = "x", GameSource = "g", Order = 0, IsCompleted = false }]);
        var addObjective = await client.AddQuestObjectiveAsync("quest-id", "Objective desc", gameSource: "Doom");
        var removeObjective = await client.RemoveQuestObjectiveAsync("quest-id", "objective-id");
        var addSubQuest = await client.AddSubQuestAsync("quest-id", "Sub-quest desc", gameSource: "ODOOM");
        var setPrereqs = await client.SetQuestPrerequisitesAsync("quest-id", new[] { "prereq-id" });
        var activeQuests = await client.GetActiveQuestsAsync();
        var queueGetQuests = await client.QueueGetActiveQuestsAsync();
        var createMonsterNft = await client.CreateMonsterNftAsync("boss", "desc", "game", "{}");
        var deployBossNft = await client.DeployBossNftAsync("nft", "game");
        var nftCollection = await client.GetNftCollectionAsync();
        var setApiKey = client.SetApiKey("key", "avatar");
        var setWeb4 = client.SetWeb4OasisApiBaseUrl("https://web4.example.com");
        var setWeb5 = client.SetWeb5StarApiBaseUrl("https://web5.example.com");

        AssertNotInitialized(auth);
        AssertNotInitialized(queueAuth);
        AssertNotInitialized(getCurrentAvatar);
        AssertNotInitialized(queueGetAvatar);
        AssertNotInitialized(hasItem);
        AssertNotInitialized(getInventory);
        AssertNotInitialized(queueGetInventory);
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
        AssertNotInitialized(addObjective);
        AssertNotInitialized(removeObjective);
        AssertNotInitialized(addSubQuest);
        AssertNotInitialized(setPrereqs);
        AssertNotInitialized(activeQuests);
        AssertNotInitialized(queueGetQuests);
        AssertNotInitialized(createMonsterNft);
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

    [Fact]
    public async Task GetQuestsByStatusAsync_WhenNotInitialized_ReturnsNotInitialized()
    {
        using var client = new StarApiClient();
        var result = await client.GetQuestsByStatusAsync("InProgress");
        AssertNotInitialized(result);
    }

    [Fact]
    public async Task GetActiveQuestsAsync_WhenNotInitialized_ReturnsNotInitialized()
    {
        using var client = new StarApiClient();
        var result = await client.GetActiveQuestsAsync();
        AssertNotInitialized(result);
    }

    [Fact]
    public async Task GetQuestsByStatusAsync_WhenStatusEmpty_ReturnsInvalidParam()
    {
        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = "https://web5.example.com" });
        var result = await client.GetQuestsByStatusAsync("");
        Assert.True(result.IsError);
        Assert.Equal(((int)StarApiResultCode.InvalidParam).ToString(), result.ErrorCode);
    }

    [Fact]
    public void SerializeQuestsForGame_EmptyList_ReturnsEmptyString()
    {
        var serialized = StarApiClient.SerializeQuestsForGame(new List<StarQuestInfo>());
        Assert.NotNull(serialized);
        Assert.Empty(serialized);
    }

    [Fact]
    public void SerializeQuestsForGame_NullList_ReturnsEmptyString()
    {
        var serialized = StarApiClient.SerializeQuestsForGame(null);
        Assert.NotNull(serialized);
        Assert.Empty(serialized);
    }

    [Fact]
    public void SerializeQuestsForGame_WithObjectives_IncludesObjectiveLines()
    {
        var quests = new List<StarQuestInfo>
        {
            new()
            {
                Id = "q1",
                Name = "Quest With Objs",
                Description = "Desc",
                Status = "InProgress",
                Objectives = new List<StarQuestObjective>
                {
                    new() { Id = "o1", Description = "Objective 1", IsCompleted = false },
                    new() { Id = "o2", Description = "Objective 2", IsCompleted = true }
                }
            }
        };
        var serialized = StarApiClient.SerializeQuestsForGame(quests);
        Assert.NotNull(serialized);
        Assert.Contains("Q\tq1\t", serialized);
        Assert.Contains("O\t", serialized);
        Assert.Contains("Objective 1", serialized);
        Assert.Contains("Objective 2", serialized);
    }

    [Fact]
    public void SerializeQuestsForGame_WithSubquestParentId_PreservesParentQuestId()
    {
        var quests = new List<StarQuestInfo>
        {
            new() { Id = "parent-1", Name = "Parent", Description = "P", Status = "InProgress", ParentQuestId = "" },
            new() { Id = "child-1", Name = "Sub", Description = "S", Status = "NotStarted", ParentQuestId = "parent-1" }
        };
        var serialized = StarApiClient.SerializeQuestsForGame(quests);
        Assert.NotNull(serialized);
        Assert.Contains("parent-1", serialized);
        Assert.Contains("child-1", serialized);
        Assert.Contains("Sub", serialized);
    }

    [Fact]
    public async Task AddQuestObjectiveAsync_WhenQuestIdEmpty_ReturnsInvalidParam()
    {
        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = "http://localhost:8888", Web4OasisApiBaseUrl = "http://localhost:7777" });
        var result = await client.AddQuestObjectiveAsync("", "Objective description");
        Assert.True(result.IsError);
        Assert.Equal(((int)StarApiResultCode.InvalidParam).ToString(), result.ErrorCode);
    }

    [Fact]
    public async Task RemoveQuestObjectiveAsync_WhenQuestIdOrObjectiveIdEmpty_ReturnsInvalidParam()
    {
        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = "http://localhost:8888", Web4OasisApiBaseUrl = "http://localhost:7777" });
        var result = await client.RemoveQuestObjectiveAsync("quest-id", "");
        Assert.True(result.IsError);
        Assert.Equal(((int)StarApiResultCode.InvalidParam).ToString(), result.ErrorCode);
    }

    [Fact]
    public async Task AddSubQuestAsync_WhenNotInitialized_ReturnsNotInitialized()
    {
        using var client = new StarApiClient();
        var result = await client.AddSubQuestAsync("quest-id", "Sub-quest description", gameSource: "ODOOM");
        AssertNotInitialized(result);
    }

    [Fact]
    public async Task SetQuestPrerequisitesAsync_WhenNotInitialized_ReturnsNotInitialized()
    {
        using var client = new StarApiClient();
        var result = await client.SetQuestPrerequisitesAsync("quest-id", new[] { "prereq-quest-id" });
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


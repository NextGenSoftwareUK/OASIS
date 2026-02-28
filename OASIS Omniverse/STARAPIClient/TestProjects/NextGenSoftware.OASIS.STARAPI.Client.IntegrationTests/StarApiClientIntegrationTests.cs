using NextGenSoftware.OASIS.STARAPI.Client;

namespace NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests;

/// <summary>Integration tests run against real APIs by default (WEB5 localhost:5556, WEB4 localhost:5555).
/// Set STARAPI_INTEGRATION_USE_FAKE=true to use in-process fake servers instead (e.g. for CI with no real servers).</summary>
public class StarApiClientIntegrationTests : IAsyncLifetime
{
    private FakeStarApiServer? _web5Server;
    private FakeStarApiServer? _web4Server;
    private string _web5BaseUrl = "http://localhost:5556";
    private string _web4BaseUrl = "http://localhost:5555";
    private bool _useFakeServer;

    public Task InitializeAsync()
    {
        _useFakeServer = GetEnv("STARAPI_INTEGRATION_USE_FAKE", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
        _web5BaseUrl = GetEnv("STARAPI_WEB5_BASE_URL", "http://localhost:5556");
        _web4BaseUrl = GetEnv("STARAPI_WEB4_BASE_URL", "http://localhost:5555");

        if (_useFakeServer)
        {
            _web5Server = new FakeStarApiServer();
            _web4Server = new FakeStarApiServer();
            _web5BaseUrl = _web5Server.BaseUrl;
            _web4BaseUrl = _web4Server.BaseUrl;
        }

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_web5Server is not null)
            await _web5Server.DisposeAsync();

        if (_web4Server is not null)
            await _web4Server.DisposeAsync();
    }

    private static string GetEnv(string key, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    [Fact]
    public async Task FullWorkflow_ExercisesAllClientEndpoints_AndSucceeds()
    {
        using var client = new StarApiClient();
        var callbackCodes = new List<StarApiResultCode>();
        client.SetCallback((code, _) => callbackCodes.Add(code), null);

        var init = client.Init(new StarApiConfig
        {
            Web5StarApiBaseUrl = _web5BaseUrl,
            Web4OasisApiBaseUrl = _web4BaseUrl
        });
        Assert.False(init.IsError);

        Assert.False(client.SetWeb5StarApiBaseUrl(_web5BaseUrl).IsError);
        Assert.False(client.SetWeb4OasisApiBaseUrl(_web4BaseUrl).IsError);
        var username = _useFakeServer ? "integration-user" : GetEnv("STARAPI_USERNAME", "integration-user");
        var password = _useFakeServer ? "integration-password" : GetEnv("STARAPI_PASSWORD", "integration-password");
        if (_useFakeServer)
            Assert.False(client.SetApiKey("local-api-key", "11111111-1111-1111-1111-111111111111").IsError);

        var auth = await client.AuthenticateAsync(username, password);
        Assert.False(auth.IsError);
        Assert.True(string.IsNullOrWhiteSpace(auth.ErrorCode) || auth.ErrorCode == "0");

        var currentAvatar = await client.GetCurrentAvatarAsync();
        Assert.False(currentAvatar.IsError);
        if (_useFakeServer)
        {
            Assert.Equal("11111111-1111-1111-1111-111111111111", currentAvatar.Result!.Id.ToString());
            Assert.Equal("integration_user", currentAvatar.Result!.Username);
        }
        else
        {
            Assert.NotNull(currentAvatar.Result!.Id);
            Assert.NotEqual(Guid.Empty, currentAvatar.Result.Id);
        }

        var added = await client.AddItemAsync("Red Keycard", "Collected in room 101", "Doom", "KeyItem");
        Assert.False(added.IsError);

        var queuedAdd = await client.QueueAddItemAsync("Blue Keycard", "Collected in room 102", "Doom", "KeyItem");
        Assert.False(queuedAdd.IsError);

        var batchAdd = await client.QueueAddItemsAsync(
        [
            new StarItem { Name = "Ammo Box", Description = "Ammo pickup", GameSource = "Doom", ItemType = "PowerUp" },
            new StarItem { Name = "Medkit", Description = "Health pickup", GameSource = "Doom", ItemType = "Healing" }
        ]);
        Assert.False(batchAdd.IsError);
        Assert.False((await client.FlushAddItemJobsAsync()).IsError);

        var hasBlue = await client.HasItemAsync("Blue Keycard");
        Assert.False(hasBlue.IsError);
        Assert.True(hasBlue.Result);

        var inventory = await client.GetInventoryAsync();
        Assert.False(inventory.IsError);
        Assert.True(inventory.Result!.Count >= 4);
        Assert.Contains(inventory.Result, x => string.Equals(x.Name, "Red Keycard", StringComparison.OrdinalIgnoreCase));
        // When using fake server we also assert exact quest IDs below

        var useBlue = await client.UseItemAsync("Blue Keycard", "door_a");
        Assert.False(useBlue.IsError);
        Assert.True(useBlue.Result);

        var queueUse = await client.QueueUseItemAsync("Ammo Box", "combat_use");
        Assert.False(queueUse.IsError);
        Assert.False((await client.FlushUseItemJobsAsync()).IsError);

        string questId;
        string obj1Id;
        string obj2Id;
        if (_useFakeServer)
        {
            questId = "quest-main";
            obj1Id = "obj-1";
            obj2Id = "obj-2";
        }
        else
        {
            var createQuestFirst = await client.CreateCrossGameQuestAsync(
                "Cross Quest",
                "Integration quest",
                [new StarQuestObjective { Description = "Collect 1 key", GameSource = "Doom", ItemRequired = "Key", IsCompleted = false }]);
            Assert.False(createQuestFirst.IsError);
            var activeFirst = await client.GetActiveQuestsAsync();
            Assert.False(activeFirst.IsError);
            Assert.NotEmpty(activeFirst.Result!);
            questId = activeFirst.Result![0].Id;
            var objs = activeFirst.Result[0].Objectives;
            obj1Id = objs.Count > 0 ? objs[0].Id : questId;
            obj2Id = objs.Count > 1 ? objs[1].Id : obj1Id;
        }

        Assert.False((await client.StartQuestAsync(questId)).IsError);
        Assert.False((await client.CompleteQuestObjectiveAsync(questId, obj1Id, "Doom")).IsError);

        var queuedObjective = await client.QueueCompleteQuestObjectiveAsync(questId, obj2Id, "Doom");
        Assert.False(queuedObjective.IsError);
        Assert.False((await client.FlushQuestObjectiveJobsAsync()).IsError);

        Assert.False((await client.CompleteQuestAsync(questId)).IsError);

        if (_useFakeServer)
        {
            var createQuest = await client.CreateCrossGameQuestAsync(
                "Cross Quest",
                "Integration quest",
                [new StarQuestObjective { Description = "Collect 1 key", GameSource = "Doom", ItemRequired = "Key", IsCompleted = false }]);
            Assert.False(createQuest.IsError);
        }

        var activeQuests = await client.GetActiveQuestsAsync();
        Assert.False(activeQuests.IsError);
        Assert.NotEmpty(activeQuests.Result!);
        if (_useFakeServer)
        {
            Assert.Equal("quest-001", activeQuests.Result![0].Id);
            Assert.NotEmpty(activeQuests.Result[0].Objectives);
        }

        var bossNft = await client.CreateBossNftAsync("CyberDemon", "Boss drop", "Doom", "{\"hp\":1000}");
        Assert.False(bossNft.IsError);
        if (_useFakeServer)
            Assert.Equal("nft-001", bossNft.Result);
        else
            Assert.False(string.IsNullOrWhiteSpace(bossNft.Result));

        var deploy = await client.DeployBossNftAsync(bossNft.Result!, "Doom", "spawn_001");
        Assert.False(deploy.IsError);

        var nftCollection = await client.GetNftCollectionAsync();
        Assert.False(nftCollection.IsError);
        Assert.NotEmpty(nftCollection.Result!);
        if (_useFakeServer)
            Assert.Equal("nft-001", nftCollection.Result![0].Id);

        var lastError = client.GetLastError();
        Assert.False(lastError.IsError);

        var cleanup = client.Cleanup();
        Assert.False(cleanup.IsError);

        Assert.True(callbackCodes.Count > 0);
        if (_useFakeServer && _web4Server is not null && _web5Server is not null)
        {
            Assert.True(_web4Server.WasHit("POST", "/api/avatar/authenticate"));
            Assert.True(_web4Server.WasHit("POST", "/api/nft/mint-nft"));
            Assert.True(_web5Server.WasHit("GET", "/api/avatar/current"));
            Assert.True(_web5Server.WasHit("GET", "/api/avatar/inventory"));
            Assert.True(_web5Server.WasHit("POST", "/api/avatar/inventory"));
            Assert.True(_web5Server.WasHitWithPathPrefix("DELETE", "/api/avatar/inventory/"));
            Assert.True(_web5Server.WasHit("POST", "/api/quests/quest-main/start"));
            Assert.True(_web5Server.WasHit("POST", "/api/quests/quest-main/objectives/obj-1/complete"));
            Assert.True(_web5Server.WasHit("POST", "/api/quests/quest-main/objectives/obj-2/complete"));
            Assert.True(_web5Server.WasHit("POST", "/api/quests/quest-main/complete"));
            Assert.True(_web5Server.WasHit("POST", "/api/quests/create"));
            Assert.True(_web5Server.WasHit("GET", "/api/quests/by-status/InProgress"));
            Assert.True(_web5Server.WasHit("POST", "/api/nfts/nft-001/activate"));
            Assert.True(_web5Server.WasHit("GET", "/api/nfts/load-all-for-avatar"));
            Assert.True(_web5Server.HitCount("POST", "/api/avatar/inventory") >= 4);
            Assert.True(_web5Server.HitCount("GET", "/api/avatar/inventory") >= 3);
            Assert.True(_web5Server.HitCount("POST", "/api/quests/quest-main/objectives/obj-2/complete") >= 1);
        }
    }

    [Fact]
    public async Task NftMinting_Direct_ReturnsIdAndHash()
    {
        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = _web5BaseUrl, Web4OasisApiBaseUrl = _web4BaseUrl });
        var username = _useFakeServer ? "u" : GetEnv("STARAPI_USERNAME", "u");
        var password = _useFakeServer ? "p" : GetEnv("STARAPI_PASSWORD", "p");
        await client.AuthenticateAsync(username, password);

        var mint = await client.MintInventoryItemNftAsync("MintTestKey", "Description", "Doom", "KeyItem");
        Assert.False(mint.IsError);
        Assert.NotNull(mint.Result.NftId);
        Assert.NotEmpty(mint.Result.NftId);
        if (_useFakeServer)
            Assert.Equal("tx-integration-mint-001", mint.Result.Hash);
        if (_useFakeServer && _web4Server is not null)
            Assert.True(_web4Server.WasHit("POST", "/api/nft/mint-nft"));
    }

    [Fact]
    public async Task NftMinting_PickupWithMint_ThenConsumeLastMintResult_ReturnsResult()
    {
        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = _web5BaseUrl, Web4OasisApiBaseUrl = _web4BaseUrl });
        var username = _useFakeServer ? "u" : GetEnv("STARAPI_USERNAME", "u");
        var password = _useFakeServer ? "p" : GetEnv("STARAPI_PASSWORD", "p");
        await client.AuthenticateAsync(username, password);

        client.EnqueuePickupWithMintJobOnly("PickupMintKey", "Desc", "Doom", "KeyItem", doMint: true, quantity: 1);
        var flush = await client.FlushAddItemJobsAsync();
        Assert.False(flush.IsError);
        await Task.Delay(_useFakeServer ? 800 : 2000);

        var consumed = client.ConsumeLastMintResult(out var itemName, out var nftId, out var hash);
        Assert.True(consumed);
        Assert.NotNull(itemName);
        Assert.Equal("PickupMintKey", itemName);
        Assert.NotNull(nftId);
        if (_useFakeServer)
            Assert.Equal("nft-001", nftId);
        if (_useFakeServer && _web4Server is not null)
            Assert.True(_web4Server.WasHit("POST", "/api/nft/mint-nft"));
    }

    [Fact]
    public async Task SendItemToAvatar_Succeeds()
    {
        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = _web5BaseUrl, Web4OasisApiBaseUrl = _web4BaseUrl });
        var username = _useFakeServer ? "u" : GetEnv("STARAPI_USERNAME", "u");
        var password = _useFakeServer ? "p" : GetEnv("STARAPI_PASSWORD", "p");
        await client.AuthenticateAsync(username, password);
        await client.AddItemAsync("SendAvatarItem", "For send test", "Doom", "KeyItem");

        var send = await client.SendItemToAvatarAsync("target-avatar", "SendAvatarItem", 1);
        Assert.False(send.IsError);
        Assert.True(send.Result);
        if (_useFakeServer && _web5Server is not null)
            Assert.True(_web5Server.WasHit("POST", "/api/avatar/inventory/send-to-avatar"));
    }

    [Fact]
    public async Task SendItemToClan_Succeeds()
    {
        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = _web5BaseUrl, Web4OasisApiBaseUrl = _web4BaseUrl });
        var username = _useFakeServer ? "u" : GetEnv("STARAPI_USERNAME", "u");
        var password = _useFakeServer ? "p" : GetEnv("STARAPI_PASSWORD", "p");
        await client.AuthenticateAsync(username, password);
        await client.AddItemAsync("SendClanItem", "For clan send test", "Doom", "KeyItem");

        var send = await client.SendItemToClanAsync("TestClan", "SendClanItem", 1);
        Assert.False(send.IsError);
        Assert.True(send.Result);
        if (_useFakeServer && _web5Server is not null)
            Assert.True(_web5Server.WasHit("POST", "/api/avatar/inventory/send-to-clan"));
    }

    [Fact]
    public async Task InvalidateInventoryCache_ThenGetInventory_Refetches()
    {
        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = _web5BaseUrl, Web4OasisApiBaseUrl = _web4BaseUrl });
        var username = _useFakeServer ? "u" : GetEnv("STARAPI_USERNAME", "u");
        var password = _useFakeServer ? "p" : GetEnv("STARAPI_PASSWORD", "p");
        await client.AuthenticateAsync(username, password);
        var first = await client.GetInventoryAsync();
        Assert.False(first.IsError);
        client.InvalidateInventoryCache();
        var second = await client.GetInventoryAsync();
        Assert.False(second.IsError);
        if (_useFakeServer && _web5Server is not null)
            Assert.True(_web5Server.HitCount("GET", "/api/avatar/inventory") >= 2);
    }
}


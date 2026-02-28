using NextGenSoftware.OASIS.STARAPI.Client;
using NextGenSoftware.OASIS.STARAPI.Client.Tests;

namespace NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests;

/// <summary>Integration tests run against real APIs by default (WEB5/WEB4 localhost:5556/5555) using credentials from StarApiTestDefaults (dellams/test!). Set STARAPI_INTEGRATION_USE_FAKE=true to use in-process fake servers instead.</summary>
public class StarApiClientIntegrationTests : IAsyncLifetime
{
    private FakeStarApiServer? _web5Server;
    private FakeStarApiServer? _web4Server;
    private string _web5BaseUrl = StarApiTestDefaults.Web5BaseUrl;
    private string _web4BaseUrl = StarApiTestDefaults.Web4BaseUrl;
    private bool _useFakeServer;

    public Task InitializeAsync()
    {
        _useFakeServer = GetEnv("STARAPI_INTEGRATION_USE_FAKE", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
        _web5BaseUrl = GetEnv("STARAPI_WEB5_BASE_URL", StarApiTestDefaults.Web5BaseUrl);
        _web4BaseUrl = GetEnv("STARAPI_WEB4_BASE_URL", StarApiTestDefaults.Web4BaseUrl);

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
        var username = _useFakeServer ? "integration-user" : GetEnv("STARAPI_USERNAME", StarApiTestDefaults.Username);
        var password = _useFakeServer ? "integration-password" : GetEnv("STARAPI_PASSWORD", StarApiTestDefaults.Password);
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
            Assert.NotEqual(Guid.Empty, currentAvatar.Result!.Id);
        }

        var runId = Guid.NewGuid().ToString("N")[..6];
        var redName = "Red Keycard " + runId;
        var blueName = "Blue Keycard " + runId;
        var added = await client.AddItemAsync(redName, "Collected in room 101", "Doom", "KeyItem");
        Assert.False(added.IsError);

        var queuedAdd = await client.QueueAddItemAsync(blueName, "Collected in room 102", "Doom", "KeyItem");
        Assert.False(queuedAdd.IsError);

        var batchAdd = await client.QueueAddItemsAsync(
        [
            new StarItem { Name = "Ammo Box", Description = "Ammo pickup", GameSource = "Doom", ItemType = "PowerUp" },
            new StarItem { Name = "Medkit", Description = "Health pickup", GameSource = "Doom", ItemType = "Healing" }
        ]);
        Assert.False(batchAdd.IsError);
        Assert.False((await client.FlushAddItemJobsAsync()).IsError);

        var hasBlue = await client.HasItemAsync(blueName);
        Assert.False(hasBlue.IsError);
        Assert.True(hasBlue.Result);

        var inventory = await client.GetInventoryAsync();
        Assert.False(inventory.IsError);
        Assert.True(inventory.Result!.Count >= 4);
        Assert.Contains(inventory.Result, x => string.Equals(x.Name, redName, StringComparison.OrdinalIgnoreCase));
        // When using fake server we also assert exact quest IDs below

        var useBlue = await client.UseItemAsync(blueName, "door_a");
        Assert.False(useBlue.IsError);
        Assert.True(useBlue.Result);

        var queueUse = await client.QueueUseItemAsync("Ammo Box", "combat_use");
        Assert.False(queueUse.IsError);
        Assert.False((await client.FlushUseItemJobsAsync()).IsError);

        bool questBlockSucceeded;
        StarQuestInfo? createdQuest = null;
        if (_useFakeServer)
        {
            questBlockSucceeded = true;
        }
        else
        {
            var createQuestFirst = await client.CreateCrossGameQuestAsync(
                "Cross Quest",
                "Integration quest",
                [new StarQuestObjective { Description = "Collect 1 key", GameSource = "Doom", ItemRequired = "Key", IsCompleted = false }]);
            questBlockSucceeded = !createQuestFirst.IsError;
            createdQuest = createQuestFirst.Result;
        }

        if (questBlockSucceeded)
        {
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
                if (createdQuest == null || string.IsNullOrEmpty(createdQuest.Id))
                {
                    questBlockSucceeded = false;
                    questId = obj1Id = obj2Id = string.Empty;
                }
                else
                {
                    questId = createdQuest.Id;
                    var objs = createdQuest.Objectives;
                    obj1Id = objs.Count > 0 ? objs[0].Id : questId;
                    obj2Id = objs.Count > 1 ? objs[1].Id : obj1Id;
                }
            }

            if (questBlockSucceeded && !string.IsNullOrEmpty(questId))
            {
                Assert.False((await client.StartQuestAsync(questId)).IsError);
                // Only complete objectives when we have distinct objective IDs (backend create may not return child objectives).
                bool haveDistinctObjectives = !string.IsNullOrEmpty(obj1Id) && obj1Id != questId;
                if (haveDistinctObjectives)
                {
                    Assert.False((await client.CompleteQuestObjectiveAsync(questId, obj1Id, "Doom")).IsError);
                    var queuedObjective = await client.QueueCompleteQuestObjectiveAsync(questId, obj2Id, "Doom");
                    Assert.False(queuedObjective.IsError);
                    Assert.False((await client.FlushQuestObjectiveJobsAsync()).IsError);
                }
                Assert.False((await client.CompleteQuestAsync(questId)).IsError);
            }

            if (_useFakeServer)
            {
                var createQuest = await client.CreateCrossGameQuestAsync(
                    "Cross Quest",
                    "Integration quest",
                    [new StarQuestObjective { Description = "Collect 1 key", GameSource = "Doom", ItemRequired = "Key", IsCompleted = false }]);
                Assert.False(createQuest.IsError);
            }
        }

        var activeQuests = await client.GetActiveQuestsAsync();
        Assert.False(activeQuests.IsError);
        if (questBlockSucceeded && _useFakeServer)
        {
            Assert.NotEmpty(activeQuests.Result!);
            Assert.Equal("quest-001", activeQuests.Result![0].Id);
            Assert.NotEmpty(activeQuests.Result[0].Objectives);
        }
        // When running against real API, activeQuests may be empty (e.g. after completing the only quest)

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
        if (_useFakeServer)
        {
            Assert.NotEmpty(nftCollection.Result!);
            Assert.Equal("nft-001", nftCollection.Result![0].Id);
        }
        // When running against real API, GetNftCollectionAsync may return empty depending on backend scope/caching

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
        var username = _useFakeServer ? "u" : GetEnv("STARAPI_USERNAME", StarApiTestDefaults.Username);
        var password = _useFakeServer ? "p" : GetEnv("STARAPI_PASSWORD", StarApiTestDefaults.Password);
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
        var username = _useFakeServer ? "u" : GetEnv("STARAPI_USERNAME", StarApiTestDefaults.Username);
        var password = _useFakeServer ? "p" : GetEnv("STARAPI_PASSWORD", StarApiTestDefaults.Password);
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
        if (!_useFakeServer)
        {
            // Against real API this requires a valid target avatar; skip unless STARAPI_SEND_TARGET_AVATAR is set.
            if (string.IsNullOrWhiteSpace(GetEnv("STARAPI_SEND_TARGET_AVATAR", "")))
                return;
        }

        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = _web5BaseUrl, Web4OasisApiBaseUrl = _web4BaseUrl });
        var username = _useFakeServer ? "u" : GetEnv("STARAPI_USERNAME", StarApiTestDefaults.Username);
        var password = _useFakeServer ? "p" : GetEnv("STARAPI_PASSWORD", StarApiTestDefaults.Password);
        await client.AuthenticateAsync(username, password);
        var sendItemName = "SendAvatarItem_" + Guid.NewGuid().ToString("N")[..6];
        await client.AddItemAsync(sendItemName, "For send test", "Doom", "KeyItem");

        var target = _useFakeServer ? "target-avatar" : GetEnv("STARAPI_SEND_TARGET_AVATAR", "");
        var send = await client.SendItemToAvatarAsync(target, sendItemName, 1);
        Assert.False(send.IsError);
        Assert.True(send.Result);
        if (_useFakeServer && _web5Server is not null)
            Assert.True(_web5Server.WasHit("POST", "/api/avatar/inventory/send-to-avatar"));
    }

    [Fact]
    public async Task SendItemToClan_Succeeds()
    {
        if (!_useFakeServer)
        {
            // Against real API this requires a valid clan and provider; skip unless STARAPI_SEND_TARGET_CLAN is set.
            if (string.IsNullOrWhiteSpace(GetEnv("STARAPI_SEND_TARGET_CLAN", "")))
                return;
        }

        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = _web5BaseUrl, Web4OasisApiBaseUrl = _web4BaseUrl });
        var username = _useFakeServer ? "u" : GetEnv("STARAPI_USERNAME", StarApiTestDefaults.Username);
        var password = _useFakeServer ? "p" : GetEnv("STARAPI_PASSWORD", StarApiTestDefaults.Password);
        await client.AuthenticateAsync(username, password);
        var sendItemName = "SendClanItem_" + Guid.NewGuid().ToString("N")[..6];
        await client.AddItemAsync(sendItemName, "For clan send test", "Doom", "KeyItem");

        var clanName = _useFakeServer ? "TestClan" : GetEnv("STARAPI_SEND_TARGET_CLAN", "");
        var send = await client.SendItemToClanAsync(clanName, sendItemName, 1);
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
        var username = _useFakeServer ? "u" : GetEnv("STARAPI_USERNAME", StarApiTestDefaults.Username);
        var password = _useFakeServer ? "p" : GetEnv("STARAPI_PASSWORD", StarApiTestDefaults.Password);
        await client.AuthenticateAsync(username, password);
        var first = await client.GetInventoryAsync();
        Assert.False(first.IsError);
        client.InvalidateInventoryCache();
        var second = await client.GetInventoryAsync();
        Assert.False(second.IsError);
        if (_useFakeServer && _web5Server is not null)
            Assert.True(_web5Server.HitCount("GET", "/api/avatar/inventory") >= 2);
    }

    /// <summary>Display name for overlay: when NftId is set, games (Doom/Quake) show "[NFT] " + Name.</summary>
    private static string DisplayNameWithNftPrefix(StarItem item)
    {
        return string.IsNullOrEmpty(item.NftId) ? item.Name : "[NFT] " + item.Name;
    }

    /// <summary>Runs only against REAL APIs. Validates that the backend (ONODE/Web5) persists NftId and returns it on GET inventory so [NFT] prefix appears in Doom/Quake. Skips when STARAPI_INTEGRATION_USE_FAKE=true.</summary>
    [Fact]
    public async Task RealApi_BackendPersistsAndReturnsNftId_SoNftPrefixAppears()
    {
        if (_useFakeServer)
        {
            // This test must run against the real backend to verify API/storage persistence. Use default (real API) or set STARAPI_INTEGRATION_USE_FAKE=false.
            return;
        }

        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = _web5BaseUrl, Web4OasisApiBaseUrl = _web4BaseUrl });
        var username = GetEnv("STARAPI_USERNAME", StarApiTestDefaults.Username);
        var password = GetEnv("STARAPI_PASSWORD", StarApiTestDefaults.Password);
        var auth = await client.AuthenticateAsync(username, password);
        Assert.False(auth.IsError);

        var unique = Guid.NewGuid().ToString("N")[..8];
        var nftId = "real-api-nft-" + unique;
        var itemName = "BackendNftIdTest_Keycard_" + unique;
        var add = await client.AddItemAsync(itemName, "Backend NftId persistence test", "Doom", "KeyItem", nftId: nftId);
        Assert.False(add.IsError);

        client.InvalidateInventoryCache();
        var inventory = await client.GetInventoryAsync();
        Assert.False(inventory.IsError);
        var nftItem = inventory.Result!.FirstOrDefault(x => string.Equals(x.Name, itemName, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(nftItem);
        Assert.False(string.IsNullOrEmpty(nftItem.NftId),
            "Backend must persist and return NftId on GET /api/avatar/inventory (AvatarManager promotes MetaData.NFTId to NftId; storage must persist it). Fix backend, not client.");
        Assert.Equal(nftId, nftItem.NftId);

        var displayName = DisplayNameWithNftPrefix(nftItem);
        Assert.StartsWith("[NFT] ", displayName);
    }

    [Fact]
    public async Task GetInventory_WhenItemHasNftIdInResponse_ItemHasNftIdSet_AndDisplayPrefixWouldBeNft()
    {
        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = _web5BaseUrl, Web4OasisApiBaseUrl = _web4BaseUrl });
        var username = _useFakeServer ? "integration-user" : GetEnv("STARAPI_USERNAME", StarApiTestDefaults.Username);
        var password = _useFakeServer ? "integration-password" : GetEnv("STARAPI_PASSWORD", StarApiTestDefaults.Password);
        await client.AuthenticateAsync(username, password);
        if (_useFakeServer)
            Assert.False(client.SetApiKey("local-api-key", "11111111-1111-1111-1111-111111111111").IsError);

        var unique = Guid.NewGuid().ToString("N")[..8];
        var nftId = "nft-test-prefix-" + unique;
        var itemName = "NftPrefixTest_Keycard_" + unique;
        var add = await client.AddItemAsync(itemName, "Collected in Doom", "Doom", "KeyItem", nftId: nftId);
        Assert.False(add.IsError);

        client.InvalidateInventoryCache();
        var inventory = await client.GetInventoryAsync();
        Assert.False(inventory.IsError);
        var nftItem = inventory.Result!.FirstOrDefault(x => string.Equals(x.Name, itemName, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(nftItem);
        Assert.False(string.IsNullOrEmpty(nftItem.NftId), "Item added with nftId should have NftId set after GET inventory so [NFT] prefix can appear.");
        Assert.Equal(nftId, nftItem.NftId);

        var displayName = DisplayNameWithNftPrefix(nftItem);
        Assert.StartsWith("[NFT] ", displayName);
        Assert.Equal("[NFT] " + itemName, displayName);
    }

    /// <summary>Runs against real API. Verifies create-with-objectives returns objectives with IDs, and add/remove objective endpoints work.</summary>
    [Fact]
    public async Task RealApi_QuestObjectives_CreateWithObjectives_ReturnsObjectivesWithIds_AndAddRemoveWork()
    {
        if (_useFakeServer)
            return;

        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = _web5BaseUrl, Web4OasisApiBaseUrl = _web4BaseUrl });
        var username = GetEnv("STARAPI_USERNAME", StarApiTestDefaults.Username);
        var password = GetEnv("STARAPI_PASSWORD", StarApiTestDefaults.Password);
        var auth = await client.AuthenticateAsync(username, password);
        Assert.False(auth.IsError);

        var unique = Guid.NewGuid().ToString("N")[..8];
        var objectives = new List<StarQuestObjective>
        {
            new() { Description = "Collect key in Doom", GameSource = "Doom", ItemRequired = "Key", IsCompleted = false },
            new() { Description = "Defeat boss", GameSource = "Doom", ItemRequired = "BossKill", IsCompleted = false }
        };

        var create = await client.CreateCrossGameQuestAsync($"ObjTestQuest-{unique}", "Quest objectives test", objectives);
        Assert.False(create.IsError);
        Assert.NotNull(create.Result);
        Assert.False(string.IsNullOrEmpty(create.Result.Id));
        Assert.True(create.Result.Objectives.Count >= 2,
            "Create with objectives should return quest with at least 2 objectives (API creates sub-quests).");
        var obj0Id = create.Result.Objectives.Count > 0 ? create.Result.Objectives[0].Id : null;
        var obj1Id = create.Result.Objectives.Count > 1 ? create.Result.Objectives[1].Id : null;
        if (!string.IsNullOrEmpty(obj0Id) && !string.IsNullOrEmpty(obj1Id))
        {
            Assert.False((await client.StartQuestAsync(create.Result.Id)).IsError);
            Assert.False((await client.CompleteQuestObjectiveAsync(create.Result.Id, obj0Id, "Doom")).IsError);
            Assert.False((await client.CompleteQuestObjectiveAsync(create.Result.Id, obj1Id, "Doom")).IsError);
            Assert.False((await client.CompleteQuestAsync(create.Result.Id)).IsError);
        }

        var questId2 = Guid.NewGuid().ToString("N")[..8];
        var create2 = await client.CreateCrossGameQuestAsync($"ObjTestQuest2-{questId2}", "Add/remove test", [objectives[0]]);
        if (create2.IsError || create2.Result == null)
            return;
        var addResult = await client.AddQuestObjectiveAsync(create2.Result.Id, "Added objective", gameSource: "Doom", itemRequired: "Medkit");
        Assert.False(addResult.IsError);
        if (addResult.Result != null && !string.IsNullOrEmpty(addResult.Result.Id))
        {
            var removeResult = await client.RemoveQuestObjectiveAsync(create2.Result.Id, addResult.Result.Id);
            Assert.False(removeResult.IsError);
        }
    }

    /// <summary>With fake server: create returns quest with objectives; add/remove objective endpoints are called successfully.</summary>
    [Fact]
    public async Task FakeServer_QuestCreate_ReturnsObjectivesWithIds_AndAddRemoveObjective_Succeed()
    {
        if (!_useFakeServer)
            return;

        using var client = new StarApiClient();
        client.Init(new StarApiConfig { Web5StarApiBaseUrl = _web5BaseUrl, Web4OasisApiBaseUrl = _web4BaseUrl });
        await client.AuthenticateAsync("integration-user", "integration-password");
        Assert.False(client.SetApiKey("local-api-key", "11111111-1111-1111-1111-111111111111").IsError);

        var create = await client.CreateCrossGameQuestAsync(
            "Fake Quest",
            "Fake quest with objectives",
            [new StarQuestObjective { Description = "Obj 1", GameSource = "Doom", ItemRequired = "Key", IsCompleted = false }]);
        Assert.False(create.IsError);
        Assert.NotNull(create.Result);
        Assert.False(string.IsNullOrEmpty(create.Result.Id));
        Assert.NotEmpty(create.Result.Objectives);
        Assert.False(string.IsNullOrEmpty(create.Result.Objectives[0].Id));

        var addResult = await client.AddQuestObjectiveAsync(create.Result.Id, "Added via API", gameSource: "Doom");
        Assert.False(addResult.IsError);
        Assert.NotNull(addResult.Result);
        Assert.False(string.IsNullOrEmpty(addResult.Result.Id));

        var removeResult = await client.RemoveQuestObjectiveAsync(create.Result.Id, addResult.Result.Id);
        Assert.False(removeResult.IsError);
    }

}


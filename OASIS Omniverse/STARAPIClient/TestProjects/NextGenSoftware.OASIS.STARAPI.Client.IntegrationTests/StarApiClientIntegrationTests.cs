using NextGenSoftware.OASIS.STARAPI.Client;

namespace NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests;

public class StarApiClientIntegrationTests : IAsyncLifetime
{
    private FakeStarApiServer? _web5Server;
    private FakeStarApiServer? _web4Server;

    public Task InitializeAsync()
    {
        _web5Server = new FakeStarApiServer();
        _web4Server = new FakeStarApiServer();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_web5Server is not null)
            await _web5Server.DisposeAsync();

        if (_web4Server is not null)
            await _web4Server.DisposeAsync();
    }

    [Fact]
    public async Task FullWorkflow_ExercisesAllClientEndpoints_AndSucceeds()
    {
        Assert.NotNull(_web5Server);
        Assert.NotNull(_web4Server);

        using var client = new StarApiClient();
        var callbackCodes = new List<StarApiResultCode>();
        client.SetCallback((code, _) => callbackCodes.Add(code), null);

        var init = client.Init(new StarApiConfig
        {
            Web5StarApiBaseUrl = _web5Server!.BaseUrl,
            Web4OasisApiBaseUrl = _web4Server!.BaseUrl
        });
        Assert.False(init.IsError);

        Assert.False(client.SetWeb5StarApiBaseUrl(_web5Server.BaseUrl).IsError);
        Assert.False(client.SetWeb4OasisApiBaseUrl(_web4Server.BaseUrl).IsError);
        Assert.False(client.SetApiKey("local-api-key", "11111111-1111-1111-1111-111111111111").IsError);

        var auth = await client.AuthenticateAsync("integration-user", "integration-password");
        Assert.False(auth.IsError);
        Assert.True(string.IsNullOrWhiteSpace(auth.ErrorCode) || auth.ErrorCode == "0");

        var currentAvatar = await client.GetCurrentAvatarAsync();
        Assert.False(currentAvatar.IsError);
        Assert.Equal("11111111-1111-1111-1111-111111111111", currentAvatar.Result!.Id.ToString());
        Assert.Equal("integration_user", currentAvatar.Result!.Username);

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

        var useBlue = await client.UseItemAsync("Blue Keycard", "door_a");
        Assert.False(useBlue.IsError);
        Assert.True(useBlue.Result);

        var queueUse = await client.QueueUseItemAsync("Ammo Box", "combat_use");
        Assert.False(queueUse.IsError);
        Assert.False((await client.FlushUseItemJobsAsync()).IsError);

        Assert.False((await client.StartQuestAsync("quest-main")).IsError);
        Assert.False((await client.CompleteQuestObjectiveAsync("quest-main", "obj-1", "Doom")).IsError);

        var queuedObjective = await client.QueueCompleteQuestObjectiveAsync("quest-main", "obj-2", "Doom");
        Assert.False(queuedObjective.IsError);
        Assert.False((await client.FlushQuestObjectiveJobsAsync()).IsError);

        Assert.False((await client.CompleteQuestAsync("quest-main")).IsError);

        var createQuest = await client.CreateCrossGameQuestAsync(
            "Cross Quest",
            "Integration quest",
            [new StarQuestObjective { Description = "Collect 1 key", GameSource = "Doom", ItemRequired = "Key", IsCompleted = false }]);
        Assert.False(createQuest.IsError);

        var activeQuests = await client.GetActiveQuestsAsync();
        Assert.False(activeQuests.IsError);
        Assert.NotEmpty(activeQuests.Result!);
        Assert.Equal("quest-001", activeQuests.Result![0].Id);
        Assert.NotEmpty(activeQuests.Result[0].Objectives);

        var bossNft = await client.CreateBossNftAsync("CyberDemon", "Boss drop", "Doom", "{\"hp\":1000}");
        Assert.False(bossNft.IsError);
        Assert.Equal("nft-001", bossNft.Result);

        var deploy = await client.DeployBossNftAsync("nft-001", "Doom", "spawn_001");
        Assert.False(deploy.IsError);

        var nftCollection = await client.GetNftCollectionAsync();
        Assert.False(nftCollection.IsError);
        Assert.NotEmpty(nftCollection.Result!);
        Assert.Equal("nft-001", nftCollection.Result![0].Id);

        var lastError = client.GetLastError();
        Assert.False(lastError.IsError);

        var cleanup = client.Cleanup();
        Assert.False(cleanup.IsError);

        Assert.True(callbackCodes.Count > 0);
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


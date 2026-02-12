using NextGenSoftware.OASIS.STARAPI.Client;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STARAPI.Client.TestHarness;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static async Task Main()
    {
        var web5BaseUrl = GetEnv("STARAPI_WEB5_BASE_URL", "http://localhost:5055");
        var web4BaseUrl = GetEnv("STARAPI_WEB4_BASE_URL", "http://localhost:5056");
        var username = GetEnv("STARAPI_USERNAME", string.Empty);
        var password = GetEnv("STARAPI_PASSWORD", string.Empty);
        var apiKey = GetEnv("STARAPI_API_KEY", string.Empty);
        var avatarId = GetEnv("STARAPI_AVATAR_ID", string.Empty);

        Console.WriteLine("==============================================");
        Console.WriteLine(" WEB5 STAR API Client Test Harness");
        Console.WriteLine("==============================================");
        Console.WriteLine($"WEB5 URI: {web5BaseUrl}");
        Console.WriteLine($"WEB4 URI: {web4BaseUrl}");

        using var client = new StarApiClient();
        client.SetCallback((code, _) => Console.WriteLine($"[callback] {code}"), null);

        var init = client.Init(new StarApiConfig
        {
            Web5StarApiBaseUrl = web5BaseUrl,
            Web4OasisApiBaseUrl = web4BaseUrl,
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
            AvatarId = string.IsNullOrWhiteSpace(avatarId) ? null : avatarId
        });
        Check("Init", init);
        if (init.IsError)
        {
            ExitWithSummary();
            return;
        }

        if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(avatarId))
            Check("SetApiKey", client.SetApiKey(apiKey, avatarId));

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            Check("AuthenticateAsync", await client.AuthenticateAsync(username, password));
        else
            Console.WriteLine("AuthenticateAsync skipped (STARAPI_USERNAME/STARAPI_PASSWORD not provided).");

        Check("SetWeb5StarApiBaseUrl", client.SetWeb5StarApiBaseUrl(web5BaseUrl));
        Check("SetWeb4OasisApiBaseUrl", client.SetWeb4OasisApiBaseUrl(web4BaseUrl));

        Check("GetCurrentAvatarAsync", await client.GetCurrentAvatarAsync());

        var suffix = DateTime.UtcNow.Ticks.ToString()[^6..];
        var itemA = $"HarnessKeyA-{suffix}";
        var itemB = $"HarnessKeyB-{suffix}";
        var itemC = $"HarnessKeyC-{suffix}";

        var add = await client.AddItemAsync(itemA, "Harness direct add", "Harness", "KeyItem");
        Check("AddItemAsync", add);

        Check("QueueAddItemAsync", await client.QueueAddItemAsync(itemB, "Harness queued add", "Harness", "KeyItem"));
        Check("QueueAddItemsAsync", await client.QueueAddItemsAsync(
        [
            new StarItem { Name = itemC, Description = "Harness batch add", GameSource = "Harness", ItemType = "KeyItem" }
        ]));
        Check("FlushAddItemJobsAsync", await client.FlushAddItemJobsAsync());

        Check("HasItemAsync", await client.HasItemAsync(itemA));
        Check("GetInventoryAsync", await client.GetInventoryAsync());

        Check("UseItemAsync", await client.UseItemAsync(itemA, "harness_use_direct"));
        Check("QueueUseItemAsync", await client.QueueUseItemAsync(itemB, "harness_use_queued"));
        Check("FlushUseItemJobsAsync", await client.FlushUseItemJobsAsync());

        const string questId = "harness-quest-main";
        Check("StartQuestAsync", await client.StartQuestAsync(questId));
        Check("CompleteQuestObjectiveAsync", await client.CompleteQuestObjectiveAsync(questId, "obj-1", "Harness"));
        Check("QueueCompleteQuestObjectiveAsync", await client.QueueCompleteQuestObjectiveAsync(questId, "obj-2", "Harness"));
        Check("FlushQuestObjectiveJobsAsync", await client.FlushQuestObjectiveJobsAsync());
        Check("CompleteQuestAsync", await client.CompleteQuestAsync(questId));

        Check("CreateCrossGameQuestAsync", await client.CreateCrossGameQuestAsync(
            $"HarnessCrossQuest-{suffix}",
            "Quest created by test harness",
            [new StarQuestObjective { Description = "Collect harness key", GameSource = "Harness", ItemRequired = "KeyItem", IsCompleted = false }]));
        Check("GetActiveQuestsAsync", await client.GetActiveQuestsAsync());

        var createBossNft = await client.CreateBossNftAsync(
            $"HarnessBoss-{suffix}",
            "Boss NFT from harness",
            "Harness",
            "{\"level\":10,\"hp\":5000}");
        Check("CreateBossNftAsync", createBossNft);
        if (!createBossNft.IsError && !string.IsNullOrWhiteSpace(createBossNft.Result))
            Check("DeployBossNftAsync", await client.DeployBossNftAsync(createBossNft.Result, "Harness", "arena_01"));
        else
            Console.WriteLine("DeployBossNftAsync skipped because CreateBossNftAsync did not return an NFT id.");

        Check("GetNftCollectionAsync", await client.GetNftCollectionAsync());
        Check("GetLastError", client.GetLastError());
        Check("Cleanup", client.Cleanup());

        ExitWithSummary();
    }

    private static void Check<T>(string step, OASISResult<T> result)
    {
        if (result.IsError)
        {
            _failed++;
            Console.WriteLine($"[FAIL] {step} => {result.Message}");
            return;
        }

        _passed++;
        Console.WriteLine($"[PASS] {step} => {result.Message}");
    }

    private static void ExitWithSummary()
    {
        Console.WriteLine("----------------------------------------------");
        Console.WriteLine($"Passed: {_passed}");
        Console.WriteLine($"Failed: {_failed}");
        Console.WriteLine("----------------------------------------------");
        Environment.ExitCode = _failed == 0 ? 0 : 1;
    }

    private static string GetEnv(string key, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}

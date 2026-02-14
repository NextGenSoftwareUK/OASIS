using NextGenSoftware.OASIS.STARAPI.Client;
using NextGenSoftware.OASIS.Common;
using System.Xml.Linq;

namespace NextGenSoftware.OASIS.STARAPI.Client.TestHarness;

internal static class Program
{
    private static int _passed;
    private static int _failed;
    private static readonly List<(string Name, bool Passed, string Message)> _results = [];

    private static async Task Main()
    {
        var harnessMode = GetEnv("STARAPI_HARNESS_MODE", "fake").Trim().ToLowerInvariant();
        var useFakeServer = harnessMode == "fake" ||
                            GetEnv("STARAPI_HARNESS_USE_FAKE_SERVER", "true").Equals("true", StringComparison.OrdinalIgnoreCase);
        var web5BaseUrl = GetEnv("STARAPI_WEB5_BASE_URL", "http://localhost:5055");
        var web4BaseUrl = GetEnv("STARAPI_WEB4_BASE_URL", "http://localhost:5056");
        var username = GetEnv("STARAPI_USERNAME", string.Empty);
        var password = GetEnv("STARAPI_PASSWORD", string.Empty);
        var apiKey = GetEnv("STARAPI_API_KEY", string.Empty);
        var avatarId = GetEnv("STARAPI_AVATAR_ID", string.Empty);
        var junitPath = GetEnv("STARAPI_HARNESS_JUNIT_PATH", string.Empty);

        await using var web5Fake = useFakeServer ? new FakeHarnessApiServer() : null;
        await using var web4Fake = useFakeServer ? new FakeHarnessApiServer() : null;
        if (useFakeServer)
        {
            web5BaseUrl = web5Fake!.BaseUrl;
            web4BaseUrl = web4Fake!.BaseUrl;
            username = string.IsNullOrWhiteSpace(username) ? "harness-user" : username;
            password = string.IsNullOrWhiteSpace(password) ? "harness-pass" : password;
        }
        else if (harnessMode == "real-local" && string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password))
        {
            username = "dellams";
            password = "test!";
            if (string.IsNullOrWhiteSpace(avatarId))
                avatarId = "f9ccf5b2-3991-42ec-8155-65b94b2e0f0c";
        }

        Console.WriteLine("==============================================");
        Console.WriteLine(" WEB5 STAR API Client Test Harness");
        Console.WriteLine("==============================================");
        Console.WriteLine($"WEB5 URI: {web5BaseUrl}");
        Console.WriteLine($"WEB4 URI: {web4BaseUrl}");
        Console.WriteLine($"Harness mode: {harnessMode}");
        Console.WriteLine($"Use fake server: {useFakeServer}");

        using var client = new StarApiClient();
        client.SetCallback((code, _) => Console.WriteLine($"[callback] {code}"), null);

        var init = client.Init(new StarApiConfig
        {
            Web5StarApiBaseUrl = web5BaseUrl,
            Web4OasisApiBaseUrl = web4BaseUrl,
            TimeoutSeconds = harnessMode == "real-local" ? 180 : 30,
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
            AvatarId = string.IsNullOrWhiteSpace(avatarId) ? null : avatarId
        });
        Check("Init", init);
        if (init.IsError)
        {
            ExitWithSummary(junitPath);
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

        Check("CreateCrossGameQuestAsync", await client.CreateCrossGameQuestAsync(
            $"HarnessCrossQuest-{suffix}",
            "Quest created by test harness",
            [new StarQuestObjective { Description = "Collect harness key", GameSource = "Harness", ItemRequired = "KeyItem", IsCompleted = false }]));
        var activeQuests = await client.GetActiveQuestsAsync();
        Check("GetActiveQuestsAsync", activeQuests);

        var questId = Guid.Empty;
        var objectiveId = Guid.Empty;
        if (!activeQuests.IsError && activeQuests.Result is not null && activeQuests.Result.Count > 0)
        {
            Guid.TryParse(activeQuests.Result[0].Id, out questId);
            if (activeQuests.Result[0].Objectives.Count > 0)
                Guid.TryParse(activeQuests.Result[0].Objectives[0].Id, out objectiveId);
        }
        if (questId == Guid.Empty) questId = Guid.NewGuid();
        if (objectiveId == Guid.Empty) objectiveId = Guid.NewGuid();

        Check("StartQuestAsync", await client.StartQuestAsync(questId.ToString()));
        Check("CompleteQuestObjectiveAsync", await client.CompleteQuestObjectiveAsync(questId.ToString(), objectiveId.ToString(), "Harness"));
        Check("QueueCompleteQuestObjectiveAsync", await client.QueueCompleteQuestObjectiveAsync(questId.ToString(), objectiveId.ToString(), "Harness"));
        Check("FlushQuestObjectiveJobsAsync", await client.FlushQuestObjectiveJobsAsync());
        Check("CompleteQuestAsync", await client.CompleteQuestAsync(questId.ToString()));

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

        ExitWithSummary(junitPath);
    }

    private static void Check<T>(string step, OASISResult<T> result)
    {
        if (result.IsError)
        {
            _failed++;
            _results.Add((step, false, result.Message ?? "Unknown error"));
            Console.WriteLine($"[FAIL] {step} => {result.Message}");
            return;
        }

        _passed++;
        _results.Add((step, true, result.Message ?? string.Empty));
        Console.WriteLine($"[PASS] {step} => {result.Message}");
    }

    private static void ExitWithSummary(string junitPath)
    {
        Console.WriteLine("----------------------------------------------");
        Console.WriteLine($"Passed: {_passed}");
        Console.WriteLine($"Failed: {_failed}");
        Console.WriteLine("----------------------------------------------");
        WriteJunitReport(junitPath);
        Environment.ExitCode = _failed == 0 ? 0 : 1;
    }

    private static string GetEnv(string key, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static void WriteJunitReport(string junitPath)
    {
        if (string.IsNullOrWhiteSpace(junitPath))
            return;

        var directory = Path.GetDirectoryName(junitPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var suite = new XElement("testsuite",
            new XAttribute("name", "STARAPIClient.TestHarness"),
            new XAttribute("tests", _results.Count),
            new XAttribute("failures", _failed));

        foreach (var result in _results)
        {
            var testCase = new XElement("testcase",
                new XAttribute("classname", "STARAPIClient.TestHarness"),
                new XAttribute("name", result.Name));

            if (!result.Passed)
                testCase.Add(new XElement("failure", new XAttribute("message", result.Message)));

            suite.Add(testCase);
        }

        var doc = new XDocument(new XElement("testsuites", suite));
        doc.Save(junitPath);
    }
}

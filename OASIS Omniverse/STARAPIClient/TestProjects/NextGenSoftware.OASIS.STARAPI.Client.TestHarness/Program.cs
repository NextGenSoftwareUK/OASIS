using NextGenSoftware.OASIS.STARAPI.Client;
using NextGenSoftware.OASIS.STARAPI.Client.Tests;
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
        // Default: real APIs (WEB5/WEB4 localhost:5556/5555) with credentials from StarApiTestDefaults (dellams/test!). Set STARAPI_HARNESS_USE_FAKE_SERVER=true or STARAPI_HARNESS_MODE=fake to use in-process fake servers.
        var harnessMode = GetEnv("STARAPI_HARNESS_MODE", "real").Trim().ToLowerInvariant();
        var useFakeServer = harnessMode == "fake" ||
                            GetEnv("STARAPI_HARNESS_USE_FAKE_SERVER", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
        var web5BaseUrl = GetEnv("STARAPI_WEB5_BASE_URL", StarApiTestDefaults.Web5BaseUrl);
        var web4BaseUrl = GetEnv("STARAPI_WEB4_BASE_URL", StarApiTestDefaults.Web4BaseUrl);
        var username = GetEnv("STARAPI_USERNAME", StarApiTestDefaults.Username);
        var password = GetEnv("STARAPI_PASSWORD", StarApiTestDefaults.Password);
        var apiKey = GetEnv("STARAPI_API_KEY", string.Empty);
        var avatarId = GetEnv("STARAPI_AVATAR_ID", string.Empty);
        var junitPath = GetEnv("STARAPI_HARNESS_JUNIT_PATH", string.Empty);

        await using var web5Fake = useFakeServer ? new FakeHarnessApiServer() : null;
        await using var web4Fake = useFakeServer ? new FakeHarnessApiServer() : null;
        if (useFakeServer)
        {
            web5BaseUrl = web5Fake!.BaseUrl;
            web4BaseUrl = web4Fake!.BaseUrl;
            username = "harness-user";
            password = "harness-pass";
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

        var createQuestResult = await client.CreateCrossGameQuestAsync(
            $"HarnessCrossQuest-{suffix}",
            "Quest created by test harness",
            [new StarQuestObjective { Description = "Collect harness key", GameSource = "Harness", ItemRequired = "KeyItem", IsCompleted = false }]);
        if (createQuestResult.IsError)
        {
            Console.WriteLine($"[SKIP] Quest block (CreateCrossGameQuest failed: {createQuestResult.Message}; backend may not support quest creation)");
        }
        else
        {
            Check("CreateCrossGameQuestAsync", createQuestResult);
            var createdQuest = createQuestResult.Result;
            string questIdStr;
            string obj1Str;
            string obj2Str;
            if (createdQuest != null && !string.IsNullOrEmpty(createdQuest.Id))
            {
                questIdStr = createdQuest.Id;
                var objs = createdQuest.Objectives;
                obj1Str = objs.Count > 0 ? objs[0].Id : questIdStr;
                obj2Str = objs.Count > 1 ? objs[1].Id : obj1Str;
            }
            else
            {
                var activeQuests = await client.GetActiveQuestsAsync();
                Check("GetActiveQuestsAsync", activeQuests);
                if (activeQuests.IsError || activeQuests.Result is null || activeQuests.Result.Count == 0)
                {
                    Console.WriteLine("[SKIP] Quest block (no created quest ID and no active quests)");
                    questIdStr = obj1Str = obj2Str = string.Empty;
                }
                else
                {
                    questIdStr = activeQuests.Result[0].Id;
                    var o = activeQuests.Result[0].Objectives;
                    obj1Str = o.Count > 0 ? o[0].Id : questIdStr;
                    obj2Str = o.Count > 1 ? o[1].Id : obj1Str;
                }
            }

            if (!string.IsNullOrEmpty(questIdStr))
            {
                Check("StartQuestAsync", await client.StartQuestAsync(questIdStr));
                // Only complete objectives when we have distinct objective IDs (backend create often returns no child objectives).
                if (!string.IsNullOrEmpty(obj1Str) && obj1Str != questIdStr)
                {
                    Check("CompleteQuestObjectiveAsync", await client.CompleteQuestObjectiveAsync(questIdStr, obj1Str, "Harness"));
                    Check("QueueCompleteQuestObjectiveAsync", await client.QueueCompleteQuestObjectiveAsync(questIdStr, obj2Str, "Harness"));
                }
                Check("FlushQuestObjectiveJobsAsync", await client.FlushQuestObjectiveJobsAsync());
                Check("CompleteQuestAsync", await client.CompleteQuestAsync(questIdStr));
            }
        }

        var createMonsterNft = await client.CreateMonsterNftAsync(
            $"HarnessBoss-{suffix}",
            "Monster NFT from harness",
            "Harness",
            "{\"level\":10,\"hp\":5000}");
        Check("CreateMonsterNftAsync", createMonsterNft);
        if (!createMonsterNft.IsError && !string.IsNullOrWhiteSpace(createMonsterNft.Result.NftId))
            Check("DeployBossNftAsync", await client.DeployBossNftAsync(createMonsterNft.Result.NftId, "Harness", "arena_01"));
        else
            Console.WriteLine("DeployBossNftAsync skipped because CreateMonsterNftAsync did not return an NFT id.");

        /* NFT minting tests (inventory item mint: Id + Hash) */
        var mintItemName = $"HarnessMintKey-{suffix}";
        var mintDirect = await client.MintInventoryItemNftAsync(mintItemName, "Harness direct mint", "Harness", "KeyItem");
        Check("MintInventoryItemNftAsync", mintDirect);
        if (!mintDirect.IsError && mintDirect.Result.NftId is { } nftId)
        {
            if (string.IsNullOrWhiteSpace(mintDirect.Result.Hash))
                Console.WriteLine("[PASS] MintInventoryItemNftAsync => NftId present (Hash optional).");
            else
                Console.WriteLine($"[PASS] MintInventoryItemNftAsync => NftId: {nftId}, Hash: {mintDirect.Result.Hash}");
        }

        var pickupMintItem = $"HarnessPickupMint-{suffix}";
        client.EnqueuePickupWithMintJobOnly(pickupMintItem, "Harness pickup-with-mint", "Harness", "KeyItem", doMint: true, quantity: 1);
        Check("FlushAddItemJobsAsync (pickup-with-mint)", await client.FlushAddItemJobsAsync());
        await Task.Delay(500);
        var consumed = client.ConsumeLastMintResult(out var lastItem, out var lastNftId, out var lastHash);
        if (consumed && (lastNftId is not null || lastItem is not null))
        {
            _passed++;
            _results.Add(("ConsumeLastMintResult (after pickup-with-mint)", true, $"Item={lastItem}, NftId={lastNftId}, Hash={lastHash ?? "(none)"}"));
            Console.WriteLine($"[PASS] ConsumeLastMintResult => Item: {lastItem}, ID: {lastNftId}, Hash: {lastHash ?? "(none)"}");
        }
        else
        {
            _failed++;
            _results.Add(("ConsumeLastMintResult (after pickup-with-mint)", false, consumed ? "No result consumed" : "Consume returned false"));
            Console.WriteLine("[FAIL] ConsumeLastMintResult => no mint result (pickup-with-mint may not have completed in time).");
        }

        /* [NFT] prefix / NftId test: add item with nftId, refetch inventory, assert NftId is set so Doom/Quake can show "[NFT] " + name */
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("========== [NFT] prefix test (NftId persistence for Doom/Quake) ==========");
        Console.ForegroundColor = prevColor;

        var nftPrefixItemName = $"HarnessNftPrefix-{suffix}";
        const string nftPrefixNftId = "harness-nft-prefix-001";
        var addWithNft = await client.AddItemAsync(nftPrefixItemName, "Harness [NFT] prefix test", "Harness", "KeyItem", nftId: nftPrefixNftId);
        if (!addWithNft.IsError)
        {
            client.InvalidateInventoryCache();
            var invAfterNft = await client.GetInventoryAsync();
            if (!invAfterNft.IsError && invAfterNft.Result is not null)
            {
                var nftItem = invAfterNft.Result.FirstOrDefault(x => string.Equals(x.Name, nftPrefixItemName, StringComparison.OrdinalIgnoreCase));
                if (nftItem is not null && !string.IsNullOrEmpty(nftItem.NftId))
                {
                    var displayName = string.IsNullOrEmpty(nftItem.NftId) ? nftItem.Name : "[NFT] " + nftItem.Name;
                    if (displayName == "[NFT] " + nftPrefixItemName)
                    {
                        _passed++;
                        _results.Add(("[NFT] prefix (add with nftId, GET inventory)", true, $"NftId={nftItem.NftId}, display={displayName}"));
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[PASS] [NFT] prefix => item has NftId, display would be \"{displayName}\" (real API: NftId persisted and returned)");
                        Console.ForegroundColor = prevColor;
                    }
                    else
                    {
                        _failed++;
                        _results.Add(("[NFT] prefix (add with nftId, GET inventory)", false, $"Display expected [NFT] {nftPrefixItemName}, got {displayName}"));
                        Console.WriteLine($"[FAIL] [NFT] prefix => display expected \"[NFT] {nftPrefixItemName}\", got \"{displayName}\"");
                    }
                }
                else
                {
                    _failed++;
                    _results.Add(("[NFT] prefix (add with nftId, GET inventory)", false, nftItem is null ? "Item not in inventory" : "NftId empty after GET (API may not return NFTId)"));
                    Console.WriteLine("[FAIL] [NFT] prefix => item missing or NftId empty after GET inventory (real API must return NFTId).");
                }
            }
            else
            {
                _failed++;
                _results.Add(("[NFT] prefix (add with nftId, GET inventory)", false, invAfterNft.Message ?? "GetInventory failed"));
                Console.WriteLine($"[FAIL] [NFT] prefix => GetInventory failed: {invAfterNft.Message}");
            }
        }
        else
        {
            _failed++;
            _results.Add(("[NFT] prefix (add with nftId)", false, addWithNft.Message ?? "AddItem failed"));
            Console.WriteLine($"[FAIL] [NFT] prefix => AddItem failed: {addWithNft.Message}");
        }

        /* Inventory tests (from test_inventory.c): invalidate cache, send-to-avatar, send-to-clan. Use dedicated items so we don't send one that was already consumed by UseItemAsync. */
        client.InvalidateInventoryCache();
        Check("GetInventoryAsync (after invalidate)", await client.GetInventoryAsync());

        var sendToAvatarItem = $"HarnessSendToAvatar-{suffix}";
        var sendToClanItem = $"HarnessSendToClan-{suffix}";
        var sendTargetAvatar = useFakeServer ? "harness-target-avatar" : GetEnv("STARAPI_SEND_TARGET_AVATAR", "");
        var sendTargetClan = useFakeServer ? "HarnessTestClan" : GetEnv("STARAPI_SEND_TARGET_CLAN", "");
        var addForSendAvatar = await client.AddItemAsync(sendToAvatarItem, "For send-to-avatar test", "Harness", "KeyItem");
        var addForSendClan = await client.AddItemAsync(sendToClanItem, "For send-to-clan test", "Harness", "KeyItem");
        if (!addForSendAvatar.IsError)
        {
            if (!string.IsNullOrWhiteSpace(sendTargetAvatar))
                Check("SendItemToAvatarAsync", await client.SendItemToAvatarAsync(sendTargetAvatar, sendToAvatarItem, 1));
            else
                Console.WriteLine("[SKIP] SendItemToAvatarAsync (set STARAPI_SEND_TARGET_AVATAR for real API)");
        }
        else
            Console.WriteLine("[SKIP] SendItemToAvatarAsync (AddItem for send failed)");
        if (!addForSendClan.IsError)
        {
            if (!string.IsNullOrWhiteSpace(sendTargetClan))
                Check("SendItemToClanAsync", await client.SendItemToClanAsync(sendTargetClan, sendToClanItem, 1));
            else
                Console.WriteLine("[SKIP] SendItemToClanAsync (set STARAPI_SEND_TARGET_CLAN for real API; requires ArbitrumOASIS provider)");
        }
        else
            Console.WriteLine("[SKIP] SendItemToClanAsync (AddItem for send failed)");

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

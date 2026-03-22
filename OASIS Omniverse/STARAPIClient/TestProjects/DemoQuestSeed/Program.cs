using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Contracts;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STARAPI.Client;
using NextGenSoftware.OASIS.STARAPI.Client.Tests;

namespace DemoQuestSeed;

/// <summary>
/// Seeds the STAR API with demo quests and objectives for testing ODOOM and OQuake quest UIs.
/// Quest names use a " v2" suffix and descriptions note explicit objective dictionaries (vs legacy inferred-only rows).
/// Objectives are created as the Quest.Objectives collection (CreateCrossGameQuestAsync name, description, objectivesList).
/// Sub-quests are added separately via AddSubQuestAsync. Uses same env vars as TestHarness.
/// </summary>
internal static class Program
{
    private static string GetEnv(string name, string defaultValue)
    {
        var v = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(v) ? defaultValue : v.Trim();
    }

    static async Task Main(string[] args)
    {
        var web5BaseUrl = GetEnv("STARAPI_WEB5_BASE_URL", StarApiTestDefaults.Web5BaseUrl);
        var web4BaseUrl = GetEnv("STARAPI_WEB4_BASE_URL", StarApiTestDefaults.Web4BaseUrl);
        var username = GetEnv("STARAPI_USERNAME", StarApiTestDefaults.Username);
        var password = GetEnv("STARAPI_PASSWORD", StarApiTestDefaults.Password);

        Console.WriteLine("==============================================");
        Console.WriteLine(" OASIS STAR API – Demo Quest Seed (quests named … v2)");
        Console.WriteLine("==============================================");
        Console.WriteLine($"WEB5 (STAR): {web5BaseUrl}");
        Console.WriteLine($"WEB4 (OASIS): {web4BaseUrl}");
        Console.WriteLine($"User: {username}");
        Console.WriteLine();

        using var client = new StarApiClient();
        var init = client.Init(new StarApiConfig
        {
            Web5StarApiBaseUrl = web5BaseUrl,
            Web4OasisApiBaseUrl = web4BaseUrl,
            TimeoutSeconds = 60
        });
        if (init.IsError)
        {
            Console.WriteLine($"Init failed: {init.Message}");
            Environment.Exit(1);
        }

        var auth = await client.AuthenticateAsync(username, password);
        if (auth.IsError || !auth.Result)
        {
            Console.WriteLine($"Authenticate failed: {auth.Message}");
            Environment.Exit(1);
        }
        Console.WriteLine("Authenticated.");


        OASISResult<List<StarQuestInfo>> quests = await client.GetAllQuestsForAvatarAsync();

        if (quests != null && quests.Result != null && !quests.IsError)
        {
            var list = quests.Result;
            int withObjectives = list.Count(q => q.Objectives != null && q.Objectives.Count > 0);
            int subQuests = list.Count(q => !string.IsNullOrWhiteSpace(q.ParentQuestId));
            int topLevel = list.Count(q => string.IsNullOrWhiteSpace(q.ParentQuestId) || q.ParentQuestId == Guid.Empty.ToString());

            Console.WriteLine($"Loaded {list.Count} quest(s): {topLevel} top-level, {subQuests} sub-quests, {withObjectives} with objectives.");
            Console.WriteLine();

            foreach (StarQuestInfo quest in list)
            {
                if (quest.Objectives != null && quest.Objectives.Count > 0)
                {
                    Console.WriteLine($"  [{quest.Id}] {quest.Name}: {quest.Objectives.Count} objective(s)");
                    foreach (var obj in quest.Objectives)
                        Console.WriteLine($"      - {obj.Description} (GameSource: {obj.GameSource}, Done: {obj.IsCompleted})");
                }
            }

            string qstring = StarApiClient.SerializeQuestsForGame(list);
            Console.WriteLine();
            Console.WriteLine($"Serialized quest list: {qstring?.Length ?? 0} chars.");
            if (!string.IsNullOrEmpty(qstring))
            {
                int previewLen = Math.Min(400, qstring.Length);
                Console.WriteLine($"  Preview: {qstring[..previewLen].Replace("\t", "|").Replace("\n", " ")}...");
            }

            // Verify cache path: objectives for first quest that has them (same path Doom/Quake use for detail panel).
            var firstWithObjectives = list.FirstOrDefault(q => q.Objectives != null && q.Objectives.Count > 0);
            if (firstWithObjectives != null)
            {
                var fromCache = client.GetQuestObjectivesFromCache(firstWithObjectives.Id);
                Console.WriteLine();
                Console.WriteLine($"Cache check (objectives for {firstWithObjectives.Id}): GetQuestObjectivesFromCache returned {fromCache?.Count ?? 0} line(s).");
            }
            Console.WriteLine();
        }
        else
            Console.WriteLine($"Error Loading Quests: {quests?.Message ?? "Unknown"}");


            // Resolve avatar ID so create/start requests send X-Avatar-Id (same avatar as when game fetches quests).
            var activeCheck = await client.GetActiveQuestsAsync();
        if (activeCheck.IsError)
            Console.WriteLine($"Warning: Could not resolve avatar ID: {activeCheck.Message}. Quests will use JWT avatar.");
        else
            Console.WriteLine($"Avatar ID for quests: {client.GetCachedAvatarId() ?? "(none)"} (use this avatar when beaming in from ODOOM/OQuake).");
        Console.WriteLine();

        var questsToCreate = new[]
        {
            new DemoQuest(
                "Demo: Doom & Quake v2",
                "Complete objectives in both Doom and Quake to earn rewards. (v2 = explicit objective dictionaries)",
                new[]
                {
                    Obj("Kill 5 monsters in Doom", "ODOOM", 0, new StarQuestObjectiveDictionaries
                    {
                        NeedToKillMonsters = NeedN("ODOOM", "5")
                    }),
                    Obj("Collect a key in Doom", "ODOOM", 1, new StarQuestObjectiveDictionaries
                    {
                        NeedToCollectKeys = Need1("ODOOM")
                    }),
                    Obj("Pick up health in Quake", "OQUAKE", 2, new StarQuestObjectiveDictionaries
                    {
                        NeedToCollectHealth = Need1("OQUAKE")
                    })
                }),
            new DemoQuest(
                "Demo: Quake Explorer v2",
                "Explore Quake and complete these objectives. (v2 = explicit objective dictionaries)",
                new[]
                {
                    Obj("Find a Mega Health in Quake", "OQUAKE", 0, new StarQuestObjectiveDictionaries
                    {
                        NeedToCollectHealth = Need1("OQUAKE")
                    }),
                    Obj("Kill an enemy in Quake", "OQUAKE", 1, new StarQuestObjectiveDictionaries
                    {
                        NeedToKillMonsters = Need1("OQUAKE")
                    })
                }),
            new DemoQuest(
                "Demo: Doom Runner v2",
                "Quick Doom objectives for testing the quest UI. (v2 = explicit objective dictionaries)",
                new[]
                {
                    Obj("Collect armor in Doom", "ODOOM", 0, new StarQuestObjectiveDictionaries
                    {
                        NeedToCollectArmor = Need1("ODOOM")
                    }),
                    Obj("Use a Stimpack in Doom", "ODOOM", 1, new StarQuestObjectiveDictionaries
                    {
                        NeedToCollectHealth = Need1("ODOOM")
                    })
                })
        };

        string? firstStartedQuestId = null;
        foreach (var q in questsToCreate)
        {
            var objectivesList = q.Objectives.ToList();
            var create = await client.CreateCrossGameQuestAsync(q.Name, q.Description, objectivesList);
            if (create.IsError)
            {
                Console.WriteLine($"Create quest \"{q.Name}\" failed: {create.Message}");
                continue;
            }
            var quest = create.Result;
            if (quest == null || string.IsNullOrEmpty(quest.Id))
            {
                Console.WriteLine($"Create quest \"{q.Name}\" returned no ID.");
                continue;
            }
            Console.WriteLine($"Created quest: {q.Name} (Id: {quest.Id})");

            var start = await client.StartQuestAsync(quest.Id);
            if (start.IsError)
                Console.WriteLine($"  Start quest failed: {start.Message}");
            else
            {
                if (firstStartedQuestId == null) firstStartedQuestId = quest.Id;
                Console.WriteLine($"  Started (InProgress for UI).");
            }
            Console.WriteLine();
        }

        /* Create a quest that has BOTH objectives (Option B) AND sub-quests for testing the 3-list UI (Prereqs, Objectives, Sub-quests). */
        var parentWithSub = new DemoQuest(
            "Demo: Parent with Objectives + Sub-quests v2",
            "This quest has embedded objectives and child sub-quests for testing all three right-panel lists. (v2 = explicit objective dictionaries)",
            new[]
            {
                Obj("Collect Red key in ODOOM", "ODOOM", 0, new StarQuestObjectiveDictionaries
                {
                    NeedToCollectKeys = Need1("ODOOM")
                }),
                Obj("Earn 100 XP in OQUAKE", "OQUAKE", 1, new StarQuestObjectiveDictionaries
                {
                    NeedToEarnXP = NeedN("OQUAKE", "100")
                })
            });
        var parentCreate = await client.CreateCrossGameQuestAsync(parentWithSub.Name, parentWithSub.Description, parentWithSub.Objectives.ToList());
        if (!parentCreate.IsError && parentCreate.Result != null && !string.IsNullOrEmpty(parentCreate.Result.Id))
        {
            var parentId = parentCreate.Result.Id;
            Console.WriteLine($"Created parent quest with objectives: {parentWithSub.Name} (Id: {parentId})");
            var startParent = await client.StartQuestAsync(parentId);
            if (!startParent.IsError) Console.WriteLine("  Started.");

            var sub1 = await client.AddSubQuestAsync(parentId, "Nested: Clear Doom level v2", name: "Doom Level Clear v2", gameSource: "ODOOM", itemRequired: "Complete level", order: 0);
            if (!sub1.IsError && sub1.Result != null) Console.WriteLine($"  Added sub-quest: Doom Level Clear v2 (Id: {sub1.Result.Id})");
            else if (sub1.IsError) Console.WriteLine($"  Add sub-quest failed: {sub1.Message}");

            var sub2 = await client.AddSubQuestAsync(parentId, "Nested: Find Quake rune v2", name: "Quake Rune v2", gameSource: "OQUAKE", itemRequired: "Rune", order: 1);
            if (!sub2.IsError && sub2.Result != null) Console.WriteLine($"  Added sub-quest: Quake Rune v2 (Id: {sub2.Result.Id})");
            else if (sub2.IsError) Console.WriteLine($"  Add sub-quest failed: {sub2.Message}");
            Console.WriteLine();
        }
        else if (parentCreate.IsError)
            Console.WriteLine($"Create parent quest failed: {parentCreate.Message}");

        // Create quests with prerequisites and multiple objectives for testing the right-panel lists (Prerequisites, Sub-quests/Objectives).
        var step1 = new DemoQuest(
            "Step 1: First Quest v2",
            "Complete this first to unlock Step 2. Used to test prerequisite chain in the quest popup. (v2 = explicit objective dictionaries)",
            new[]
            {
                Obj("Get a key in any game", "ODOOM", 0, new StarQuestObjectiveDictionaries
                {
                    NeedToCollectKeys = Need1("ODOOM")
                }),
                Obj("Pick up health once", "OQUAKE", 1, new StarQuestObjectiveDictionaries
                {
                    NeedToCollectHealth = Need1("OQUAKE")
                })
            });
        var step2 = new DemoQuest(
            "Step 2: Unlock Second v2",
            "Requires Step 1 v2 completed. Tests prerequisites list and objectives in the UI. (v2 = explicit objective dictionaries)",
            new[]
            {
                Obj("Find armor in Doom", "ODOOM", 0, new StarQuestObjectiveDictionaries
                {
                    NeedToCollectArmor = Need1("ODOOM")
                }),
                Obj("Kill one enemy in Quake", "OQUAKE", 1, new StarQuestObjectiveDictionaries
                {
                    NeedToKillMonsters = Need1("OQUAKE")
                })
            });
        var step3 = new DemoQuest(
            "Step 3: Final Step v2",
            "Requires Step 2 v2 completed. Full chain: Step 1 v2 -> Step 2 v2 -> Step 3 v2. (v2 = explicit objective dictionaries)",
            new[]
            {
                Obj("Use a Stimpack in Doom", "ODOOM", 0, new StarQuestObjectiveDictionaries
                {
                    NeedToCollectHealth = Need1("ODOOM")
                }),
                Obj("Find Mega Health in Quake", "OQUAKE", 1, new StarQuestObjectiveDictionaries
                {
                    NeedToCollectHealth = Need1("OQUAKE")
                })
            });

        string? step1Id = null;
        string? step2Id = null;
        foreach (var q in new[] { step1, step2, step3 })
        {
            var objectivesList = q.Objectives.ToList();
            var create = await client.CreateCrossGameQuestAsync(q.Name, q.Description, objectivesList);
            if (create.IsError)
            {
                Console.WriteLine($"Create quest \"{q.Name}\" failed: {create.Message}");
                continue;
            }
            var quest = create.Result;
            if (quest == null || string.IsNullOrEmpty(quest.Id))
            {
                Console.WriteLine($"Create quest \"{q.Name}\" returned no ID.");
                continue;
            }
            Console.WriteLine($"Created quest (with objectives): {q.Name} (Id: {quest.Id})");

            if (q == step1)
            {
                step1Id = quest.Id;
                var start = await client.StartQuestAsync(quest.Id);
                if (start.IsError)
                    Console.WriteLine($"  Start quest failed: {start.Message}");
                else
                    Console.WriteLine($"  Started (InProgress).");
            }
            else if (q == step2)
            {
                step2Id = quest.Id;
                if (!string.IsNullOrEmpty(step1Id))
                {
                    var setPrereq = await client.SetQuestPrerequisitesAsync(quest.Id, new[] { step1Id });
                    if (setPrereq.IsError)
                        Console.WriteLine($"  Set prerequisites failed: {setPrereq.Message}");
                    else
                        Console.WriteLine($"  Prerequisite set: Step 1 v2.");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(step2Id))
                {
                    var setPrereq = await client.SetQuestPrerequisitesAsync(quest.Id, new[] { step2Id });
                    if (setPrereq.IsError)
                        Console.WriteLine($"  Set prerequisites failed: {setPrereq.Message}");
                    else
                        Console.WriteLine($"  Prerequisite set: Step 2 v2.");
                }
            }
            Console.WriteLine();
        }

        var active = await client.GetQuestsByStatusAsync("InProgress");
        if (!active.IsError && active.Result != null && active.Result.Count > 0)
        {
            Console.WriteLine($"Active quests: {active.Result.Count}");
            foreach (var q in active.Result)
                Console.WriteLine($"  - {q.Name} ({q.Id})");
        }

        Console.WriteLine();
        Console.WriteLine("Done. Open ODOOM or OQuake, press Q to view quests.");
    }

    private record DemoQuest(string Name, string Description, IReadOnlyList<StarQuestObjective> Objectives);

    private static Dictionary<string, List<string>> Need1(string game) =>
        new(StringComparer.OrdinalIgnoreCase) { [game] = new List<string> { "1" } };

    private static Dictionary<string, List<string>> NeedN(string game, string n) =>
        new(StringComparer.OrdinalIgnoreCase) { [game] = new List<string> { n } };

    private static StarQuestObjective Obj(string description, string gameSource, int order, StarQuestObjectiveDictionaries dictionaries) =>
        new()
        {
            Description = description,
            GameSource = gameSource,
            Order = order,
            IsCompleted = false,
            Dictionaries = dictionaries
        };
}

using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STARAPI.Client;
using NextGenSoftware.OASIS.STARAPI.Client.Tests;

namespace DemoQuestSeed;

/// <summary>
/// Seeds the STAR API with demo quests and objectives for testing ODOOM and OQuake quest UIs.
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
        Console.WriteLine(" OASIS STAR API – Demo Quest Seed");
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
                "Demo: Doom & Quake",
                "Complete objectives in both Doom and Quake to earn rewards.",
                new[]
                {
                    new StarQuestObjective { Description = "Kill 5 monsters in Doom", GameSource = "ODOOM", ItemRequired = "Kill", IsCompleted = false },
                    new StarQuestObjective { Description = "Collect a key in Doom", GameSource = "ODOOM", ItemRequired = "Key", IsCompleted = false },
                    new StarQuestObjective { Description = "Pick up health in Quake", GameSource = "OQUAKE", ItemRequired = "Health", IsCompleted = false }
                }),
            new DemoQuest(
                "Demo: Quake Explorer",
                "Explore Quake and complete these objectives.",
                new[]
                {
                    new StarQuestObjective { Description = "Find a Mega Health in Quake", GameSource = "OQUAKE", ItemRequired = "Megahealth", IsCompleted = false },
                    new StarQuestObjective { Description = "Kill an enemy in Quake", GameSource = "OQUAKE", ItemRequired = "Kill", IsCompleted = false }
                }),
            new DemoQuest(
                "Demo: Doom Runner",
                "Quick Doom objectives for testing the quest UI.",
                new[]
                {
                    new StarQuestObjective { Description = "Collect armor in Doom", GameSource = "ODOOM", ItemRequired = "Armor", IsCompleted = false },
                    new StarQuestObjective { Description = "Use a Stimpack in Doom", GameSource = "ODOOM", ItemRequired = "Stimpack", IsCompleted = false }
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
            "Demo: Parent with Objectives + Sub-quests",
            "This quest has embedded objectives and child sub-quests for testing all three right-panel lists.",
            new[]
            {
                new StarQuestObjective { Description = "Collect Red key in ODOOM", GameSource = "ODOOM", ItemRequired = "Red key", IsCompleted = false },
                new StarQuestObjective { Description = "Earn 100 XP in OQUAKE", GameSource = "OQUAKE", ItemRequired = "100 XP", IsCompleted = false }
            });
        var parentCreate = await client.CreateCrossGameQuestAsync(parentWithSub.Name, parentWithSub.Description, parentWithSub.Objectives.ToList());
        if (!parentCreate.IsError && parentCreate.Result != null && !string.IsNullOrEmpty(parentCreate.Result.Id))
        {
            var parentId = parentCreate.Result.Id;
            Console.WriteLine($"Created parent quest with objectives: {parentWithSub.Name} (Id: {parentId})");
            var startParent = await client.StartQuestAsync(parentId);
            if (!startParent.IsError) Console.WriteLine("  Started.");

            var sub1 = await client.AddSubQuestAsync(parentId, "Nested: Clear Doom level", name: "Doom Level Clear", gameSource: "ODOOM", itemRequired: "Complete level", order: 0);
            if (!sub1.IsError && sub1.Result != null) Console.WriteLine($"  Added sub-quest: Doom Level Clear (Id: {sub1.Result.Id})");
            else if (sub1.IsError) Console.WriteLine($"  Add sub-quest failed: {sub1.Message}");

            var sub2 = await client.AddSubQuestAsync(parentId, "Nested: Find Quake rune", name: "Quake Rune", gameSource: "OQUAKE", itemRequired: "Rune", order: 1);
            if (!sub2.IsError && sub2.Result != null) Console.WriteLine($"  Added sub-quest: Quake Rune (Id: {sub2.Result.Id})");
            else if (sub2.IsError) Console.WriteLine($"  Add sub-quest failed: {sub2.Message}");
            Console.WriteLine();
        }
        else if (parentCreate.IsError)
            Console.WriteLine($"Create parent quest failed: {parentCreate.Message}");

        // Create quests with prerequisites and multiple objectives for testing the right-panel lists (Prerequisites, Sub-quests/Objectives).
        var step1 = new DemoQuest(
            "Step 1: First Quest",
            "Complete this first to unlock Step 2. Used to test prerequisite chain in the quest popup.",
            new[]
            {
                new StarQuestObjective { Description = "Get a key in any game", GameSource = "ODOOM", ItemRequired = "Key", IsCompleted = false },
                new StarQuestObjective { Description = "Pick up health once", GameSource = "OQUAKE", ItemRequired = "Health", IsCompleted = false }
            });
        var step2 = new DemoQuest(
            "Step 2: Unlock Second",
            "Requires Step 1 completed. Tests prerequisites list and objectives in the UI.",
            new[]
            {
                new StarQuestObjective { Description = "Find armor in Doom", GameSource = "ODOOM", ItemRequired = "Armor", IsCompleted = false },
                new StarQuestObjective { Description = "Kill one enemy in Quake", GameSource = "OQUAKE", ItemRequired = "Kill", IsCompleted = false }
            });
        var step3 = new DemoQuest(
            "Step 3: Final Step",
            "Requires Step 2 completed. Full chain: Step 1 -> Step 2 -> Step 3.",
            new[]
            {
                new StarQuestObjective { Description = "Use a Stimpack in Doom", GameSource = "ODOOM", ItemRequired = "Stimpack", IsCompleted = false },
                new StarQuestObjective { Description = "Find Mega Health in Quake", GameSource = "OQUAKE", ItemRequired = "Megahealth", IsCompleted = false }
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
                        Console.WriteLine($"  Prerequisite set: Step 1.");
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
                        Console.WriteLine($"  Prerequisite set: Step 2.");
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
}

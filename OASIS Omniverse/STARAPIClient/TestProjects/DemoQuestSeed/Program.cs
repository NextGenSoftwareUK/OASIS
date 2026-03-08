using NextGenSoftware.OASIS.STARAPI.Client;
using NextGenSoftware.OASIS.STARAPI.Client.Tests;

namespace DemoQuestSeed;

/// <summary>
/// Seeds the STAR API with demo quests and objectives for testing ODOOM and OQuake quest UIs.
/// Uses same env vars as TestHarness: STARAPI_WEB5_BASE_URL, STARAPI_WEB4_BASE_URL, STARAPI_USERNAME, STARAPI_PASSWORD.
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
        Console.WriteLine();

        var questsToCreate = new[]
        {
            new DemoQuest(
                "Demo: Doom & Quake",
                "Complete objectives in both Doom and Quake to earn rewards.",
                new[]
                {
                    new StarQuestObjective { Description = "Kill 5 monsters in Doom", GameSource = "Doom", ItemRequired = "Kill", IsCompleted = false },
                    new StarQuestObjective { Description = "Collect a key in Doom", GameSource = "Doom", ItemRequired = "Key", IsCompleted = false },
                    new StarQuestObjective { Description = "Pick up health in Quake", GameSource = "Quake", ItemRequired = "Health", IsCompleted = false }
                }),
            new DemoQuest(
                "Demo: Quake Explorer",
                "Explore Quake and complete these objectives.",
                new[]
                {
                    new StarQuestObjective { Description = "Find a Mega Health in Quake", GameSource = "Quake", ItemRequired = "Megahealth", IsCompleted = false },
                    new StarQuestObjective { Description = "Kill an enemy in Quake", GameSource = "Quake", ItemRequired = "Kill", IsCompleted = false }
                }),
            new DemoQuest(
                "Demo: Doom Runner",
                "Quick Doom objectives for testing the quest UI.",
                new[]
                {
                    new StarQuestObjective { Description = "Collect armor in Doom", GameSource = "Doom", ItemRequired = "Armor", IsCompleted = false },
                    new StarQuestObjective { Description = "Use a Stimpack in Doom", GameSource = "Doom", ItemRequired = "Stimpack", IsCompleted = false }
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
            else if (firstStartedQuestId == null)
            {
                firstStartedQuestId = quest.Id;
                Console.WriteLine($"  Started (active for UI).");
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

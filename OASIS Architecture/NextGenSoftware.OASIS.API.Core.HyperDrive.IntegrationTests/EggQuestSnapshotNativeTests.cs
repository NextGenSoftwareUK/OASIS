using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task EggQuestSnapshotsAndHatchPersistAcrossThreeOwnedNativeStores()
    {
        RequireOwnedFixtures();
        var actor = Guid.NewGuid();
        var other = Guid.NewGuid();
        await using (var mongo = new MongoStore())
        {
            await mongo.Activate();
            await using var bolt = new BoltStore();
            await bolt.Activate(actor);
            await bolt.TrackOwnedActorAsync(other);
            using var sqlite = new SqliteStore();
            Assert.False((await sqlite.Provider.ActivateProviderAsync()).IsError);
            var stores = new (IOASISStorageProvider Provider, Func<Guid, Task<string>> Native)[]
            {
                (mongo.Provider, mongo.ReadDocumentNativeAsync),
                (sqlite.Provider, sqlite.ReadMetadataJsonNativeAsync),
                (bolt.Provider, bolt.ReadDocumentNativeAsync)
            };
            foreach (var store in stores)
            {
                await SeedRoutingStores([store.Provider], actor);
                await SeedRoutingStores([store.Provider], other);
                var protectedProfile = await Read(store.Provider, actor);
                var foreignBefore = await store.Native(other);
                var routing = Routing([store.Provider], false);
                Assert.False(routing.PublishRoutingOverrides(new ProviderRoutingOverrides(
                    autoFailoverEnabled: false, autoReplicationEnabled: false, autoLoadBalancingEnabled: false),
                    store.Provider.ProviderType.Value).IsError);
                EggsManager Cold() => (EggsManager)Activator.CreateInstance(typeof(EggsManager),
                    BindingFlags.Instance | BindingFlags.NonPublic, null,
                    [store.Provider, routing.OASISDNA, routing], null)!;
                var preferences = new HolonManager(store.Provider, routing.OASISDNA, routing);
                var eggId = Guid.NewGuid();
                var collection = new[] { new Egg { Id = eggId, AvatarId = actor, Name = "Owned hatchable egg", IsHatchable = true } };
                var seeded = await preferences.SaveSettingAsync(actor, "Eggs", "collection",
                    Newtonsoft.Json.JsonConvert.SerializeObject(collection));
                Assert.False(seeded.IsError, seeded.Message);
                Assert.True(seeded.Result);

                var quests = await Cold().SaveEggQuestsAsync(actor,
                [
                    new EggQuest { AvatarId = other, QuestName = "Owned active egg quest", IsActive = true,
                        RequiredSteps = 3, CompletedSteps = 0, Rewards = new() { ["coins"] = 0 } },
                    new EggQuest { QuestName = "Owned completed egg quest", IsActive = false,
                        RequiredSteps = 2, CompletedSteps = 2 }
                ]);
                Assert.False(quests.IsError, quests.Message);
                Assert.True(quests.IsSaved);
                Assert.Equal(2, quests.Result.Count);
                Assert.All(quests.Result, quest => { Assert.NotEqual(Guid.Empty, quest.Id); Assert.Equal(actor, quest.AvatarId); });
                Assert.True(quests.Result[1].IsCompleted);
                Assert.NotNull(quests.Result[1].CompletedAt);
                var board = await Cold().SaveEggQuestLeaderboardAsync(actor,
                [
                    new EggQuestLeaderboard { AvatarId = actor, AvatarName = "Owned zero-score avatar", Score = 0, Rank = 2 },
                    new EggQuestLeaderboard { AvatarId = other, AvatarName = "Owned leading avatar", Score = 10, Rank = 1 }
                ]);
                Assert.False(board.IsError, board.Message);
                Assert.True(board.IsSaved);

                var native = await store.Native(actor);
                Assert.Contains("Owned active egg quest", native);
                Assert.Contains("Owned zero-score avatar", native);
                Assert.Contains("questLeaderboard", native);
                var current = await Cold().GetCurrentEggQuestsAsync(actor);
                Assert.False(current.IsError, current.Message);
                Assert.Equal(quests.Result[0].Id, Assert.Single(current.Result).Id);
                Assert.Equal(0, current.Result[0].CompletedSteps);
                var ranked = await Cold().GetCurrentEggQuestLeaderboardAsync(actor);
                Assert.False(ranked.IsError, ranked.Message);
                Assert.Equal(new[] { 1, 2 }, ranked.Result.Select(entry => entry.Rank));
                Assert.Equal(0, ranked.Result[1].Score);
                var rejected = await Cold().SaveEggQuestsAsync(actor,
                    [new EggQuest { QuestName = "Invalid replacement", RequiredSteps = 1, CompletedSteps = 2 }]);
                Assert.True(rejected.IsError);
                Assert.Equal(quests.Result[0].Id, Assert.Single((await Cold().GetCurrentEggQuestsAsync(actor)).Result).Id);

                var hatched = await Cold().HatchEggAsync(actor, eggId);
                Assert.False(hatched.IsError, hatched.Message);
                Assert.Equal(eggId, hatched.Result.Id);
                Assert.True(hatched.Result.IsHatched);
                Assert.True(Assert.Single((await Cold().GetAllEggsAsync(actor)).Result).IsHatched);
                Assert.Equal(2, (await Cold().GetCurrentEggQuestLeaderboardAsync(actor)).Result.Count);
                var cleared = await Cold().SaveEggQuestsAsync(actor, []);
                Assert.False(cleared.IsError, cleared.Message);
                Assert.Empty((await Cold().GetCurrentEggQuestsAsync(actor)).Result);
                Assert.Equal(foreignBefore, await store.Native(other));
                var after = await Read(store.Provider, actor);
                Assert.Equal(protectedProfile.FirstName, after.FirstName);
                Assert.Equal(protectedProfile.LastName, after.LastName);
                Assert.Equal(protectedProfile.Username, after.Username);
                Assert.Equal(protectedProfile.Email, after.Email);
                Assert.Equal(protectedProfile.Address, after.Address);
                Console.WriteLine($"Egg quest native round trip verified: {store.Provider.ProviderType.Value}; cold quest/leaderboard reads, zero score, hatch and collection preservation; foreign profile unchanged.");
            }
        }
        Console.WriteLine("Owned egg quest Mongo/SQLite/Neo4j scopes disposed and absence verified.");
    }
}

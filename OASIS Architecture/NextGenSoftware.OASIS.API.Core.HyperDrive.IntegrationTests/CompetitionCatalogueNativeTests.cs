using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task CompetitionCataloguesRankAndRegistrationPersistAcrossThreeOwnedNativeStores()
    {
        RequireOwnedFixtures();
        var actor = Guid.NewGuid();
        var other = Guid.NewGuid();
        const CompetitionType type = CompetitionType.Karma;
        const SeasonType season = SeasonType.Yearly;
        await using (var mongo = new MongoStore())
        {
            await mongo.Activate();
            await using var bolt = new BoltStore();
            await bolt.Activate(actor);
            await bolt.TrackOwnedActorAsync(other);
            using var sqlite = new SqliteStore();
            Assert.False((await sqlite.Provider.ActivateProviderAsync()).IsError);
            var stores = new (IOASISStorageProvider Provider, Func<Guid, Task<string>> Native,
                Func<Guid, Task<bool>> Exists, Func<Guid, Task<string>> Account)[]
            {
                (mongo.Provider, mongo.ReadHolonDocumentNativeAsync, mongo.HolonExistsNativeAsync, mongo.ReadDocumentNativeAsync),
                (sqlite.Provider, sqlite.ReadHolonMetadataJsonNativeAsync, sqlite.HolonExistsNativeAsync, sqlite.ReadMetadataJsonNativeAsync),
                (bolt.Provider, bolt.ReadHolonDocumentNativeAsync, bolt.HolonExistsNativeAsync, bolt.ReadDocumentNativeAsync)
            };
            foreach (var store in stores)
            {
                await SeedRoutingStores([store.Provider], actor);
                await SeedRoutingStores([store.Provider], other);
                var accountBefore = await store.Account(actor);
                var foreignBefore = await store.Account(other);
                var routing = Routing([store.Provider], false);
                Assert.False(routing.PublishRoutingOverrides(new ProviderRoutingOverrides(
                    autoFailoverEnabled: false, autoReplicationEnabled: false, autoLoadBalancingEnabled: false),
                    store.Provider.ProviderType.Value).IsError);
                CompetitionManager Cold() => (CompetitionManager)Activator.CreateInstance(typeof(CompetitionManager),
                    BindingFlags.Instance | BindingFlags.NonPublic, null,
                    [store.Provider, routing.OASISDNA, routing], null)!;
                var manager = Cold();
                var managerType = typeof(CompetitionManager);
                var category = (string)managerType.GetMethod("GetLeaderboardCategory", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(manager, [type, season])!;
                Guid SettingsId(Guid owner, string name) => (Guid)typeof(HolonManager)
                    .GetMethod("GetSettingsHolonId", BindingFlags.Static | BindingFlags.NonPublic)!
                    .Invoke(null, [owner, name])!;
                var boardId = SettingsId(Guid.Empty, category);
                var leaguesId = SettingsId(Guid.Empty, (string)managerType.GetMethod("GetLeagueCategory", BindingFlags.Static | BindingFlags.NonPublic)!
                    .Invoke(null, [type, season])!);
                var tournamentsId = SettingsId(Guid.Empty, (string)managerType.GetMethod("GetTournamentCategory", BindingFlags.Static | BindingFlags.NonPublic)!
                    .Invoke(null, [type])!);
                var projectionId = SettingsId(actor, "leaderboard");
                var ownedSettings = new[] { boardId, leaguesId, tournamentsId, projectionId };
                if (store.Provider == bolt.Provider)
                    foreach (var id in ownedSettings) await bolt.TrackOwnedActorAsync(id);

                var emptyBoard = await Cold().GetLeaderboardAsync(type, season);
                Assert.False(emptyBoard.IsError, emptyBoard.Message); Assert.Empty(emptyBoard.Result);
                var emptyLeagues = await Cold().GetAvailableLeaguesAsync(type, season);
                Assert.False(emptyLeagues.IsError, emptyLeagues.Message); Assert.Empty(emptyLeagues.Result);
                var emptyTournaments = await Cold().GetActiveTournamentsAsync(type);
                Assert.False(emptyTournaments.IsError, emptyTournaments.Message); Assert.Empty(emptyTournaments.Result);
                var unranked = await Cold().GetAvatarRankAsync(actor, type, season);
                Assert.True(unranked.IsError); Assert.Equal(OASISResultErrorCodes.RecordNotFound, unranked.ErrorCode);
                foreach (var id in ownedSettings) Assert.False(await store.Exists(id));

                var published = await Cold().SaveLeaguesAsync(type, season,
                [
                    new League { Name = "Owned zero-score league", MinScore = -100, MaxScore = 0, IsActive = true },
                    new League { Name = "Owned positive league", MinScore = 1, MaxScore = 100, IsActive = true }
                ]);
                Assert.False(published.IsError, published.Message); Assert.True(published.IsSaved);
                Assert.All(published.Result, league => { Assert.Equal(type, league.CompetitionType); Assert.Equal(season, league.SeasonType); });
                Assert.Contains("Owned zero-score league", await store.Native(leaguesId));
                var score = await Cold().UpdateAvatarScoreAsync(actor, type, season, 0);
                Assert.False(score.IsError, score.Message); Assert.True(score.Result);
                var now = DateTime.UtcNow;
                var tournament = await Cold().SaveTournamentAsync(new Tournament
                {
                    Name = "Owned registration tournament", CompetitionType = type, CreatedBy = actor,
                    Status = TournamentStatus.Registration, RegistrationStart = now.AddHours(-1),
                    RegistrationEnd = now.AddHours(1), StartDate = now.AddHours(2), EndDate = now.AddHours(3),
                    MaxParticipants = 5
                });
                Assert.False(tournament.IsError, tournament.Message); Assert.True(tournament.IsSaved);
                Assert.Contains("Owned registration tournament", await store.Native(tournamentsId));
                var snapshots = new Dictionary<Guid, string>();
                foreach (var id in ownedSettings) snapshots[id] = await store.Native(id);

                var rank = await Cold().GetAvatarRankAsync(actor, type, season);
                Assert.False(rank.IsError, rank.Message); Assert.Equal(0, rank.Result.Score); Assert.Equal(1, rank.Result.Rank);
                var league = await Cold().GetAvatarLeagueAsync(actor, type, season);
                Assert.False(league.IsError, league.Message); Assert.Equal(published.Result[0].Id, league.Result.Id);
                var page = await Cold().GetLeaderboardAsync(type, season, 50, 0);
                Assert.False(page.IsError, page.Message); Assert.Equal(actor, Assert.Single(page.Result).AvatarId);
                var available = await Cold().GetActiveTournamentsAsync(type);
                Assert.False(available.IsError, available.Message); Assert.Equal(tournament.Result.Id, Assert.Single(available.Result).Id);
                foreach (var snapshot in snapshots) Assert.Equal(snapshot.Value, await store.Native(snapshot.Key));

                var joined = await Cold().JoinTournamentAsync(actor, tournament.Result.Id, type);
                Assert.False(joined.IsError, joined.Message); Assert.True(joined.Result); Assert.True(joined.IsSaved);
                var reopened = await Cold().GetActiveTournamentsAsync(type);
                Assert.False(reopened.IsError, reopened.Message);
                var actual = Assert.Single(reopened.Result);
                Assert.Equal(1, actual.CurrentParticipants); Assert.Equal(actor, Assert.Single(actual.Participants).AvatarId);
                var joinedNative = await store.Native(tournamentsId);
                Assert.Contains(actor.ToString(), joinedNative);
                var acknowledgedAgain = await Cold().JoinTournamentAsync(actor, tournament.Result.Id, type);
                Assert.False(acknowledgedAgain.IsError, acknowledgedAgain.Message); Assert.True(acknowledgedAgain.Result);
                Assert.Equal(joinedNative, await store.Native(tournamentsId));
                Assert.Equal(accountBefore, await store.Account(actor));
                Assert.Equal(foreignBefore, await store.Account(other));
                Console.WriteLine($"Competition native main flow verified: {store.Provider.ProviderType.Value}; empty reads create no holons; published leagues, zero-score rank, immutable reads, cold registration and duplicate acknowledgement; profiles unchanged.");
            }
        }
        Console.WriteLine("Owned Competition Mongo/SQLite/Neo4j scopes disposed and absence verified.");
    }
}

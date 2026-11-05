using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.IntegrationTests
{
    /// <summary>
    /// Exercises the real CRUD surface against a live backend.
    ///
    /// Connection details come from SURREALDBOASIS_&lt;PARAM&gt; environment variables. When the
    /// backend is not reachable the provider fails to activate and every test is
    /// inconclusive rather than failed - so an unconfigured machine reports "skipped"
    /// instead of red.
    /// </summary>
    [TestClass]
    public class SurrealDBOASISIntegrationTests
    {
        private SurrealDBOASIS _provider = null!;
        private bool _live;

        [TestInitialize]
        public void Setup()
        {
            _provider = SurrealDBOASISTestFactory.Create();
            try { _live = !_provider.ActivateProvider().IsError && _provider.IsProviderActivated; }
            catch { _live = false; }
        }

        private void RequireLive()
        {
            if (!_live)
                Assert.Inconclusive(
                    "No live SurrealDBOASIS backend configured - set the SURREALDBOASIS_* environment variables to run this.");
        }

        [TestMethod]
        public async Task SaveAndLoadAvatar_RoundTrips()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsNotNull(loaded.Result);
            Assert.AreEqual(avatar.Username, loaded.Result!.Username);

            await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
        }

        [TestMethod]
        public async Task LoadAvatarByUsername_FindsTheSavedAvatar()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);

            var found = await _provider.LoadAvatarByUsernameAsync(avatar.Username);
            Assert.IsFalse(found.IsError, found.Message);
            Assert.AreEqual(avatar.Id, found.Result!.Id);

            await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
        }

        [TestMethod]
        public async Task SaveAndLoadHolon_RoundTrips()
        {
            RequireLive();

            var holon = new Holon { Id = Guid.NewGuid(), Name = $"OASIS IT Holon {Guid.NewGuid():N}" };

            var saved = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadHolonAsync(holon.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(holon.Name, loaded.Result!.Name);

            await _provider.DeleteHolonAsync(holon.Id);
        }

        [TestMethod]
        public async Task DeletedAvatar_IsNotReturnedAsLive()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            await _provider.SaveAvatarAsync(avatar);
            var deleted = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
            Assert.IsFalse(deleted.IsError, deleted.Message);

            var reloaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsTrue(reloaded.IsError || reloaded.Result == null || reloaded.Result.IsDeleted,
                "A soft-deleted avatar should not come back as a live record.");
        }

        [TestMethod]
        public async Task LoadAllAvatars_ReturnsAResult()
        {
            RequireLive();
            var all = await _provider.LoadAllAvatarsAsync();
            Assert.IsNotNull(all);
        }

        [TestMethod]
        public async Task BoundedCqlReturnsOnlyAdmittedLiveRows()
        {
            RequireLive();
            const ProviderType authority = ProviderType.SurrealDBOASIS;
            var actor = Guid.NewGuid();
            var liveId = Guid.NewGuid();
            var missingId = Guid.NewGuid();
            var foreignId = Guid.NewGuid();
            var liveDeleted = false;

            CosmicQueryPlan<Holon> Plan(Guid id, Guid? admittedId = null)
            {
                var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
                    .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, authority, new[] { admittedId ?? id }).Result).Result
                    .Where(row => row.Id == id).Result;
                var plan = CosmicQueryPlanner.Plan(query, authority,
                    new[] { new CosmicQueryTarget(authority, _provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                return plan.Result;
            }

            try
            {
                foreach (var (id, owner) in new[] { (liveId, actor), (foreignId, Guid.NewGuid()) })
                {
                    var saved = await _provider.SaveHolonAsync(new Holon
                    {
                        Id = id, Name = "SurrealDB CQL fixture", CreatedByAvatarId = owner
                    }, saveChildren: false);
                    Assert.IsFalse(saved.IsError, saved.Message);
                }

                var found = await _provider.ExecuteCosmicQueryAsync(Plan(liveId));
                Assert.IsFalse(found.IsError, found.Message + found.Exception);
                Assert.AreEqual(liveId, found.Result.Single().Id);

                var missing = await _provider.ExecuteCosmicQueryAsync(Plan(missingId));
                Assert.IsFalse(missing.IsError, missing.Message + missing.Exception);
                Assert.AreEqual(0, missing.Result.Count);

                var foreign = await _provider.ExecuteCosmicQueryAsync(Plan(foreignId, liveId));
                Assert.IsFalse(foreign.IsError, foreign.Message + foreign.Exception);
                Assert.AreEqual(0, foreign.Result.Count);

                var cancelled = await _provider.ExecuteCosmicQueryAsync(Plan(liveId),
                    cancellationToken: new CancellationToken(canceled: true));
                Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
                Assert.IsNull(cancelled.Result);

                var deleted = await _provider.DeleteHolonAsync(liveId);
                Assert.IsFalse(deleted.IsError, deleted.Message);
                liveDeleted = true;
                var tombstone = await _provider.ExecuteCosmicQueryAsync(Plan(liveId));
                Assert.IsFalse(tombstone.IsError, tombstone.Message + tombstone.Exception);
                Assert.AreEqual(0, tombstone.Result.Count);
            }
            finally
            {
                var liveCleanup = liveDeleted ? null : await _provider.DeleteHolonAsync(liveId);
                var foreignCleanup = await _provider.DeleteHolonAsync(foreignId);
                if (liveCleanup != null) Assert.IsFalse(liveCleanup.IsError, liveCleanup.Message);
                Assert.IsFalse(foreignCleanup.IsError, foreignCleanup.Message);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}

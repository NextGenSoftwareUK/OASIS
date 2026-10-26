using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CosmicCqlDemo;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
public sealed partial class CqlV1Web4NativeTests
{
    [TestMethod]
    public async Task SignedHttpQueriesUseNativeVisibilityPagingAndDurableReceiptsOnAllThreeProviders()
    {
        if (Environment.GetEnvironmentVariable("OASIS_COSMIC_RESTART_PHASE") is { } restartPhase)
        {
            await RunRecoveryProcessPhase(restartPhase);
            return;
        }
        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string storageDb = "cosmic_cql_v1_web4_storage", usageDb = "cosmic_cql_v1_web4_usage";
        Assert.AreEqual(mongoUri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("bolt://127.0.0.1:17687", Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var manifest = Path.Combine(work, "cql-v1-web4-fixture.json");
        var sqlite = Path.Combine(work, "cql-v1-web4.sqlite");
        Assert.IsFalse(File.Exists(manifest), "Reconcile any previous fixture before another run.");
        foreach (var path in new[] { sqlite, sqlite + "-wal", sqlite + "-shm" })
            Assert.IsFalse(File.Exists(path), "Preserve existing SQLite resources: " + path);

        var mongo = new MongoClient(mongoUri);
        using (var databases = await mongo.ListDatabaseNamesAsync())
        {
            var existing = await databases.ToListAsync();
            Assert.IsFalse(existing.Contains(storageDb) || existing.Contains(usageDb), "Never reset retained test databases.");
        }
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var inspection = driver.AsyncSession();
        var count = await (await inspection.RunAsync("MATCH (h:Holon) RETURN count(h) AS n")).SingleAsync();
        Assert.AreEqual(0L, count["n"].As<long>(), "Native fixtures require an empty retained Holon namespace.");

        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var tag = Guid.NewGuid().ToString("D");
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        var childIds = Enumerable.Range(0,4).Select(_=>Guid.NewGuid()).ToArray();
        var federationEncryptedId = Guid.NewGuid();
        var scalarIds = Enumerable.Range(0,137).Select(_=>Guid.NewGuid()).ToArray();
        var restActor = Guid.NewGuid();
        var restIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var transportActor = Guid.NewGuid();
        var transportIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var recoveryIds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var scalarWrites = new List<string>();
        var sharingOperations = new List<string>();
        var sharingProvidersVerified = 0;
        var ownedWritesProvidersVerified = 0;
        var ownedBatchProvidersVerified = 0;
        var hostedWriteBoundariesVerified = 0;
        var journal = new List<object>();
        using var journalGate = new SemaphoreSlim(1);
        async Task Journal(string stage, object? request = null)
        {
            await journalGate.WaitAsync();
            try
            {
                if (request != null) journal.Add(request);
                await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new
                { stage, sqlite, storageDb, usageDb, alice, bob, outsider, ids=ids.Concat(childIds).Append(federationEncryptedId).ToArray(), scalarIds,
                    restActor, restIds, transportActor, transportIds, recoveryIds, scalarWrites, sharingOperations, tag, requests = journal }));
            }
            finally { journalGate.Release(); }
        }
        await Journal("before-create");

        var providers = new Dictionary<ProviderType, IOASISStorageProvider>
        {
            [ProviderType.MongoDBOASIS] = new MongoProvider(mongoUri, storageDb),
            [ProviderType.SQLLiteDBOASIS] = new SqliteProvider("Data Source=" + sqlite),
            [ProviderType.Neo4jOASIS] = new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only")
        };
        var activated = new List<IOASISStorageProvider>();
        var complete = false;
        var walkthroughRequests = 0;
        var localSdkQueries = 0;
        var sdkFederationExamples = 0;
        var demoFederationRequests = 0;
        try
        {
            foreach (var provider in providers.Values)
            {
                var activation = await provider.ActivateProviderAsync();
                Assert.IsFalse(activation.IsError, activation.Message);
                activated.Add(provider);
                var names = new[] { "A-private", "A-index", "B-owned", "C-public", "D-owned" };
                for (var i = 0; i < names.Length; i++)
                {
                    var holon = new Holon
                    {
                        Id = ids[i], Name = names[i], CreatedByAvatarId = i is 2 or 4 ? alice : bob,
                        IsPublic = i == 3, IsActive = true,
                        MetaData = new()
                        {
                            ["fixture"] = tag, ["stock"] = 7L, ["guid"] = ids[i],
                            ["enabled"] = i >= 2, ["nullable"] = null!,
                            ["precision"] = 9007199254740993L,
                            // Adjacent UTC ticks exercise exact date filters and
                            // detached date output on every native provider.
                            ["counter"] = ulong.MaxValue - (ulong)i,
                            ["price"] = 1m + i * 0.0000000000000000000000000001m,
                            ["reading"] = 1d + i * 2.220446049250313e-16d,
                            ["singleReading"] = 1f + i * 1.1920929e-7f,
                            ["observedAt"] = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc).AddTicks(i),
                            // Hidden roots carry stronger extrema; signed Min/Max
                            // must apply actor visibility before native reduction.
                            ["extremum"] = i == 0 ? long.MinValue : i == 1 ? long.MaxValue : -9007199254740993L + i,
                            ["mixedInteger"] = i == 4 ? (object)"7" : 7L,
                            ["mixedGuid"] = i == 4 ? (object)"not-a-guid" : ids[i],
                            ["location"] = new Dictionary<string, object> { ["city"] = "London" },
                            // An index value is deliberately insufficient to grant Alice access.
                            ["SHARED_WITH"] = alice.ToString("D")
                        }
                    };
                    if(i==3)
                    {
                        holon.Children.Add(new Holon{Id=childIds[0],ParentHolonId=holon.Id,Name="public child",IsPublic=true,
                            CreatedByAvatarId=bob,HolonType=HolonType.Quest});
                        var hidden=new Holon{Id=childIds[1],ParentHolonId=holon.Id,Name="private child",CreatedByAvatarId=bob,HolonType=HolonType.Quest};
                        hidden.MetaData["SHARED_WITH"]=alice.ToString("D");
                        hidden.Children.Add(new Holon{Id=childIds[3],ParentHolonId=hidden.Id,Name="public behind private",IsPublic=true,CreatedByAvatarId=bob});
                        holon.Children.Add(hidden);
                        holon.Children.Add(new Holon{Id=childIds[2],ParentHolonId=holon.Id,Name="owned child",CreatedByAvatarId=alice,HolonType=HolonType.Cafe});
                        // The save owner captures this logical order and returns
                        // its persisted observation. Callers never fabricate it.
                    }
                    var seeded = await provider.SaveHolonAsync(holon, saveChildren: i==3);
                    Assert.IsFalse(seeded.IsError, seeded.Message + seeded.Exception);
                }
            }

            await VerifyAuthoritativeAccess(providers, outsider, ids, childIds);
            var nativeFederationQueries = await VerifyNativeFederation(providers, alice, outsider, tag, ids);

            var outbox = new MongoUsageOutboxStore(mongoUri, usageDb);
            await outbox.InitializeAsync(CancellationToken.None);
            var authority = new Authority();
            var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
            var mode = Environment.GetEnvironmentVariable("OASIS_CQL_V1_DEMO_MODE");
            Assert.IsTrue(mode == null || mode is "console" or "serve", "Explicit demo mode required.");
            foreach (var actor in new[] { alice, bob, outsider })
                authority.Tokens.Add(Token(actor, key, lifetimeMinutes: mode == null ? 5 : 60), actor);
            var hostedReadPolicyRequests = await VerifyShippingReadPolicy(providers, outbox, authority, secret,
                outsider, ids, childIds, tag, Journal);
            Assert.AreEqual(30, hostedReadPolicyRequests);
            var hostedFederation = await VerifyShippingFederation(providers, outbox, authority, secret,
                outsider, ids, federationEncryptedId, tag, Journal);
            Assert.AreEqual(13, hostedFederation.Requests);
            Assert.AreEqual(12, hostedFederation.Settled);
            Assert.AreEqual(23, hostedFederation.Attempts);
            var hostedCursors = await VerifyShippingFederationCursors(providers, outbox, authority, secret,
                outsider, ids, federationEncryptedId, tag, Journal);
            var demo = mode == null ? null : new V1DemoSession
            {
                FixtureTag = tag, Ids = ids,
                Tokens = new Dictionary<string, string>
                {
                    ["Alice"] = authority.Tokens.Single(pair => pair.Value == alice).Key,
                    ["Bob"] = authority.Tokens.Single(pair => pair.Value == bob).Key,
                    ["Visitor"] = authority.Tokens.Single(pair => pair.Value == outsider).Key
                }
            };
            // Sharing writes use the same generic manager and actual stores.
            // Record every conditional operation before dispatch for reviewed cleanup.
            foreach (var pair in providers)
            {
                var manager = new Sources(providers).CreateManager(pair.Value);
                var actor = new Avatar { Id = bob };
                var original = await manager.LoadHolonAsync(ids[0], loadChildren: false);
                Assert.IsFalse(original.IsError, original.Message);
                var originalRevision = CosmicConcurrency.GetProviderRevisionId(original.Result, pair.Key);
                Assert.IsNotNull(originalRevision, "Sharing requires a real provider revision observation.");

                async Task<CosmicWriteOptions> SharingIntent(Guid revision)
                {
                    var operation = Guid.NewGuid();
                    sharingOperations.Add(operation.ToString("D"));
                    await Journal("before-sharing-mutation", new { provider = pair.Key.ToString(), operation });
                    return new CosmicWriteOptions(operation,
                        new Dictionary<Guid, Guid?> { [ids[0]] = revision });
                }

                var grant = await NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonAccess.ShareAsync(
                    manager, ids[0], new[] { alice }, actor, await SharingIntent(originalRevision.Value));
                Assert.IsFalse(grant.IsError, pair.Key + ": " + grant.Message + grant.Exception);
                Assert.IsTrue(grant.Result && grant.IsSaved);

                var granted = await manager.LoadHolonAsync(ids[0], loadChildren: false);
                Assert.IsFalse(granted.IsError, granted.Message);
                Assert.IsTrue(NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonAccess.CanRead(granted.Result, alice));
                var grantedRevision = CosmicConcurrency.GetProviderRevisionId(granted.Result, pair.Key)!.Value;
                Assert.AreNotEqual(originalRevision.Value, grantedRevision);

                var revoke = await NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonAccess.RevokeAsync(
                    manager, ids[0], new[] { alice }, actor, await SharingIntent(grantedRevision));
                Assert.IsFalse(revoke.IsError, pair.Key + ": " + revoke.Message + revoke.Exception);
                Assert.IsTrue(revoke.Result && revoke.IsSaved);

                // A stale grant must not resurrect a recipient after revocation.
                var stale = await NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonAccess.ShareAsync(
                    manager, ids[0], new[] { alice }, actor, await SharingIntent(grantedRevision));
                Assert.IsTrue(stale.IsError);
                Assert.AreEqual(CosmicConcurrencyException.Code, stale.ErrorCode);

                var final = await manager.LoadHolonAsync(ids[0], loadChildren: false);
                Assert.IsFalse(final.IsError, final.Message);
                Assert.IsFalse(NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonAccess.CanRead(final.Result, alice));
                Assert.IsFalse(final.Result.MetaData.ContainsKey(
                    NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonAccess.SharedWithIndexKey(alice)));
                // A trusted fixture writer simulates malformed historical metadata.
                // Readers must not extract the valid first ID from an invalid list.
                final.Result.MetaData[HolonReadGrants.MetadataKey] = "[\"" + alice.ToString("D") + "\",true]";
                var malformedWrite = await manager.SaveHolonAsync(final.Result, bob,
                    await SharingIntent(CosmicConcurrency.GetProviderRevisionId(final.Result, pair.Key)!.Value),
                    saveChildren: false);
                Assert.IsFalse(malformedWrite.IsError, malformedWrite.Message + malformedWrite.Exception);
                var malformed = await manager.LoadHolonAsync(ids[0], loadChildren: false);
                Assert.IsFalse(malformed.IsError, malformed.Message);
                Assert.AreEqual(HolonReadGrants.InvalidCode, HolonReadGrants.Read(malformed.Result).ErrorCode);
                Assert.IsFalse(NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonAccess.CanRead(malformed.Result, alice));
                var malformedRevision = CosmicConcurrency.GetProviderRevisionId(malformed.Result, pair.Key)!.Value;

                foreach (var revokeMalformed in new[] { false, true })
                {
                    var options = await SharingIntent(malformedRevision);
                    var rejected = revokeMalformed
                        ? await NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonAccess.RevokeAsync(
                            manager, ids[0], new[] { alice }, actor, options)
                        : await NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonAccess.ShareAsync(
                            manager, ids[0], new[] { alice }, actor, options);
                    Assert.IsTrue(rejected.IsError);
                    Assert.AreEqual(HolonReadGrants.InvalidCode, rejected.ErrorCode);
                }

                var unchanged = await manager.LoadHolonAsync(ids[0], loadChildren: false);
                Assert.IsFalse(unchanged.IsError, unchanged.Message);
                Assert.AreEqual(malformedRevision, CosmicConcurrency.GetProviderRevisionId(unchanged.Result, pair.Key));
                Assert.AreEqual(malformed.Result.MetaData[HolonReadGrants.MetadataKey].ToString(),
                    unchanged.Result.MetaData[HolonReadGrants.MetadataKey].ToString());

                // Repair is an explicit owner-controlled conditional mutation,
                // never an incidental side effect of granting another recipient.
                NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonAccess.RecordShares(unchanged.Result, Array.Empty<Guid>());
                var repaired = await manager.SaveHolonAsync(unchanged.Result, bob,
                    await SharingIntent(malformedRevision), saveChildren: false);
                Assert.IsFalse(repaired.IsError, repaired.Message + repaired.Exception);
                // The V2 owned-write facade must authorize the whole selected
                // graph, including a child whose payload forges its creator.
                async Task<Guid> OwnedIntent()
                {
                    var operation = Guid.NewGuid();
                    sharingOperations.Add(operation.ToString("D"));
                    await Journal("before-owned-graph-write", new { provider = pair.Key.ToString(), operation });
                    return operation;
                }
                var ownedRoot = await manager.LoadHolonAsync(ids[0], loadChildren: true, loadChildrenFromProvider: true);
                Assert.IsFalse(ownedRoot.IsError, ownedRoot.Message);
                var beforeOwnedRevision = CosmicConcurrency.GetProviderRevisionId(ownedRoot.Result, pair.Key)!.Value;
                var originalName = ownedRoot.Result.Name;
                var forgedChild = new Holon { Id = ids[2], ParentHolonId = ids[0],
                    CreatedByAvatarId = bob, Name = "forged foreign child edit" };
                ownedRoot.Result.Children.Add(forgedChild);
                var rejectedGraph = await manager.SaveOwnedHolonAsync(ownedRoot.Result, bob,
                    new CosmicWriteOptions(await OwnedIntent(), new Dictionary<Guid, Guid?>
                    { [ids[0]] = beforeOwnedRevision, [ids[2]] = Guid.NewGuid() }));
                Assert.IsTrue(rejectedGraph.IsError);
                Assert.AreEqual("COSMIC_WRITE_FORBIDDEN", rejectedGraph.ErrorCode);
                var untouchedRoot = await manager.LoadHolonAsync(ids[0], loadChildren: false);
                Assert.IsFalse(untouchedRoot.IsError, untouchedRoot.Message);
                Assert.AreEqual(beforeOwnedRevision, CosmicConcurrency.GetProviderRevisionId(untouchedRoot.Result, pair.Key));
                var untouchedChild = await manager.LoadHolonAsync(ids[2], loadChildren: false);
                Assert.IsFalse(untouchedChild.IsError, untouchedChild.Message);
                Assert.AreEqual("B-owned", untouchedChild.Result.Name);
                Assert.AreEqual(alice, untouchedChild.Result.CreatedByAvatarId);

                var staleOwned = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(untouchedRoot.Result);
                untouchedRoot.Result.MetaData["owned-write-control"] = true;
                var ownedSaved = await manager.SaveOwnedHolonAsync(untouchedRoot.Result, bob,
                    new CosmicWriteOptions(await OwnedIntent()), saveChildren: false);
                Assert.IsFalse(ownedSaved.IsError, ownedSaved.Message + ownedSaved.Exception);
                Assert.IsTrue(ownedSaved.IsSaved);
                staleOwned.Name = "stale owned overwrite";
                var ownedConflict = await manager.SaveOwnedHolonAsync(staleOwned, bob,
                    new CosmicWriteOptions(await OwnedIntent()), saveChildren: false);
                Assert.IsTrue(ownedConflict.IsError);
                Assert.AreEqual(CosmicConcurrencyException.Code, ownedConflict.ErrorCode);
                var winner = await manager.LoadHolonAsync(ids[0], loadChildren: false);
                Assert.IsFalse(winner.IsError, winner.Message);
                Assert.AreEqual(originalName, winner.Result.Name);
                Assert.IsTrue(JsonSerializer.SerializeToElement(winner.Result.MetaData["owned-write-control"]).GetBoolean());
                // All roots must pass ownership before any batch mutation.
                // The later foreign root must leave the first root untouched.
                var batchFirst = await manager.LoadHolonAsync(ids[0], loadChildren: false);
                var batchForeign = await manager.LoadHolonAsync(ids[2], loadChildren: false);
                Assert.IsFalse(batchFirst.IsError, batchFirst.Message);
                Assert.IsFalse(batchForeign.IsError, batchForeign.Message);
                var beforeBatch = CosmicConcurrency.GetProviderRevisionId(batchFirst.Result, pair.Key);
                batchFirst.Result.MetaData["owned-batch-control"] = "denied";
                batchForeign.Result.CreatedByAvatarId = bob;
                var forbiddenBatch = await manager.SaveOwnedHolonsAsync(
                    new[] { batchFirst.Result, batchForeign.Result }, bob,
                    new CosmicWriteOptions(await OwnedIntent()), saveChildren: false);
                Assert.IsTrue(forbiddenBatch.IsError);
                Assert.AreEqual("COSMIC_WRITE_FORBIDDEN", forbiddenBatch.ErrorCode);

                batchFirst = await manager.LoadHolonAsync(ids[0], loadChildren: false);
                var batchSecond = await manager.LoadHolonAsync(ids[1], loadChildren: false);
                Assert.IsFalse(batchFirst.IsError, batchFirst.Message);
                Assert.IsFalse(batchSecond.IsError, batchSecond.Message);
                Assert.AreEqual(beforeBatch, CosmicConcurrency.GetProviderRevisionId(batchFirst.Result, pair.Key));
                Assert.IsFalse(batchFirst.Result.MetaData.ContainsKey("owned-batch-control"));

                var staleBatchFirst = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(batchFirst.Result);
                batchFirst.Result.MetaData["owned-batch-control"] = "committed";
                batchSecond.Result.MetaData["owned-batch-control"] = "committed";
                var savedBatch = await manager.SaveOwnedHolonsAsync(
                    new[] { batchFirst.Result, batchSecond.Result }, bob,
                    new CosmicWriteOptions(await OwnedIntent()), saveChildren: false);
                Assert.IsFalse(savedBatch.IsError, savedBatch.Message + savedBatch.Exception);
                Assert.IsTrue(savedBatch.IsSaved);

                // A stale member must reject the entire native transaction,
                // including an otherwise valid edit of the other root.
                var secondRevision = CosmicConcurrency.GetProviderRevisionId(batchSecond.Result, pair.Key);
                staleBatchFirst.Name = "stale batch overwrite";
                batchSecond.Result.MetaData["owned-batch-control"] = "must not commit";
                var staleBatch = await manager.SaveOwnedHolonsAsync(
                    new[] { staleBatchFirst, batchSecond.Result }, bob,
                    new CosmicWriteOptions(await OwnedIntent()), saveChildren: false);
                Assert.IsTrue(staleBatch.IsError);
                Assert.AreEqual(CosmicConcurrencyException.Code, staleBatch.ErrorCode);
                var secondWinner = await manager.LoadHolonAsync(ids[1], loadChildren: false);
                Assert.IsFalse(secondWinner.IsError, secondWinner.Message);
                Assert.AreEqual(secondRevision, CosmicConcurrency.GetProviderRevisionId(secondWinner.Result, pair.Key));
                Assert.AreEqual("committed", secondWinner.Result.MetaData["owned-batch-control"].ToString());

                // Invoke the actual WEB4 save boundary with its wire DTOs. These
                // are native service controls, separate from the signed CQL HTTP
                // walkthrough and not a claim of full DataController HTTP proof.
                var hostedRoot = await manager.LoadHolonAsync(ids[0], loadChildren: false);
                var hostedForeign = await manager.LoadHolonAsync(ids[2], loadChildren: false);
                Assert.IsFalse(hostedRoot.IsError, hostedRoot.Message);
                Assert.IsFalse(hostedForeign.IsError, hostedForeign.Message);
                hostedForeign.Result.CreatedByAvatarId = bob;
                var deniedHosted = await NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonWriteEndpoint.SaveBatchAsync(
                    manager, new NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.SaveHolonsRequest
                    {
                        Holons = new()
                        {
                            NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(hostedRoot.Result),
                            NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(hostedForeign.Result)
                        },
                        OperationId = await OwnedIntent(), SaveChildren = false, ProviderType = pair.Key.ToString()
                    }, bob, false);
                Assert.AreEqual("COSMIC_WRITE_FORBIDDEN", deniedHosted.ErrorCode);
                Assert.AreEqual(HttpStatusCode.Forbidden,
                    NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonWriteEndpoint.Status(deniedHosted));

                var hostedStale = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(hostedRoot.Result);
                hostedRoot.Result.MetaData["hosted-write-control"] = "committed";
                var hostedSaved = await NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonWriteEndpoint.SaveAsync(
                    manager, new NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.SaveHolonRequest
                    {
                        Holon = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(hostedRoot.Result),
                        SaveChildren = false, ProviderType = pair.Key.ToString()
                    }, bob, false, (await OwnedIntent()).ToString("D"));
                Assert.IsFalse(hostedSaved.IsError, hostedSaved.Message + hostedSaved.Exception);
                Assert.IsTrue(hostedSaved.IsSaved);
                hostedStale.Name = "stale hosted overwrite";
                var hostedConflict = await NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonWriteEndpoint.SaveAsync(
                    manager, new NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.SaveHolonRequest
                    {
                        Holon = hostedStale, OperationId = await OwnedIntent(),
                        SaveChildren = false, ProviderType = pair.Key.ToString()
                    }, bob, false);
                Assert.AreEqual(CosmicConcurrencyException.Code, hostedConflict.ErrorCode);
                Assert.AreEqual(HttpStatusCode.Conflict,
                    NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonWriteEndpoint.Status(hostedConflict));
                hostedWriteBoundariesVerified++;
                ownedBatchProvidersVerified++;
                ownedWritesProvidersVerified++;
                sharingProvidersVerified++;
            }

            await using (var app = Host(providers, outbox, authority, secret, demo,
                (actor, operation) => Journal("before-http-request", new { actor, operation })))
            {
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(20) };

                    async Task<HttpResponseMessage> Send(object body, Guid actor, string? operationKey = null, string? tokenOverride = null,bool expectContinue = false,bool scalar = false,bool projection = false,bool join = false,bool composition = false)
                    {
                        var operation = operationKey ?? Guid.NewGuid().ToString("D");
                        await Journal("before-http-request", new { actor, operation });
                        using var request = new HttpRequestMessage(HttpMethod.Post, composition ? "/api/data/query-cql/composition" : join ? "/api/data/query-cql/join" : projection ? "/api/data/query-cql/projection" : scalar ? "/api/data/query-cql/scalar" : "/api/data/query-cql")
                        {
                            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                        };
                        request.Headers.Add("Idempotency-Key", operation);
                        request.Headers.ExpectContinue=expectContinue;
                        if (actor != Guid.Empty)
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                                tokenOverride ?? authority.Tokens.Single(pair => pair.Value == actor).Key);
                        return await client.SendAsync(request);
                    }

                    async Task<string[]> Read(object body, Guid actor, string? operationKey = null,Action<JsonElement>? inspect=null)
                    {
                        using var response = await Send(body, actor, operationKey);
                        var text = await response.Content.ReadAsStringAsync();
                        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                        Assert.AreEqual("settled", response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                        var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                        Assert.AreEqual("settled", operation.State);
                        Assert.AreEqual(1, operation.ProviderAttempts.Count);
                        Assert.AreEqual(1, operation.Receipts.Count);
                        Assert.AreEqual(0.002m, operation.Receipts.Single().ActualCostUsd);
                        Assert.AreEqual("CQL-service-query-attempt-tariff", operation.Receipts.Single().CostSource);
                        Assert.AreEqual(operation.ProviderAttempts.Single().ClientRequestId, operation.Receipts.Single().ClientRequestId);
                        using var json = JsonDocument.Parse(text);
                        Assert.IsFalse(json.RootElement.GetProperty("isError").GetBoolean());
                        Assert.AreEqual(operation.Receipts.Single().Provider, json.RootElement.GetProperty("result").GetProperty("provider").GetString());
                        inspect?.Invoke(json.RootElement.GetProperty("result"));
                        return json.RootElement.GetProperty("result").GetProperty("rows").EnumerateArray()
                            .Select(row => row.GetProperty("name").GetString()!).ToArray();
                    }

                    var beforeRejectedAuthentication = authority.Started.Count;
                    using (var anonymous = await Send(new { }, Guid.Empty)) Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);
                    using (var forged = await Send(new { }, alice, tokenOverride: Token(alice,
                               new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)))))
                        Assert.AreEqual(HttpStatusCode.Unauthorized, forged.StatusCode);
                    using (var issuer = await Send(new { }, alice, tokenOverride: Token(alice, key, "wrong-issuer")))
                        Assert.AreEqual(HttpStatusCode.Unauthorized, issuer.StatusCode);
                    using (var expired = await Send(new { }, alice, tokenOverride: Token(alice, key, expired: true)))
                        Assert.AreEqual(HttpStatusCode.Unauthorized, expired.StatusCode);
                    using (var claims = await Send(new { }, alice, tokenOverride: Token(alice, key, claimId: bob)))
                        Assert.AreEqual(HttpStatusCode.Unauthorized, claims.StatusCode);
                    Assert.AreEqual(beforeRejectedAuthentication, authority.Started.Count, "Invalid authentication must never reserve or dispatch.");

                    foreach (var provider in providers.Keys)
                    {
                        var fixture = new { MetadataPath = new[] { "fixture" }, Operator = "Equal", Scalar = "String", Value = tag };
                        // Independent source windows must apply the signed actor on BOTH
                        // sides. A left join preserves absent right rows; an inner join
                        // publishes only the public matching row. No extra seed data needed.
                        async Task Joined(Guid actor, string kind, int expectedRows, bool hiddenRight = false)
                        {
                            var logical = Guid.NewGuid().ToString("D");
                            object keySelection = hiddenRight
                                ? new { MetadataPath = new[] { "stock" }, ValueType = "Int64" }
                                : new { Field = "Name" };
                            var body = new
                            {
                                Provider = provider.ToString(), Kind = kind,
                                Left = new { Filters = new[] { fixture } },
                                Right = new { Filters = new object[] { fixture,
                                    new { Field = "Name", Operator = "Equal", Value = hiddenRight ? "A-private" : "C-public" } } },
                                Keys = new[] { new { Alias = "key", Left = keySelection, Right = keySelection } },
                                Columns = new object[]
                                {
                                    new { Alias = "name", Source = "Left", Selection = new { Field = "Name" } },
                                    new { Alias = "precision", Source = "Right", AllowMissing = true,
                                        Selection = new { MetadataPath = new[] { "precision" }, ValueType = "Int64" } },
                                    new { Alias = "present", Source = "RightPresence" }
                                }
                            };

                            using var response = await Send(body, actor, logical, join: true);
                            var text = await response.Content.ReadAsStringAsync();
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                            var operationId = response.Headers.GetValues("X-OASIS-Operation-Id").Single();
                            var operation = await outbox.GetAsync(operationId, CancellationToken.None);
                            Assert.AreEqual("settled", operation.State);
                            Assert.AreEqual(1, operation.ProviderAttempts.Count);
                            Assert.AreEqual(1, operation.Receipts.Count);
                            Assert.AreEqual(0.002m, operation.Receipts.Single().ActualCostUsd);
                            Assert.AreEqual(operation.ProviderAttempts.Single().ClientRequestId,
                                operation.Receipts.Single().ClientRequestId);

                            using var json = JsonDocument.Parse(text);
                            var result = json.RootElement.GetProperty("result");
                            Assert.AreEqual("cql-join-v1", result.GetProperty("version").GetString());
                            Assert.AreEqual(provider.ToString(), result.GetProperty("provider").GetString());
                            var rows = result.GetProperty("rows").EnumerateArray().ToArray();
                            Assert.AreEqual(expectedRows, rows.Length);
                            Assert.AreEqual(hiddenRight ? 0 : 1, rows.Count(row => row.GetProperty("hasRight").GetBoolean()));
                            foreach (var row in rows)
                            {
                                var values = row.GetProperty("values");
                                var present = row.GetProperty("hasRight").GetBoolean();
                                Assert.AreEqual(present, values[2].GetBoolean());
                                if (present)
                                {
                                    Assert.AreEqual("C-public", values[0].GetString());
                                    Assert.AreEqual("9007199254740993", values[1].GetString());
                                }
                                else Assert.AreEqual(JsonValueKind.Null, values[1].ValueKind);
                            }

                            // Reusing a logical operation must never cause another native
                            // join, even though the first request has already settled.
                            using var replay = await Send(body, actor, logical, join: true);
                            Assert.AreEqual(HttpStatusCode.Conflict, replay.StatusCode);
                            var retained = await outbox.GetAsync(operationId, CancellationToken.None);
                            Assert.AreEqual(1, retained.ProviderAttempts.Count);
                            Assert.AreEqual(1, retained.Receipts.Count);
                        }

                        await Joined(alice, "Left", 3);
                        await Joined(alice, "Inner", 1);
                        await Joined(outsider, "Left", 1);

                        // All visible left rows share stock=7 with the hidden right
                        // root. Any right-side policy leak would create matches.
                        await Joined(alice, "Left", 3, hiddenRight: true);
                        await Joined(outsider, "Left", 1, hiddenRight: true);

                        // Composition preserves stage order and actor visibility on
                        // all three stores, with one receipt and no replayed I/O.
                        async Task Composed(Guid actor, bool takeFirst, params string[] expectedNames)
                        {
                            var filter = new { operation = "Where", filters = new[]
                                { new { alias = "Name", @operator = "NotEqual", value = "B-owned" } } };
                            var take = new { operation = "Take", count = 1 };
                            var body = new
                            {
                                provider = provider.ToString(),
                                source = new
                                {
                                    filters = new[] { new { metadataPath = new[] { "fixture" }, scalar = "String", @operator = "Equal", value = tag } },
                                    orderBy = "Name", take = 3,
                                    columns = new object[]
                                    {
                                        new { alias = "Name", field = "Name" },
                                        new { alias = "Precision", metadataPath = new[] { "precision" }, valueType = "Int64" }
                                    }
                                },
                                stages = takeFirst ? new object[] { take, filter } : new object[] { filter, take }
                            };
                            var logical = Guid.NewGuid().ToString("D");
                            using var response = await Send(body, actor, logical, composition: true);
                            var text = await response.Content.ReadAsStringAsync();
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                            Assert.AreEqual("settled", response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                            var operationId = response.Headers.GetValues("X-OASIS-Operation-Id").Single();
                            var operation = await outbox.GetAsync(operationId, CancellationToken.None);
                            Assert.AreEqual(1, operation.ProviderAttempts.Count); Assert.AreEqual(1, operation.Receipts.Count);
                            Assert.AreEqual(0.002m, operation.Receipts.Single().ActualCostUsd);
                            using var json = JsonDocument.Parse(text);
                            var result = json.RootElement.GetProperty("result");
                            Assert.AreEqual("cql-composition-v1", result.GetProperty("version").GetString());
                            Assert.AreEqual(provider.ToString(), result.GetProperty("provider").GetString());
                            var rows = result.GetProperty("rows").EnumerateArray().ToArray();
                            CollectionAssert.AreEqual(expectedNames, rows.Select(row => row[0].GetString()).ToArray());
                            Assert.IsTrue(rows.All(row => row[1].GetString() == "9007199254740993"));
                            using var replay = await Send(body, actor, logical, composition: true);
                            Assert.AreEqual(HttpStatusCode.Conflict, replay.StatusCode);
                            var retained = await outbox.GetAsync(operationId, CancellationToken.None);
                            Assert.AreEqual(1, retained.ProviderAttempts.Count); Assert.AreEqual(1, retained.Receipts.Count);
                        }
                        await Composed(alice, false, "C-public");
                        await Composed(alice, true);
                        await Composed(outsider, false, "C-public");

                        // Unknown stage members must be rejected by the real MVC
                        // binder, with affirmative zero-dispatch settlement.
                        using (var malformed = await Send(new
                        {
                            provider = provider.ToString(),
                            source = new { columns = new[] { new { alias = "Name", field = "Name" } } },
                            stages = new[] { new { operation = "Take", count = 1, nativeSql = "untrusted" } }
                        }, alice, composition: true))
                        {
                            Assert.AreEqual(HttpStatusCode.BadRequest, malformed.StatusCode);
                            Assert.AreEqual("settled", malformed.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                            var rejected = await outbox.GetAsync(malformed.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                            Assert.AreEqual(0, rejected.ProviderAttempts.Count); Assert.AreEqual(0, rejected.Receipts.Count);
                        }

                        // Native selection must apply visibility before paging and return
                        // exact scalar cells, without hydrating source entities.
                        async Task Projection(Guid actor, int skip, bool replay)
                        {
                            var logical = Guid.NewGuid().ToString("D");
                            var body = new
                            {
                                Provider = provider.ToString(), Filters = new[] { fixture }, Skip = skip, Take = 1,
                                Columns = new object[]
                                {
                                    new { Alias = "name", Field = "Name" },
                                    new { Alias = "precision", MetadataPath = new[] { "precision" }, ValueType = "Int64" },
                                    new { Alias = "empty", MetadataPath = new[] { "nullable" }, ValueType = "Int64", Nullable = true },
                                    new { Alias = "code", MetadataPath = new[] { "guid" }, ValueType = "Guid" },
                                    new { Alias = "city", MetadataPath = new[] { "location", "city" }, ValueType = "String" }
                                }
                            };
                            using var response = await Send(body, actor, logical, projection: true);
                            var text = await response.Content.ReadAsStringAsync();
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                            Assert.AreEqual("settled", response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                            var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                            Assert.AreEqual("settled", operation.State); Assert.AreEqual(1, operation.ProviderAttempts.Count);
                            Assert.AreEqual(1, operation.Receipts.Count); Assert.AreEqual(0.002m, operation.Receipts.Single().ActualCostUsd);
                            using var json = JsonDocument.Parse(text);
                            var value = json.RootElement.GetProperty("result");
                            Assert.AreEqual("cql-projection-v1", value.GetProperty("version").GetString());
                            Assert.AreEqual(provider.ToString(), value.GetProperty("provider").GetString());
                            Assert.AreEqual("Int64", value.GetProperty("columns")[1].GetProperty("valueType").GetString());
                            var row = value.GetProperty("rows").EnumerateArray().Single();
                            Assert.AreEqual("C-public", row[0].GetString()); Assert.AreEqual("9007199254740993", row[1].GetString());
                            Assert.AreEqual(JsonValueKind.Null, row[2].ValueKind); Assert.AreEqual(ids[3].ToString("D"), row[3].GetString());
                            Assert.AreEqual("London", row[4].GetString());
                            if (replay)
                            {
                                var starts = authority.Started.Count;
                                using var duplicate = await Send(body, actor, logical, projection: true);
                                Assert.AreEqual(HttpStatusCode.Conflict, duplicate.StatusCode); Assert.AreEqual(starts, authority.Started.Count);
                                var retained = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                                Assert.AreEqual(1, retained.ProviderAttempts.Count); Assert.AreEqual(1, retained.Receipts.Count);
                            }
                        }
                        await Projection(alice, skip: 1, replay: true);
                        await Projection(outsider, skip: 0, replay: false);
                        using (var invalidColumns = await Send(new
                        {
                            Provider = provider.ToString(), Columns = new[] { new { Alias = "same", Field = "Name" }, new { Alias = "same", Field = "Id" } }
                        }, alice, projection: true)) Assert.AreEqual(HttpStatusCode.BadRequest, invalidColumns.StatusCode);
                        if (provider == ProviderType.Neo4jOASIS)
                            using (var unsupportedScalar = await Send(new
                            {
                                Provider = provider.ToString(), Columns = new[] { new { Alias = "amount", MetadataPath = new[] { "amount" }, ValueType = "NotAScalar" } }
                            }, alice, projection: true)) Assert.AreEqual(HttpStatusCode.BadRequest, unsupportedScalar.StatusCode);

                        // Scalar requests share signed admission and exact durable receipts.
                        // The fixture marker applies only to its five roots: Alice sees3,
                        // the outsider sees1. A private/shared-index root cannot participate.
                        async Task Scalar(object body, Guid actor, string domain, string expected, bool verifyReplay = false)
                        {
                            var logicalKey = Guid.NewGuid().ToString("D");
                            using var response = await Send(body, actor, logicalKey, scalar: true);
                            var text = await response.Content.ReadAsStringAsync();
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                            Assert.AreEqual("settled", response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                            var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                            Assert.AreEqual("settled", operation.State);
                            Assert.AreEqual(1, operation.ProviderAttempts.Count); Assert.AreEqual(1, operation.Receipts.Count);
                            Assert.AreEqual(0.002m, operation.Receipts.Single().ActualCostUsd);
                            Assert.AreEqual(operation.ProviderAttempts.Single().ClientRequestId, operation.Receipts.Single().ClientRequestId);
                            using var json = JsonDocument.Parse(text);
                            var value = json.RootElement.GetProperty("result");
                            Assert.AreEqual("cql-scalar-v1", value.GetProperty("version").GetString());
                            Assert.AreEqual(provider.ToString(), value.GetProperty("provider").GetString());
                            Assert.AreEqual(domain, value.GetProperty("valueType").GetString());
                            Assert.AreEqual(expected, domain == "Int64" ? value.GetProperty("value").GetString() : value.GetProperty("value").GetRawText());
                            if (verifyReplay)
                            {
                                var starts = authority.Started.Count;
                                using var duplicate = await Send(body, actor, logicalKey, scalar: true);
                                Assert.AreEqual(HttpStatusCode.Conflict, duplicate.StatusCode);
                                Assert.AreEqual(starts, authority.Started.Count, "Scalar replay must not reserve another execution.");
                                var retained = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                                Assert.AreEqual(1, retained.ProviderAttempts.Count, "Scalar replay must not dispatch another native attempt.");
                                Assert.AreEqual(1, retained.Receipts.Count, "Scalar replay must not duplicate receipts.");
                            }
                        }
                        await Scalar(new { Provider = provider.ToString(), Operation = "LongCount", Filters = new[] { fixture } }, alice, "Int64", "3");
                        await Scalar(new { Provider = provider.ToString(), Operation = "Count", Filters = new[] { fixture } }, outsider, "Int32", "1");
                        await Scalar(new { Provider = provider.ToString(), Operation = "Count", Filters = new[] { fixture }, Skip = 1, Take = 1 }, alice, "Int32", "1");
                        await Scalar(new { Provider = provider.ToString(), Operation = "Any", Filters = new[] { fixture }, Take = 0 }, alice, "Boolean", "false");
                        await Scalar(new { Provider = provider.ToString(), Operation = "Any", Filters = new[] { new { Field = "Id", Operator = "Equal", Value = ids[0] } } }, alice, "Boolean", "false");
                        await Scalar(new { Provider = provider.ToString(), Operation = "LongCount", Filters = new[] { fixture },
                            AnyDescendants = new[] { new { MaximumDepth = 2, MaximumNodes = 3, Filters = new[] { new { Field = "Name", Operator = "Equal", Value = "public behind private" } } } } }, outsider, "Int64", "0");

                        // Typed selected scalars retain exact Int64 text, visibility,
                        // source windows and the existing one-attempt receipt owner.
                        var selected = new { MetadataPath = new[] { "extremum" }, ValueType = "Int64" };
                        await Scalar(new { Provider = provider.ToString(), Operation = "Min", Selection = selected, Filters = new[] { fixture } },
                            alice, "Int64", "-9007199254740991", verifyReplay: true);
                        await Scalar(new { Provider = provider.ToString(), Operation = "Max", Selection = selected, Filters = new[] { fixture } },
                            alice, "Int64", "-9007199254740989");
                        await Scalar(new { Provider = provider.ToString(), Operation = "Min", Selection = selected, Filters = new[] { fixture } },
                            outsider, "Int64", "-9007199254740990");
                        await Scalar(new { Provider = provider.ToString(), Operation = "Max", Selection = selected, Filters = new[] { fixture }, Skip = 1, Take = 1 },
                            alice, "Int64", "-9007199254740990");
                        await Scalar(new { Provider = provider.ToString(), Operation = "Min", Selection = new
                            { MetadataPath = new[] { "absentSelectedValue" }, ValueType = "Int64", Nullable = true }, Filters = new[] { fixture } },
                            alice, "Int64", null!);

                        foreach(var graphActor in new[]{alice,outsider})
                        {
                            var expected=graphActor==alice?new[]{childIds[0],childIds[2]}:new[]{childIds[0]};
                            await Read(new{Provider=provider.ToString(),Take=1,
                                Filters=new[]{new{Field="Id",Operator="Equal",Value=ids[3]}},
                                Graph=new{MaxChildDepth=2,MaximumNodes=expected.Length+1}},graphActor,
                                inspect:result=>
                                {
                                    var row=result.GetProperty("rows").EnumerateArray().Single();
                                    Assert.AreEqual(Guid.Empty,row.GetProperty("parentHolonId").GetGuid());
                                    var children=row.GetProperty("children").EnumerateArray().ToArray();
                                    CollectionAssert.AreEqual(expected,children.Select(child=>child.GetProperty("id").GetGuid()).ToArray());
                                    Assert.IsTrue(children.All(child=>child.GetProperty("children").GetArrayLength()==0));
                                    var text=result.GetRawText();
                                    Assert.IsFalse(text.Contains(childIds[1].ToString()) || text.Contains(childIds[3].ToString()));
                                    Assert.IsFalse(text.Contains("childRoleManifest",StringComparison.OrdinalIgnoreCase));
                                    Assert.IsFalse(text.Contains("providerUniqueStorageKey",StringComparison.OrdinalIgnoreCase));
                                });
                        }

                        // Child predicates run under the signed actor before root paging.
                        // SHARED_WITH metadata on a private child is not an access grant.
                        foreach(var childActor in new[]{alice,outsider})
                        {
                            var childFilter=new{Field="Name",Operator="Equal",Value="owned child"};
                            var request=new{Provider=provider.ToString(),Filters=new[]{fixture},
                                AnyChildren=new[]{new{Filters=new[]{childFilter}}},Take=1};
                            CollectionAssert.AreEqual(childActor==alice?new[]{"C-public"}:Array.Empty<string>(),
                                await Read(request,childActor),provider+" child ownership");
                            var privateRequest=new{Provider=provider.ToString(),Filters=new[]{fixture},
                                AnyChildren=new[]{new{Filters=new[]{new{Field="Name",Operator="Equal",Value="private child"}}}},Take=1};
                            CollectionAssert.AreEqual(Array.Empty<string>(),await Read(privateRequest,childActor),provider+" private child");

                            // Descendant filtering shares the same native policy and
                            // cannot cross a private intermediate, even to a public leaf.
                            var nodeBudget=childActor==alice?2:1;
                            var descendantRequest=new {Provider=provider.ToString(),Filters=new[]{fixture},
                                AnyChildren=new[]{new {Filters=new[]{new {Field="Name",Operator="Equal",Value="public child"}}}},
                                AnyDescendants=new[]{new {MaximumDepth=2,MaximumNodes=nodeBudget,
                                    Filters=new[]{childFilter}}},Take=1};
                            CollectionAssert.AreEqual(childActor==alice?new[]{"C-public"}:Array.Empty<string>(),
                                await Read(descendantRequest,childActor),provider+" descendant ownership");
                            var hiddenBranch=new {Provider=provider.ToString(),Filters=new[]{fixture},
                                AnyDescendants=new[]{new {MaximumDepth=2,MaximumNodes=nodeBudget,
                                    Filters=new[]{new {Field="Name",Operator="Equal",Value="public behind private"}}}},Take=1};
                            CollectionAssert.AreEqual(Array.Empty<string>(),await Read(hiddenBranch,childActor),
                                provider+" inaccessible intermediates must not count or match");
                            var publicDescendant=new {Provider=provider.ToString(),Filters=new[]{fixture},
                                AnyDescendants=new[]{new {MaximumDepth=2,MaximumNodes=nodeBudget,
                                    Filters=new[]{new {Field="Name",Operator="Equal",Value="public child"}}}},Take=1};
                            CollectionAssert.AreEqual(new[]{"C-public"},await Read(publicDescendant,childActor),
                                provider+" descendant matching precedes root paging");
                        }

                        var page = new { Provider = provider.ToString(), Filters = new[] { fixture }, Skip = 1, Take = 1 };
                        var operationKey = Guid.NewGuid().ToString("D");
                        CollectionAssert.AreEqual(new[] { "C-public" }, await Read(page, alice, operationKey), provider.ToString());

                        var starts = authority.Started.Count;
                        using (var duplicate = await Send(page, alice, operationKey))
                            Assert.AreEqual(HttpStatusCode.Conflict, duplicate.StatusCode);
                        Assert.AreEqual(starts, authority.Started.Count, "No duplicate native invocation.");

                        var all = new { Provider = provider.ToString(), Filters = new[] { fixture }, Take = 3 };
                        CollectionAssert.AreEqual(new[] { "B-owned", "C-public", "D-owned" }, await Read(all, alice));
                        CollectionAssert.AreEqual(new[] { "A-index", "A-private", "C-public" }, await Read(all, bob));
                        CollectionAssert.AreEqual(new[] { "C-public" }, await Read(all, outsider));

                        var nested = new
                        {
                            Provider = provider.ToString(), Take = 3,
                            Filters = new object[] { fixture,
                                new { MetadataPath = new[] { "location", "city" }, Scalar = "String", Operator = "Equal", Value = "London" },
                                new { MetadataPath = new[] { "stock" }, Scalar = "Integer", Operator = "GreaterThanOrEqual", Value = 7 } },
                            AnyOf = new[] {
                                new { Field = "Name", Operator = "Equal", Value = "A-private" },
                                new { Field = "Name", Operator = "Equal", Value = "D-owned" } }
                        };
                        CollectionAssert.AreEqual(new[] { "D-owned" }, await Read(nested, alice), "OR must not escape visibility.");
                        CollectionAssert.AreEqual(Array.Empty<string>(), await Read(new
                        {
                            Provider = provider.ToString(), Filters = new[] { new { Field = "Name", Operator = "Contains", Value = "' OR 1=1 --" } }
                        }, alice), "Native literals must remain data.");

                        // Certify every advertised operator and each metadata scalar on
                        // all three native engines through the same shipping HTTP compiler.
                        async Task Check(object filter, params string[] expected) => CollectionAssert.AreEqual(expected,
                            await Read(new { Provider = provider.ToString(), Filters = new object[] { fixture, filter } }, alice),
                            provider + " " + JsonSerializer.Serialize(filter));
                        foreach (var comparison in new[] { "Equal", "GreaterThanOrEqual", "LessThanOrEqual" })
                            await Check(new { MetadataPath = new[] { "stock" }, Scalar = "Integer", Operator = comparison, Value = 7 },
                                "B-owned", "C-public", "D-owned");
                        await Check(new { MetadataPath = new[] { "stock" }, Scalar = "Integer", Operator = "GreaterThan", Value = 6 },
                            "B-owned", "C-public", "D-owned");
                        await Check(new { MetadataPath = new[] { "stock" }, Scalar = "Integer", Operator = "LessThan", Value = 8 },
                            "B-owned", "C-public", "D-owned");
                        await Check(new { Field = "Id", Operator = "Equal", Value = ids[2] }, "B-owned");
                        await Check(new { Field = "Id", Operator = "NotEqual", Value = ids[2] }, "C-public", "D-owned");
                        await Check(new { Field = "ParentHolonId", Operator = "Equal", Value = Guid.Empty }, "B-owned", "C-public", "D-owned");
                        await Check(new { Field = "Version", Operator = "GreaterThanOrEqual", Value = 0 }, "B-owned", "C-public", "D-owned");
                        await Check(new { Field = "IsActive", Operator = "Equal", Value = true }, "B-owned", "C-public", "D-owned");
                        await Check(new { MetadataPath = new[] { "enabled" }, Scalar = "Boolean", Operator = "Equal", Value = true },
                            "B-owned", "C-public", "D-owned");
                        await Check(new { MetadataPath = new[] { "guid" }, Scalar = "Guid", Operator = "Equal", Value = ids[2].ToString("D").ToUpperInvariant() },
                            "B-owned");
                        await Check(new { MetadataPath = new[] { "mixedGuid" }, Scalar = "Guid", Operator = "NotEqual", Value = ids[2] }, "C-public");
                        await Check(new { MetadataPath = new[] { "mixedInteger" }, Scalar = "Integer", Operator = "Equal", Value = 7 },
                            "B-owned", "C-public");
                        await Check(new { MetadataPath = new[] { "precision" }, Scalar = "Integer", Operator = "Equal", Value = 9007199254740993L },
                            "B-owned", "C-public", "D-owned");
                        await Check(new { MetadataPath = new[] { "precision" }, Scalar = "Integer", Operator = "NotEqual", Value = 9007199254740992L },
                            "B-owned", "C-public", "D-owned");
                        await Check(new { Field = "Name", Operator = "Contains", Value = "owned" }, "B-owned", "D-owned");
                        await Check(new { Field = "Name", Operator = "StartsWith", Value = "B-" }, "B-owned");
                        await Check(new { Field = "Name", Operator = "EndsWith", Value = "-owned" }, "B-owned", "D-owned");
                        await Check(new { MetadataPath = new[] { "nullable" }, Operator = "Exists" }, "B-owned", "C-public", "D-owned");
                        await Check(new { MetadataPath = new[] { "nullable" }, Operator = "IsNull" }, "B-owned", "C-public", "D-owned");
                        await Check(new { MetadataPath = new[] { "missing" }, Operator = "Exists" });
                        await Check(new { MetadataPath = new[] { "missing" }, Operator = "IsNull" });
                        CollectionAssert.AreEqual(new[] { "D-owned", "C-public", "B-owned" }, await Read(new
                        { Provider = provider.ToString(), Filters = new[] { fixture }, Descending = true, Take = 3, IncludeMetadata = true }, alice));
                        var byId = ids.Select((id, index) => new { id, index }).Where(row => row.index is 2 or 3 or 4)
                            .OrderBy(row => row.id).Select(row => new[] { "A-private", "A-index", "B-owned", "C-public", "D-owned" }[row.index]).ToArray();
                        foreach (var orderBy in new[] { "Id", "Version" })
                            CollectionAssert.AreEqual(byId, await Read(new
                            { Provider = provider.ToString(), Filters = new[] { fixture }, OrderBy = orderBy, Take = 3 }, alice));

                        // The same signed HTTP route must count beyond both the row
                        // default25 and maximum100, without allocating a new environment.
                        foreach (var id in scalarIds)
                        {
                            await Journal("before-owned-scalar-seed");
                            var seeded = await providers[provider].SaveHolonAsync(new Holon
                            {
                                Id = id, Name = "Z-owned scalar " + id.ToString("D"), CreatedByAvatarId = alice,
                                MetaData = new() { ["scalarFixture"] = tag }
                            }, saveChildren: false);
                            Assert.IsFalse(seeded.IsError, seeded.Message + seeded.Exception);
                            scalarWrites.Add(provider + ":" + id.ToString("D"));
                        }
                        await Journal("owned-scalar-seed-acknowledged");
                        await Scalar(new { Provider = provider.ToString(), Operation = "LongCount",
                            Filters = new[] { new { MetadataPath = new[] { "scalarFixture" }, Operator = "Equal", Scalar = "String", Value = tag } } },
                            alice, "Int64", "137", verifyReplay: true);
                    }

                    CollectionAssert.AreEqual(new[] { "B-owned", "C-public", "D-owned" }, await Read(new { Take = 3 }, alice),
                        "Omitted provider resolves the server's active current default.");

                    var beforeOversize = authority.Started.Count;
                    using (var oversized = await Send(new { Provider = new string('x', 40000) }, alice, expectContinue:true))
                        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, oversized.StatusCode);
                    Assert.AreEqual(beforeOversize, authority.Started.Count, "Request budget rejects before reservation.");

                    var beforeInvalid = authority.Started.Count;
                    using (var invalid = await Send(new { actorId = bob }, alice))
                        Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
                    Assert.AreEqual(beforeInvalid + 1, authority.Started.Count);
                    Assert.AreEqual(authority.Started.Count, authority.Settlements.Count, "All initiated operations settled durably.");
                    var nativeQueryAttempts = 0;
                    foreach (var operationId in authority.Started.Keys)
                    {
                        var operation = await outbox.GetAsync(operationId, CancellationToken.None);
                        Assert.AreEqual("settled", operation.State);
                        nativeQueryAttempts += operation.ProviderAttempts.Count;
                        if (operation.Receipts.Count == 0)
                        {
                            Assert.AreEqual(0, operation.ProviderAttempts.Count);
                            Assert.IsFalse(string.IsNullOrEmpty(operation.NoProviderExecutionReason));
                            Assert.AreEqual(0m, authority.Settlements[operationId].ActualCostUsd);
                        }
                    }
                    await File.WriteAllTextAsync(Path.Combine(work, "cql-v1-web4-native-evidence.json"),
                        JsonSerializer.Serialize(new { providers = providers.Keys.Select(p => p.ToString()),
                            settledOperations = authority.Settlements.Count,
                            nativeDatabaseQueryAttempts = nativeQueryAttempts, ids, tag,
                            actorVisibilityBeforePaging = true, signedJwt = true, duplicateDispatchPrevented = true }));

                    if (demo != null)
                    {
                        var demoWork = Path.GetFullPath(Path.Combine(work, "../CosmicCqlDemo"));
                        Directory.CreateDirectory(demoWork);

                        // Verify the actual generated schema, not a second mock contract.
                        using (var swagger = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json")))
                        {
                            var paths = swagger.RootElement.GetProperty("paths");
                            Assert.IsTrue(paths.TryGetProperty("/api/data/query-cql", out _));
                            Assert.IsTrue(paths.TryGetProperty("/api/data/query-cql/scalar", out _));
                            Assert.IsTrue(paths.TryGetProperty("/api/data/query-cql/projection", out _));
                            Assert.IsTrue(paths.TryGetProperty("/api/data/query-cql/join", out _));
                            Assert.IsTrue(paths.TryGetProperty("/api/data/query-cql/composition", out _));
                            var compositionExample = swagger.RootElement.GetProperty("components").GetProperty("schemas")
                                .GetProperty("CqlCompositionV1Request").GetProperty("example");
                            Assert.AreEqual("Where", compositionExample.GetProperty("stages")[0].GetProperty("operation").GetString());
                            Assert.AreEqual("Int64", compositionExample.GetProperty("source").GetProperty("columns")[1].GetProperty("valueType").GetString());
                            var joinExample = swagger.RootElement.GetProperty("components").GetProperty("schemas")
                                .GetProperty("CqlJoinV1Request").GetProperty("example");
                            Assert.AreEqual("Left", joinExample.GetProperty("kind").GetString());
                            Assert.AreEqual("C-public", joinExample.GetProperty("right").GetProperty("filters")[1].GetProperty("value").GetString());
                            Assert.AreEqual("RightPresence", joinExample.GetProperty("columns")[2].GetProperty("source").GetString());
                            // Swagger's unsigned presets must keep input bounds as strings.
                            var unsignedProjection = paths.GetProperty("/api/data/query-cql/projection").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                                .GetProperty("examples").GetProperty("ExactUInt64").GetProperty("value");
                            Assert.AreEqual("UInt64", unsignedProjection.GetProperty("columns")[1].GetProperty("valueType").GetString());
                            Assert.AreEqual("18446744073709551612", unsignedProjection.GetProperty("filters")[1].GetProperty("value").GetString());
                            var unsignedGroups = paths.GetProperty("/api/data/query-cql/grouped").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                                .GetProperty("examples").GetProperty("ExactUInt64").GetProperty("value");
                            Assert.AreEqual("UInt64", unsignedGroups.GetProperty("keys")[0].GetProperty("valueType").GetString());
                            Assert.AreEqual("18446744073709551614", unsignedGroups.GetProperty("resultFilters")[1].GetProperty("value").GetString());
                            // Numeric unsigned examples use the shared scalar/grouped binder.
                            var unsignedScalarExamples = paths.GetProperty("/api/data/query-cql/scalar").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("examples");
                            var unsignedAverage = unsignedScalarExamples.GetProperty("UInt64Average").GetProperty("value");
                            Assert.AreEqual("Average", unsignedAverage.GetProperty("operation").GetString());
                            Assert.AreEqual("UInt64", unsignedAverage.GetProperty("selection").GetProperty("valueType").GetString());
                            Assert.AreEqual(2, unsignedAverage.GetProperty("take").GetInt32());
                            Assert.AreEqual("Sum", unsignedScalarExamples.GetProperty("UInt64Sum").GetProperty("value").GetProperty("operation").GetString());
                            var unsignedMeanGroups = paths.GetProperty("/api/data/query-cql/grouped").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                                .GetProperty("examples").GetProperty("UInt64Average").GetProperty("value");
                            Assert.AreEqual("UInt64", unsignedMeanGroups.GetProperty("columns")[0].GetProperty("selection").GetProperty("valueType").GetString());
                            Assert.AreEqual(1, unsignedMeanGroups.GetProperty("resultTake").GetInt32());

                            // Arithmetic examples share the exact Decimal input binder.
                            var decimalSum = unsignedScalarExamples.GetProperty("DecimalSum").GetProperty("value");
                            Assert.AreEqual("Sum", decimalSum.GetProperty("operation").GetString());
                            Assert.AreEqual("Decimal", decimalSum.GetProperty("selection").GetProperty("valueType").GetString());
                            Assert.AreEqual(2, decimalSum.GetProperty("take").GetInt32());
                            Assert.AreEqual("Average", unsignedScalarExamples.GetProperty("DecimalAverage")
                                .GetProperty("value").GetProperty("operation").GetString());
                            var decimalTotals = paths.GetProperty("/api/data/query-cql/grouped").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                                .GetProperty("examples").GetProperty("DecimalTotalsAndMeans").GetProperty("value");
                            Assert.AreEqual("Decimal", decimalTotals.GetProperty("columns")[0].GetProperty("selection").GetProperty("valueType").GetString());
                            Assert.AreEqual("2.0000000000000000000000000005", decimalTotals.GetProperty("resultFilters")[0].GetProperty("value").GetString());
                            Assert.AreEqual("1.0000000000000000000000000002", decimalTotals.GetProperty("resultFilters")[1].GetProperty("value").GetString());

                            // Generated Swagger must preserve the same domains/nullability
                            // and canonical thresholds as walkthrough examples 41–46.
                            var groupedExamples = paths.GetProperty("/api/data/query-cql/grouped").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                                .GetProperty("examples");

                            foreach (var domain in new[] { "Single", "Double" })
                            {
                                var sum = unsignedScalarExamples.GetProperty(domain + "Sum").GetProperty("value");
                                var mean = unsignedScalarExamples.GetProperty(domain + "Average").GetProperty("value");
                                var totals = groupedExamples.GetProperty(domain + "TotalsAndMeans").GetProperty("value");

                                Assert.AreEqual("Sum", sum.GetProperty("operation").GetString());
                                Assert.AreEqual("Average", mean.GetProperty("operation").GetString());
                                Assert.AreEqual(domain, sum.GetProperty("selection").GetProperty("valueType").GetString());
                                Assert.AreEqual(domain, mean.GetProperty("selection").GetProperty("valueType").GetString());
                                Assert.IsFalse(sum.GetProperty("selection").GetProperty("nullable").GetBoolean());
                                Assert.IsTrue(mean.GetProperty("selection").GetProperty("nullable").GetBoolean());
                                Assert.AreEqual(2, sum.GetProperty("take").GetInt32());
                                Assert.AreEqual(2, mean.GetProperty("take").GetInt32());
                                Assert.AreEqual(domain, totals.GetProperty("columns")[0].GetProperty("selection").GetProperty("valueType").GetString());
                                Assert.IsFalse(totals.GetProperty("columns")[0].GetProperty("selection").GetProperty("nullable").GetBoolean());
                                Assert.IsTrue(totals.GetProperty("columns")[1].GetProperty("selection").GetProperty("nullable").GetBoolean());
                                Assert.AreEqual(domain == "Single" ? "2.0000005" : "2.000000000000001",
                                    totals.GetProperty("resultFilters")[0].GetProperty("value").GetString());
                                Assert.AreEqual(domain == "Single" ? "1.0000002" : "1.0000000000000004",
                                    totals.GetProperty("resultFilters")[1].GetProperty("value").GetString());
                                Assert.AreEqual("Mean", totals.GetProperty("resultOrder")[0].GetProperty("alias").GetString());
                                Assert.AreEqual(1, totals.GetProperty("resultTake").GetInt32());
                            }

                            // Swagger's decimal presets must keep input bounds as strings.
                            var decimalProjection = paths.GetProperty("/api/data/query-cql/projection").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                                .GetProperty("examples").GetProperty("ExactDecimal").GetProperty("value");
                            Assert.AreEqual("Decimal", decimalProjection.GetProperty("columns")[1].GetProperty("valueType").GetString());
                            Assert.AreEqual("1.0000000000000000000000000002", decimalProjection.GetProperty("filters")[1].GetProperty("value").GetString());
                            var decimalGroups = paths.GetProperty("/api/data/query-cql/grouped").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                                .GetProperty("examples").GetProperty("ExactDecimal").GetProperty("value");
                            Assert.AreEqual("Decimal", decimalGroups.GetProperty("keys")[0].GetProperty("valueType").GetString());
                            Assert.AreEqual("1.0000000000000000000000000004", decimalGroups.GetProperty("resultFilters")[1].GetProperty("value").GetString());
                            // Swagger's double presets must keep input bounds as strings.
                            var doubleProjection = paths.GetProperty("/api/data/query-cql/projection").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                                .GetProperty("examples").GetProperty("FiniteDouble").GetProperty("value");
                            Assert.AreEqual("Double", doubleProjection.GetProperty("columns")[1].GetProperty("valueType").GetString());
                            Assert.AreEqual("1.0000000000000004", doubleProjection.GetProperty("filters")[1].GetProperty("value").GetString());
                            var doubleGroups = paths.GetProperty("/api/data/query-cql/grouped").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                                .GetProperty("examples").GetProperty("FiniteDouble").GetProperty("value");
                            Assert.AreEqual("Double", doubleGroups.GetProperty("keys")[0].GetProperty("valueType").GetString());
                            Assert.AreEqual("1.0000000000000009", doubleGroups.GetProperty("resultFilters")[1].GetProperty("value").GetString());
                            var singleProjection = paths.GetProperty("/api/data/query-cql/projection").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                                .GetProperty("examples").GetProperty("FiniteSingle").GetProperty("value");
                            Assert.AreEqual("Single", singleProjection.GetProperty("columns")[1].GetProperty("valueType").GetString());
                            Assert.AreEqual("1.0000002", singleProjection.GetProperty("filters")[1].GetProperty("value").GetString());
                            var singleGroups = paths.GetProperty("/api/data/query-cql/grouped").GetProperty("post")
                                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                                .GetProperty("examples").GetProperty("FiniteSingle").GetProperty("value");
                            Assert.AreEqual("Single", singleGroups.GetProperty("keys")[0].GetProperty("valueType").GetString());
                            Assert.AreEqual("1.0000005", singleGroups.GetProperty("resultFilters")[1].GetProperty("value").GetString());
                            var example = swagger.RootElement.GetProperty("components").GetProperty("schemas")
                                .GetProperty("CqlScalarV1Request").GetProperty("example");
                            Assert.AreEqual("Average", example.GetProperty("operation").GetString());
                            Assert.AreEqual("Int64", example.GetProperty("selection").GetProperty("valueType").GetString());
                            Assert.AreEqual("extremum", example.GetProperty("selection").GetProperty("metadataPath")[0].GetString());
                            Assert.AreEqual(2, example.GetProperty("take").GetInt32(), "Average demo intentionally selects an ordered two-row window.");
                            var projectionExample = swagger.RootElement.GetProperty("components").GetProperty("schemas")
                                .GetProperty("CqlProjectionV1Request").GetProperty("example");
                            Assert.AreEqual("Int64", projectionExample.GetProperty("columns")[1].GetProperty("valueType").GetString());
                            Assert.AreEqual("DateTime", projectionExample.GetProperty("columns")[2].GetProperty("valueType").GetString());
                            Assert.AreEqual("DateTime", projectionExample.GetProperty("filters")[1].GetProperty("scalar").GetString());
                            var groupedExample = swagger.RootElement.GetProperty("components").GetProperty("schemas")
                                .GetProperty("CqlGroupedV1Request").GetProperty("example");
                            Assert.AreEqual("ObservedAt", groupedExample.GetProperty("resultOrder")[0].GetProperty("alias").GetString());
                            Assert.IsFalse(groupedExample.GetProperty("resultOrder")[0].GetProperty("descending").GetBoolean());
                            Assert.AreEqual(2, groupedExample.GetProperty("resultTake").GetInt32());
                            Assert.AreEqual("ObservedAt", groupedExample.GetProperty("resultFilters")[0].GetProperty("alias").GetString());
                            Assert.AreEqual("GreaterThanOrEqual", groupedExample.GetProperty("resultFilters")[0].GetProperty("operator").GetString());
                            Assert.AreEqual("2026-10-24T00:00:00.0000002Z", groupedExample.GetProperty("resultFilters")[0].GetProperty("value").GetString());
                            Assert.AreEqual("LessThan", groupedExample.GetProperty("resultFilters")[1].GetProperty("operator").GetString());
                            var federationExample = swagger.RootElement.GetProperty("components").GetProperty("schemas")
                                .GetProperty("CqlFederationV1Request").GetProperty("example");
                            Assert.AreEqual("Named", federationExample.GetProperty("scope").GetString());
                            Assert.AreEqual(3, federationExample.GetProperty("providers").GetArrayLength());
                            Assert.AreEqual("ProviderPriority", federationExample.GetProperty("identityMode").GetString());
                            Assert.AreEqual(2, federationExample.GetProperty("take").GetInt32());
                            Assert.IsFalse(federationExample.GetProperty("source").TryGetProperty("provider", out _));
                        }

                        // These examples are the same readable C# client code developers
                        // can copy. They execute against this actual three-provider host.
                        await using (var transcript = new StreamWriter(Path.Combine(demoWork, "v1-walkthrough.txt")))
                        {
                            foreach (var provider in providers.Values)
                                localSdkQueries += await V1NativeQueries.RunAsync(new Sources(providers).CreateManager(provider),
                                    (NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicQueryStorageProvider)provider, alice, tag, transcript);
                            walkthroughRequests = await V1Walkthrough.RunAsync(client, demo.Tokens["Alice"], tag, transcript);
                            sdkFederationExamples = await V1FederationWalkthrough.RunSdkAsync(
                                new Sources(providers).CreateManager(providers.Values.First()),
                                providers.Values.Cast<ICosmicQueryStorageProvider>().ToArray(), alice, tag, transcript);
                            var federationOperationId = await V1FederationWalkthrough.RunHttpAsync(client, demo.Tokens["Alice"], tag, transcript);
                            var federationOperation = await outbox.GetAsync(federationOperationId, CancellationToken.None);
                            Assert.AreEqual("settled", federationOperation.State);
                            Assert.AreEqual(3, federationOperation.ProviderAttempts.Count);
                            Assert.AreEqual(3, federationOperation.Receipts.Count);
                            Assert.AreEqual(0.006m, authority.Settlements[federationOperationId].ActualCostUsd);
                            demoFederationRequests = 1;
                        }

                        if (mode == "serve")
                        {
                            demo.Ready = true;
                            await File.WriteAllTextAsync(Path.Combine(demoWork, "v1-session.json"),
                                JsonSerializer.Serialize(new { url = address + "/demo", swagger = address + "/swagger" }));
                            await demo.Finished.Task;
                        }
                    }
                }
                finally { await app.StopAsync(); }
            }
            // Exercise signed requests through the shipping DataController only
            // after the read demo, so these private records cannot change it.
            var recoveryProvidersVerified = await VerifyConditionalRecoveryAsync(providers, transportActor, recoveryIds,
                tag, sqlite, Journal, sharingOperations);
            Assert.AreEqual(3, recoveryProvidersVerified);
            var shippingRestRequests = await VerifyShippingRestAsync(providers, outbox, authority,
                secret, restActor, restIds, ids[0], tag, Journal, sharingOperations);
            Assert.AreEqual(78, shippingRestRequests);
            var shippingTransportRequests = await VerifyShippingTransportsAsync(providers, outbox, authority,
                secret, transportActor, transportIds, ids[0], tag, Journal, sharingOperations);
            Assert.AreEqual(48, shippingTransportRequests);
            if (mode != null)
            {
                // Shutdown waits for in-flight requests. Never erase an unsettled
                // interactive request merely because the presenter clicked Finish.
                var operations = await mongo.GetDatabase(usageDb)
                    .GetCollection<UsageOutboxOperation>("subscription_usage_outbox_v1")
                    .Find(Builders<UsageOutboxOperation>.Filter.Empty).ToListAsync();
                Assert.IsTrue(operations.All(operation => operation.State == "settled"), "Preserve any uncertain demo operation.");
                var demoWork = Path.GetFullPath(Path.Combine(work, "../CosmicCqlDemo"));
                await File.WriteAllTextAsync(Path.Combine(demoWork, "v1-demo-result.json"), JsonSerializer.Serialize(new
                {
                    status = "passed", providers = providers.Keys.Select(provider => provider.ToString()),
                    nativeFederationQueries, nativeFederationProvidersVerified = providers.Count,
                    sdkFederationExamples, demoFederationRequests,
                    walkthroughRequests, localSdkQueries, sharingProvidersVerified, ownedWritesProvidersVerified, ownedBatchProvidersVerified, hostedWriteBoundariesVerified,
                    recoveryProvidersVerified, processRestartProvidersVerified = recoveryProvidersVerified,
                    hostedReadPolicyRequests, hostedReadPolicyProvidersVerified = providers.Count,
                    hostedFederationRequests = hostedFederation.Requests,
                    settledFederationOperations = hostedFederation.Settled,
                    federationProviderAttempts = hostedFederation.Attempts,
                    hostedFederationProvidersVerified = providers.Count,
                    hostedFederationEncryptedRecordId = federationEncryptedId.ToString("D"),
                    hostedCursorRequests = hostedCursors.Requests,
                    settledCursorOperations = hostedCursors.Settled,
                    cursorProviderAttempts = hostedCursors.Attempts,
                    cursorDuplicateRejections = hostedCursors.Duplicates,
                    cursorProvidersVerified = providers.Count,
                    cursorHostRestarts = 2, cursorKeyRotations = 1, cursorKeyRevocations = 1,
                    shippingRestRequests, shippingRestProvidersVerified = providers.Count,
                    shippingTransportRequests, shippingTransportProvidersVerified = providers.Count,
                    settledRestOperations = operations.Count(operation => operation.UserId == restActor.ToString("D") && operation.Endpoint != "POST " + CosmicOperationRecoveryRoutes.Recover),
                    settledTransportOperations = operations.Count(operation => operation.UserId == transportActor.ToString("D")),
                    settledCqlOperations = operations.Count(operation => operation.UserId != restActor.ToString("D") && operation.UserId != transportActor.ToString("D") && operation.Endpoint != "POST " + CosmicOperationRecoveryRoutes.Recover),
                    settledRecoveryOperations = operations.Count(operation => operation.Endpoint == "POST " + CosmicOperationRecoveryRoutes.Recover),
                    settledOperations = operations.Count,
                    providerAttempts = operations.Sum(operation => operation.ProviderAttempts.Count)
                }));
                File.Delete(Path.Combine(demoWork, "v1-session.json"));
            }
            complete = true;
        }
        finally
        {
            foreach (var provider in activated.AsEnumerable().Reverse())
            {
                var deactivated = await provider.DeActivateProviderAsync();
                Assert.IsFalse(deactivated.IsError, deactivated.Message);
            }
            if (complete)
            {
                // These exact IDs/databases were absent at preflight and journalled before creation.
                var ownedIds = ids.Concat(childIds).Append(federationEncryptedId).Concat(scalarIds).Concat(restIds).Concat(transportIds).Concat(recoveryIds).Select(id => id.ToString("D")).ToArray();
                await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = ownedIds })).ConsumeAsync();
                await (await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v", new { ids = ownedIds })).ConsumeAsync();
                await (await inspection.RunAsync("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $operations DETACH DELETE r",
                    new { operations = sharingOperations.ToArray() })).ConsumeAsync();
                await mongo.DropDatabaseAsync(storageDb);
                await mongo.DropDatabaseAsync(usageDb);
                // The runner removes the exact SQLite files only after this test process exits.
                await Journal("successful-awaiting-runner-cleanup");
            }
        }
    }
}

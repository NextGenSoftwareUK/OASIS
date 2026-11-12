using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.UrbitOASIS;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private sealed class UrbitNativeFederatedSources(MongoProvider mongo, UrbitContactBookQuerySource urbit) : ICqlV1Providers
    {
        public IOASISStorageProvider Resolve(ProviderType? requested)
        {
            var source = requested is null or ProviderType.MongoDBOASIS ? (IOASISStorageProvider)mongo
                : requested == ProviderType.UrbitOASIS ? urbit : throw new InvalidOperationException("Explicit source required.");
            if (!source.IsProviderActivated) throw new InvalidOperationException("Every source must be active.");
            return source;
        }
        public HolonManager CreateManager(IOASISStorageProvider provider)
        {
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
            return new HolonManager(provider, dna, new ProviderManager(null, dna) {
                IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false
            });
        }
    }

    [UrbitNativeWeb4Fact]
    public async Task UrbitNativeWeb4FederationReadsOwnedMongoCopies()
    {
        using var session = await OpenUrbitNativeSessionAsync(includeMongo: true);
        var client = new MongoClient(session.MongoUri!);
        var database = client.GetDatabase(session.Database!);
        var outDir = Path.Combine(session.Root, "TestResults/UrbitContactsInstallCurrent");
        var journal = Path.Combine(outDir, "mongo-db-intent.json");
        var nativeIds = UrbitCids.Select(cid => UrbitContactBookQuerySource.LogicalId(session.Owner, UrbitConnection, cid)).ToArray();
        Guid[] ownedIds;
        using (var names = await client.ListDatabaseNamesAsync())
        {
            var exists = (await names.ToListAsync()).Contains(session.Database);
            Assert.Equal(session.Phase == "afterRestart", exists);
        }
        if (session.Phase == "beforeRestart")
        {
            ownedIds = new[] { nativeIds[0], nativeIds[2], Guid.NewGuid() };
            Assert.DoesNotContain(ownedIds[2], nativeIds);
            await using var intent = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(intent, new {
                owner = session.ResourceOwner, database = session.Database, sourceOwner = session.Owner, ids = ownedIds
            });
        }
        else
        {
            using var intent = JsonDocument.Parse(await File.ReadAllBytesAsync(journal));
            Assert.Equal(session.ResourceOwner, intent.RootElement.GetProperty("owner").GetString());
            Assert.Equal(session.Database, intent.RootElement.GetProperty("database").GetString());
            Assert.Equal(session.Owner, intent.RootElement.GetProperty("sourceOwner").GetGuid());
            ownedIds = intent.RootElement.GetProperty("ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
            Assert.Equal(3, ownedIds.Length);
            Assert.Equal(new[] { nativeIds[0], nativeIds[2] }, ownedIds.Take(2));
            Assert.DoesNotContain(ownedIds[2], nativeIds);
            var marker = await database.GetCollection<BsonDocument>("CosmicUrbitQualificationOwner")
                .Find(new BsonDocument("_id", "owner")).SingleAsync();
            Assert.Equal(session.ResourceOwner, marker["owner"].AsString);
            Assert.Equal(session.Owner.ToString("D"), marker["sourceOwner"].AsString);
        }
        var mongo = new MongoProvider(session.MongoUri!, session.Database!) { IsVersionControlEnabled = false };
        try
        {
            var activation = await mongo.ActivateProviderAsync();
            Assert.False(activation.IsError, activation.Message);
            if (session.Phase == "beforeRestart")
            {
                await database.GetCollection<BsonDocument>("CosmicUrbitQualificationOwner").InsertOneAsync(new BsonDocument {
                    { "_id", "owner" }, { "owner", session.ResourceOwner }, { "sourceOwner", session.Owner.ToString("D") }
                });
                foreach (var pair in ownedIds.Select((id, index) => (id, index)))
                {
                    var saved = await mongo.SaveHolonAsync(new Holon { Id = pair.id, CreatedByAvatarId = session.Owner,
                        IsPublic = false, Name = "Owned Urbit canonical copy " + pair.index }, saveChildren: false);
                    Assert.False(saved.IsError, saved.Message);
                }
            }
            var holons = database.GetCollection<BsonDocument>("Holon");
            Assert.Equal(3, await holons.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            async Task<string> Snapshot()
            {
                var documents = await holons.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                foreach (var document in documents.OrderBy(document => document["_id"].ToString(), StringComparer.Ordinal))
                    hash.AppendData(document.ToBson());
                return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
            }
            var before = await Snapshot();
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("profile", 2));
            var profile = database.GetCollection<BsonDocument>("system.profile");
            var reads = new BsonDocument("$or", new BsonArray {
                new BsonDocument("command.find", "Holon"), new BsonDocument("command.aggregate", "Holon"),
                new BsonDocument { { "ns", session.Database + ".Holon" }, { "op", new BsonDocument("$in", new BsonArray { "query", "getmore" }) } }
            });
            var sources = new UrbitNativeFederatedSources(mongo, session.Adapter);
            var completed = Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true });
            var foreign = Guid.Parse("372a7b25-32cf-4316-88bc-ea1b56a24b37");
            await using var app = BuildUrbitMvcFixture(sources, session.Owner, foreign, completed, UrbitFederationProviders);
            await app.StartAsync();
            string? apiHash = null;
            try
            {
                var address = Assert.Single(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses);
                using var http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
                http.DefaultRequestHeaders.Authorization = new("Bearer", "urbit-owned-fixture");
                async Task<int> Receipts()
                {
                    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    return await completed.Reader.ReadAsync(deadline.Token);
                }
                foreach (var selected in new[] { nativeIds, new[] { nativeIds[0], nativeIds[2] } })
                {
                    using var response = await http.PostAsJsonAsync("/api/data/query-cql/federation", UrbitFederationRequest(selected));
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(2, await Receipts());
                    using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    var output = body.RootElement.GetProperty("result");
                    Assert.Equal(selected.Length + 2, output.GetProperty("observedInputRows").GetInt32());
                    var rows = output.GetProperty("rows").EnumerateArray().Select(row => row.GetProperty("selected")).ToArray();
                    Assert.Equal(selected.Length + 2, rows.Length);
                    Assert.Equal(selected.Length, rows.Count(row => row.GetProperty("provider").GetString() == "UrbitOASIS"));
                    Assert.Equal(2, rows.Count(row => row.GetProperty("provider").GetString() == "MongoDBOASIS"));
                    Assert.DoesNotContain(rows, row => row.GetProperty("row").GetProperty("id").GetGuid() == ownedIds[2]);
                    Assert.All(rows, row => {
                        Assert.False(row.GetProperty("row").GetProperty("isPublic").GetBoolean());
                        Assert.Equal(session.Owner, row.GetProperty("row").GetProperty("createdByAvatarId").GetGuid());
                    });
                    if (ReferenceEquals(selected, nativeIds))
                    {
                        var projected = rows.OrderBy(row => row.GetProperty("provider").GetString(), StringComparer.Ordinal)
                            .ThenBy(row => row.GetProperty("row").GetProperty("id").GetGuid()).Select(row => new {
                                Provider = row.GetProperty("provider").GetString(), Id = row.GetProperty("row").GetProperty("id").GetGuid(),
                                Owner = row.GetProperty("row").GetProperty("createdByAvatarId").GetGuid(),
                                IsPublic = row.GetProperty("row").GetProperty("isPublic").GetBoolean(),
                                Name = row.GetProperty("row").GetProperty("name").GetString()
                            }).ToArray();
                        apiHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(projected))).ToLowerInvariant();
                    }
                }
                Assert.Equal(6, session.Observed.Calls);
                var readCount = await profile.CountDocumentsAsync(reads);
                Assert.True(readCount >= 2, "Native canonical queries must be visible in the owned profiler.");
                http.DefaultRequestHeaders.Authorization = new("Bearer", "urbit-foreign-fixture");
                using var denied = await http.PostAsJsonAsync("/api/data/query-cql/federation", UrbitFederationRequest(nativeIds));
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
                Assert.Equal(0, await Receipts());
                Assert.Equal(readCount, await profile.CountDocumentsAsync(reads));
                Assert.Equal(6, session.Observed.Calls);
                http.DefaultRequestHeaders.Authorization = new("Bearer", "urbit-owned-fixture");
                Assert.False((await session.Adapter.DeActivateProviderAsync()).IsError);
                using var inactive = await http.PostAsJsonAsync("/api/data/query-cql/federation", UrbitFederationRequest(nativeIds));
                Assert.Equal(HttpStatusCode.ServiceUnavailable, inactive.StatusCode);
                Assert.Equal(0, await Receipts());
                Assert.DoesNotContain("\"rows\"", await inactive.Content.ReadAsStringAsync());
                Assert.Equal(readCount, await profile.CountDocumentsAsync(reads));
                Assert.Equal(6, session.Observed.Calls);
            }
            finally { await app.StopAsync(); }
            Assert.Equal(before, await Snapshot());
            Assert.NotNull(apiHash);
            if (session.Phase == "afterRestart")
            {
                await client.DropDatabaseAsync(session.Database!);
                using var names = await client.ListDatabaseNamesAsync();
                Assert.DoesNotContain(session.Database, await names.ToListAsync());
            }
            var proof = new {
                phase = session.Phase, nativeFederationQualified = true, fullCount = 5, subsetCount = 4,
                nativePointReadCount = session.Observed.Calls, unrelatedCanonicalCopyExcluded = true,
                rejectedRequestsReadNeitherSource = true, sourceDocumentsPreserved = true,
                canonicalSourceSha256 = before, federationProjectionSha256 = apiHash,
                copySeedCount = session.Phase == "beforeRestart" ? 3 : 0,
                ownedDatabaseAbsent = session.Phase == "afterRestart", controlledIdentityAndOutbox = true,
                productionJwtQualified = false, durableBillingQualified = false, cqlQualified = false, liveDeployment = false
            };
            await using var result = new FileStream(Path.Combine(outDir, session.Phase + "-federation.json"),
                FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(result, proof);
        }
        finally { await mongo.DeActivateProviderAsync(); }
    }
}

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
        var scalarIds = Enumerable.Range(0,137).Select(_=>Guid.NewGuid()).ToArray();
        var scalarWrites = new List<string>();
        var journal = new List<object>();
        using var journalGate = new SemaphoreSlim(1);
        async Task Journal(string stage, object? request = null)
        {
            await journalGate.WaitAsync();
            try
            {
                if (request != null) journal.Add(request);
                await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new
                { stage, sqlite, storageDb, usageDb, alice, bob, outsider, ids=ids.Concat(childIds).ToArray(), scalarIds, scalarWrites, tag, requests = journal }));
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
                        // This example promises persisted role order, not the GUID
                        // order used by the older parent-link-only representation.
                        hidden.ChildRoleManifestJson=NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Serialize(
                            new HolonChildRoleManifest(1,hidden.Id,new[]{new HolonChildRole("Children",new[]{childIds[3]})}));
                        holon.ChildRoleManifestJson=NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Serialize(
                            new HolonChildRoleManifest(1,holon.Id,new[]{new HolonChildRole("Children",childIds.Take(3).ToArray())}));
                    }
                    var seeded = await provider.SaveHolonAsync(holon, saveChildren: i==3);
                    Assert.IsFalse(seeded.IsError, seeded.Message + seeded.Exception);
                }
            }

            var outbox = new MongoUsageOutboxStore(mongoUri, usageDb);
            await outbox.InitializeAsync(CancellationToken.None);
            var authority = new Authority();
            var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
            var mode = Environment.GetEnvironmentVariable("OASIS_CQL_V1_DEMO_MODE");
            Assert.IsTrue(mode == null || mode is "console" or "serve", "Explicit demo mode required.");
            foreach (var actor in new[] { alice, bob, outsider })
                authority.Tokens.Add(Token(actor, key, lifetimeMinutes: mode == null ? 5 : 60), actor);
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
            await using (var app = Host(providers, outbox, authority, secret, demo,
                (actor, operation) => Journal("before-http-request", new { actor, operation })))
            {
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(20) };

                    async Task<HttpResponseMessage> Send(object body, Guid actor, string? operationKey = null, string? tokenOverride = null,bool expectContinue = false,bool scalar = false,bool projection = false)
                    {
                        var operation = operationKey ?? Guid.NewGuid().ToString("D");
                        await Journal("before-http-request", new { actor, operation });
                        using var request = new HttpRequestMessage(HttpMethod.Post, projection ? "/api/data/query-cql/projection" : scalar ? "/api/data/query-cql/scalar" : "/api/data/query-cql")
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
                    Assert.AreEqual(0, authority.Started.Count, "Invalid authentication must never reserve or dispatch.");

                    foreach (var provider in providers.Keys)
                    {
                        var fixture = new { MetadataPath = new[] { "fixture" }, Operator = "Equal", Scalar = "String", Value = tag };
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
                            using (var unsupportedDate = await Send(new
                            {
                                Provider = provider.ToString(), Columns = new[] { new { Alias = "when", MetadataPath = new[] { "when" }, ValueType = "DateTime" } }
                            }, alice, projection: true)) Assert.AreEqual(HttpStatusCode.BadRequest, unsupportedDate.StatusCode);

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
                            var example = swagger.RootElement.GetProperty("components").GetProperty("schemas")
                                .GetProperty("CqlScalarV1Request").GetProperty("example");
                            Assert.AreEqual("Average", example.GetProperty("operation").GetString());
                            Assert.AreEqual("Int64", example.GetProperty("selection").GetProperty("valueType").GetString());
                            Assert.AreEqual("extremum", example.GetProperty("selection").GetProperty("metadataPath")[0].GetString());
                            Assert.AreEqual(2, example.GetProperty("take").GetInt32(), "Average demo intentionally selects an ordered two-row window.");
                            var projectionExample = swagger.RootElement.GetProperty("components").GetProperty("schemas")
                                .GetProperty("CqlProjectionV1Request").GetProperty("example");
                            Assert.AreEqual("Int64", projectionExample.GetProperty("columns")[1].GetProperty("valueType").GetString());
                            var groupedExample = swagger.RootElement.GetProperty("components").GetProperty("schemas")
                                .GetProperty("CqlGroupedV1Request").GetProperty("example");
                            Assert.AreEqual("Total", groupedExample.GetProperty("resultOrder")[0].GetProperty("alias").GetString());
                            Assert.IsTrue(groupedExample.GetProperty("resultOrder")[0].GetProperty("descending").GetBoolean());
                            Assert.AreEqual(1, groupedExample.GetProperty("resultTake").GetInt32());
                        }

                        // These examples are the same readable C# client code developers
                        // can copy. They execute against this actual three-provider host.
                        await using (var transcript = new StreamWriter(Path.Combine(demoWork, "v1-walkthrough.txt")))
                        {
                            foreach (var provider in providers.Values)
                                localSdkQueries += await V1NativeQueries.RunAsync(new Sources(providers).CreateManager(provider),
                                    (NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicQueryStorageProvider)provider, alice, tag, transcript);
                            walkthroughRequests = await V1Walkthrough.RunAsync(client, demo.Tokens["Alice"], tag, transcript);
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
                    walkthroughRequests, localSdkQueries, settledOperations = operations.Count,
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
                var ownedIds = ids.Concat(childIds).Concat(scalarIds).Select(id => id.ToString("D")).ToArray();
                await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = ownedIds })).ConsumeAsync();
                await (await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v", new { ids = ownedIds })).ConsumeAsync();
                await mongo.DropDatabaseAsync(storageDb);
                await mongo.DropDatabaseAsync(usageDb);
                // The runner removes the exact SQLite files only after this test process exits.
                await Journal("successful-awaiting-runner-cleanup");
            }
        }
    }
}

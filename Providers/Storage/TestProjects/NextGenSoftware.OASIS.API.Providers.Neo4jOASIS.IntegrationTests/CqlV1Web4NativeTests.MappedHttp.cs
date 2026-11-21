using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private sealed record NativeMappingRegistration(ProviderType Authority, INativeDataProjectionQueryProvider Reader);

    private sealed partial class Sources
    {
        // Discovery is fixture-owned. Definition storage, owner access, mapping decoding,
        // native projection, HTTP/JWT and the usage outbox use the real implementations.
        public async Task<OASISResult<IReadOnlyList<VirtualHolonProjectionRead>>> QueryVirtualMappingAsync(
            Guid mappingId, IAvatar avatar, CqlVirtualProjection projection, CancellationToken token)
        {
            OASISResult<IReadOnlyList<VirtualHolonProjectionRead>> Refuse(string code) => new()
                { IsError = true, ErrorCode = code, Message = "The owned fixture mapping is unavailable." };
            if (!nativeMappings.TryGetValue(mappingId, out var registration))
                return Refuse("VIRTUAL_MAPPING_NOT_FOUND");
            var storage = nativeProviders[registration.Authority];
            var adapter = (ICosmicQueryStorageProvider)storage;
            var query = CqlV1Compiler.Compile(new CqlV1Request
            {
                OrderBy = "Id", Take = 1,
                Filters = new[] { new CqlV1Filter { Field = "Id", Scalar = CqlV1Scalar.Guid,
                    Operator = CqlV1Operator.Equal, Value = JsonSerializer.SerializeToElement(mappingId.ToString("D")) } }
            }, avatar.Id, registration.Authority);
            var plan = CosmicQueryPlanner.Plan(query, registration.Authority,
                new[] { new CosmicQueryTarget(registration.Authority, adapter.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var loaded = await CreateManager(storage).ExecuteQueryAsync(plan.Result, new[] { adapter },
                maximumResults: 1, cancellationToken: token);
            Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
            var definition = loaded.Result.Providers.Single().Rows.SingleOrDefault();
            if (definition == null || !definition.IsActive) return Refuse("VIRTUAL_MAPPING_NOT_FOUND");
            var mapping = VirtualHolonMapping.FromDefinitionHolon(avatar.Id, definition,
                registration.Reader.DescribeVirtualQueryConnection(null));
            Assert.AreEqual(registration.Reader.NativeDataProviderType, mapping.Provider);
            return await mapping.QueryProjectionAsync(avatar.Id, registration.Reader, null,
                projection.Matches, projection.Columns, CqlVirtualProjection.MaximumRows, token);
        }
    }

    internal static async Task VerifyMappedNativeHttpAsync(IOASISStorageProvider provider,
        VirtualHolonMapping mapping, VirtualHolonRead original, bool groupedDistinctOnly = false,
        Action<Guid>? createdObserver = null, bool guidDistinct = false,
        Dictionary<string, long>? queryTimings = null, Guid? virtualOnlyId = null, bool guidRows = false, bool predicateCrossing = false, bool guidFilters = false, bool int32Http = false)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var reader = (INativeDataProjectionQueryProvider)provider;
        var type = reader.NativeDataProviderType;
        var client = new MongoClient(uri);
        var usageDb = "cosmic_cql_mapped_http_" + type.ToString().ToLowerInvariant();
        using (var existing = await client.ListDatabaseNamesAsync())
            Assert.IsFalse((await existing.ToListAsync()).Contains(usageDb), "Never reset an existing HTTP evidence database.");
        var definition = mapping.ToDefinitionHolon(); definition.Version = 1; definition.IsActive = true;
        var snapshot = original.PrepareSnapshot(mapping.OwnerAvatarId); snapshot.Version = 1;
        // A legitimate cached copy can retain an old value after the native source changes.
        // Both old and new text pass LENGTH=2; reconciliation must remove the stale group key.
        snapshot.MetaData["textAlias"] = "zz";
        var important = new Holon
        {
            Id = Guid.NewGuid(), Version = 1, Name = "owned important record",
            CreatedByAvatarId = mapping.OwnerAvatarId, IsPublic = false, IsActive = true,
            MetaData = new() { ["textAlias"] = "xy" }
        };
        var longer = new Holon
        {
            Id = Guid.NewGuid(), Version = 1, Name = "owned longer record",
            CreatedByAvatarId = mapping.OwnerAvatarId, IsPublic = false, IsActive = true,
            MetaData = new() { ["textAlias"] = groupedDistinctOnly ? "😀" : "important" }
        };
        if (guidDistinct)
        {
            Assert.IsTrue(groupedDistinctOnly);
            snapshot.MetaData["guidAlias"] = MixedGuidStale;
            important.MetaData["guidAlias"] = MixedGuidSecond;
            longer.MetaData["guidAlias"] = MixedGuidFirst;
        }
        if (int32Http)
        {
            snapshot.MetaData["stockAlias"] = -1;
            important.MetaData["stockAlias"] = 7;
            longer.MetaData["stockAlias"] = 5;
        }
        var created = new List<Guid>();
        var authority = new Authority();
        var outbox = new MongoUsageOutboxStore(uri, usageDb);
        var initialized = false;
        try
        {
            foreach (var row in new IHolon[] { definition, snapshot, important, longer })
            {
                var prior = await provider.LoadHolonAsync(row.Id);
                Assert.IsNull(prior.Result, "Never overwrite an existing fixture record.");
                if (guidDistinct || predicateCrossing)
                {
                    created.Add(row.Id);
                    createdObserver?.Invoke(row.Id);
                }
                var saved = await provider.SaveHolonAsync(row);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                if (!guidDistinct && !predicateCrossing)
                {
                    created.Add(row.Id);
                    createdObserver?.Invoke(row.Id);
                }
            }
            await outbox.InitializeAsync(CancellationToken.None); initialized = true;
            var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
            var other = Guid.NewGuid();
            var ownerToken = Token(mapping.OwnerAvatarId, key);
            var otherToken = Token(other, key);
            authority.Tokens.Add(ownerToken, mapping.OwnerAvatarId); authority.Tokens.Add(otherToken, other);
            var stores = new Dictionary<ProviderType, IOASISStorageProvider> { [type] = provider };
            var registrations = new Dictionary<Guid, NativeMappingRegistration>
                { [mapping.Id] = new(type, reader) };
            await using var app = Host(stores, outbox, authority, secret, nativeMappings: registrations);
            await app.StartAsync();
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(20) };
            var body = new CqlProjectionV1Request
            {
                Provider = type.ToString(), VirtualMappingId = mapping.Id, OrderBy = "Id", Take = 5,
                Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "textAlias" }, Scalar = CqlV1Scalar.String,
                    Function = CqlV1Function.Utf16Length, Operator = CqlV1Operator.Equal,
                    Value = JsonSerializer.SerializeToElement("2") } },
                Columns = new[] {
                    new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
                    new CqlProjectionV1Column { MetadataPath = new[] { "textAlias" }, ValueType = "String", Alias = "Original" },
                    new CqlProjectionV1Column { MetadataPath = new[] { "textAlias" }, ValueType = "String", Alias = "Units",
                        Function = CqlV1Function.Utf16Length, Nullable = true },
                    new CqlProjectionV1Column { MetadataPath = new[] { "textAlias" }, ValueType = "String", Alias = "Again",
                        Function = CqlV1Function.Utf16Length, Nullable = true }
                }
            };
            async Task<HttpResponseMessage> Send(string bearer)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/data/query-cql/projection/mixed")
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
                var watch = queryTimings == null ? null : System.Diagnostics.Stopwatch.StartNew();
                var response = await http.SendAsync(request);
                if (watch != null) queryTimings!.Add("request" + queryTimings.Count, watch.ElapsedMilliseconds);
                return response;
            }
            if (int32Http)
            {
                Assert.IsTrue(virtualOnlyId.HasValue);
                body.Columns = new[] {
                    new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
                    new CqlProjectionV1Column { MetadataPath = new[] { "stockAlias" }, ValueType = "Int32", Nullable = false, Alias = "Stock" }
                };
                body.Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "stockAlias" },
                    Scalar = CqlV1Scalar.Int32, Operator = CqlV1Operator.Equal, Value = JsonSerializer.SerializeToElement(5) } };
                var expected = new Dictionary<Guid,int> { [original.Id]=5, [longer.Id]=5,
                    [virtualOnlyId.Value]=7, [important.Id]=7 };
                async Task<JsonElement> ReadInt32(int attempts = 4)
                {
                    using var response = await Send(ownerToken);
                    var content = await response.Content.ReadAsStringAsync();
                    Assert.AreEqual(HttpStatusCode.OK,response.StatusCode,content);
                    Assert.AreEqual("settled",response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                    var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(),CancellationToken.None);
                    Assert.AreEqual("settled",operation.State);
                    Assert.AreEqual(attempts,operation.ProviderAttempts.Count); Assert.AreEqual(attempts,operation.Receipts.Count);
                    Assert.AreEqual(attempts*0.002m,authority.Settlements[operation.Id].ActualCostUsd);
                    using var parsed = JsonDocument.Parse(content);
                    var result = parsed.RootElement.GetProperty("result");
                    Assert.AreEqual("cql-projection-v1",result.GetProperty("version").GetString());
                    var rows = result.GetProperty("rows").EnumerateArray().ToArray();
                    Assert.AreEqual(rows.Length,result.GetProperty("provenance").GetArrayLength());
                    foreach(var row in rows) {
                        Assert.AreEqual(2,row.GetArrayLength());
                        Assert.IsTrue(expected.ContainsKey(row[0].GetGuid()));
                        Assert.AreEqual(expected[row[0].GetGuid()],row[1].GetInt32());
                    }
                    return result.Clone();
                }
                static Guid[] Keys(JsonElement result) => result.GetProperty("rows").EnumerateArray().Select(row=>row[0].GetGuid()).ToArray();
                var first = await ReadInt32();
                CollectionAssert.AreEquivalent(new[]{original.Id,longer.Id},Keys(first));
                Assert.IsTrue(first.GetProperty("provenance").EnumerateArray().Any(row=>
                    row.GetProperty("representation").GetString()=="Virtual" && row.GetProperty("alsoMaterialized").GetBoolean()));
                body.Filters[0].Operator = CqlV1Operator.NotEqual;
                var otherStock = await ReadInt32();
                CollectionAssert.AreEquivalent(new[]{virtualOnlyId.Value,important.Id},Keys(otherStock));
                body.Filters[0].Operator = CqlV1Operator.Equal;
                body.Filters[0].Value = JsonSerializer.SerializeToElement(-1);
                Assert.AreEqual(0,Keys(await ReadInt32(3)).Length,"Stale cached stock must not satisfy the predicate.");
                body.Filters[0].Operator = CqlV1Operator.GreaterThanOrEqual;
                body.Filters[0].Value = JsonSerializer.SerializeToElement(0);
                body.OrderBy="None"; body.SelectedOrder=new[]{
                    new CqlGroupedV1Ordering{Alias="Stock",Descending=true}, new CqlGroupedV1Ordering{Alias="Id"} };
                body.Skip=1; body.Take=2;
                var page=await ReadInt32();
                CollectionAssert.AreEqual(expected.OrderByDescending(row=>row.Value).ThenBy(row=>row.Key).Skip(1).Take(2).Select(row=>row.Key).ToArray(),Keys(page));
                using(var response=await Send(otherToken)) {
                    Assert.AreEqual(HttpStatusCode.NotFound,response.StatusCode,await response.Content.ReadAsStringAsync());
                    var operation=await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(),CancellationToken.None);
                    Assert.AreEqual("settled",operation.State);
                    Assert.AreEqual(2,operation.ProviderAttempts.Count);Assert.AreEqual(2,operation.Receipts.Count);
                }
                Assert.AreEqual(5,authority.Settlements.Count);
                var preserved=await provider.LoadHolonAsync(snapshot.Id);
                Assert.IsNotNull(preserved.Result); Assert.AreEqual("-1",preserved.Result.MetaData["stockAlias"]?.ToString());
                Assert.IsNull((await provider.LoadHolonAsync(virtualOnlyId.Value)).Result,"Read must not materialize the native-only row.");
                await app.StopAsync();
                return;
            }
            if (predicateCrossing)
            {
                body.Columns = new[] {
                    new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
                    new CqlProjectionV1Column { MetadataPath = new[] { "textAlias" }, ValueType = "String", Alias = "Text" }
                };
                body.Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "textAlias" },
                    Scalar = CqlV1Scalar.String, Operator = CqlV1Operator.Equal,
                    Value = JsonSerializer.SerializeToElement("zz") } };
                async Task<JsonElement> ReadCrossingRows()
                {
                    using var response = await Send(ownerToken);
                    var content = await response.Content.ReadAsStringAsync();
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, content);
                    Assert.AreEqual("settled", response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                    var operation = await outbox.GetAsync(
                        response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                    Assert.AreEqual("settled", operation.State);
                    var attempts = body.Filters[0].Value.GetString() == "zz" ? 3 : 4;
                    Assert.AreEqual(attempts, operation.ProviderAttempts.Count);
                    Assert.AreEqual(attempts, operation.Receipts.Count);
                    Assert.AreEqual(attempts * 0.002m, authority.Settlements[operation.Id].ActualCostUsd);
                    using var parsed = JsonDocument.Parse(content);
                    return parsed.RootElement.GetProperty("result").GetProperty("rows").Clone();
                }
                var stale = await ReadCrossingRows();
                Assert.AreEqual(0, stale.GetArrayLength(),
                    "The live native value is emoji, so its old materialized zz cache must not satisfy WHERE text = zz.");
                body.Filters[0].Value = JsonSerializer.SerializeToElement("😀");
                var live = await ReadCrossingRows();
                CollectionAssert.AreEquivalent(new[] { original.Id, longer.Id },
                    live.EnumerateArray().Select(row => row[0].GetGuid()).ToArray());
                Assert.IsTrue(live.EnumerateArray().All(row => row[1].GetString() == "😀"));
                using (var response = await Send(otherToken))
                {
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, await response.Content.ReadAsStringAsync());
                    var operation = await outbox.GetAsync(
                        response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                    Assert.AreEqual("settled", operation.State);
                    Assert.AreEqual(2, operation.ProviderAttempts.Count);
                    Assert.AreEqual(2, operation.Receipts.Count);
                }
                Assert.AreEqual(3, authority.Settlements.Count);
                var preserved = await provider.LoadHolonAsync(snapshot.Id);
                Assert.IsFalse(preserved.IsError, preserved.Message + preserved.Exception);
                Assert.IsNotNull(preserved.Result);
                Assert.AreEqual("zz", preserved.Result.MetaData["textAlias"]?.ToString());
                await app.StopAsync();
                return;
            }
            if (guidRows)
            {
                Assert.IsTrue(guidDistinct && virtualOnlyId.HasValue);
                body.Filters[0].Operator = CqlV1Operator.GreaterThanOrEqual;
                body.Columns = new[] {
                    new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
                    new CqlProjectionV1Column { MetadataPath = new[] { "textAlias" }, ValueType = "String", Alias = "Text" },
                    new CqlProjectionV1Column { MetadataPath = new[] { "guidAlias" }, ValueType = "Guid",
                        Nullable = false, Alias = "Identity" }
                };
                var expected = new Dictionary<Guid, (string Text, Guid Identity)> {
                    [original.Id] = ("😀", MixedGuidFirst),
                    [virtualOnlyId.Value] = ("abc\0def", MixedGuidSecond),
                    [important.Id] = ("xy", MixedGuidSecond),
                    [longer.Id] = ("😀", MixedGuidFirst)
                };
                async Task<JsonElement> ReadGuidRows(int expectedAttempts = 4)
                {
                    using var response = await Send(ownerToken);
                    var content = await response.Content.ReadAsStringAsync();
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, content);
                    Assert.AreEqual("settled", response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                    using var parsed = JsonDocument.Parse(content);
                    var result = parsed.RootElement.GetProperty("result");
                    Assert.AreEqual("cql-projection-v1", result.GetProperty("version").GetString());
                    var operation = await outbox.GetAsync(
                        response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                    Assert.AreEqual("settled", operation.State);
                    Assert.AreEqual(expectedAttempts, operation.ProviderAttempts.Count);
                    Assert.AreEqual(expectedAttempts, operation.Receipts.Count);
                    Assert.AreEqual(expectedAttempts * 0.002m, authority.Settlements[operation.Id].ActualCostUsd);
                    foreach (var row in result.GetProperty("rows").EnumerateArray())
                    {
                        Assert.AreEqual(3, row.GetArrayLength());
                        Assert.IsTrue(expected.TryGetValue(row[0].GetGuid(), out var value));
                        Assert.AreEqual(value.Text, row[1].GetString());
                        Assert.AreEqual(value.Identity, row[2].GetGuid());
                    }
                    return result.Clone();
                }
                if (guidFilters)
                {
                    body.Filters = new[] { new CqlV1Filter {
                        MetadataPath = new[] { "guidAlias" }, Scalar = CqlV1Scalar.Guid,
                        Operator = CqlV1Operator.Equal,
                        Value = JsonSerializer.SerializeToElement(MixedGuidFirst.ToString("D").ToUpperInvariant())
                    } };
                    static Guid[] Identities(JsonElement result) => result.GetProperty("rows").EnumerateArray()
                        .Select(row => row[0].GetGuid()).ToArray();
                    var first = await ReadGuidRows();
                    CollectionAssert.AreEquivalent(new[] { original.Id, longer.Id }, Identities(first));
                    Assert.IsTrue(first.GetProperty("provenance").EnumerateArray().Any(row =>
                        row.GetProperty("representation").GetString() == "Virtual" &&
                        row.GetProperty("alsoMaterialized").GetBoolean()));
                    body.Filters[0].Operator = CqlV1Operator.NotEqual;
                    var different = await ReadGuidRows();
                    CollectionAssert.AreEquivalent(new[] { virtualOnlyId.Value, important.Id }, Identities(different));
                    body.Filters[0].Operator = CqlV1Operator.Equal;
                    body.Filters[0].Value = JsonSerializer.SerializeToElement(MixedGuidStale.ToString("D"));
                    var stale = await ReadGuidRows(expectedAttempts: 3);
                    Assert.AreEqual(0, Identities(stale).Length,
                        "A materialized UUID must not satisfy the predicate when the native UUID changed.");
                    body.Filters[0].Value = JsonSerializer.SerializeToElement(MixedGuidSecond.ToString("D"));
                    var second = await ReadGuidRows();
                    CollectionAssert.AreEquivalent(new[] { virtualOnlyId.Value, important.Id }, Identities(second));
                }
                else
                {
                var all = await ReadGuidRows();
                CollectionAssert.AreEquivalent(expected.Keys.ToArray(), all.GetProperty("rows").EnumerateArray()
                    .Select(row => row[0].GetGuid()).ToArray());
                var provenance = all.GetProperty("provenance").EnumerateArray().ToArray();
                Assert.AreEqual(4, provenance.Length);
                var virtualRows = provenance.Where(row => row.GetProperty("representation").GetString() == "Virtual").ToArray();
                Assert.AreEqual(2, virtualRows.Length);
                foreach (var row in virtualRows)
                    Assert.AreEqual(mapping.Id.ToString("D"), row.GetProperty("mappingId").GetString());
                Assert.AreEqual(1, virtualRows.Count(row => row.GetProperty("alsoMaterialized").GetBoolean()));
                Assert.AreEqual(2, provenance.Count(row => row.GetProperty("representation").GetString() == "Materialized"));
                body.OrderBy = "None";
                body.SelectedOrder = new[] {
                    new CqlGroupedV1Ordering { Alias = "Identity" },
                    new CqlGroupedV1Ordering { Alias = "Id" }
                };
                body.Skip = 1; body.Take = 2;
                var ascending = await ReadGuidRows();
                CollectionAssert.AreEqual(expected.OrderBy(row => row.Value.Identity).ThenBy(row => row.Key)
                    .Skip(1).Take(2).Select(row => row.Key).ToArray(),
                    ascending.GetProperty("rows").EnumerateArray().Select(row => row[0].GetGuid()).ToArray());
                body.SelectedOrder[0].Descending = true;
                var descending = await ReadGuidRows();
                CollectionAssert.AreEqual(expected.OrderByDescending(row => row.Value.Identity).ThenBy(row => row.Key)
                    .Skip(1).Take(2).Select(row => row.Key).ToArray(),
                    descending.GetProperty("rows").EnumerateArray().Select(row => row[0].GetGuid()).ToArray());
                }
                using (var response = await Send(otherToken))
                {
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, await response.Content.ReadAsStringAsync());
                    var operation = await outbox.GetAsync(
                        response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                    Assert.AreEqual("settled", operation.State);
                    Assert.AreEqual(2, operation.ProviderAttempts.Count);
                    Assert.AreEqual(2, operation.Receipts.Count);
                }
                Assert.AreEqual(guidFilters ? 5 : 4, authority.Settlements.Count);
                var preserved = await provider.LoadHolonAsync(snapshot.Id);
                Assert.IsFalse(preserved.IsError, preserved.Message + preserved.Exception);
                Assert.IsNotNull(preserved.Result);
                Assert.AreEqual("zz", preserved.Result.MetaData["textAlias"]?.ToString());
                Assert.AreEqual(MixedGuidStale.ToString("D"), preserved.Result.MetaData["guidAlias"]?.ToString());
                Assert.IsNull((await provider.LoadHolonAsync(virtualOnlyId.Value)).Result,
                    "Read-only GUID rows must not materialize the native-only record.");
                await app.StopAsync();
                return;
            }
            if (groupedDistinctOnly)
            {
                body.Columns = new[] {
                    new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
                    new CqlProjectionV1Column { MetadataPath = new[] { "textAlias" },
                        ValueType = "String", Nullable = true, Alias = "Text" }
                };
                if (guidDistinct)
                {
                    Assert.IsTrue(virtualOnlyId.HasValue);
                    body.Filters[0].Operator = CqlV1Operator.GreaterThanOrEqual;
                    body.Columns = body.Columns.Append(new CqlProjectionV1Column { Alias = "Identity",
                        MetadataPath = new[] { "guidAlias" }, ValueType = "Guid", Nullable = true }).ToArray();
                }
                body.ResultOperation = CqlProjectionV1ResultOperation.Group;
                body.Skip = 0;
                body.Take = 1;
                body.Grouping = new CqlMixedGroupingV1Request
                {
                    Keys = new[] { new CqlProjectionV1Column { Alias = "Text",
                        MetadataPath = new[] { "textAlias" }, ValueType = "String", Nullable = true } },
                    Columns = new[]
                    {
                        new CqlGroupedV1Column { Alias = "Text", KeyAlias = "Text" },
                        new CqlGroupedV1Column { Alias = "Rows", Operation = "LongCount" },
                        new CqlGroupedV1Column { Alias = "Present", Operation = "LongCount",
                            Selection = new CqlScalarV1Selection { MetadataPath = new[] { guidDistinct ? "guidAlias" : "textAlias" },
                                ValueType = guidDistinct ? "Guid" : "String", Nullable = true } },
                        new CqlGroupedV1Column { Alias = "Unique", Operation = "LongCount", Distinct = true,
                            Selection = new CqlScalarV1Selection { MetadataPath = new[] { guidDistinct ? "guidAlias" : "textAlias" },
                                ValueType = guidDistinct ? "Guid" : "String", Nullable = true } }
                    },
                    ResultOrder = new[] { new CqlGroupedV1Ordering { Alias = "Present", Descending = true } }
                };
                async Task<JsonElement> SuccessfulRows(HttpResponseMessage response)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, content);
                    Assert.AreEqual("settled", response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                    using var parsed = JsonDocument.Parse(content);
                    var result = parsed.RootElement.GetProperty("result");
                    Assert.AreEqual("cql-grouped-v1", result.GetProperty("version").GetString());
                    Assert.AreEqual(mapping.Id.ToString("D"), result.GetProperty("virtualMappingId").GetString());
                    Assert.IsFalse(result.GetProperty("canPrepareSnapshot").GetBoolean());
                    var operation = await outbox.GetAsync(
                        response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                    Assert.AreEqual("settled", operation.State);
                    Assert.AreEqual(4, operation.ProviderAttempts.Count);
                    Assert.AreEqual(4, operation.Receipts.Count);
                    Assert.AreEqual(0.008m, authority.Settlements[operation.Id].ActualCostUsd);
                    return result.GetProperty("rows").Clone();
                }
                using (var response = await Send(ownerToken))
                {
                    var rows = (await SuccessfulRows(response)).EnumerateArray().ToArray();
                    Assert.AreEqual(guidDistinct ? 3 : 2, rows.Length);
                    Assert.AreEqual("😀", rows[0][0].GetString());
                    Assert.AreEqual("2", rows[0][1].GetString());
                    Assert.AreEqual("2", rows[0][2].GetString());
                    Assert.AreEqual("1", rows[0][3].GetString());
                    var importantIndex = guidDistinct ? 2 : 1;
                    Assert.AreEqual("xy", rows[importantIndex][0].GetString());
                    Assert.AreEqual("1", rows[importantIndex][1].GetString());
                    Assert.AreEqual("1", rows[importantIndex][2].GetString());
                    Assert.AreEqual("1", rows[importantIndex][3].GetString());
                    if (guidDistinct)
                    {
                        Assert.AreEqual("abc\0def", rows[1][0].GetString());
                        Assert.AreEqual("1", rows[1][1].GetString());
                        Assert.AreEqual("1", rows[1][2].GetString());
                        Assert.AreEqual("1", rows[1][3].GetString());
                    }
                    Assert.IsFalse(rows.Any(row => row[0].GetString() == "zz"),
                        "The live native winner must remove the stale materialized group key.");
                }
                body.Grouping.ResultFilters = new[] {
                    new CqlGroupedV1Filter { Alias = "Present", Operator = CqlV1Operator.Equal,
                        Value = JsonSerializer.SerializeToElement("2") },
                    new CqlGroupedV1Filter { Alias = "Unique", Operator = CqlV1Operator.Equal,
                        Value = JsonSerializer.SerializeToElement("1") }
                };
                body.Grouping.ResultTake = 1;
                using (var response = await Send(ownerToken))
                {
                    var row = (await SuccessfulRows(response)).EnumerateArray().Single();
                    Assert.AreEqual("😀", row[0].GetString());
                    Assert.AreEqual("2", row[1].GetString());
                    Assert.AreEqual("2", row[2].GetString());
                    Assert.AreEqual("1", row[3].GetString());
                }
                using (var response = await Send(otherToken))
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, content);
                    var operation = await outbox.GetAsync(
                        response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                    Assert.AreEqual("settled", operation.State);
                    Assert.AreEqual(2, operation.ProviderAttempts.Count);
                    Assert.AreEqual(2, operation.Receipts.Count);
                }
                Assert.AreEqual(3, authority.Settlements.Count);
                var preserved = await provider.LoadHolonAsync(snapshot.Id);
                Assert.IsFalse(preserved.IsError, preserved.Message + preserved.Exception);
                Assert.IsNotNull(preserved.Result);
                Assert.AreEqual("zz", preserved.Result.MetaData["textAlias"]?.ToString(),
                    "Read-only reconciliation must not rewrite the stored cached copy.");

                if (guidDistinct)
                {
                    Assert.AreEqual(4, created.Count);
                    Assert.AreEqual(MixedGuidStale.ToString("D"), preserved.Result.MetaData["guidAlias"]?.ToString(),
                        "Read-only reconciliation must not rewrite the stale cached GUID.");
                    Assert.IsNull((await provider.LoadHolonAsync(virtualOnlyId!.Value)).Result,
                        "The participating virtual-only GUID row must remain unstored.");
                }
                await app.StopAsync();
                return;
            }
            using (var response = await Send(ownerToken))
            {
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                Assert.AreEqual("settled", response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                using var json = JsonDocument.Parse(text);
                var result = json.RootElement.GetProperty("result");
                var rows = result.GetProperty("rows").EnumerateArray().ToArray();
                Assert.AreEqual(2, rows.Length);
                var live = rows.Single(row => row[0].GetString() == original.Id.ToString("D"));
                Assert.AreEqual("😀", live[1].GetString()); Assert.AreEqual(2, live[2].GetInt32());
                Assert.AreEqual(2, live[3].GetInt32());
                Assert.IsTrue(rows.Any(row => row[0].GetString() == important.Id.ToString("D") && row[1].GetString() == "xy"));
                var provenance = result.GetProperty("provenance").EnumerateArray().ToArray();
                Assert.IsTrue(provenance.Single(row => row.GetProperty("representation").GetString() == "Virtual")
                    .GetProperty("alsoMaterialized").GetBoolean());
                Assert.AreEqual(1, provenance.Count(row => row.GetProperty("representation").GetString() == "Materialized"));
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(4, operation.ProviderAttempts.Count); Assert.AreEqual(4, operation.Receipts.Count);
                Assert.AreEqual(0.008m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            // Both branches must participate completely before de-duplication,
            // selected scalar ordering and one global page.
            body.Filters[0].Operator = CqlV1Operator.GreaterThanOrEqual;
            body.OrderBy = "None";
            body.SelectedOrder = new[] {
                new CqlGroupedV1Ordering { Alias = "Units", Descending = true },
                new CqlGroupedV1Ordering { Alias = "Original" }
            };
            body.Skip = 1; body.Take = 1;
            // All three owned sources contain this seven-unit native value.
            // It must remain virtual-only before AND after the HTTP queries.
            var expectedNative = await mapping.QueryAsync(mapping.OwnerAvatarId, reader, null,
                new[] { new NativeRecordFilter("textAlias", CosmicQueryComparisonOperator.Equal, 7L,
                    NativeRecordFunction.Utf16Length) }, new[] { "textAlias" }, 1);
            Assert.IsFalse(expectedNative.IsError, expectedNative.Message + expectedNative.Exception);
            var orderedId = expectedNative.Result.Single().Id;
            Assert.IsNull((await provider.LoadHolonAsync(orderedId)).Result,
                "The ordered virtual-only fixture must not already have a materialized copy.");
            var orderedText = "abc\u0000def"; var orderedUnits = 7; var orderedRepresentation = "Virtual";
            using (var response = await Send(ownerToken))
            {
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                using var json = JsonDocument.Parse(text);
                var result = json.RootElement.GetProperty("result");
                var row = result.GetProperty("rows").EnumerateArray().Single();
                Assert.AreEqual(orderedId.ToString("D"), row[0].GetString());
                Assert.AreEqual(orderedText, row[1].GetString()); Assert.AreEqual(orderedUnits, row[2].GetInt32());
                var orderedProvenance = result.GetProperty("provenance").EnumerateArray().Single();
                Assert.AreEqual(orderedRepresentation, orderedProvenance.GetProperty("representation").GetString());
                if (orderedRepresentation == "Virtual")
                    Assert.IsFalse(orderedProvenance.GetProperty("alsoMaterialized").GetBoolean());
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(4, operation.ProviderAttempts.Count); Assert.AreEqual(4, operation.Receipts.Count);
                Assert.AreEqual(0.008m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            body.SelectedOrder = new[] { new CqlGroupedV1Ordering { Alias = "Original", Descending = true } };
            body.Skip = 0;
            using (var response = await Send(ownerToken))
            {
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                using var json = JsonDocument.Parse(text);
                var result = json.RootElement.GetProperty("result");
                var row = result.GetProperty("rows").EnumerateArray().Single();
                Assert.AreEqual(original.Id.ToString("D"), row[0].GetString());
                Assert.AreEqual("😀", row[1].GetString());
                var provenance = result.GetProperty("provenance").EnumerateArray().Single();
                Assert.AreEqual("Virtual", provenance.GetProperty("representation").GetString());
                Assert.IsTrue(provenance.GetProperty("alsoMaterialized").GetBoolean());
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(4, operation.ProviderAttempts.Count); Assert.AreEqual(4, operation.Receipts.Count);
                Assert.AreEqual(0.008m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            body.Columns = new[] { body.Columns[0] };
            body.Filters[0].Operator = CqlV1Operator.Equal;
            body.OrderBy = "Id"; body.SelectedOrder = null;
            body.Skip = 0; body.Take = 5;
            using (var response = await Send(ownerToken))
            {
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                using var json = JsonDocument.Parse(text);
                var result = json.RootElement.GetProperty("result");
                Assert.AreEqual(1, result.GetProperty("columns").GetArrayLength());
                var ids = result.GetProperty("rows").EnumerateArray().ToArray();
                Assert.AreEqual(2, ids.Length);
                Assert.IsTrue(ids.All(row => row.GetArrayLength() == 1));
                CollectionAssert.AreEquivalent(new[] { original.Id, important.Id },
                    ids.Select(row => Guid.Parse(row[0].GetString()!)).ToArray());
                var liveIdentity = result.GetProperty("provenance").EnumerateArray()
                    .Single(item => item.GetProperty("representation").GetString() == "Virtual");
                Assert.IsTrue(liveIdentity.GetProperty("alsoMaterialized").GetBoolean());
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(4, operation.ProviderAttempts.Count); Assert.AreEqual(4, operation.Receipts.Count);
                Assert.AreEqual(0.008m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            body.ResultOperation = CqlProjectionV1ResultOperation.LongCount;
            body.Take = 1;
            body.Filters[0].Operator = CqlV1Operator.GreaterThanOrEqual;
            using (var response = await Send(ownerToken))
            {
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                using var json = JsonDocument.Parse(text);
                var result = json.RootElement.GetProperty("result");
                Assert.AreEqual("cql-scalar-v1", result.GetProperty("version").GetString());
                Assert.AreEqual("LongCount", result.GetProperty("operation").GetString());
                Assert.AreEqual("Int64", result.GetProperty("valueType").GetString());
                Assert.AreEqual("4", result.GetProperty("value").GetString());
                Assert.IsFalse(result.GetProperty("canPrepareSnapshot").GetBoolean());
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(4, operation.ProviderAttempts.Count); Assert.AreEqual(4, operation.Receipts.Count);
                Assert.AreEqual(0.008m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            body.Filters[0].Value = JsonSerializer.SerializeToElement(long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            using (var response = await Send(ownerToken))
            {
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                using var json = JsonDocument.Parse(text);
                Assert.AreEqual("0", json.RootElement.GetProperty("result").GetProperty("value").GetString());
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(3, operation.ProviderAttempts.Count); Assert.AreEqual(3, operation.Receipts.Count);
                Assert.AreEqual(0.006m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            body.Filters[0].Value = JsonSerializer.SerializeToElement("2");
            body.Columns = new[] { body.Columns[0], new CqlProjectionV1Column {
                MetadataPath = new[] { "textAlias" }, ValueType = "String", Alias = "Units",
                Function = CqlV1Function.Utf16Length, Nullable = true } };
            foreach (var aggregateOperation in new[] { CqlProjectionV1ResultOperation.Min, CqlProjectionV1ResultOperation.Max,
                CqlProjectionV1ResultOperation.Sum, CqlProjectionV1ResultOperation.Average })
            {
                body.ResultOperation = aggregateOperation;
                using var response = await Send(ownerToken);
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                using var json = JsonDocument.Parse(text);
                var result = json.RootElement.GetProperty("result");
                Assert.AreEqual("cql-scalar-v1", result.GetProperty("version").GetString());
                Assert.AreEqual(aggregateOperation.ToString(), result.GetProperty("operation").GetString());
                if (aggregateOperation == CqlProjectionV1ResultOperation.Average)
                {
                    Assert.AreEqual("Decimal", result.GetProperty("valueType").GetString());
                    Assert.AreEqual("5", result.GetProperty("value").GetString());
                }
                else
                {
                    Assert.AreEqual("Int32", result.GetProperty("valueType").GetString());
                    Assert.AreEqual(aggregateOperation == CqlProjectionV1ResultOperation.Min ? 2
                        : aggregateOperation == CqlProjectionV1ResultOperation.Max ? 9 : 20, result.GetProperty("value").GetInt32());
                }
                Assert.IsFalse(result.GetProperty("canPrepareSnapshot").GetBoolean());
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(4, operation.ProviderAttempts.Count); Assert.AreEqual(4, operation.Receipts.Count);
                Assert.AreEqual(0.008m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            foreach (var countOperation in new[] { CqlProjectionV1ResultOperation.CountValues, CqlProjectionV1ResultOperation.CountDistinct })
            {
                body.ResultOperation = countOperation;
                using var response = await Send(ownerToken);
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                using var json = JsonDocument.Parse(text);
                var result = json.RootElement.GetProperty("result");
                Assert.AreEqual("cql-scalar-v1", result.GetProperty("version").GetString());
                Assert.AreEqual(countOperation.ToString(), result.GetProperty("operation").GetString());
                Assert.AreEqual("Int64", result.GetProperty("valueType").GetString());
                Assert.AreEqual(countOperation == CqlProjectionV1ResultOperation.CountDistinct ? "3" : "4",
                    result.GetProperty("value").GetString());
                Assert.IsFalse(result.GetProperty("canPrepareSnapshot").GetBoolean());
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(4, operation.ProviderAttempts.Count); Assert.AreEqual(4, operation.Receipts.Count);
                Assert.AreEqual(0.008m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            body.Filters[0].Operator = CqlV1Operator.Equal;
            body.Columns = new[] { body.Columns[0], new CqlProjectionV1Column {
                MetadataPath = new[] { "textAlias" }, ValueType = "String", Alias = "Text", Nullable = true } };
            foreach (var textOperation in new[] { CqlProjectionV1ResultOperation.Min, CqlProjectionV1ResultOperation.Max })
            {
                body.ResultOperation = textOperation;
                using var response = await Send(ownerToken);
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                using var json = JsonDocument.Parse(text);
                var result = json.RootElement.GetProperty("result");
                Assert.AreEqual("cql-scalar-v1", result.GetProperty("version").GetString());
                Assert.AreEqual(textOperation.ToString(), result.GetProperty("operation").GetString());
                Assert.AreEqual("String", result.GetProperty("valueType").GetString());
                Assert.AreEqual(textOperation == CqlProjectionV1ResultOperation.Min ? "xy" : "😀",
                    result.GetProperty("value").GetString());
                Assert.IsFalse(result.GetProperty("canPrepareSnapshot").GetBoolean());
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(4, operation.ProviderAttempts.Count); Assert.AreEqual(4, operation.Receipts.Count);
                Assert.AreEqual(0.008m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            body.ResultOperation = CqlProjectionV1ResultOperation.Group;
            body.Grouping = new CqlMixedGroupingV1Request
            {
                Keys = new[] { new CqlProjectionV1Column { Alias = "Text",
                    MetadataPath = new[] { "textAlias" }, ValueType = "String", Nullable = true } },
                Columns = new[]
                {
                    new CqlGroupedV1Column { Alias = "Text", KeyAlias = "Text" },
                    new CqlGroupedV1Column { Alias = "Rows", Operation = "LongCount" },
                    new CqlGroupedV1Column { Alias = "Present", Operation = "LongCount",
                        Selection = new CqlScalarV1Selection { MetadataPath = new[] { "textAlias" },
                            ValueType = "String", Nullable = true } }
                },
                ResultFilters = new[] { new CqlGroupedV1Filter { Alias = "Rows",
                    Operator = CqlV1Operator.Equal, Value = JsonSerializer.SerializeToElement("1") } },
                ResultOrder = new[] { new CqlGroupedV1Ordering { Alias = "Text", Descending = true } },
                ResultSkip = 1, ResultTake = 1
            };
            using (var response = await Send(ownerToken))
            {
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                using var json = JsonDocument.Parse(text);
                var result = json.RootElement.GetProperty("result");
                Assert.AreEqual("cql-grouped-v1", result.GetProperty("version").GetString());
                Assert.AreEqual(mapping.Id.ToString("D"), result.GetProperty("virtualMappingId").GetString());
                Assert.IsFalse(result.GetProperty("canPrepareSnapshot").GetBoolean());
                var row = result.GetProperty("rows").EnumerateArray().Single();
                Assert.AreEqual("xy", row[0].GetString());
                Assert.AreEqual("1", row[1].GetString()); Assert.AreEqual("1", row[2].GetString());
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(4, operation.ProviderAttempts.Count); Assert.AreEqual(4, operation.Receipts.Count);
                Assert.AreEqual(0.008m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            body.Grouping.Columns = new[] { body.Grouping.Columns[0] };
            body.Grouping.ResultFilters = Array.Empty<CqlGroupedV1Filter>();
            using (var response = await Send(ownerToken))
            {
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                using var json = JsonDocument.Parse(text);
                var result = json.RootElement.GetProperty("result");
                Assert.AreEqual("cql-grouped-v1", result.GetProperty("version").GetString());
                Assert.AreEqual(1, result.GetProperty("columns").GetArrayLength());
                Assert.IsFalse(result.GetProperty("canPrepareSnapshot").GetBoolean());
                var row = result.GetProperty("rows").EnumerateArray().Single();
                Assert.AreEqual(1, row.GetArrayLength()); Assert.AreEqual("xy", row[0].GetString());
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(4, operation.ProviderAttempts.Count); Assert.AreEqual(4, operation.Receipts.Count);
                Assert.AreEqual(0.008m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            using (var response = await Send(otherToken))
            {
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, text);
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(2, operation.ProviderAttempts.Count); Assert.AreEqual(2, operation.Receipts.Count);
            }
            Assert.IsNull((await provider.LoadHolonAsync(orderedId)).Result,
                "Read-only mixed queries must not materialize the virtual-only source record.");
            await app.StopAsync();
            Assert.AreEqual(17, authority.Settlements.Count);
        }
        finally
        {
            foreach (var id in created.AsEnumerable().Reverse())
            {
                var removed = await provider.DeleteHolonAsync(id);
                Assert.IsFalse(removed.IsError, removed.Message + removed.Exception);
            }
            if (initialized)
            {
                foreach (var id in authority.Started.Keys)
                {
                    var operation = await outbox.GetAsync(id, CancellationToken.None);
                    Assert.AreEqual("settled", operation.State, "Preserve uncertain owned operation evidence.");
                }
                await client.DropDatabaseAsync(usageDb);
                using var remaining = await client.ListDatabaseNamesAsync();
                Assert.IsFalse((await remaining.ToListAsync()).Contains(usageDb));
            }
        }
    }
}

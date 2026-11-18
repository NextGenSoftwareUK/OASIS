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
        VirtualHolonMapping mapping, VirtualHolonRead original)
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
            MetaData = new() { ["textAlias"] = "important" }
        };
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
                var saved = await provider.SaveHolonAsync(row);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                created.Add(row.Id);
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
                return await http.SendAsync(request);
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
                Assert.AreEqual(3, operation.ProviderAttempts.Count); Assert.AreEqual(3, operation.Receipts.Count);
                Assert.AreEqual(0.006m, authority.Settlements[operation.Id].ActualCostUsd);
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
            using (var response = await Send(ownerToken))
            {
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                using var json = JsonDocument.Parse(text);
                var result = json.RootElement.GetProperty("result");
                var row = result.GetProperty("rows").EnumerateArray().Single();
                Assert.AreEqual(important.Id.ToString("D"), row[0].GetString());
                Assert.AreEqual("xy", row[1].GetString()); Assert.AreEqual(2, row[2].GetInt32());
                Assert.AreEqual("Materialized", result.GetProperty("provenance").EnumerateArray().Single()
                    .GetProperty("representation").GetString());
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(3, operation.ProviderAttempts.Count); Assert.AreEqual(3, operation.Receipts.Count);
                Assert.AreEqual(0.006m, authority.Settlements[operation.Id].ActualCostUsd);
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
                Assert.AreEqual(3, operation.ProviderAttempts.Count); Assert.AreEqual(3, operation.Receipts.Count);
                Assert.AreEqual(0.006m, authority.Settlements[operation.Id].ActualCostUsd);
            }
            using (var response = await Send(otherToken))
            {
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, text);
                var operation = await outbox.GetAsync(response.Headers.GetValues("X-OASIS-Operation-Id").Single(), CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(2, operation.ProviderAttempts.Count); Assert.AreEqual(2, operation.Receipts.Count);
            }
            await app.StopAsync();
            Assert.AreEqual(4, authority.Settlements.Count);
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

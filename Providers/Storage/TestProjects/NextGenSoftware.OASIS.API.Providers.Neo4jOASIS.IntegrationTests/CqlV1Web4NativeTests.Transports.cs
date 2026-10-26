extern alias ShippingWeb4;

using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using ShippingMutation = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL.Mutation;
using ShippingGrpcService = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices.DataGrpcService;
using ShippingGrpcRequest = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc.SaveHolonGrpcRequest;
using ShippingDeleteRequest = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc.DeleteHolonGrpcRequest;
using ShippingDeleteResponse = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc.OASISDataResponse;
using ShippingGrpcResponse = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc.HolonResponse;
using ShippingJson = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonJsonContracts;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private static void AddShippingTransportServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddGrpc();
        // Exercise the shipping resolver and output contract explicitly. This
        // fixture does not claim unrelated GraphQL mutations are certified.
        services.AddGraphQLServer()
            .AddQueryType(descriptor => descriptor.Name("Query").Field("ping").Resolve("pong"))
            .AddMutationType(new ObjectType<ShippingMutation>(descriptor =>
            {
                descriptor.BindFieldsExplicitly();
                descriptor.Field(value => value.SaveHolon(default!, default!, default!, default!, default, default!, default));
                descriptor.Field(value => value.DeleteHolon(default, default!, default, default, default, default!));
            }))
            .AddType<ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL.Types.HolonType>();
    }

    private static void MapShippingTransports(WebApplication app)
    {
        app.MapGraphQL();
        app.MapGrpcService<ShippingGrpcService>();
    }

    private static async Task<int> VerifyShippingTransportsAsync(
        Dictionary<ProviderType, IOASISStorageProvider> providers, MongoUsageOutboxStore outbox,
        Authority authority, string secret, Guid actor, Guid[] ids, Guid foreignId, string tag,
        Func<string, object?, Task> journal, List<string> conditionalOperations)
    {
        var bearer = Token(actor, new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)), lifetimeMinutes: 15);
        authority.Tokens.Add(bearer, actor);
        var priorDna = OASISDNAManager.OASISDNA;
        var priorBoot = OASISBootLoader.OASISBootLoader.OASISDNA;
        var globals = ProviderManager.Instance;
        var priorRegistryDna = globals.OASISDNA;
        var priorCurrent = globals.CurrentStorageProvider;
        var requests = 0;
        try
        {
            foreach (var pair in providers)
            {
                var manager = new Sources(providers).CreateManager(pair.Value);
                OASISDNAManager.OASISDNA = OASISBootLoader.OASISBootLoader.OASISDNA = globals.OASISDNA = manager.OASISDNA;
                Assert.AreSame(pair.Value, globals.GetProvider(pair.Key), "The preceding REST cohort registered this exact provider.");
                Assert.IsFalse(globals.SetAndActivateCurrentStorageProvider(pair.Value).IsError);
                await journal("before-transport-seed", new { provider = pair.Key.ToString(), actor, ids });
                var root = new Holon { Id = ids[1], Name = "transport root", CreatedByAvatarId = actor, IsActive = true };
                root.MetaData["fixture"] = tag;
                root.Children.Add(new Holon { Id = ids[2], ParentHolonId = ids[1], Name = "transport child", CreatedByAvatarId = actor, IsActive = true });
                var seed = await manager.SaveHolonAsync(root, actor, providerType: pair.Key);
                Assert.IsFalse(seed.IsError, seed.Message + seed.Exception);

                await using var app = RestHost(pair.Key, pair.Value, outbox, authority, secret, transportMode: true);
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(30),
                        DefaultRequestVersion = HttpVersion.Version20, DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact };
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                    var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { ReferenceHandler = ReferenceHandler.IgnoreCycles };
                    json.Converters.Add(new JsonStringEnumConverter());
                    ShippingJson.Configure(json);

                    async Task<Holon> Read(Guid id, bool children = false)
                    {
                        var read = await manager.LoadHolonAsync(id, loadChildren: children,
                            loadChildrenFromProvider: children, providerType: pair.Key);
                        Assert.IsFalse(read.IsError, read.Message + read.Exception);
                        return (Holon)read.Result;
                    }

                    async Task<byte[]> Send(string path, byte[] payload, string contentType, Guid intent,
                        int attempts, decimal cost, string outcome)
                    {
                        var key = "transport-" + Guid.NewGuid().ToString("N");
                        if (intent != Guid.Empty) conditionalOperations.Add(intent.ToString("D"));
                        await journal("before-transport-http", new { provider = pair.Key.ToString(), actor, path, key, intent });
                        using var request = new HttpRequestMessage(HttpMethod.Post, path)
                        {
                            Version = HttpVersion.Version20, VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                            Content = new ByteArrayContent(payload)
                        };
                        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                        request.Headers.Add("Idempotency-Key", key);
                        if (contentType == "application/grpc") request.Headers.TryAddWithoutValidation("TE", "trailers");
                        using var response = await client.SendAsync(request);
                        var body = await response.Content.ReadAsByteArrayAsync();
                        Assert.AreEqual(HttpVersion.Version20, response.Version);
                        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, Encoding.UTF8.GetString(body));
                        Assert.IsTrue(response.Headers.TryGetValues("X-OASIS-Operation-Id", out var operationHeaders));
                        var operationId = operationHeaders!.Single();
                        var operation = await outbox.GetAsync(operationId, CancellationToken.None);
                        Assert.AreEqual("settled", operation.State, "Preserve uncertain transport operations.");
                        Assert.AreEqual(actor.ToString("D"), operation.UserId);
                        Assert.AreEqual(attempts, operation.ProviderAttempts.Count);
                        Assert.AreEqual(attempts, operation.Receipts.Count);
                        Assert.AreEqual(cost, authority.Settlements[operationId].ActualCostUsd);
                        Assert.AreEqual(outcome, authority.Settlements[operationId].Outcome);
                        requests++;
                        return body;
                    }

                    async Task<JsonElement> Graph(Guid operation, string expectedCode, int attempts, decimal cost)
                    {
                        var operationArgument = operation == Guid.Empty ? "" : $", operationId: \"{operation:D}\"";
                        var query = $"mutation {{ saveHolon(name: \"GraphQL native create\", description: \"signed transport\", holonType: \"Quest\", id: \"{ids[0]:D}\", providerType: \"{pair.Key}\"{operationArgument}) {{ id name }} }}";
                        var body = await Send("/graphql", JsonSerializer.SerializeToUtf8Bytes(new { query }), "application/json",
                            operation, attempts, cost, expectedCode.Length == 0 ? "succeeded" : "failed");
                        using var parsed = JsonDocument.Parse(body);
                        if (expectedCode.Length != 0)
                            Assert.AreEqual(expectedCode, parsed.RootElement.GetProperty("errors")[0].GetProperty("extensions").GetProperty("code").GetString());
                        else Assert.IsFalse(parsed.RootElement.TryGetProperty("errors", out _), Encoding.UTF8.GetString(body));
                        return parsed.RootElement.Clone();
                    }

                    await Graph(Guid.NewGuid(), "", 2, 0.004m);
                    Assert.AreEqual(actor, (await Read(ids[0])).CreatedByAvatarId);
                    await Graph(Guid.NewGuid(), CosmicConcurrencyException.Code, 2, 0.004m);
                    await Graph(Guid.Empty, "COSMIC_WRITE_INTENT_INVALID", 0, 0);

                    async Task<ShippingGrpcResponse> Grpc(ShippingGrpcRequest wire, string expectedCode, int attempts, decimal cost)
                    {
                        var payload = wire.ToByteArray();
                        var frame = new byte[payload.Length + 5];
                        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(1, 4), payload.Length);
                        payload.CopyTo(frame, 5);
                        var body = await Send("/oasis.web4.DataService/SaveHolon", frame, "application/grpc",
                            Guid.TryParse(wire.OperationId, out var operation) ? operation : Guid.Empty,
                            attempts, cost, expectedCode.Length == 0 ? "succeeded" : "failed");
                        Assert.IsTrue(body.Length >= 5);
                        Assert.AreEqual(0, body[0]);
                        Assert.AreEqual(body.Length - 5, BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(1, 4)));
                        var result = ShippingGrpcResponse.Parser.ParseFrom(body, 5, body.Length - 5);
                        Assert.AreEqual(expectedCode, result.ErrorCode);
                        Assert.AreEqual(expectedCode.Length != 0, result.IsError);
                        return result;
                    }

                    var original = await Read(ids[1], children: true);
                    original.Name = "gRPC acknowledged graph";
                    var originalJson = JsonSerializer.Serialize(original, json);
                    ShippingGrpcRequest Update(Guid operation)
                    {
                        var wire = new ShippingGrpcRequest { OperationId = operation.ToString("D"), ProviderType = pair.Key.ToString(),
                            HolonJson = originalJson, SaveChildren = true, Recursive = true, MaxChildDepth = 1 };
                        foreach (var record in new[] { original }.Cast<IHolon>().Concat(original.Children))
                            wire.ExpectedRevisions.Add(record.Id.ToString("D"), CosmicConcurrency.GetProviderRevisionId(record, pair.Key)!.Value.ToString("D"));
                        return wire;
                    }
                    var saved = await Grpc(Update(Guid.NewGuid()), "", 3, 0.005m);
                    Assert.IsTrue(saved.IsSaved);
                    Assert.IsFalse(string.IsNullOrEmpty(saved.CommittedDeltaJson));
                    Assert.AreEqual("gRPC acknowledged graph", (await Read(ids[1])).Name);
                    await Grpc(Update(Guid.NewGuid()), CosmicConcurrencyException.Code, 3, 0.005m);
                    var foreign = new ShippingGrpcRequest { OperationId = Guid.NewGuid().ToString("D"),
                        ProviderType = pair.Key.ToString(), HolonJson = JsonSerializer.Serialize(new Holon
                            { Id = foreignId, Name = "forged", CreatedByAvatarId = actor }, json) };
                    foreign.ExpectedRevisions.Add(foreignId.ToString("D"), Guid.NewGuid().ToString("D"));
                    await Grpc(foreign, "COSMIC_WRITE_FORBIDDEN", 1, 0.001m);
                    await Grpc(new ShippingGrpcRequest { HolonJson = "{}" }, "COSMIC_WRITE_INTENT_INVALID", 0, 0);
                    await Grpc(new ShippingGrpcRequest { HolonJson = "not-json" }, "COSMIC_WRITE_INTENT_INVALID", 0, 0);
                    // Deletion uses the caller's original revision and the same
                    // admitted authority; none of these cases retries a write.
                    async Task GraphDelete(Guid id, Guid intent, Guid revision, string code, int attempts)
                    {
                        var operationArgument = intent == Guid.Empty ? "" : $", operationId: \"{intent:D}\"";
                        var query = $"mutation {{ deleteHolon(id: \"{id:D}\", providerType: \"{pair.Key}\", expectedRevision: \"{revision:D}\"{operationArgument}) }}";
                        var cost = attempts == 0 ? 0m : attempts == 1 ? 0.001m : 0.004m;
                        var body = await Send("/graphql", JsonSerializer.SerializeToUtf8Bytes(new { query }), "application/json",
                            intent, attempts, cost, code.Length == 0 ? "succeeded" : "failed");
                        using var parsed = JsonDocument.Parse(body);
                        if (code.Length != 0)
                            Assert.AreEqual(code, parsed.RootElement.GetProperty("errors")[0].GetProperty("extensions").GetProperty("code").GetString());
                        else Assert.IsTrue(parsed.RootElement.GetProperty("data").GetProperty("deleteHolon").GetBoolean());
                    }

                    var graphRevision = CosmicConcurrency.GetProviderRevisionId(await Read(ids[0]), pair.Key)!.Value;
                    await GraphDelete(ids[0], Guid.Empty, graphRevision, "COSMIC_WRITE_INTENT_INVALID", 0);
                    await GraphDelete(foreignId, Guid.NewGuid(), Guid.NewGuid(), "COSMIC_WRITE_FORBIDDEN", 1);
                    await GraphDelete(ids[0], Guid.NewGuid(), Guid.NewGuid(), CosmicConcurrencyException.Code, 2);
                    await GraphDelete(ids[0], Guid.NewGuid(), graphRevision, "", 2);
                    var softDeleted = await pair.Value.LoadHolonAsync(ids[0], false, true, 0, true, false, 0);
                    Assert.IsNotNull(softDeleted.Result);
                    Assert.IsTrue(softDeleted.Result.IsDeleted);
                    Assert.AreEqual(actor, softDeleted.Result.DeletedByAvatarId);

                    async Task GrpcDelete(Guid id, Guid intent, Guid revision, string code, int attempts)
                    {
                        var wire = new ShippingDeleteRequest { Id = id.ToString("D"), ProviderType = pair.Key.ToString(),
                            SoftDelete = false, OperationId = intent == Guid.Empty ? "" : intent.ToString("D"),
                            ExpectedRevision = revision.ToString("D") };
                        var payload = wire.ToByteArray();
                        var frame = new byte[payload.Length + 5];
                        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(1, 4), payload.Length);
                        payload.CopyTo(frame, 5);
                        var cost = attempts == 0 ? 0m : attempts == 1 ? 0.001m : 0.004m;
                        var body = await Send("/oasis.web4.DataService/DeleteHolon", frame, "application/grpc", intent,
                            attempts, cost, code.Length == 0 ? "succeeded" : "failed");
                        Assert.IsTrue(body.Length >= 5);
                        Assert.AreEqual(0, body[0]);
                        Assert.AreEqual(body.Length - 5, BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(1, 4)));
                        var result = ShippingDeleteResponse.Parser.ParseFrom(body, 5, body.Length - 5);
                        Assert.AreEqual(code, result.ErrorCode);
                        Assert.AreEqual(code.Length != 0, result.IsError);
                        Assert.AreEqual(code.Length == 0, result.IsDeleted);
                        if (code.Length == 0) Assert.IsFalse(string.IsNullOrEmpty(result.CommittedDeltaJson));
                    }
                    var hardRevision = CosmicConcurrency.GetProviderRevisionId(await Read(ids[2]), pair.Key)!.Value;
                    await GrpcDelete(ids[2], Guid.Empty, hardRevision, "COSMIC_WRITE_INTENT_INVALID", 0);
                    await GrpcDelete(foreignId, Guid.NewGuid(), Guid.NewGuid(), "COSMIC_WRITE_FORBIDDEN", 1);
                    await GrpcDelete(ids[2], Guid.NewGuid(), Guid.NewGuid(), CosmicConcurrencyException.Code, 2);
                    await GrpcDelete(ids[2], Guid.NewGuid(), hardRevision, "", 2);
                    var hardDeleted = await pair.Value.LoadHolonAsync(ids[2], false, true, 0, true, false, 0);
                    Assert.IsNull(hardDeleted.Result, "Acknowledged hard deletion must remove the native record.");
                    await journal("transport-provider-complete", new { provider = pair.Key.ToString(), requests });
                }
                finally { await app.StopAsync(); }
            }
        }
        finally
        {
            if (priorCurrent != null) Assert.IsFalse(globals.SetAndActivateCurrentStorageProvider(priorCurrent).IsError);
            globals.OASISDNA = priorRegistryDna;
            OASISBootLoader.OASISBootLoader.OASISDNA = priorBoot;
            OASISDNAManager.OASISDNA = priorDna;
        }
        return requests;
    }
}

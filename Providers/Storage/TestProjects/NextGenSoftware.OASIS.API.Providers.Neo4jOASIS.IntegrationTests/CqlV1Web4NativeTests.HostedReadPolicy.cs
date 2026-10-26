extern alias ShippingWeb4;

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using ShippingQuery = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.CqlV1Controller;
using ShippingPolicy = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.ICqlReadAccessPolicy;
using ShippingProviders = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.ICqlV1Providers;
using ShippingFilter = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.CqlV1ResourceFilter;
using ShippingJwt = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware.OasisJwtValidation;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private sealed class OnlyShippingQuery : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var controller in feature.Controllers.ToArray())
                if (controller.AsType() != typeof(ShippingQuery)) feature.Controllers.Remove(controller);
        }
    }

    private sealed class ShippingQuerySources(Dictionary<ProviderType, IOASISStorageProvider> providers, ProviderType selected) : ShippingProviders
    {
        public int Lookups;
        public IOASISStorageProvider Resolve(ProviderType? requested)
        {
            Lookups++;
            Assert.IsTrue(requested == null || requested == selected);
            return providers[selected];
        }
        public HolonManager CreateManager(IOASISStorageProvider storage) => new Sources(providers).CreateManager(storage);
    }

    private sealed class ShippingReadPolicy(Guid expectedActor, ProviderType expectedAuthority, Guid[] ids) : ShippingPolicy
    {
        public int Mode, Calls;
        public OASISResult<CosmicQueryReadAccess> Authorize(Guid actor, ProviderType authority)
        {
            Assert.AreEqual(expectedActor, actor);
            Assert.AreEqual(expectedAuthority, authority);
            Calls++;
            if (Mode == 1) return new() { IsError = true, ErrorCode = "owned-denial" };
            return CosmicQueryReadAccess.AuthoritativeRecords(actor, authority, Mode == 2 ? Array.Empty<Guid>() : ids);
        }
    }

    private static WebApplication ShippingReadHost(ShippingQuerySources sources, ShippingReadPolicy policy,
        ProviderType provider, MongoUsageOutboxStore outbox, Authority authority, string secret)
        => ShippingReadHost(sources, policy, new[] { provider }, outbox, authority, secret);

    private static WebApplication ShippingReadHost(ShippingProviders sources, ShippingPolicy policy,
        ProviderType[] providers, MongoUsageOutboxStore outbox, Authority authority, string secret,
        ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.CqlFederationCursorHost? cursors = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ShippingProviders>(sources);
        builder.Services.AddSingleton<ShippingPolicy>(policy);
        if (cursors != null) builder.Services.AddSingleton(cursors);
        ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.CqlReadPolicyEnrollment.AddConfiguredCqlReadAccessPolicy(
            builder.Services, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
            { ["CQL_REQUIRE_AUTHORITATIVE_READ_POLICY"]="true" }).Build());
        builder.Services.AddSingleton<ShippingFilter>();
        builder.Services.AddSingleton<IUsageOutboxStore>(outbox);
        builder.Services.AddSingleton<IUsageReservationAuthority>(authority);
        builder.Services.AddSingleton(new Web4UsageLedgerOptions { Service = "WEB4", EndpointPolicy = http =>
            IsQueryRoute(http) ? new UsageEndpointPolicy { MeterCategory = "cql.query", ReservedUnits = 1,
                EstimatedCostUsd = 0.002m * providers.Length, PricingCatalogueVersion = "owned-fixture-v1",
                RequiresProviderMeasurement = true, MaximumProviderAttempts = providers.Length, MaximumNativeFeeCostUsd = 0 } : null });
        builder.Services.AddSingleton<Web4UsageExecution>();
        builder.Services.AddSingleton<IPreparedStorageAdmissionFactory>(new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers,
            providers.Select(provider => new StorageQueryTariff(provider, 0.002m, "owned-fixture-v1"))));
        builder.Services.AddControllers(options => options.Filters.Add<UsageResultFilter>())
            .AddApplicationPart(typeof(ShippingQuery).Assembly)
            .ConfigureApplicationPartManager(parts => parts.FeatureProviders.Add(new OnlyShippingQuery()));
        var app = builder.Build();
        app.UseRouting();
        app.Use(async (http, next) =>
        {
            if (AuthenticationHeaderValue.TryParse(http.Request.Headers.Authorization, out var header) && header.Scheme == "Bearer")
            {
                http.User = ShippingJwt.Validate(header.Parameter!, secret, "owned-cql-v1-fixture");
                http.Items["Avatar"] = new Avatar { Id = Guid.Parse(http.User.FindFirstValue("sub")!) };
            }
            await next();
        });
        app.UseMiddleware<Web4UsageLedgerMiddleware>();
        app.UseWhen(IsQueryRoute, branch => branch.UseMiddleware<PreparedStorageAdmissionMiddleware>());
        app.MapControllers();
        return app;
    }

    private static async Task<int> VerifyShippingReadPolicy(Dictionary<ProviderType, IOASISStorageProvider> providers,
        MongoUsageOutboxStore outbox, Authority authority, string secret, Guid actor, Guid[] ids, Guid[] childIds,
        string tag, Func<string, object?, Task> journal)
    {
        var requests = 0;
        var bearer = authority.Tokens.Single(pair => pair.Value == actor).Key;
        foreach (var pair in providers)
        {
            var sources = new ShippingQuerySources(providers, pair.Key);
            var policy = new ShippingReadPolicy(actor, pair.Key, new[] { ids[0], ids[3], childIds[0] });
            await using var app = ShippingReadHost(sources, policy, pair.Key, outbox, authority, secret);
            await app.StartAsync();
            try
            {
                var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(30) };
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                var filters = new[] { new { MetadataPath = new[] { "fixture" }, Operator = "Equal", Scalar = "String", Value = tag } };
                var columns = new[] { new { Alias = "Name", Field = "Name" } };
                async Task<JsonElement> Send(string suffix, object payload, int attempts = 1, HttpStatusCode expected = HttpStatusCode.OK)
                {
                    var key = "read-policy-" + Guid.NewGuid().ToString("N");
                    var path = "/api/data/query-cql" + suffix;
                    await journal("before-hosted-read-policy", new { provider = pair.Key.ToString(), actor, key, path, policy.Mode });
                    using var request = new HttpRequestMessage(HttpMethod.Post, path)
                    { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
                    request.Headers.Add("Idempotency-Key", key);
                    using var response = await client.SendAsync(request);
                    var raw = await response.Content.ReadAsStringAsync();
                    Assert.AreEqual(expected, response.StatusCode, raw);
                    var operationId = response.Headers.GetValues("X-OASIS-Operation-Id").Single();
                    var operation = await outbox.GetAsync(operationId, CancellationToken.None);
                    Assert.AreEqual("settled", operation.State);
                    Assert.AreEqual(actor.ToString("D"), operation.UserId);
                    Assert.AreEqual(attempts, operation.ProviderAttempts.Count);
                    Assert.AreEqual(attempts, operation.Receipts.Count);
                    Assert.AreEqual(attempts * 0.002m, authority.Settlements[operationId].ActualCostUsd);
                    if (attempts == 0) Assert.IsFalse(string.IsNullOrWhiteSpace(operation.NoProviderExecutionReason));
                    foreach (var receipt in operation.Receipts) Assert.AreEqual(pair.Key.ToString(), receipt.Provider);
                    requests++;
                    using var json = JsonDocument.Parse(raw);
                    return json.RootElement.Clone();
                }

                var rows = await Send("", new { Filters = filters, Skip = 0, Take = 1 });
                Assert.AreEqual(ids[0], rows.GetProperty("result").GetProperty("rows")[0].GetProperty("id").GetGuid());
                var scalar = await Send("/scalar", new { Operation = "Count", Filters = filters });
                Assert.AreEqual(2, scalar.GetProperty("result").GetProperty("value").GetInt32());
                var projection = await Send("/projection", new { Filters = filters, Columns = columns });
                Assert.AreEqual(2, projection.GetProperty("result").GetProperty("rows").GetArrayLength());
                var grouped = await Send("/grouped", new { Filters = filters, Keys = columns,
                    Columns = new object[] { new { Alias = "Name", KeyAlias = "Name" }, new { Alias = "Items", Operation = "Count" } } });
                Assert.AreEqual(2, grouped.GetProperty("result").GetProperty("rows").GetArrayLength());
                var joined = await Send("/join", new { Left = new { Filters = filters }, Right = new { Filters = filters },
                    Keys = new[] { new { Alias = "Name", Left = new { Field = "Name" }, Right = new { Field = "Name" } } },
                    Columns = new[] { new { Alias = "Name", Source = "Left", Selection = new { Field = "Name" } } } });
                Assert.AreEqual(2, joined.GetProperty("result").GetProperty("rows").GetArrayLength());
                var composed = await Send("/composition", new { Source = new { Filters = filters, Columns = columns },
                    Stages = new[] { new { Operation = "Take", Count = 1 } } });
                Assert.AreEqual("A-private", composed.GetProperty("result").GetProperty("rows")[0][0].GetString());
                var graph = await Send("", new { Filters = new[] { new { Field = "Id", Operator = "Equal", Scalar = "Guid", Value = ids[3].ToString("D") } },
                    Graph = new { Recursive = true, MaxChildDepth = 2, MaximumNodes = 10, MaximumPayloadBytes = 65536, ChildType = "All", Consistency = "ProviderDefault" } });
                var children = graph.GetProperty("result").GetProperty("rows")[0].GetProperty("children");
                Assert.AreEqual(1, children.GetArrayLength());
                Assert.AreEqual(childIds[0], children[0].GetProperty("id").GetGuid());
                policy.Mode = 1;
                var denied = await Send("", new { Filters = filters }, 0, HttpStatusCode.Forbidden);
                Assert.AreEqual("CQL_ACCESS_FORBIDDEN", denied.GetProperty("errorCode").GetString());
                policy.Mode = 2;
                var revoked = await Send("", new { Filters = filters });
                Assert.AreEqual(0, revoked.GetProperty("result").GetProperty("rows").GetArrayLength());
                var started = authority.Started.Count;
                var calls = policy.Calls;
                client.DefaultRequestHeaders.Authorization = null;
                using var anonymous = await client.PostAsync("/api/data/query-cql", new StringContent("{}", Encoding.UTF8, "application/json"));
                Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);
                requests++;
                Assert.AreEqual(started, authority.Started.Count);
                Assert.AreEqual(calls, policy.Calls);
                Assert.AreEqual(9, calls);
                Assert.AreEqual(9, sources.Lookups, "Every admitted request captures its selected provider once.");
            }
            finally { await app.StopAsync(); }
        }
        return requests;
    }
}

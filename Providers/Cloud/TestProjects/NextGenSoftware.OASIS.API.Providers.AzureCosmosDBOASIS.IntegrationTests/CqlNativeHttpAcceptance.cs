using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

internal static class CqlNativeHttpAcceptance
{
    private sealed class Providers(AzureCosmosDBOASIS provider, HolonManager manager) : ICqlV1Providers
    {
        internal int Lookups;
        public IOASISStorageProvider Resolve(ProviderType? requested)
        {
            Interlocked.Increment(ref Lookups);
            if (requested.HasValue && requested != ProviderType.AzureCosmosDBOASIS || !provider.IsProviderActivated)
                throw new InvalidOperationException("Only the already active owned Cosmos fixture is eligible.");
            return provider;
        }
        public HolonManager CreateManager(IOASISStorageProvider selected)
            => ReferenceEquals(provider, selected) ? manager : throw new InvalidOperationException("Provider substitution rejected.");
    }

    private sealed class Catalogue(Guid actor, Guid record) : ICqlReadAccessPolicy
    {
        public OASISResult<CosmicQueryReadAccess> Authorize(Guid authenticatedActor, ProviderType authority)
        {
            if (authenticatedActor != actor || authority != ProviderType.AzureCosmosDBOASIS)
                throw new InvalidOperationException("Unexpected actor/authority in controlled catalogue.");
            return CosmicQueryReadAccess.AuthoritativeRecords(actor, authority, new[] { record });
        }
    }

    internal static async Task VerifyAsync(AzureCosmosDBOASIS provider, HolonManager manager, Guid actor,
        Guid privateRecord, string expectedName)
    {
        var providers = new Providers(provider, manager);
        var receipts = new ConcurrentBag<UsageProviderReceipt>();
        var failures = new ConcurrentBag<Exception>();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(CqlV1Controller).Assembly.FullName,
            Args = Array.Empty<string>()
        });
        builder.Logging.ClearProviders();
        var serve = Environment.GetEnvironmentVariable("OASIS_COSMOS_CQL_SERVE") == "1";
        builder.WebHost.UseUrls(serve ? "http://127.0.0.1:5088" : "http://127.0.0.1:0");
        builder.Services.AddSingleton<ICqlV1Providers>(providers);
        builder.Services.AddSingleton<ICqlReadAccessPolicy>(new Catalogue(actor, privateRecord));
        builder.Services.AddSingleton(new CqlReadPolicyRequirement(true));
        builder.Services.AddSingleton<CqlV1ResourceFilter>();
        builder.Services.AddControllers().AddApplicationPart(typeof(CqlV1Controller).Assembly);
        await using var app = builder.Build();
        app.UseRouting();
        app.Use(async (http, next) =>
        {
            if (http.Request.Headers.Authorization != "Bearer controlled-cloud-cql")
            {
                await next();
                return;
            }
            // Controlled validated identity/reservation fixture, matching the
            // existing WEB4 host tests. This is not production JWT or billing
            // durability proof. Endpoints, manager and native Cosmos are real.
            try
            {
                var store = new Mock<IUsageOutboxStore>();
                store.Setup(s => s.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
                    .Callback((string id, string owner, UsageProviderReceipt receipt, CancellationToken token) =>
                    {
                        receipt.Validate();
                        receipts.Add(receipt);
                    }).Returns(Task.CompletedTask);
                var handle = new UsageExecutionHandle(new UsageOutboxOperation
                {
                    Id = Guid.NewGuid().ToString("D"), UserId = actor.ToString("D"), ExecutionOwner = "cloud-native-http-test",
                    Policy = new UsageEndpointPolicy { RequiresProviderMeasurement = true }
                }, store.Object, new Web4UsageLedgerOptions(), new SubscriptionAuthorizationResult(), Mock.Of<IUsageReservationAuthority>());
                http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, actor.ToString("D")) }, "controlled-native-test"));
                http.Items["Avatar"] = new Avatar { Id = actor };
                http.Items[Web4UsageMeasurement.ExecutionKey] = handle;
                var prepared = new PreparedStorageExecution(handle, "cosmos-native-http", new Dictionary<ProviderType, string>(),
                    new[] { ProviderType.AzureCosmosDBOASIS }, new[] { new StorageQueryTariff(ProviderType.AzureCosmosDBOASIS, 0.002m, "native-test-v1") });
                await prepared.RunAsync(async () => { await next(); return true; });
            }
            catch (Exception cause) { failures.Add(cause); throw; }
        });
        app.MapControllers();
        var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = 0;
        app.MapGet("/", () => Results.Content(CosmicCqlDemo.CloudCqlDemoPage.Html, "text/html"));
        app.MapGet("/demo/health", () => Volatile.Read(ref ready) == 1
            ? Results.Ok(new { provider = "AzureCosmosDBOASIS", ready = true })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
        app.MapPost("/demo/shutdown", () => { shutdown.TrySetResult(); return Results.Ok(new { stopping = true }); });
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) };
            var page = await client.GetStringAsync("/");
            StringAssert.Contains(page, "CloudRowsWalkthrough.cs");
            StringAssert.Contains(page, "data-query=\"projection\"");
            using var health = await client.GetAsync("/demo/health");
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, health.StatusCode,
                "Interactive readiness must wait for automatic native acceptance.");
            Assert.AreEqual(0, providers.Lookups, "Teaching page discovery must not query storage.");
            using var anonymous = await client.PostAsJsonAsync("/api/data/query-cql", new { });
            Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            Assert.AreEqual(0, providers.Lookups);
            client.DefaultRequestHeaders.Authorization = new("Bearer", "controlled-cloud-cql");
            using var forged = await client.PostAsync("/api/data/query-cql", new StringContent("{\"actorId\":\"forged\"}", Encoding.UTF8, "application/json"));
            Assert.AreEqual(HttpStatusCode.BadRequest, forged.StatusCode,
                string.Join("; ", failures.Select(cause => cause.ToString())));
            Assert.AreEqual(0, providers.Lookups);

            async Task<JsonDocument> Post(string route, object request)
            {
                using var response = await client.PostAsJsonAsync(route, request);
                var body = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, body);
                Assert.IsFalse(body.Contains("providerUniqueStorageKey", StringComparison.OrdinalIgnoreCase));
                return JsonDocument.Parse(body);
            }
            var filters = new[] { new { MetadataPath = new[] { "fixture" }, Scalar = "String", Operator = "Equal", Value = "cosmic-cloud-rows-demo" } };
            using var rows = await Post("/api/data/query-cql", new { Provider = "AzureCosmosDBOASIS", OrderBy = "Id", Take = 10, Filters = filters });
            var rowResult = rows.RootElement.GetProperty("result");
            Assert.AreEqual(1, rowResult.GetProperty("count").GetInt32());
            Assert.AreEqual(privateRecord, rowResult.GetProperty("rows")[0].GetProperty("id").GetGuid());

            using var scalar = await Post("/api/data/query-cql/scalar", new { Provider = "AzureCosmosDBOASIS", OrderBy = "Id", Operation = "LongCount", Filters = filters });
            Assert.AreEqual("1", scalar.RootElement.GetProperty("result").GetProperty("value").GetString());

            using var projection = await Post("/api/data/query-cql/projection", new
            {
                Provider = "AzureCosmosDBOASIS", OrderBy = "Id", Take = 10, Filters = filters,
                Columns = new[] { new { Alias = "name", Field = "Name" } }
            });
            var selected = projection.RootElement.GetProperty("result").GetProperty("rows");
            Assert.AreEqual(1, selected.GetArrayLength());
            Assert.AreEqual(expectedName, selected[0][0].GetString());
            Assert.AreEqual(3, receipts.Count);
            foreach (var receipt in receipts)
            {
                Assert.AreEqual("AzureCosmosDBOASIS", receipt.Provider);
                Assert.AreEqual(1, receipt.Units);
                Assert.AreEqual(0.002m, receipt.ActualCostUsd);
                Assert.AreEqual("native-test-v1", receipt.PricingCatalogueVersion);
            }
            Volatile.Write(ref ready, 1);
            using var readyHealth = await client.GetAsync("/demo/health");
            Assert.AreEqual(HttpStatusCode.OK, readyHealth.StatusCode);
            if (serve)
            {
                Console.WriteLine($"Cosmos DB CQL teaching page ready: {address}/ (POST /demo/shutdown to finish).");
                // Explicit shutdown owns teardown. A bounded one-hour teaching
                // session prevents an abandoned fixture from living forever.
                await shutdown.Task.WaitAsync(TimeSpan.FromHours(1));
            }
            else
            {
                using var stopped = await client.PostAsync("/demo/shutdown", null);
                Assert.AreEqual(HttpStatusCode.OK, stopped.StatusCode);
                Assert.IsTrue(shutdown.Task.IsCompletedSuccessfully);
            }
        }
        finally { await app.StopAsync(); }
    }
}

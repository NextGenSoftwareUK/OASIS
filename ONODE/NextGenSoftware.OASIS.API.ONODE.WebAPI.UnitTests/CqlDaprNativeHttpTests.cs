using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class DaprNativeHttpFactAttribute : FactAttribute
{
    public DaprNativeHttpFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_DAPR_NATIVE_HTTP") != "owned-loopback-54862")
            Skip = "Requires the isolated Dapr/Garnet teaching fixture.";
    }
}

public sealed class CqlDaprNativeHttpTests
{
    private const ProviderType Provider = ProviderType.DaprOASIS;

    [DaprNativeHttpFact]
    public async Task Registered_Dapr_profile_returns_exact_public_rows_over_WEB4_HTTP()
    {
        var sidecarToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
        Assert.False(string.IsNullOrWhiteSpace(sidecarToken));
        var idPrefix = Environment.GetEnvironmentVariable("COSMIC_DAPR_NATIVE_ID_PREFIX")
            ?? throw new InvalidOperationException("The exact owned native fixture identity prefix is required.");
        Assert.Equal(35, idPrefix.Length);
        var actor = Guid.Parse(idPrefix + "0");
        var live = new[] { Guid.Parse(idPrefix + "e"), Guid.Parse(idPrefix + "1"), Guid.Parse(idPrefix + "2") };
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CQL_DAPR_QUERY_STORE"] = "cosmic-teaching",
            ["CQL_DAPR_QUERY_GRPC_ENDPOINT"] = "http://127.0.0.1:54862/",
            ["CQL_DAPR_QUERY_PROBE_ID"] = live[0].ToString("D"),
            ["CQL_DAPR_QUERY_SIDECAR_TOKEN"] = sidecarToken,
            [CqlPublicReadCatalogue.ConfigurationKey] = JsonSerializer.Serialize(new Dictionary<string, Guid[]> { [Provider.ToString()] = live })
        });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers();
        builder.Services.AddConfiguredCqlReadAccessPolicy(builder.Configuration);
        builder.Services.AddDaprReadSource(builder.Configuration);
        await using var app = builder.Build();
        var registry = new CqlHostedSourceRegistry(app.Services.GetServices<CqlReadSourceProfile>());
        await registry.ActivateReadSourceAsync(Provider, default);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var providers = new FixtureProviders(registry, dna, new ProviderManager(null, dna));
        var receipts = new List<UsageProviderReceipt>();
        var outbox = new Mock<IUsageOutboxStore>();
        outbox.Setup(value => value.TryBeginAsync(It.IsAny<UsageOutboxOperation>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        outbox.Setup(value => value.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string _, UsageProviderReceipt receipt, CancellationToken _) => { receipt.Validate(); receipts.Add(receipt); })
            .Returns(Task.CompletedTask);
        var authority = new Mock<IUsageReservationAuthority>();
        authority.Setup(value => value.AuthorizeUsageAsync(It.IsAny<string>(), It.IsAny<UsageAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, UsageAuthorizationRequest request, CancellationToken _) => new SubscriptionAuthorizationResult
            {
                Allowed = true, UserId = actor.ToString("D"), OperationId = request.OperationId,
                Status = "authorized", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
            });
        authority.Setup(value => value.StartUsageAsync(It.IsAny<UsageStartRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsageStartRequest request, CancellationToken _) => new UsageStartResult { Started = true, OperationId = request.OperationId });
        var execution = new Web4UsageExecution(authority.Object, outbox.Object, new Web4UsageLedgerOptions { Service = "WEB4" });
        var fixtureBearer = "Bearer " + Guid.NewGuid().ToString("N");
        app.MapPost("/api/data/query-cql", async (HttpContext http) =>
        {
            if (http.Request.Headers.Authorization != fixtureBearer) { http.Response.StatusCode = 401; return; }
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, actor.ToString("D")) }, "controlled-native-test"));
            http.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = actor };
            http.Request.EnableBuffering();
            var fingerprint = Convert.ToHexString(await SHA256.HashDataAsync(http.Request.Body)).ToLowerInvariant();
            http.Request.Body.Position = 0;
            var handle = await execution.BeginAsync(fixtureBearer, actor.ToString("D"), http.Request.Headers["Idempotency-Key"].ToString(),
                fingerprint, http.Request.Path, new UsageEndpointPolicy { MeterCategory = "cql.native-test", ReservedUnits = 1,
                    PricingCatalogueVersion = "native-test", RequiresProviderMeasurement = true, MaximumProviderAttempts = 1 });
            http.Items[Web4UsageMeasurement.ExecutionKey] = handle;
            var admission = new PreparedStorageExecution(handle, "native-dapr-query", new Dictionary<ProviderType, string>(),
                new[] { Provider }, new[] { new StorageQueryTariff(Provider, 0, "native-test") }, null!, false, null!, new[] { Provider });
            var request = await http.Request.ReadFromJsonAsync<CqlV1Request>();
            var result = await admission.RunAsync(() => CqlV1Endpoint.ExecuteAsync(http, request!, providers));
            await result.ExecuteResultAsync(new ActionContext(http, new RouteData(), new ActionDescriptor()));
        });
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(20) };
            http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", fixtureBearer);
            foreach (var selection in new[] { live, live[..2] })
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/data/query-cql")
                {
                    Content = JsonContent.Create(new { provider = Provider.ToString(), orderBy = "None", take = 8,
                        filters = new[] { new { field = "Id", scalar = "Guid", @operator = "In", value = selection } } })
                };
                request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
                using var response = await http.SendAsync(request);
                Assert.True(response.IsSuccessStatusCode, "HTTP " + (int)response.StatusCode);
                using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
                var rows = body.RootElement.GetProperty("result").GetProperty("rows").EnumerateArray().ToArray();
                Assert.Equal(selection.Order(), rows.Select(row => row.GetProperty("id").GetGuid()).Order());
                Assert.All(rows, row => Assert.True(row.GetProperty("isPublic").GetBoolean()));
            }
            Assert.Equal(2, receipts.Count);
        }
        finally { await app.StopAsync(); }
    }

    private sealed class FixtureProviders(CqlHostedSourceRegistry sources, OASISDNA dna, ProviderManager registry) : ICqlV1Providers
    {
        public IOASISStorageProvider Resolve(ProviderType? requested) => requested == Provider
            ? sources.Find(Provider) : throw new InvalidOperationException("Only Dapr is enrolled in this fixture.");
        public HolonManager CreateManager(IOASISStorageProvider provider) => CqlV1Providers.CreateQueryManager(dna, registry);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Boot = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

[CollectionDefinition("COSMIC hosted global state", DisableParallelization = true)]
public sealed class CosmicHostedGlobalStateCollection { }

[Collection("COSMIC hosted global state")]
public sealed partial class CosmicDataControllerHttpTests
{
    [Fact]
    public void OrdinaryChildWireContractPreservesItsStoredMembershipObservation()
    {
        var child = new Holon { Id = Guid.NewGuid(), ChildRoleManifestJson = "stored-child-membership" };
        var root = new Holon { Id = Guid.NewGuid(), Children = new List<IHolon> { child } };
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };
        NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonJsonContracts.Configure(options);

        var wire = JsonSerializer.Serialize(root, options);
        var decoded = JsonSerializer.Deserialize<Holon>(wire, options)!;

        Assert.Equal(child.ChildRoleManifestJson, Assert.IsType<Holon>(Assert.Single(decoded.Children)).ChildRoleManifestJson);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task WireChildGraphsBindAndHonorTheSelectedSaveDepth(int selection)
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            var root = Guid.NewGuid();
            var child = Guid.NewGuid();
            var foreign = Guid.NewGuid();
            foreach (var id in new[] { root, child, foreign })
                provider.Setup(value => value.LoadHolonAsync(id, false, true, 0, true, false, 0))
                    .ReturnsAsync(new OASISResult<IHolon>(new Holon
                    {
                        Id = id, CreatedByAvatarId = id == foreign ? Guid.NewGuid() : actor
                    }) { IsLoaded = true });
            var revisions = new Dictionary<Guid, Guid?> { [root] = Guid.NewGuid() };
            if (selection != 0) revisions[child] = Guid.NewGuid();

            using var response = await client.PostAsJsonAsync("/api/data/save-holon", new
            {
                holon = new
                {
                    id = root, name = "wire parent",
                    children = new[]
                    {
                        new
                        {
                            id = child, parentHolonId = root, name = "wire child",
                            children = new[] { new { id = foreign, parentHolonId = child, name = "excluded foreign grandchild" } }
                        }
                    }
                },
                operationId = Guid.NewGuid(), expectedRevisions = revisions,
                saveChildren = selection != 0, recursive = selection != 2,
                maxChildDepth = selection == 1 ? 1 : 0
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); // Controlled conditional provider reached.
            var loaded = provider.Invocations.Where(call => call.Method.Name == "LoadHolonAsync")
                .Select(call => (Guid)call.Arguments[0]).ToArray();
            Assert.Equal(selection == 0 ? new[] { root } : new[] { root, child }, loaded);
            Assert.Single(provider.Invocations.Where(call => call.Method.Name == "ExecuteConditionalHolonWriteAsync"));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectedPricedSavesRecordAffirmativeNoProviderExecution(bool malformed)
    {
        var recorded = false;
        await WithHost(true, async (client, actor, provider) =>
        {
            using var response = malformed
                ? await client.PostAsync("/api/data/save-holon",
                    new StringContent("{", System.Text.Encoding.UTF8, "application/json"))
                : await client.PostAsJsonAsync("/api/data/save-holon", new { holon = new { name = "missing intent" } });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True(recorded, "A priced rejection needs affirmative evidence before its reservation can settle.");
            Assert.Empty(provider.Invocations.Where(call => call.Method.Name.Contains("Load") ||
                call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional")));
        }, admitted: true, configureOutbox: store =>
            store.Setup(value => value.RecordNoProviderExecutionAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback(() => recorded = true).Returns(Task.CompletedTask));
    }

    [Fact]
    public async Task AnActualOwnershipAttemptIsNeverReportedAsZeroExecution()
    {
        var recorded = false;
        await WithHost(true, async (client, actor, provider) =>
        {
            var id = Guid.NewGuid();
            provider.Setup(value => value.LoadHolonAsync(id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(new Holon
                {
                    Id = id, CreatedByAvatarId = Guid.NewGuid()
                }) { IsLoaded = true });

            using var response = await client.PostAsJsonAsync("/api/data/save-holon", new
            {
                holon = new { id, name = "denied after lookup" }, operationId = Guid.NewGuid(), saveChildren = false
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.False(recorded);
            Assert.Contains(provider.Invocations, call => call.Method.Name == "LoadHolonAsync");
        }, admitted: true, configureOutbox: store =>
            store.Setup(value => value.RecordNoProviderExecutionAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback(() => recorded = true).Returns(Task.CompletedTask));
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(false, "unsupported-mode")]
    [InlineData(true, "unsupported-mode")]
    public async Task UnknownEngineModeNeverSelectsAnUnconditionalWrite(bool bulk, string? mode)
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            if (mode == null) Boot.OASISDNA = null!;
            else Boot.OASISDNA.OASIS.HyperDriveMode = mode;
            var record = new { name = "cold-start intent" };
            object body = bulk
                ? new { holons = new[] { record }, operationId = Guid.NewGuid() }
                : new { holon = record, operationId = Guid.NewGuid() };

            using var response = await client.PostAsJsonAsync(
                bulk ? "/api/data/save-holons" : "/api/data/save-holon", body);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("COSMIC_WRITE_ENGINE_MODE_REQUIRED",
                json.RootElement.GetProperty("result").GetProperty("errorCode").GetString());
            Assert.Empty(provider.Invocations.Where(call => call.Method.Name.Contains("Load") ||
                call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional")));
        });
    }

    [Theory]
    [InlineData(false, "autoReplicationMode")]
    [InlineData(true, "autoReplicationMode")]
    [InlineData(false, "autoFailOverMode")]
    [InlineData(true, "autoFailOverMode")]
    [InlineData(false, "autoLoadBalanceMode")]
    [InlineData(true, "autoLoadBalanceMode")]
    [InlineData(false, "autoFailOverProviders")]
    [InlineData(true, "autoFailOverProviders")]
    public async Task InvalidEngineOptionsReturnActualBadRequestWithoutChangingSettings(bool bulk, string field)
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            ProviderManager.Instance.IsAutoReplicationEnabled = true;
            var body = new Dictionary<string, object>
            {
                [bulk ? "holons" : "holon"] = bulk ? new[] { new { name = "invalid options" } } : new { name = "invalid options" },
                ["operationId"] = Guid.NewGuid(), ["setGlobally"] = true,
                [field] = "not-an-accepted-value"
            };
            using var response = await client.PostAsJsonAsync(
                bulk ? "/api/data/save-holons" : "/api/data/save-holon", body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True(ProviderManager.Instance.IsAutoReplicationEnabled);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("COSMIC_WRITE_CONFIGURATION_INVALID",
                json.RootElement.GetProperty("result").GetProperty("errorCode").GetString());
            Assert.True(json.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
            Assert.Empty(provider.Invocations.Where(call => call.Method.Name.Contains("Load") ||
                call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional")));
        });
    }

    [Fact]
    public async Task AdmittedExplicitProviderActivatesOnlyAfterItsPhysicalAttemptBegins()
    {
        await WithHost(true, async (client, actor, provider) =>
        {
            var id = Guid.NewGuid();
            provider.Object.IsProviderActivated = false;
            provider.Setup(value => value.LoadHolonAsync(id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(new Holon
                {
                    Id = id, CreatedByAvatarId = Guid.NewGuid()
                }) { IsLoaded = true });

            using var response = await client.PostAsJsonAsync("/api/data/save-holon", new
            {
                holon = new { id, name = "admitted authority lookup" },
                operationId = Guid.NewGuid(), saveChildren = false,
                providerType = "SQLLiteDBOASIS"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            provider.Verify(value => value.ActivateProviderAsync(), Times.Once);
        }, admitted: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidIntentCannotChangeGlobalSettingsEvenWhenSetGloballyWasRequested(bool bulk)
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            ProviderManager.Instance.IsAutoReplicationEnabled = true;
            var record = new { id = Guid.NewGuid(), name = "invalid intent" };
            object body = bulk
                ? new { holons = new[] { record }, setGlobally = true, autoReplicationMode = "OFF" }
                : new { holon = record, setGlobally = true, autoReplicationMode = "OFF" };

            using var response = await client.PostAsJsonAsync(
                bulk ? "/api/data/save-holons" : "/api/data/save-holon", body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True(ProviderManager.Instance.IsAutoReplicationEnabled);
            Assert.Empty(provider.Invocations.Where(call => call.Method.Name.Contains("Load") ||
                call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional")));
        });
    }

    [Fact]
    public async Task ConfiguredSingleSaveAliasCannotExecuteWithoutItsOwnAdmission()
    {
        await WithHost(true, async (client, actor, provider) =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                "/api/data/save-holon/false/true/0/true")
            {
                Content = JsonContent.Create(new { name = "unenrolled alias" })
            };
            request.Headers.Add("X-COSMIC-Operation-Id", Guid.NewGuid().ToString("D"));

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("COSMIC_WRITE_ADMISSION_REQUIRED",
                json.RootElement.GetProperty("result").GetProperty("errorCode").GetString());
            Assert.Empty(provider.Invocations.Where(call => call.Method.Name.Contains("Load") ||
                call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional")));
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task RejectedWritesReportRequestSettingsWithoutMutatingGlobalPolicy(bool bulk, bool lookupThrows)
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            var id = Guid.NewGuid();
            var sawUnchangedGlobalPolicy = false;
            provider.Setup(value => value.LoadHolonAsync(id, false, true, 0, true, false, 0))
                .ReturnsAsync(() =>
                {
                    sawUnchangedGlobalPolicy = ProviderManager.Instance.IsAutoReplicationEnabled;
                    if (lookupThrows)
                        throw new IOException("Controlled authority outage");
                    return new OASISResult<IHolon>(new Holon
                    {
                        Id = id, CreatedByAvatarId = Guid.NewGuid()
                    }) { IsLoaded = true };
                });
            ProviderManager.Instance.IsAutoReplicationEnabled = true;
            var record = new { id, name = "forged owner", createdByAvatarId = actor };
            object body = bulk
                ? new { holons = new[] { record }, operationId = Guid.NewGuid(), saveChildren = false, autoReplicationMode = "OFF" }
                : new { holon = record, operationId = Guid.NewGuid(), saveChildren = false, autoReplicationMode = "OFF" };

            using var response = await client.PostAsJsonAsync(
                bulk ? "/api/data/save-holons" : "/api/data/save-holon", body);

            Assert.Equal(lookupThrows ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.Forbidden, response.StatusCode);
            Assert.True(sawUnchangedGlobalPolicy);
            Assert.True(ProviderManager.Instance.IsAutoReplicationEnabled);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.False(json.RootElement.GetProperty("autoReplicationEnabled").GetBoolean());
            Assert.Equal(lookupThrows ? "COSMIC_WRITE_OWNERSHIP_LOOKUP_FAILED" : "COSMIC_WRITE_FORBIDDEN",
                json.RootElement.GetProperty("result").GetProperty("errorCode").GetString());
            Assert.Empty(provider.Invocations.Where(call => call.Method.Name.Contains("Save") ||
                call.Method.Name.Contains("Conditional")));
        });
    }

    [Fact]
    public async Task ConditionalProviderConflictIsAnActualHttpConflict()
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            using var response = await client.PostAsJsonAsync("/api/data/save-holon", new
            {
                holon = new { name = "conditional create" },
                operationId = Guid.NewGuid(), saveChildren = false
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(CosmicConcurrencyException.Code,
                json.RootElement.GetProperty("result").GetProperty("errorCode").GetString());
            Assert.Single(provider.Invocations.Where(call => call.Method.Name == "ExecuteConditionalHolonWriteAsync"));
        });
    }

    private static async Task WithHost(bool configuredAdmission,
        Func<HttpClient, Guid, Mock<IOASISStorageProvider>, Task> test, bool admitted = false,
        Action<Mock<IUsageOutboxStore>>? configureOutbox = null)
    {
        var previousDna = Boot.OASISDNA;
        var globals = ProviderManager.Instance;
        var previousReplication = globals.IsAutoReplicationEnabled;
        var actor = Guid.NewGuid();
        var dna = new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = HyperDriveModes.V2,
                StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
            }
        };
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.As<IConditionalHolonStorageProvider>()
            .Setup(value => value.ExecuteConditionalHolonWriteAsync(
                It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true, ErrorCode = CosmicConcurrencyException.Code, Message = "Controlled conflict"
            });
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.IsProviderActivated = true;
        provider.Setup(value => value.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var providers = new ProviderManager(null, dna);
        providers.RegisterProvider(provider.Object);
        Assert.False(providers.SetAndActivateCurrentStorageProvider(provider.Object).IsError);
        // This host owns an isolated SQLite provider, independently of the global
        // registry's default. Declare that affinity explicitly so a captured
        // missing global default cannot supply or replace this fixture's authority.
        var manager = new HolonManager(provider.Object, dna, providers);
        PreparedStorageExecution? admission = null;
        UsageExecutionHandle? admittedHandle = null;
        if (admitted)
        {
            var attemptStarted = false;
            var outbox = new Mock<IUsageOutboxStore>();
            configureOutbox?.Invoke(outbox);
            outbox.Setup(value => value.TryBeginAsync(It.IsAny<UsageOutboxOperation>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            outbox.Setup(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()))
                .Callback(() => attemptStarted = true).Returns(Task.CompletedTask);
            provider.Setup(value => value.ActivateProviderAsync()).ReturnsAsync(() =>
            {
                Assert.True(attemptStarted, "Provider activation must follow the admitted physical attempt.");
                provider.Object.IsProviderActivated = true;
                return new OASISResult<bool>(true);
            });
            var authority = new Mock<IUsageReservationAuthority>();
            authority.Setup(value => value.AuthorizeUsageAsync(It.IsAny<string>(),
                    It.IsAny<UsageAuthorizationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string bearer, UsageAuthorizationRequest request, CancellationToken token) =>
                    new SubscriptionAuthorizationResult
                    {
                        Allowed = true, UserId = actor.ToString("D"), OperationId = request.OperationId,
                        Status = "authorized", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
                    });
            authority.Setup(value => value.StartUsageAsync(It.IsAny<UsageStartRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UsageStartRequest request, CancellationToken token) =>
                    new UsageStartResult { Started = true, OperationId = request.OperationId });
            var execution = new Web4UsageExecution(authority.Object, outbox.Object,
                new Web4UsageLedgerOptions { Service = "WEB4-http-fixture" });
            var handle = await execution.BeginAsync("controlled-bearer", actor.ToString("D"),
                "http-activation-fixture", new string('a', 64), "POST /api/data/save-holon",
                new UsageEndpointPolicy
                {
                    MeterCategory = "fixture-write", PricingCatalogueVersion = "fixture-v1",
                    RequiresProviderMeasurement = true, MaximumProviderAttempts = 10
                });
            admittedHandle = handle;
            admission = new PreparedStorageExecution(handle, "http-activation",
                new Dictionary<ProviderType, string>(), new[] { ProviderType.SQLLiteDBOASIS });
        }

        try
        {
            Boot.OASISDNA = dna;
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Same wire options as Startup, so tests post what real clients post.
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: true));
                    NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonJsonContracts.Configure(options.JsonSerializerOptions);
                })
                .AddApplicationPart(typeof(DataController).Assembly)
                .AddControllersAsServices();
            builder.Services.AddTransient<DataController>(_ => new DataController(manager));
            if (admission != null)
                builder.Services.AddSingleton<IPreparedStorageAdmissionFactory>(new FixtureAdmissionFactory(admission));
            if (configuredAdmission)
                builder.Services.AddSingleton(new StorageAdmissionRoutes(new[]
                {
                    new StorageAdmissionRoute("POST", "/api/data/save-holon", new UsageEndpointPolicy
                    {
                        MeterCategory = "fixture-write", PricingCatalogueVersion = "fixture-v1",
                        ReservedUnits = 1, EstimatedCostUsd = 1, MaximumNativeFeeCostUsd = 0,
                        RequiresProviderMeasurement = true, MaximumProviderAttempts = 10
                    })
                }));

            await using var app = builder.Build();
            app.Use(async (context, next) =>
            {
                // Auth is controlled at the host boundary; the real shipping
                // AuthorizeAttribute, model binder and DataController still run.
                context.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = actor };
                if (admittedHandle != null)
                {
                    context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                        new[] { new System.Security.Claims.Claim("sub", actor.ToString("D")) }, "controlled-http-fixture"));
                    context.Items[Web4UsageMeasurement.ExecutionKey] = admittedHandle;
                }
                await next(context);
            });
            if (admission != null)
                app.UseMiddleware<PreparedStorageAdmissionMiddleware>();
            app.MapControllers();
            await app.StartAsync();
            try
            {
                var address = app.Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
                await test(client, actor, provider);
            }
            finally { await app.StopAsync(); }
        }
        finally
        {
            globals.IsAutoReplicationEnabled = previousReplication;
            Boot.OASISDNA = previousDna;
        }
    }

    private sealed class FixtureAdmissionFactory(PreparedStorageExecution policy) : IPreparedStorageAdmissionFactory
    {
        public Task<PreparedStorageExecution> CreateAsync(HttpContext context, UsageExecutionHandle execution,
            CancellationToken cancellationToken) => Task.FromResult(policy);
    }
}

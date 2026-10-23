using System.IdentityModel.Tokens.Jwt;
using System.Collections.Concurrent;
using CosmicCqlDemo;
using System.Security.Claims;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    // The reservation authority is controlled; authentication, native reads and the Mongo
    // execution outbox are real. No billing account, external identity or charge is involved.
    private sealed class Authority : IUsageReservationAuthority
    {
        public readonly Dictionary<string, Guid> Tokens = new();
        public readonly ConcurrentDictionary<string, UsageSettlementRequest> Settlements = new();
        public readonly ConcurrentDictionary<string, byte> Started = new();

        public Task<SubscriptionAuthorizationResult> AuthorizeUsageAsync(string bearer,
            UsageAuthorizationRequest request, CancellationToken cancellationToken = default)
        {
            Assert.AreEqual("WEB4", request.ConsumingService);
            Assert.IsTrue(Tokens.TryGetValue(bearer, out var actor), "Only issued fixture identities may reserve.");
            return Task.FromResult(new SubscriptionAuthorizationResult
            {
                Allowed = true, StatusCode = 200, Status = "authorized",
                UserId = actor.ToString("D"), OperationId = request.OperationId,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
            });
        }

        public Task<UsageStartResult> StartUsageAsync(UsageStartRequest request, CancellationToken cancellationToken = default)
        {
            Assert.IsTrue(Started.TryAdd(request.OperationId, 0), "A duplicate must never start execution again.");
            return Task.FromResult(new UsageStartResult { Started = true, Status = "executing", OperationId = request.OperationId });
        }

        public Task<UsageSettlementResult> SettleUsageAsync(UsageSettlementRequest request, CancellationToken cancellationToken = default)
        {
            Assert.IsTrue(Started.ContainsKey(request.OperationId));
            Assert.IsTrue(Settlements.TryAdd(request.OperationId, request), "One awaited settlement per fixture operation.");
            return Task.FromResult(new UsageSettlementResult { Settled = true, OperationId = request.OperationId });
        }
    }

    private sealed class Sources(Dictionary<ProviderType, IOASISStorageProvider> providers) : ICqlV1Providers
    {
        public IOASISStorageProvider Resolve(ProviderType? requested) => providers[requested ?? ProviderType.SQLLiteDBOASIS];

        public HolonManager CreateManager(IOASISStorageProvider storage)
        {
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = HyperDriveModes.V2,
                StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
            }};
            var registry = new ProviderManager(null, dna)
            {
                IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false
            };
            return new HolonManager(storage, dna, registry);
        }
    }

    private static WebApplication Host(Dictionary<ProviderType, IOASISStorageProvider> providers,
        MongoUsageOutboxStore outbox, Authority authority, string signingSecret,
        V1DemoSession? demo = null, Func<Guid, string, Task>? journalRequest = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(CqlV1Controller).Assembly.FullName, Args = Array.Empty<string>()
        });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("OASIS_CQL_V1_DEMO_MODE") == "serve"
            ? "http://127.0.0.1:5088" : "http://127.0.0.1:0");
        if (demo != null)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SchemaFilter<V1SwaggerExample>();
                options.OperationFilter<V1SwaggerExample>();
                options.DocInclusionPredicate((_, description) => description.RelativePath is "api/data/query-cql" or "api/data/query-cql/scalar");
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "CQL v1 · MongoDB / SQLite / Neo4j", Version = "v1",
                    Description = "Synthetic loopback fixture. Use /demo for guided examples; the query controller is the shipping WEB4 implementation."
                });
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
                });
                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    [new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
                    }] = Array.Empty<string>()
                });
            });
        }
        builder.Services.AddSingleton<ICqlV1Providers>(new Sources(providers));
        builder.Services.AddSingleton<CqlV1ResourceFilter>();
        builder.Services.AddSingleton<IUsageOutboxStore>(outbox);
        builder.Services.AddSingleton<IUsageReservationAuthority>(authority);
        builder.Services.AddSingleton(new Web4UsageLedgerOptions
        {
            Service = "WEB4", EndpointPolicy = http => IsQueryRoute(http) ? new UsageEndpointPolicy
            {
                MeterCategory = "cql.query", ReservedUnits = 1, EstimatedCostUsd = 0.002m,
                PricingCatalogueVersion = "owned-fixture-v1", RequiresProviderMeasurement = true,
                MaximumProviderAttempts = 1, MaximumNativeFeeCostUsd = 0
            } : null
        });
        builder.Services.AddSingleton<Web4UsageExecution>();
        builder.Services.AddSingleton<IPreparedStorageAdmissionFactory>(new PricedStorageAdmissionFactory(
            null, Array.Empty<NativeStorageAdmissionBinding>(), providers.Keys,
            providers.Keys.Select(provider => new StorageQueryTariff(provider, 0.002m, "owned-fixture-v1"))));
        builder.Services.AddControllers(options => options.Filters.Add<UsageResultFilter>())
            .AddApplicationPart(typeof(CqlV1Controller).Assembly);

        var app = builder.Build();
        app.UseRouting();
        if (demo != null)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "CQL v1");
                // Fetch at dispatch, after the Swagger spec is loaded. A page-load
                // authorization hook races spec initialization and loses the token.
                // Swashbuckle 6 parses function(args) and embeds its source in a
                // JSON.parse string literal. Supply one line to avoid literal
                // newline control characters after the JavaScript unescaping.
                options.UseRequestInterceptor("""
                    function(request) {
                      return (async () => {
                        const target = new URL(request.url, window.location.origin);
                        if (target.origin === window.location.origin &&
                            ['/api/data/query-cql', '/api/data/query-cql/scalar'].includes(target.pathname) && !request.headers.Authorization) {
                            const response = await fetch('/demo/session');
                            if (!response.ok) throw new Error('Demo session is not ready.');
                            const session = await response.json();
                            request.headers.Authorization = 'Bearer ' + session.tokens.Alice;
                        }
                        return request;
                      })();
                    }
                    """.ReplaceLineEndings(" "));
            });
            V1DemoPresentation.Map(app, demo);
        }
        app.Use(async (http, next) =>
        {
            // Reuse the shipping validator, including its matching sub/id claim rule.
            // Avatar-directory resolution and reservation authority are fixture-owned;
            // this is not a boot of the entire production application or a paid account.
            if (AuthenticationHeaderValue.TryParse(http.Request.Headers.Authorization, out var header) &&
                header.Scheme == "Bearer" && header.Parameter != null)
            {
                try
                {
                    http.User = OasisJwtValidation.Validate(header.Parameter, signingSecret, "owned-cql-v1-fixture");
                    var actor = Guid.Parse(http.User.FindFirstValue("sub")!);
                    http.Items["Avatar"] = new Avatar { Id = actor };
                }
                catch (Exception error) when (error is SecurityTokenException or ArgumentException or InvalidOperationException)
                {
                    // A rejected signature/claim remains anonymous and is rejected by
                    // the actual usage middleware before reservation or provider work.
                    http.User = new ClaimsPrincipal();
                }
            }
            if (demo != null && journalRequest != null && IsQueryRoute(http))
            {
                var actor = Guid.TryParse(http.User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
                await journalRequest(actor, http.Request.Headers["Idempotency-Key"].ToString());
            }
            await next();
        });
        app.UseMiddleware<Web4UsageLedgerMiddleware>();
        app.UseWhen(http => IsQueryRoute(http),
            branch => branch.UseMiddleware<PreparedStorageAdmissionMiddleware>());
        app.MapControllers();
        return app;
    }

    private static bool IsQueryRoute(HttpContext http)
        => http.Request.Path == "/api/data/query-cql" || http.Request.Path == "/api/data/query-cql/scalar";

    private static string Token(Guid actor, SymmetricSecurityKey key, string issuer = "owned-cql-v1-fixture",
        bool expired = false, Guid? claimId = null, int lifetimeMinutes = 5) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer, issuer, new[] { new Claim("sub", actor.ToString("D")), new Claim("id", (claimId ?? actor).ToString("D")) },
            DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(expired ? -1 : lifetimeMinutes),
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
}

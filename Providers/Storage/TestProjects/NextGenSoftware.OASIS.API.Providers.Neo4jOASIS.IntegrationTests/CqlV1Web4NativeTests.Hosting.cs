using System.IdentityModel.Tokens.Jwt;
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
        public readonly Dictionary<string, UsageSettlementRequest> Settlements = new();
        public readonly HashSet<string> Started = new();

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
            Assert.IsTrue(Started.Add(request.OperationId), "A duplicate must never start execution again.");
            return Task.FromResult(new UsageStartResult { Started = true, Status = "executing", OperationId = request.OperationId });
        }

        public Task<UsageSettlementResult> SettleUsageAsync(UsageSettlementRequest request, CancellationToken cancellationToken = default)
        {
            Assert.IsTrue(Started.Contains(request.OperationId));
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
        MongoUsageOutboxStore outbox, Authority authority, string signingSecret)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(CqlV1Controller).Assembly.FullName, Args = Array.Empty<string>()
        });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICqlV1Providers>(new Sources(providers));
        builder.Services.AddSingleton<CqlV1ResourceFilter>();
        builder.Services.AddSingleton<IUsageOutboxStore>(outbox);
        builder.Services.AddSingleton<IUsageReservationAuthority>(authority);
        builder.Services.AddSingleton(new Web4UsageLedgerOptions
        {
            Service = "WEB4", EndpointPolicy = _ => new UsageEndpointPolicy
            {
                MeterCategory = "cql.query", ReservedUnits = 1, EstimatedCostUsd = 0.002m,
                PricingCatalogueVersion = "owned-fixture-v1", RequiresProviderMeasurement = true,
                MaximumProviderAttempts = 1, MaximumNativeFeeCostUsd = 0
            }
        });
        builder.Services.AddSingleton<Web4UsageExecution>();
        builder.Services.AddSingleton<IPreparedStorageAdmissionFactory>(new PricedStorageAdmissionFactory(
            null, Array.Empty<NativeStorageAdmissionBinding>(), providers.Keys,
            providers.Keys.Select(provider => new StorageQueryTariff(provider, 0.002m, "owned-fixture-v1"))));
        builder.Services.AddControllers(options => options.Filters.Add<UsageResultFilter>())
            .AddApplicationPart(typeof(CqlV1Controller).Assembly);

        var app = builder.Build();
        app.UseRouting();
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
            await next();
        });
        app.UseMiddleware<Web4UsageLedgerMiddleware>();
        app.UseMiddleware<PreparedStorageAdmissionMiddleware>();
        app.MapControllers();
        return app;
    }

    private static string Token(Guid actor, SymmetricSecurityKey key, string issuer = "owned-cql-v1-fixture",
        bool expired = false, Guid? claimId = null) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer, issuer, new[] { new Claim("sub", actor.ToString("D")), new Claim("id", (claimId ?? actor).ToString("D")) },
            DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(expired ? -1 : 5),
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
}

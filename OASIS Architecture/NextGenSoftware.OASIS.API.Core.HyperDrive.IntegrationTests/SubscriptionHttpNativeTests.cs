using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    // Real persisted billing reads, not a Stripe checkout/webhook or production Startup proof.
    [Fact]
    public async Task SubscriptionSelfReadsUseActualControllerAndOwnedMongo()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var mongo = new MongoClient(MongoUri);
        var databaseName = "subscription_http_" + Guid.NewGuid().ToString("N");
        Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env")))
            root = root.Parent;
        Assert.NotNull(root);
        var configuration = typeof(AvatarProfileCommitStampNativeTests).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var project = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI");
        var artifact = Path.Combine(project, "bin", configuration, "net10.0", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact), "Build the actual WEB4 API before this test.");
        Assert.True(File.GetLastWriteTimeUtc(artifact) >= File.GetLastWriteTimeUtc(
            Path.Combine(project, "Controllers/SubscriptionController.cs")), "API artifact is stale.");
        var resolver = new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            var path = resolver.ResolveAssemblyToPath(name);
            return path == null ? null : context.LoadFromAssemblyPath(path);
        }
        NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS? avatarStore = null;
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            const string ns = "NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.";
            var repositoryType = api.GetType(ns + "MongoSubscriptionUsageRepository", true)!;
            var repository = Activator.CreateInstance(repositoryType,
                [mongo, databaseName, TimeProvider.System, null, false, null, null])!;
            var serviceType = api.GetType(ns + "SubscriptionService", true)!;
            var logger = typeof(NullLogger<>).MakeGenericType(serviceType).GetField("Instance")!.GetValue(null)!;
            var service = Activator.CreateInstance(serviceType, [logger, repository, repository, TimeProvider.System])!;
            var controller = api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SubscriptionController", true)!;
            var db = mongo.GetDatabase(databaseName);
            var accounts = new[] { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") };
            var missingAccount = Guid.NewGuid().ToString("D");
            avatarStore = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(MongoUri, databaseName);
            var activated = await avatarStore.ActivateProviderAsync();
            Assert.False(activated.IsError, activated.Message);
            foreach (var account in accounts.Append(missingAccount))
            {
                var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar
                {
                    Id = Guid.Parse(account), Username = "subscription-http-" + account,
                    Email = account + "@example.test", IsActive = true
                };
                avatar.AvatarType.Value = NextGenSoftware.OASIS.API.Core.Enums.AvatarType.User;
                var saved = await avatarStore.SaveAvatarAsync(avatar);
                Assert.False(saved.IsError, saved.Message);
            }
            var end = DateTime.UtcNow.AddDays(10);
            for (var i = 0; i < accounts.Length; i++)
            {
                // Seed owned native fixtures; these are not Stripe-issued payments.
                await db.GetCollection<BsonDocument>("subscription_records").InsertOneAsync(new BsonDocument
                {
                    { "_id", accounts[i] }, { "PlanId", i == 0 ? "bronze" : "silver" },
                    { "Status", "active" }, { "CancelAtPeriodEnd", i == 0 },
                    { "CurrentPeriodEnd", new BsonDateTime(end) }
                });
                await db.GetCollection<BsonDocument>("subscription_orders").InsertOneAsync(new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString("D") }, { "UserId", accounts[i] },
                    { "PlanId", i == 0 ? "bronze" : "silver" },
                    { "StripeInvoiceId", "owned-fixture-" + accounts[i] },
                    { "Amount", new BsonDecimal128(i == 0 ? 9m : 29m) }, { "Currency", "GBP" },
                    { "Status", "paid" }, { "CreatedAt", new BsonDateTime(DateTime.UtcNow) }
                });
            }
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            builder.Services.AddAuthorization();
            builder.Services.AddControllers().ConfigureApplicationPartManager(parts =>
            {
                parts.ApplicationParts.Clear();
                parts.ApplicationParts.Add(new AssemblyPart(api));
                parts.FeatureProviders.Add(new ProfileControllerFeature(controller));
            }).AddControllersAsServices();
            builder.Services.AddSingleton(api.GetType(ns + "ISubscriptionService", true)!, service);
            builder.Services.AddSingleton(api.GetType(ns + "ISubscriptionUsageRepository", true)!, repository);
            builder.Services.AddSingleton(api.GetType(ns + "ISubscriptionBillingRepository", true)!, repository);
            await using var app = builder.Build();
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
            var validation = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true, IssuerSigningKey = key,
                ValidateIssuer = true, ValidIssuer = "owned-subscription-fixture",
                ValidateAudience = true, ValidAudience = "owned-subscription-http",
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero
            };
            app.Use(async (context, next) =>
            {
                var header = context.Request.Headers.Authorization.ToString();
                if (!header.StartsWith("Bearer ", StringComparison.Ordinal))
                { context.Response.StatusCode = 401; return; }
                try
                {
                    var principal = new JwtSecurityTokenHandler().ValidateToken(header[7..], validation, out _);
                    var actorId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
                    if (!Guid.TryParseExact(actorId, "D", out var id)) { context.Response.StatusCode = 401; return; }
                    var actor = await avatarStore.LoadAvatarAsync(id);
                    if (actor.IsError || actor.Result == null) { context.Response.StatusCode = 401; return; }
                    context.User = principal;
                    context.Items["Avatar"] = actor.Result;
                }
                catch (SecurityTokenException) { context.Response.StatusCode = 401; return; }
                await next(context);
            });
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();
            try
            {
                var address = app.Services.GetRequiredService<IServer>().Features
                    .Get<IServerAddressesFeature>()!.Addresses.Single();
                using var http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(20) };
                Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/subscription/subscriptions/me")).StatusCode);
                string Token(string actor) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                    issuer: "owned-subscription-fixture", audience: "owned-subscription-http",
                    claims: [new Claim("sub", actor)], expires: DateTime.UtcNow.AddMinutes(5),
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
                async Task<JsonDocument> Read(string path)
                {
                    using var response = await http.GetAsync(path);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                }
                for (var i = 0; i < accounts.Length; i++)
                {
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(accounts[i]));
                    using var subscriptions = await Read("/api/subscription/subscriptions/me?userId=" + accounts[1 - i]);
                    var record = Assert.Single(subscriptions.RootElement.GetProperty("result").EnumerateArray());
                    Assert.Equal(i == 0 ? "bronze" : "silver", record.GetProperty("planId").GetString());
                    Assert.Equal(i == 0, record.GetProperty("cancelAtPeriodEnd").GetBoolean());
                    Assert.Equal(i == 0 ? JsonValueKind.Null : JsonValueKind.String, record.GetProperty("renewsOn").ValueKind);
                    Assert.Equal(i == 0 ? JsonValueKind.String : JsonValueKind.Null, record.GetProperty("endsOn").ValueKind);
                    using var orders = await Read("/api/subscription/orders/me?userId=" + accounts[1 - i]);
                    var order = Assert.Single(orders.RootElement.GetProperty("result").EnumerateArray());
                    Assert.Equal(accounts[i], order.GetProperty("userId").GetString());
                    Assert.Equal(i == 0 ? 9m : 29m, order.GetProperty("amount").GetDecimal());
                    Assert.Equal("GBP", order.GetProperty("currency").GetString());
                }
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(missingAccount));
                using var missing = await Read("/api/subscription/subscriptions/me");
                Assert.Empty(missing.RootElement.GetProperty("result").EnumerateArray());
                using var missingOrders = await Read("/api/subscription/orders/me");
                Assert.Empty(missingOrders.RootElement.GetProperty("result").EnumerateArray());
                Assert.Equal(2L, await db.GetCollection<BsonDocument>("subscription_records").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                Assert.Equal(2L, await db.GetCollection<BsonDocument>("subscription_orders").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            }
            finally { await app.StopAsync(); }
        }
        finally
        {
            AssemblyLoadContext.Default.Resolving -= Resolve;
            Assert.StartsWith("subscription_http_", databaseName);
            if (avatarStore != null)
            {
                var stopped = await avatarStore.DeActivateProviderAsync();
                Assert.False(stopped.IsError, stopped.Message);
            }
            await mongo.DropDatabaseAsync(databaseName);
            Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
        }
    }
}

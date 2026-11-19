using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    public Task SubscriptionSelfReadsUseActualControllerAndOwnedMongo() => RunSubscriptionHttp(false);

    [Fact]
    public Task SubscriptionBrowserUsesActualControllerAndOwnedMongo() => RunSubscriptionHttp(true);

    [Fact]
    public Task SubscriptionUsageBrowserUsesActualControllerAndOwnedMongo() => RunSubscriptionHttp(true, true);

    [Fact]
    public Task SubscriptionNotificationPreferencesBrowserUsesActualControllerAndOwnedMongo() => RunSubscriptionHttp(true, true, true);

    [Fact]
    public Task SubscriptionAppOwnerSettingsBrowserUsesActualControllerAndOwnedMongo() => RunSubscriptionHttp(true, true, true, true);

    [Fact]
    public Task SubscriptionAppOwnerUsageBrowserUsesActualControllerAndOwnedMongo() => RunSubscriptionHttp(true, true, true, true, true);

    [Fact]
    public Task SubscriptionAppBillingReadsBrowserUsesActualControllerAndOwnedMongo() => RunSubscriptionHttp(true, true, true, true, false, true);

    private async Task RunSubscriptionHttp(bool browser, bool usage = false, bool notifications = false, bool ownerSettings = false, bool ownerUsage = false, bool ownerBilling = false)
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
        if (usage)
        {
            var shippedPrices = Path.Combine(Path.GetDirectoryName(artifact)!, "Configuration/subscription-overage-prices.json");
            var hostedPrices = Path.Combine(AppContext.BaseDirectory, "Configuration/subscription-overage-prices.json");
            Assert.True(File.Exists(shippedPrices) && File.Exists(hostedPrices), "Both hosts must include the real shipping pricing catalogue.");
            Assert.Equal(SHA256.HashData(await File.ReadAllBytesAsync(shippedPrices)),
                SHA256.HashData(await File.ReadAllBytesAsync(hostedPrices)));
        }
        var resolver = new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            var path = resolver.ResolveAssemblyToPath(name);
            return path == null ? null : context.LoadFromAssemblyPath(path);
        }
        NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS? avatarStore = null;
        FieldInfo? avatarManagerField = null;
        object? previousAvatarManager = null;
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            const string ns = "NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.";
            var repositoryType = api.GetType(ns + "MongoSubscriptionUsageRepository", true)!;
            var repository = Activator.CreateInstance(repositoryType,
                [mongo, databaseName, TimeProvider.System, null, false, null])!;
            if (ownerBilling)
            {
                var shipping = await File.ReadAllBytesAsync(Path.Combine(Path.GetDirectoryName(artifact)!, "Configuration/app-overage-prices.json"));
                Assert.Equal(SHA256.HashData(shipping), SHA256.HashData(await File.ReadAllBytesAsync(
                    Path.Combine(AppContext.BaseDirectory, "Configuration/app-overage-prices.json"))));
                var prices = api.GetType(ns + "AppOveragePrices", true)!;
                var parse = prices.GetMethod("Parse")!;
                foreach (var invalid in new[] { "[]", "{}",
                    "{\"version\":\"owned-duplicate\"," + System.Text.Encoding.UTF8.GetString(shipping).TrimStart()[1..],
                    "{\"version\":\"owned-duplicate\",\"currency\":\"USD\",\"aiMargin\":0,\"requestRatesUsd\":{\"bronze\":0.001,\"silver\":0.0005,\"silver\":0.1,\"gold\":0.0002,\"enterprise\":0.0001}}",
                    "{\"version\":\"a\",\"currency\":\"USD\",\"aiMargin\":0,\"requestRatesUsd\":null}",
                    System.Text.Encoding.UTF8.GetString(shipping).Replace("\"USD\"", "\"GBP\"") })
                    Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() =>
                        parse.Invoke(null, [System.Text.Encoding.UTF8.GetBytes(invalid)])).InnerException);
                var bytes = shipping.ToArray();
                var parsed = parse.Invoke(null, [bytes])!;
                Array.Fill(bytes, (byte)0);
                Assert.Equal(0.0005m, (decimal)prices.GetMethod("Rate")!.Invoke(parsed, ["silver"])!);
                Assert.Null(prices.GetProperty("Version")!.GetSetMethod());
                Assert.Null(prices.GetProperty("PricingSha256")!.GetSetMethod());
            }
            var serviceType = api.GetType(ns + "SubscriptionService", true)!;
            var logger = typeof(NullLogger<>).MakeGenericType(serviceType).GetField("Instance")!.GetValue(null)!;
            var service = Activator.CreateInstance(serviceType, [logger, repository, repository, TimeProvider.System])!;
            var controller = api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SubscriptionController", true)!;
            var db = mongo.GetDatabase(databaseName);
            var accounts = new[] { Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D") };
            var missingAccount = Guid.NewGuid().ToString("D");
            if (ownerSettings)
            {
                for (var i = 0; i < accounts.Length; i++)
                    await db.GetCollection<BsonDocument>("subscription_app_accounts").InsertOneAsync(new BsonDocument
                    {
                        { "_id", i == 0 ? "owned-app-a" : "owned-app-b" }, { "Name", "Owned app " + i },
                        { "PlanId", "silver" }, { "Status", "active" },
                        { "OwnerAvatarIds", new BsonArray(new[] { accounts[i] }) },
                        { "KeyHashSha256", Convert.ToHexString(SHA256.HashData(RandomNumberGenerator.GetBytes(32))) },
                        { "Overrides", new BsonDocument { { "OwnerViewEnabled", true }, { "WarningsEnabled", false },
                            { "AccessLogEnabled", false }, { "SealedHistoryEnabled", true },
                            { "VersionPurgeEnabled", true }, { "VersionPurgeDays", 45 } } }
                    });
            }
            if (ownerUsage) await SeedOwnedAppUsageAsync(db, accounts, missingAccount);
            if (ownerBilling) await SeedOwnedAppBillingAsync(db, accounts, repositoryType, repository);
            avatarStore = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(MongoUri, databaseName);
            var activated = await avatarStore.ActivateProviderAsync();
            Assert.False(activated.IsError, activated.Message);
            foreach (var account in accounts.Append(missingAccount))
            {
                var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar
                {
                    Id = Guid.Parse(account), Username = "subscription-http-" + account,
                    Email = account + "@example.test", IsActive = true,
                    AvatarType = new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.AvatarType>(
                        NextGenSoftware.OASIS.API.Core.Enums.AvatarType.User)
                };
                var saved = await avatarStore.SaveAvatarAsync(avatar);
                Assert.False(saved.IsError, saved.Message);
                if (usage)
                {
                    var detail = await avatarStore.SaveAvatarDetailAsync(new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail
                    {
                        Id = avatar.Id, Username = avatar.Username, Email = avatar.Email,
                        Karma = account == accounts[0] ? 1000 : 0, CreatedDate = DateTime.UtcNow, Version = 1, VersionId = Guid.NewGuid()
                    });
                    Assert.False(detail.IsError, detail.Message);
                }
            }
            if (usage)
            {
                var now = DateTime.UtcNow;
                for (var i = 0; i < accounts.Length; i++)
                foreach (var periodType in new[] { "day", "month" })
                {
                    var period = now.ToString(periodType == "day" ? "yyyy-MM-dd" : "yyyy-MM");
                    await db.GetCollection<BsonDocument>("subscription_usage_buckets").InsertOneAsync(new BsonDocument
                    {
                        { "_id", accounts[i] + ":" + periodType + ":" + period },
                        { "UserId", accounts[i] }, { "PeriodType", periodType }, { "Period", period },
                        { "Requests", (long)(7 + i * 4) }, { "UpdatedAtUtc", new BsonDateTime(now) }
                    });
                }
                var routing = Routing([avatarStore], false, NextGenSoftware.OASIS.API.DNA.HyperDriveModes.Legacy);
                var program = api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Program", true)!;
                avatarManagerField = program.GetField("_avatarManager", BindingFlags.NonPublic | BindingFlags.Static)!;
                Assert.NotNull(avatarManagerField);
                previousAvatarManager = avatarManagerField.GetValue(null);
                avatarManagerField.SetValue(null, new NextGenSoftware.OASIS.API.Core.Managers.AvatarManager(null, routing.OASISDNA, routing));
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
                var selected = new List<Type> { controller };
                if (usage) selected.Add(api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SubscriptionUsageSeriesController", true)!);
                if (notifications) selected.Add(api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SubscriptionNotificationController", true)!);
                if (ownerSettings) selected.Add(api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SubscriptionAppOwnerController", true)!);
                parts.FeatureProviders.Add(new ProfileControllerFeature(selected.ToArray()));
            }).AddJsonOptions(options =>
            {
                if (ownerSettings) options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            }).AddControllersAsServices();
            builder.Services.AddSingleton(api.GetType(ns + "ISubscriptionService", true)!, service);
            builder.Services.AddSingleton(api.GetType(ns + "ISubscriptionUsageRepository", true)!, repository);
            builder.Services.AddSingleton(api.GetType(ns + "ISubscriptionBillingRepository", true)!, repository);
            if (usage)
            {
                builder.Services.AddSingleton(repositoryType, repository);
                builder.Services.AddSingleton(api.GetType(ns + "IUsageSeriesSource", true)!, repository);
                builder.Services.AddSingleton(api.GetType(ns + "IPaidOverageStatementStore", true)!, repository);
            }
            await using var app = builder.Build();
            using var portalFiles = browser
                ? new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Environment.GetEnvironmentVariable("OASIS_PROFILE_PORTAL_ROOT")
                    ?? throw new InvalidOperationException("Pinned portal checkout required."))
                : null;
            if (browser)
            {
                app.UseStaticFiles(new StaticFileOptions { FileProvider = portalFiles });
                app.MapGet("/__subscription_browser", async () => Microsoft.AspNetCore.Http.Results.Content(
                    await File.ReadAllTextAsync(Path.Combine(root.FullName, "Scripts/TestHosts/Fixtures/" +
                        (usage ? "subscription-usage-browser-host.html" : "subscription-browser-host.html"))),
                    "text/html"));
            }
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
                if (!context.Request.Path.StartsWithSegments("/api")) { await next(context); return; }
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
                if (browser)
                {
                    using var result = await ExchangeSdkRequest(new
                    {
                        baseUrl = address,
                        actors = accounts.Append(missingAccount).Select(actor => new { id = actor, token = Token(actor) }).ToArray()
                    });
                    Assert.False(result.RootElement.TryGetProperty("isError", out var failed) && failed.GetBoolean(),
                        result.RootElement.TryGetProperty("message", out var message) ? message.GetString() : "Browser worker failed.");
                    Assert.True(result.RootElement.GetProperty(ownerBilling ? "appBillingReadBrowserVerified" : ownerUsage ? "appOwnerUsageBrowserVerified" : ownerSettings ? "appOwnerSettingsBrowserVerified" : notifications ? "notificationPreferencesBrowserVerified" :
                        usage ? "subscriptionUsageBrowserVerified" : "subscriptionBrowserVerified").GetBoolean());
                    Assert.Equal(3, result.RootElement.GetProperty("actorsVerified").GetInt32());
                    Assert.True(result.RootElement.GetProperty("lateSessionResponseIgnored").GetBoolean());
                    Console.WriteLine("Subscription browser evidence: " + result.RootElement.GetRawText());
                }
                else
                {
                Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/subscription/subscriptions/me")).StatusCode);
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
                }
                Assert.Equal(2L, await db.GetCollection<BsonDocument>("subscription_records").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                Assert.Equal(2L, await db.GetCollection<BsonDocument>("subscription_orders").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                if (usage) Assert.Equal(ownerUsage ? 12L : ownerBilling ? 8L : 4L, await db.GetCollection<BsonDocument>("subscription_usage_buckets").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                if (notifications && !ownerSettings)
                {
                    var preferences = db.GetCollection<BsonDocument>("usage_notification_settings");
                    Assert.Equal(2L, await preferences.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                    var before = await preferences.Find(new BsonDocument("_id", "user:" + accounts[0])).SingleAsync();
                    Assert.Equal(35, Assert.Single(before["thresholds"].AsBsonArray).ToInt32());
                    var other = await preferences.Find(new BsonDocument("_id", "user:" + accounts[1])).SingleAsync();
                    Assert.Equal(new[] { 45, 95 }, other["thresholds"].AsBsonArray.Select(value => value.ToInt32()));
                    var audit = db.GetCollection<BsonDocument>("subscription_billing_audit");
                    var filter = new BsonDocument("Kind", "usage-notification-settings-changed");
                    Assert.Equal(5L, await audit.CountDocumentsAsync(filter));
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(accounts[0]));
                    async Task Change(int threshold)
                    {
                        using var response = await http.PostAsJsonAsync("/api/subscription/usage/notifications",
                            new { thresholds = new[] { threshold }, emailRequested = false, warningsEnabled = false });
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                    await Task.WhenAll(Change(25), Change(75));
                    Assert.Equal(7L, await audit.CountDocumentsAsync(filter));
                    var edges = new List<(int Next, int Previous)>();
                    foreach (var document in await audit.Find(filter).ToListAsync())
                    {
                        using var payload = JsonDocument.Parse(document["PayloadJson"].AsString);
                        var fields = payload.RootElement;
                        if (fields.GetProperty("Subject").GetString() != "user:" + accounts[0]) continue;
                        Assert.Equal(accounts[0], fields.GetProperty("Actor").GetString());
                        var values = fields.GetProperty("Thresholds").EnumerateArray().Select(value => value.GetInt32()).ToArray();
                        if (values.Length == 1 && values[0] is 25 or 75)
                            edges.Add((values[0], Assert.Single(fields.GetProperty("PreviousThresholds").EnumerateArray()).GetInt32()));
                    }
                    Assert.Equal(2, edges.Count);
                    var first = Assert.Single(edges, edge => edge.Previous == 35);
                    var second = Assert.Single(edges, edge => edge.Previous == first.Next);
                    var after = await preferences.Find(new BsonDocument("_id", "user:" + accounts[0])).SingleAsync();
                    Assert.Equal(second.Next, Assert.Single(after["thresholds"].AsBsonArray).ToInt32());
                    Console.WriteLine("Notification native audit verified: two concurrent changes form the actual before/after chain; seven total preference audit entries, two owned subjects.");
                }
                if (ownerSettings && !ownerUsage && !ownerBilling)
                {
                    var apps = db.GetCollection<BsonDocument>("subscription_app_accounts");
                    var audit = db.GetCollection<BsonDocument>("subscription_billing_audit");
                    var filter = new BsonDocument("Kind", "app-usage-overrides-changed");
                    Assert.Equal(3L, await audit.CountDocumentsAsync(filter));
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(accounts[0]));
                    async Task Send(string json, HttpStatusCode status)
                    {
                        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        using var response = await http.PostAsync("/api/subscription/app-owner/owned-app-a/settings", content);
                        Assert.Equal(status, response.StatusCode);
                    }
                    foreach (var invalid in new[] { "{}", "null", "[]", "{\"warningsEnabled\":1}",
                        "{\"unknown\":true}", "{\"warningsEnabled\":true,\"warningsEnabled\":false}",
                        "{\"versionPurgeDays\":1.5}", "{\"versionPurgeDays\":3651}" })
                        await Send(invalid, HttpStatusCode.BadRequest);
                    Assert.Equal(3L, await audit.CountDocumentsAsync(filter));
                    await Task.WhenAll(Send("{\"ownerViewEnabled\":false}", HttpStatusCode.OK),
                        Send("{\"accessLogEnabled\":true}", HttpStatusCode.OK));
                    var after = (await apps.Find(new BsonDocument("_id", "owned-app-a")).SingleAsync())["Overrides"].AsBsonDocument;
                    Assert.False(after["OwnerViewEnabled"].AsBoolean);
                    Assert.True(after["AccessLogEnabled"].AsBoolean);
                    Assert.True(after["SealedHistoryEnabled"].AsBoolean);
                    Assert.True(after["VersionPurgeEnabled"].AsBoolean);
                    Assert.Equal(45, after["VersionPurgeDays"].ToInt32());
                    Assert.Equal(5L, await audit.CountDocumentsAsync(filter));
                    // Revocation is enforced by the same transaction that writes, independently of any earlier UI read.
                    await apps.UpdateOneAsync(new BsonDocument("_id", "owned-app-a"),
                        new BsonDocument("$set", new BsonDocument("OwnerAvatarIds", new BsonArray())));
                    await Send("{\"warningsEnabled\":true}", HttpStatusCode.Forbidden);
                    Assert.Equal(5L, await audit.CountDocumentsAsync(filter));
                    Console.WriteLine("App settings native verified: omitted hidden choices preserved, disjoint concurrent changes retained, malformed requests and revoked owner create no audit writes.");
                }
                if (ownerUsage)
                {
                    Assert.Equal(5L, await db.GetCollection<BsonDocument>("subscription_usage_events").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                    Assert.Equal(11L, await db.GetCollection<BsonDocument>("subscription_usage_audit").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                    Assert.Equal(1L, await db.GetCollection<BsonDocument>("subscription_billing_audit").CountDocumentsAsync(
                        new BsonDocument("Kind", "app-usage-overrides-changed")));
                    Console.WriteLine("App usage native verified: five operations/eleven audit deltas/twelve total buckets unchanged; only the explicit disable-view setting wrote one settings audit.");
                }
                if (ownerBilling)
                {
                    Assert.Equal(3L, await db.GetCollection<BsonDocument>("subscription_overage_statements").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                    Assert.Equal(0L, await db.GetCollection<BsonDocument>("subscription_billing_audit").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                    var apps = await db.GetCollection<BsonDocument>("subscription_app_accounts").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                    Assert.Equal(2, apps.Count);
                    foreach (var app in apps)
                    {
                        Assert.False(app["Payg"]["Enabled"].AsBoolean);
                        Assert.Equal("owned-historical-terms", app["Payg"]["PricingVersion"].AsString);
                        Assert.Equal(app["_id"].AsString == "owned-app-a" ? 5m : 2m, app["Payg"]["MonthlyCapUsd"].ToDecimal());
                    }
                    Console.WriteLine("App billing reads native verified: three owned statement fixtures and eight buckets unchanged, no billing audit writes; immutable catalogue validation passed. Not Stripe collection or app admission acceptance.");
                }
            }
            finally { await app.StopAsync(); }
        }
        finally
        {
            if (avatarManagerField != null) avatarManagerField.SetValue(null, previousAvatarManager);
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

    private static async Task SeedOwnedAppUsageAsync(IMongoDatabase db, string[] accounts, string expiredActor)
    {
        var now = DateTime.UtcNow;
        var old = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-2);
        async Task Operation(string app, string actor, DateTime when, bool expired, long units, decimal cost, bool corrected = false)
        {
            var id = Guid.NewGuid().ToString("D");
            await db.GetCollection<BsonDocument>("subscription_usage_events").InsertOneAsync(new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() }, { "OperationId", id }, { "UserId", actor },
                { "PooledAppId", app }, { "AuthorizedAtUtc", when }, { "ExpiresAtUtc", when.AddMinutes(5) },
                { "Status", expired ? "expired" : "settled" }, { "Units", units }, { "SettledCostUsd", new BsonDecimal128(cost) }
            });
            async Task Delta(string kind, long requests, long tokens, decimal usd)
            {
                await db.GetCollection<BsonDocument>("subscription_usage_audit").InsertOneAsync(new BsonDocument
                {
                    { "_id", id + ":" + kind }, { "OperationId", id }, { "UserId", actor },
                    { "Kind", kind }, { "OccurredAtUtc", when }, { "Month", when.ToString("yyyy-MM") },
                    { "Day", when.ToString("yyyy-MM-dd") }, { "RequestsDelta", requests },
                    { "SettledUnitsDelta", tokens }, { "SettledCostDeltaUsd", new BsonDecimal128(usd) }
                });
            }
            await Delta("authorized", 1, 0, 0);
            await Delta(expired ? "expired" : "settled", expired ? -1 : 0, units, cost);
            if (corrected) await Delta("correction", 0, -2, -0.002m);
        }
        await Operation("owned-app-a", accounts[0], now, false, 10, 0.01m, true);
        await Operation("owned-app-a", accounts[1], now, false, 6, 0.006m);
        await Operation("owned-app-a", expiredActor, now, true, 0, 0);
        await Operation("owned-app-b", expiredActor, now, false, 500, 0.5m);
        await Operation("owned-app-a", expiredActor, old, false, 777, 0.777m);
        async Task Buckets(string subject, DateTime when, long requests, long units, decimal cost)
        {
            foreach (var type in new[] { "day", "month" })
            {
                var period = when.ToString(type == "day" ? "yyyy-MM-dd" : "yyyy-MM");
                await db.GetCollection<BsonDocument>("subscription_usage_buckets").InsertOneAsync(new BsonDocument
                {
                    { "_id", subject + ":" + type + ":" + period }, { "UserId", subject },
                    { "PeriodType", type }, { "Period", period }, { "Requests", requests }, { "SettledUnits", units },
                    { "SettledCostUsd", new BsonDecimal128(cost) }, { "UpdatedAtUtc", when }
                });
            }
        }
        await Buckets("app:owned-app-a", now, 1, 8, 0.008m);
        await Buckets("app:owned-app-a#3", now, 1, 6, 0.006m);
        await Buckets("app:owned-app-b", now, 1, 500, 0.5m);
        await Buckets("app:owned-app-a", old, 1, 777, 0.777m);
    }

    private static async Task SeedOwnedAppBillingAsync(IMongoDatabase db, string[] accounts, Type repositoryType, object repository)
    {
        var provider = repositoryType.GetField("_policyProvider", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(repository)!;
        var policy = provider.GetType().GetMethod("GetPolicy", [typeof(string), typeof(int)])!.Invoke(provider, ["silver", 0])!;
        long included = (int)policy.GetType().GetProperty("MonthlyRequestLimit")!.GetValue(policy)!;
        var now = DateTime.UtcNow;
        for (var i = 0; i < 2; i++)
        {
            var app = i == 0 ? "owned-app-a" : "owned-app-b";
            await db.GetCollection<BsonDocument>("subscription_app_accounts").UpdateOneAsync(new BsonDocument("_id", app),
                new BsonDocument("$set", new BsonDocument { { "Payg", new BsonDocument { { "Enabled", false },
                    { "MonthlyCapUsd", new BsonDecimal128(i == 0 ? 5m : 2m) }, { "PricingVersion", "owned-historical-terms" } } },
                    { "Overrides.OwnerViewEnabled", i != 0 } }));
            foreach (var type in new[] { "day", "month" })
            {
                var subject = "app:" + app;
                var period = now.ToString(type == "day" ? "yyyy-MM-dd" : "yyyy-MM");
                await db.GetCollection<BsonDocument>("subscription_usage_buckets").InsertOneAsync(new BsonDocument
                {
                    { "_id", subject + ":" + type + ":" + period }, { "UserId", subject },
                    { "PeriodType", type }, { "Period", period }, { "Requests", included + (i == 0 ? 2000 : 6000) }, { "UpdatedAtUtc", now }
                });
            }
        }
        for (var i = 0; i < 3; i++)
            await db.GetCollection<BsonDocument>("subscription_overage_statements").InsertOneAsync(new BsonDocument
            {
                { "_id", "owned-statement-" + Guid.NewGuid().ToString("N") },
                { "UserId", i == 0 ? "app:owned-app-a" : i == 1 ? "app:owned-app-b" : accounts[0] },
                { "Scope", "owned-read-fixture" }, { "Month", now.AddMonths(-1).ToString("yyyy-MM") },
                { "AmountMinorUsd", i == 0 ? 123L : i == 1 ? 456L : 789L },
                { "State", i == 1 ? "open" : "paid" }, { "PaymentFailed", i == 1 }, { "CreatedAtUtc", now },
                { "HostedInvoiceUrl", "https://invoice.stripe.com/i/owned-read-fixture-" + i }
            });
    }
}

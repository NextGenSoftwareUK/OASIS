using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using NextGenSoftware.Utilities;

const string ownedUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != ownedUri)
    throw new InvalidOperationException("Only the exact owned loopback fixture may be used.");
static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
var client = new MongoClient(ownedUri);
var database = "hotspot_http_" + Guid.NewGuid().ToString("N");
var dnaPath = Path.Combine(Path.GetTempPath(), database + ".json");
Require(!File.Exists(dnaPath), "Owned DNA path already exists.");
Require(!(await (await client.ListDatabaseNamesAsync()).ToListAsync()).Contains(database), "Owned database already exists.");
var secret = RandomNumberGenerator.GetBytes(64);
var mode = Environment.GetEnvironmentVariable("OASIS_HOTSPOT_HTTP_MODE") ?? "binding";
Require(mode is "binding" or "populated" or "browser" or "quest-empty" or "catalogue" or "catalogue-browser" or "competition-browser" or "competition-publisher" or "profile-http" or "billing-http" or "billing-admin-http" or "billing-warning-store" or "billing-warning-http" or "billing-warning-provider" or "session-list-http" or "mfa-native" or "mfa-http" or "mfa-middleware-http" or "session-global-http" or "mfa-browser" or "billing-warning-browser" or "billing-restoration-http", "Unsupported hotspot HTTP check mode.");
var dna = new OASISDNA { OASIS = new() {
    HyperDriveMode = HyperDriveModes.V2, OASISHyperDriveConfig = new() {
        AutoFailoverEnabled = false, AutoReplicationEnabled = false, AutoLoadBalancingEnabled = false },
    Logging = new() { LogToConsole = false, LogToFile = false }, ErrorHandling = new(),
    Security = new() { SecretKey = Convert.ToBase64String(secret) }, Email = new(),
    SubscriptionConfig = new() { PlanType = "Gold" },
    StorageProviders = new() { OASISProviderBootType = "Cold", AutoFailOverProviders = "MongoDBOASIS",
        AutoFailOverProvidersForAvatarLogin = "MongoDBOASIS", AutoFailOverProvidersForCheckIfEmailAlreadyInUse = "MongoDBOASIS",
        AutoFailOverProvidersForCheckIfUsernameAlreadyInUse = "MongoDBOASIS",
        AutoFailOverProvidersForCheckIfOASISSystemAccountExists = "MongoDBOASIS", AutoFailOverLocalProviders = "MongoDBOASIS",
        AutoLoadBalanceProviders = "MongoDBOASIS", AutoReplicationProviders = "MongoDBOASIS",
        MongoDBOASIS = new() { ConnectionString = ownedUri, DBName = database, NativeImportDBName = database } } } };
var provider = new MongoDBOASIS(ownedUri, database, dna);
WebApplication? app = null;
#if !OASIS_SESSION_MIDDLEWARE_CHECK
Require(mode != "mfa-middleware-http", "Middleware qualification requires actual WEB4-WEB10 API project references.");
#endif
var previousMfaGate = AvatarManager.SecondFactorGate;
var mfaPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
try
{
    var active = await provider.ActivateProviderAsync(); Require(!active.IsError, active.Message);
    var actor = Guid.NewGuid();
    var wizardActor = Guid.NewGuid();
    var accounts = new List<Avatar> {
        new Avatar { Id = actor, Username = "http-" + actor.ToString("N"), Email = actor.ToString("N") + "@example.test", AvatarType = new EnumValue<AvatarType>(AvatarType.User) },
        new Avatar { Id = Guid.NewGuid(), FirstName = "OASIS", LastName = "SYSTEM", Username = "root", Email = "anorak@oasisomniverse.one", AvatarType = new EnumValue<AvatarType>(AvatarType.System) } };
    if (mode is "competition-publisher" or "billing-http" or "billing-admin-http" or "billing-warning-browser" or "billing-restoration-http") accounts.Add(new Avatar { Id = wizardActor,
        Username = "wizard-" + wizardActor.ToString("N"), Email = wizardActor.ToString("N") + "@example.test",
        AvatarType = new EnumValue<AvatarType>(mode == "billing-http" ? AvatarType.User : AvatarType.Wizard) });
    if (mode == "session-global-http")
    {
        var closureActorId = Guid.NewGuid();
        accounts.Add(new Avatar { Id = closureActorId, Username = "closure-" + closureActorId.ToString("N"),
            Email = closureActorId.ToString("N") + "@example.test", AvatarType = new EnumValue<AvatarType>(AvatarType.User) });
    }
    foreach (var account in accounts)
    {
        account.IsActive = true; account.Version = 1; account.VersionId = Guid.NewGuid(); account.CreatedDate = DateTime.UtcNow;
        if (mode == "billing-warning-browser" && account.Id == actor)
        {
            account.Email = "delivered@resend.dev";
            account.Verified = DateTime.UtcNow;
        }
        if ((mode is "mfa-http" or "mfa-middleware-http" or "session-global-http" or "mfa-browser") && account.Id == actor)
        {
            account.Verified = DateTime.UtcNow;
            account.Password = BCrypt.Net.BCrypt.HashPassword(mfaPassword);
        }
        if (mode == "session-list-http" && account.Id == actor)
        {
            var now = DateTime.UtcNow;
            account.LastBeamedIn = now.AddMinutes(-10);
            account.RefreshTokens = new List<RefreshToken> {
                new() { Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)), Created = now.AddMinutes(-10),
                    Expires = now.AddDays(1), CreatedByIp = "127.0.0.1" },
                new() { Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)), Created = now.AddHours(-2),
                    Expires = now.AddDays(1), Revoked = now.AddMinutes(-1), CreatedByIp = "127.0.0.2" },
                new() { Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)), Created = now.AddDays(-2),
                    Expires = now.AddDays(-1), CreatedByIp = "127.0.0.3" }
            };
        }
        var saved = await provider.SaveAvatarAsync(account); Require(!saved.IsError, "Owned actor seed failed: " + saved.Message);
        var detail = await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = account.Id, Username = account.Username,
            Email = account.Email, Version = 1, VersionId = Guid.NewGuid(), CreatedDate = DateTime.UtcNow });
        Require(!detail.IsError, "Owned actor detail failed: " + detail.Message);
    }
    await File.WriteAllTextAsync(dnaPath, JsonConvert.SerializeObject(dna));
    NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNAPath = dnaPath;
    ProviderManager.Instance.OASISDNA = dna;
    Require(ProviderManager.Instance.RegisterProvider(provider), "Owned provider registration failed.");
    var selected = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(ProviderType.MongoDBOASIS);
    Require(!selected.IsError, "Owned provider selection failed: " + selected.Message);
    var boot = await new OASISAPI().BootOASISAsync(dna, startApolloServer: false);
    Require(!boot.IsError && boot.Result, "Real isolated runtime boot failed: " + boot.Message);
    var expected = new List<Guid>();
    if (mode is "populated" or "browser")
    {
        var data = new HolonManager(provider, dna, ProviderManager.Instance);
        foreach (var item in new[] { (lat: 0d, visible: true, active: true), (lat: .001d, visible: true, active: true),
            (lat: 5d, visible: true, active: true), (lat: 0d, visible: false, active: true), (lat: 0d, visible: true, active: false) })
        {
            var id = Guid.NewGuid();
            var hotspot = new GeoHotSpot { Id = id, Name = "Owned HTTP hotspot " + id.ToString("N"),
                Lat = item.lat, Long = 0, IsActive = true, IsVisibleOnMap = item.visible,
                HotSpotRadiusInMetres = 17, TextContent = "Native HTTP geometry",
                STARNETDNA = new STARNETDNA { Id = id, CreatedByAvatarId = actor, Version = "1.0.0", VersionSequence = 1 } };
            hotspot.MetaData["STARNETHolonId"] = id.ToString("D");
            hotspot.MetaData["GeoHotSpotType"] = GeoHotSpotType.Map.ToString();
            var saved = await data.SaveHolonAsync<GeoHotSpot>(hotspot, actor, saveChildren: false, providerType: ProviderType.MongoDBOASIS);
            Require(!saved.IsError && saved.IsSaved, "Owned HTTP hotspot seed failed: " + saved.Message);
            if (!item.active)
            {
                saved.Result.IsActive = false;
                var deactivated = await data.SaveHolonAsync<GeoHotSpot>(saved.Result, actor, saveChildren: false,
                    providerType: ProviderType.MongoDBOASIS);
                Require(!deactivated.IsError && deactivated.IsSaved && deactivated.Result != null && !deactivated.Result.IsActive,
                    "Owned existing hotspot was not deactivated: " + deactivated.Message);
                var storedInactive = await data.LoadHolonAsync<GeoHotSpot>(id, loadChildren: false, providerType: ProviderType.MongoDBOASIS);
                Require(!storedInactive.IsError && storedInactive.Result != null && !storedInactive.Result.IsActive &&
                    storedInactive.Result.DeletedDate == DateTime.MinValue,
                    "Owned inactive hotspot state was not confirmed by native readback: " + storedInactive.Message);
            }
            if (item.lat < 1 && item.visible && item.active) expected.Add(id);
        }
        if (mode == "browser")
        {
            var original = new Web4NFT { Id = Guid.NewGuid(), MintedByAvatarId = actor, Title = "Owned original NFT" };
            var savedOriginal = await data.SaveHolonAsync(new Holon { Id = original.Id, HolonType = HolonType.Web4NFT, IsActive = true,
                MetaData = new Dictionary<string, object> { ["NFT.MintedByAvatarId"] = actor.ToString(),
                    ["NFT.WEB4NFT"] = System.Text.Json.JsonSerializer.Serialize(original) } }, actor, saveChildren: false, providerType: ProviderType.MongoDBOASIS);
            Require(!savedOriginal.IsError && savedOriginal.IsSaved, "Owned original NFT seed failed: " + savedOriginal.Message);
            var placement = new Web4OASISGeoSpatialNFT { Id = Guid.NewGuid(), Title = "Owned browser GeoNFT",
                MintedByAvatarId = actor, PlacedByAvatarId = actor, OriginalWeb4OASISNFTId = original.Id,
                Lat = 0, Long = 0, IsVisibleOnMap = true };
            var geo = new Holon { Id = placement.Id, HolonType = HolonType.Web4GeoNFT, IsActive = true,
                MetaData = new Dictionary<string, object> { ["GEONFT.PlacedByAvatarId"] = actor.ToString(),
                    ["GEONFT.WEB4GEONFT"] = System.Text.Json.JsonSerializer.Serialize(placement) } };
            var savedGeo = await data.SaveHolonAsync(geo, actor, saveChildren: false, providerType: ProviderType.MongoDBOASIS);
            Require(!savedGeo.IsError && savedGeo.IsSaved, "Owned GeoNFT placement seed failed: " + savedGeo.Message);
            var questId = Guid.NewGuid();
            var quest = new Quest { Id = questId, Name = "Owned cross-app quest", IsActive = true,
                LinkedGeoHotSpotId = expected[0],
                STARNETDNA = new STARNETDNA { Id = questId, CreatedByAvatarId = actor, Version = "1.0.0", VersionSequence = 1 },
                Objectives = new List<Objective> { new() { Id = Guid.NewGuid(), Title = "Visit both hotspots",
                    LinkedGeoHotSpotId = expected[0], NeedToGoToGeoHotSpots = new Dictionary<string, IList<string>> {
                        ["Our World"] = new List<string> { expected[0].ToString(), expected[1].ToString(), "2" } } } } };
            quest.MetaData["STARNETHolonId"] = questId.ToString(); quest.MetaData["CreatedByAvatarId"] = actor.ToString(); quest.MetaData["Active"] = "1";
            var savedQuest = await data.SaveHolonAsync<Quest>(quest, actor, saveChildren: false, providerType: ProviderType.MongoDBOASIS);
            Require(!savedQuest.IsError && savedQuest.IsSaved, "Owned quest seed failed: " + savedQuest.Message);
        }
    }
    var builder = WebApplication.CreateBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
    NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa.MfaService? httpMfa = null;
    var mfaNow = DateTime.UtcNow;
    if (mode is "mfa-http" or "mfa-middleware-http" or "session-global-http" or "mfa-browser")
    {
        var settings = new MfaSettings { Enabled = mode == "session-global-http" ? dna.OASIS.Security.Mfa?.Enabled == true : true, CodeDigits = 6, TimeStepSeconds = 30, AllowedDriftSteps = 0 };
        var store = new NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa.MongoMfaStore(() => dna);
        httpMfa = new NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa.MfaService(() => settings, store,
            dna.OASIS.Security.SecretKey, () => mode == "mfa-browser" ? DateTime.UtcNow : mfaNow);
        builder.Services.AddSingleton(httpMfa);
        builder.Services.AddHttpContextAccessor();
    }
    if (mode is "mfa-http" or "mfa-middleware-http" or "session-global-http" or "mfa-browser") builder.Logging.AddProvider(new SanitizedHttpExceptionLogger());
    MongoSubscriptionUsageRepository? billingRepository = null;
    if (mode is "billing-http" or "billing-admin-http" or "billing-warning-store" or "billing-warning-http" or "billing-warning-provider" or "billing-warning-browser" or "billing-restoration-http" or "session-global-http")
    {
        billingRepository = new MongoSubscriptionUsageRepository(client, database, TimeProvider.System,
            initializeEmptyLedger: mode is "billing-http" or "billing-admin-http" or "session-global-http");
        if (mode is not ("billing-warning-store" or "billing-warning-http" or "billing-warning-provider" or "billing-warning-browser" or "billing-restoration-http"))
        {
            await billingRepository.CreateAppAccountAsync("owned-billing", "Owned billing route fixture", "enterprise",
                actor.ToString("D"), "Owned isolated fixture", false, null, CancellationToken.None);
            await billingRepository.SetAppOwnersAsync("owned-billing", new[] { actor.ToString("D") },
                actor.ToString("D"), "Owned isolated fixture", CancellationToken.None);
        }
        builder.Services.AddSingleton(billingRepository);
        builder.Services.AddSingleton<IUsageSeriesSource>(billingRepository);
        if (mode is "billing-warning-http" or "billing-warning-browser")
        {
            dna.OASIS.UsageVisibility = new UsageVisibilitySettings { EmailEnabled = mode == "billing-warning-browser", WarningsEnabled = true };
            builder.Services.AddSingleton<ISubscriptionUsageRepository>(billingRepository);
            builder.Services.AddSingleton<ISubscriptionBillingRepository>(billingRepository);
            builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();
            builder.Services.AddSingleton<UsageWarningReader>();
            builder.Services.AddHttpClient();
            builder.Services.AddTransient<UsageWarningEmailGateway>(services =>
                new UsageWarningEmailGateway(services.GetRequiredService<HttpClient>(), TimeProvider.System));
            builder.Services.AddTransient<UsageWarningEmailDispatcher>();
            builder.Services.AddTransient<UsageWarningEmailService>();
        }
    }
    if (mode == "session-global-http")
    {
        builder.Services.AddSingleton<AccountClosureAdmissionService>();
        builder.Services.AddSingleton<ISubscriptionUsageRepository>(billingRepository!);
        builder.Services.AddSingleton<ISubscriptionBillingRepository>(billingRepository!);
        builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();
    }
    if (mode == "billing-restoration-http")
    {
        Require(string.IsNullOrWhiteSpace(SubscriptionStripeConfiguration.SecretKey(builder.Configuration)),
            "Stripe credentials are forbidden in the owned no-charge contract check.");
        builder.Services.AddSingleton<ISubscriptionUsageRepository>(billingRepository!);
        builder.Services.AddSingleton<ISubscriptionBillingRepository>(billingRepository!);
        builder.Services.AddSingleton<IPaidOverageStatementStore>(billingRepository!);
        builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();
        builder.Services.AddSingleton<IPaidOverageGateway, StripePaidOverageGateway>();
        builder.Services.AddTransient<PaidOverageBillingService>();
    }
    var key = new SymmetricSecurityKey((mode is "mfa-http" or "mfa-middleware-http" or "session-global-http" or "mfa-browser") ? System.Text.Encoding.ASCII.GetBytes(dna.OASIS.Security.SecretKey) : secret);
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new() { ValidateIssuer = false, ValidateAudience = false,
            ValidateIssuerSigningKey = true, IssuerSigningKey = key, ValidateLifetime = true, ClockSkew = TimeSpan.Zero };
    });
    var mvc = builder.Services.AddControllers();
    if (mode is "mfa-http" or "mfa-middleware-http" or "session-global-http" or "mfa-browser" or "billing-warning-browser" or "billing-restoration-http")
    {
        // WEB4 hosts authentication; WEB5's proxy has its own deployment and the same public URL.
        // This multi-API fixture must use WEB4's application surface for the auth journey.
        mvc.PartManager.ApplicationParts.Clear();
        mvc.AddApplicationPart(typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.NftController).Assembly);
    }
    else
        mvc.AddApplicationPart(typeof(GeoHotSpotsController).Assembly)
            .AddApplicationPart(typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.NftController).Assembly);
    builder.Services.AddAuthorization();
    app = builder.Build();
    // Keep framework authorization after bearer authentication. With only an explicit
    // UseAuthentication call, WebApplication can insert automatic authorization first.
    app.UseAuthentication();
    app.UseAuthorization();
#if OASIS_SESSION_MIDDLEWARE_CHECK
    if (mode == "mfa-middleware-http") SessionMiddlewareHttpQualification.MapBranches(app, actor);
#endif
    if (mode is "mfa-http" or "mfa-middleware-http" or "session-global-http" or "mfa-browser")
        AvatarManager.SecondFactorGate = NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa.MfaGate.Create(httpMfa!,
            app.Services.GetRequiredService<IHttpContextAccessor>());
    var sessionBrowser = mode == "session-global-http" && Environment.GetEnvironmentVariable("OASIS_SESSION_SETTINGS_BROWSER") == "true";
    if (sessionBrowser || (mode is "browser" or "catalogue-browser" or "competition-browser" or "mfa-browser" or "billing-warning-browser"))
    {
        var portal = Environment.GetEnvironmentVariable("OASIS_GEO_PORTAL_ROOT") ?? throw new InvalidOperationException("Pinned Portal root required.");
        var browserHost = Environment.GetEnvironmentVariable("OASIS_GEO_BROWSER_HOST") ?? throw new InvalidOperationException("Geo browser host required.");
        Require(File.Exists(Path.Combine(portal, mode switch { "session-global-http" => "assets/js/settings-modal.js", "catalogue-browser" => "assets/js/missions-modal.js", "competition-browser" => "assets/js/competition-modal.js", "mfa-browser" => "assets/js/settings-modal.js", "billing-warning-browser" => "assets/js/usage-charts.js", _ => "assets/js/map-modal.js" })) && File.Exists(browserHost), "Actual Portal/browser assets missing.");
        app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(Path.Combine(portal, "assets")), RequestPath = "/assets" });
        app.MapGet(mode switch { "session-global-http" => "/__session_settings_browser", "catalogue-browser" => "/__catalogue_browser", "competition-browser" => "/__competition_browser", "mfa-browser" => "/__mfa_settings_browser", "billing-warning-browser" => "/__warning_email_browser", _ => "/__geo_browser" }, () => Results.File(browserHost, "text/html"));
    }
    app.Use(async (context, next) => {
        using var authenticationTrace = mode == "session-global-http" && context.Request.Path == "/api/avatar/authenticate"
            ? NextGenSoftware.OASIS.API.Core.Helpers.AuthenticationOperationTrace.Begin() : null;
        context.Response.OnCompleted(() => {
            if (authenticationTrace != null)
            {
                var observed = authenticationTrace.Snapshot;
                Console.WriteLine($"SESSION_HTTP_AUTH_TRACE: auth={observed.Authentication} routing={observed.Routing} provider={observed.Provider} operation={observed.Operation}");
            }
            return Task.CompletedTask;
        });
        if (context.User.Identity?.IsAuthenticated == true && Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id))
        {
            var loaded = mode == "session-global-http"
                ? await AvatarManager.Instance.LoadAvatarAsync(id, loadPrivateKeys: false, hideAuthDetails: false)
                : await provider.LoadAvatarAsync(id);
            Require(!loaded.IsError && loaded.Result != null, "Signed fixture actor unavailable.");
            context.Items["Avatar"] = loaded.Result; context.Items["AvatarId"] = id;
            OASISRequestContext.CurrentAvatar = loaded.Result; OASISRequestContext.CurrentAvatarId = id;
        }
        try { await next(context); }
        catch (Exception error) when (mode is "session-list-http" or "mfa-http" or "mfa-middleware-http" or "session-global-http")
        {
            Console.WriteLine("SESSION_HTTP_SERVER_FAILURE: type=" + error.GetType().FullName +
                " base=" + error.GetBaseException().GetType().FullName +
                " avatarType=" + context.Items["Avatar"]?.GetType().FullName);
            if (mode is "mfa-http" or "mfa-middleware-http" or "session-global-http" or "mfa-browser")
                Console.WriteLine("MFA_HTTP_FAILURE_METHODS: " + string.Join(" | ",
                    new StackTrace(error.GetBaseException(), false).GetFrames()?.Take(8).Select(frame =>
                        frame.GetMethod()?.DeclaringType?.FullName + "." + frame.GetMethod()?.Name) ?? Array.Empty<string>()));
            throw;
        }
    });
    app.MapControllers(); await app.StartAsync();
    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    Require(new Uri(address).Host == "127.0.0.1", "HTTP fixture escaped loopback.");
    using var http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
    var jwt = new JwtSecurityToken(claims: new[] { new Claim("sub", actor.ToString("D")) }, expires: DateTime.UtcNow.AddMinutes(5),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
    async Task RunOwnedBrowserAsync(object configuration, string resultProperty)
    {
        var script = Environment.GetEnvironmentVariable("OASIS_GEO_BROWSER_SCRIPT") ?? throw new InvalidOperationException("Owned browser verification script required.");
        Require(File.Exists(script), "Owned browser verification script missing.");
        var start = new ProcessStartInfo("node") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-e");
        start.ArgumentList.Add("const fs=require('node:fs');require(process.argv[1]).verify(JSON.parse(fs.readFileSync(0,'utf8'))).then(x=>console.log(JSON.stringify(x))).catch(e=>{console.error(e.message);process.exitCode=1;});");
        start.ArgumentList.Add(script);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Owned browser process did not start.");
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(System.Text.Json.JsonSerializer.Serialize(configuration));
        process.StandardInput.Close();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90)); await process.WaitForExitAsync(timeout.Token); }
        finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
        var resultText = await output; var errorText = await errors;
        errorText = System.Text.RegularExpressions.Regex.Replace(errorText, @"[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}", "[redacted fixture bearer]");
        Require(process.ExitCode == 0, "Actual owned browser failed: " + errorText);
        using var result = JsonDocument.Parse(resultText);
        Require(result.RootElement.GetProperty(resultProperty).GetBoolean(), "Owned browser completion marker missing.");
    }
    if (mode == "billing-restoration-http")
    {
        await WarningBrowserFixture.SeedAsync(billingRepository!, database, actor, wizardActor);
        var before = await provider.LoadAvatarAsync(actor);
        Require(!before.IsError && before.Result != null, "Owned billing actor unavailable.");
        await BillingRestorationHttpQualification.RunAsync(http, billingRepository!, actor);
        var after = await provider.LoadAvatarAsync(actor);
        Require(!after.IsError && after.Result!.VersionId == before.Result.VersionId,
            "Billing contract check changed the avatar.");
    }
    else if (mode == "billing-warning-browser")
    {
        Require(Environment.GetEnvironmentVariable("OASIS_WARNING_TEST_RESEND_ENABLED") == "true",
            "Warning browser requires the explicit isolated provider gate.");
        var testKey = Environment.GetEnvironmentVariable("OASIS_WARNING_TEST_RESEND_KEY");
        var sender = Environment.GetEnvironmentVariable("OASIS_WARNING_TEST_EMAIL_FROM");
        Require(!string.IsNullOrWhiteSpace(testKey) && !string.IsNullOrWhiteSpace(sender), "Temporary warning credential/sender required.");
        Require(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OASIS_RESEND_KEY")) &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RESEND_API_KEY")), "Ambient email credential forbidden.");
        await WarningBrowserFixture.SeedAsync(billingRepository!, database, actor, wizardActor);
        await billingRepository!.SaveNotificationSettingsAsync("user:" + actor.ToString("D"), new[] { 80 },
            true, actor.ToString("D"), true, CancellationToken.None);
        var before = await provider.LoadAvatarAsync(actor);
        Require(!before.IsError && before.Result?.Email == "delivered@resend.dev" && before.Result.IsVerified,
            "Owned warning actor must be verified at the fixed provider-controlled recipient.");
        dna.OASIS.Email.DisableAllEmails = false;
        try
        {
            // Credentials enter only already-booted memory, never the serialized fixture DNA.
            dna.OASIS.Email.ResendKey = testKey;
            dna.OASIS.Email.EmailFrom = sender;
            await RunOwnedBrowserAsync(new { baseUrl = address, token = new JwtSecurityTokenHandler().WriteToken(jwt),
                avatarId = actor.ToString("D") }, "warningEmailBrowserVerified");
            var deliveries = await client.GetDatabase(database).GetCollection<MongoDB.Bson.BsonDocument>("usage_notification_settings")
                .Find(new MongoDB.Bson.BsonDocument("kind", "usage-warning-email-v1")).ToListAsync();
            Require(deliveries.Count == 1, "Browser action created more than one warning event.");
            var delivery = await billingRepository!.GetWarningEmailAsync(Guid.Parse(deliveries[0]["eventId"].AsString));
            Require(delivery.Subject == "user:" + actor.ToString("D") && delivery.Email.To == "delivered@resend.dev" &&
                delivery.ProviderAcknowledgement.HasValue && delivery.ProviderAcknowledgement.Value != Guid.Empty,
                "Browser warning has no actual native provider acknowledgement or wrong recipient.");
            var after = await provider.LoadAvatarAsync(actor);
            var usage = await billingRepository.GetAggregateAsync(actor.ToString("D"), CancellationToken.None);
            Require(!after.IsError && after.Result!.VersionId == before.Result.VersionId && usage.MonthlyRequests == 24000,
                "Warning action mutated the avatar or quota fixture.");
            Console.WriteLine("WARNING_EMAIL_BROWSER_NATIVE_PROOF: actual SDK/fragment/button submitted one current warning, second explicit action reported already accepted; one persisted controlled-recipient event/provider acknowledgement, unchanged avatar/quota. Not inbox delivery/payment/full Startup/hosted acceptance.");
        }
        finally { dna.OASIS.Email.ResendKey = null; dna.OASIS.Email.EmailFrom = null; }
    }
    else if (mode == "mfa-browser")
    {
        var before = await provider.LoadAvatarAsync(actor);
        Require(!before.IsError && before.Result != null, "Owned browser actor unavailable.");
        await RunOwnedBrowserAsync(new { baseUrl = address, token = new JwtSecurityTokenHandler().WriteToken(jwt),
            avatarId = actor.ToString("D") }, "mfaSettingsBrowserVerified");
        var state = await httpMfa!.StatusAsync(actor);
        var events = await httpMfa.EventsAsync(actor, 50);
        Require(!state.Enabled && events.Any(item => item.Kind == "enrol-confirmed") &&
            events.Any(item => item.Kind == "recovery-regenerated") && events.Any(item => item.Kind == "disabled"),
            "Actual browser management did not persist its native MFA lifecycle.");
        var after = await provider.LoadAvatarAsync(actor);
        Require(!after.IsError && after.Result!.VersionId == before.Result.VersionId,
            "MFA Settings management changed the native avatar.");
        Console.WriteLine("MFA_SETTINGS_BROWSER_NATIVE_PROOF: actual SDK/fragment/controls enrollment, confirmation, recovery replacement, disable, clear/reopen and native audit; unchanged avatar. Not full site/full Startup/hosted acceptance.");
    }
    else if (mode == "session-global-http")
    {
        await GlobalSessionHttpQualification.RunAsync(http, provider, dna, actor, accounts[0].Username, mfaPassword, billingRepository!, client.GetDatabase(database),
            app.Services.GetRequiredService<AccountClosureAdmissionService>(), accounts[2].Id, verifyBillingAdmission: false,
            verifySessionBrowser: sessionBrowser ? async (current, independent) => {
                string SessionId(Newtonsoft.Json.Linq.JObject credentials) => new JwtSecurityTokenHandler()
                    .ReadJwtToken(credentials.Value<string>("jwtToken")).Claims.Single(claim => claim.Type == NextGenSoftware.OASIS.API.Core.Helpers.AvatarSessionIdentity.SessionClaim).Value;
                await RunOwnedBrowserAsync(new { baseUrl = address, token = current.Value<string>("jwtToken"), avatarId = actor.ToString("D"),
                    currentSessionId = SessionId(current), independentSessionId = SessionId(independent) }, "sessionSettingsBrowserVerified");
            } : null);
    }
    else if (mode is "mfa-http" or "mfa-middleware-http" or "session-global-http")
    {
        var authEndpoints = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Where(endpoint => string.Equals(endpoint.RoutePattern.RawText?.Trim('/'), "api/avatar/authenticate", StringComparison.OrdinalIgnoreCase) &&
                endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()?.HttpMethods.Contains("POST") == true).ToList();
        Require(authEndpoints.Count == 1 && authEndpoints[0].Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()?.ControllerName == "AvatarAuth",
            "MFA fixture must expose exactly the shipping WEB4 authentication endpoint, without the WEB5 proxy.");
        Func<string, Newtonsoft.Json.Linq.JToken, bool, Task>? middlewareProbe = null;
#if OASIS_SESSION_MIDDLEWARE_CHECK
        if (mode == "mfa-middleware-http")
            middlewareProbe = (stage, credentials, active) => SessionMiddlewareHttpQualification.ProbeAsync(http, stage, credentials, active, actor, dna);
#endif
        await MfaHttpQualification.RunAsync(http, provider, httpMfa!, () => mfaNow,
            () => mfaNow = mfaNow.AddSeconds(30), dna, actor, accounts[0].Username, mfaPassword, middlewareProbe);
#if OASIS_SESSION_MIDDLEWARE_CHECK
        if (mode == "mfa-middleware-http") SessionMiddlewareHttpQualification.VerifyComplete();
#endif
    }
    else if (mode == "mfa-native")
    {
        await MfaNativeQualification.RunAsync(dna, actor);
    }
    else if (mode == "session-list-http")
    {
        var endpointSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
        foreach (var contract in new[] {
            ("GET", "api/avatar/{avatarId}/sessions"),
            ("GET", "api/avatar/{avatarId}/sessions/stats"),
            ("POST", "api/avatar/{avatarId}/sessions"),
            ("PUT", "api/avatar/{avatarId}/sessions/{sessionId}"),
            ("POST", "api/avatar/{avatarId}/sessions/logout"),
            ("POST", "api/avatar/{avatarId}/sessions/logout-all") })
        {
            var matches = endpointSource.Endpoints.OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
                .Where(endpoint => string.Equals(endpoint.RoutePattern.RawText?.Trim('/'), contract.Item2, StringComparison.OrdinalIgnoreCase) &&
                    endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()?.HttpMethods.Contains(contract.Item1) == true)
                .ToList();
            Require(matches.Count == 1 && matches[0].Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()?.ControllerName == "AvatarSession",
                "Session route must have exactly one canonical controller owner: " + contract.Item1 + " " + contract.Item2);
        }
        var initial = await provider.LoadAvatarAsync(actor);
        Require(!initial.IsError && initial.Result?.RefreshTokens?.Count == 3, "Native session credential seed did not persist.");
        var initialVersion = initial.Result!.VersionId;
        string path = "/api/avatar/" + actor.ToString("D") + "/sessions";
        async Task<string> ReadSessionJson(HttpResponseMessage response, string stage)
        {
            var text = await response.Content.ReadAsStringAsync();
            Console.WriteLine("SESSION_HTTP_STAGE: " + stage + " status=" + (int)response.StatusCode +
                " contentType=" + response.Content.Headers.ContentType?.MediaType + " bodyCharacters=" + text.Length);
            Require(response.Content.Headers.ContentType?.MediaType == "application/json" && !string.IsNullOrWhiteSpace(text),
                "Session " + stage + " did not return its JSON contract (HTTP " + (int)response.StatusCode + ").");
            return text;
        }
        using var response = await http.GetAsync(path);
        var body = await ReadSessionJson(response, "initial-list");
        var envelope = JObject.Parse(body);
        Require(response.IsSuccessStatusCode && !envelope.Value<bool>("isError"), "Actual session-list HTTP failed.");
        var view = envelope["result"]!;
        var sessions = view["sessions"]!.ToObject<List<AvatarSession>>()!;
        Require(view.Value<int>("totalSessions") == 3 && view.Value<int>("activeSessions") == 1 && sessions.Count == 3,
            "Session-list counts do not match persisted credentials.");
        foreach (var credential in initial.Result.RefreshTokens)
        {
            byte[] digest;
            using (var hash = SHA256.Create())
                digest = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(actor.ToString("D") + "\n" + credential.Token));
            var id = BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant();
            var row = sessions.Single(item => item.Id == id);
            Require(row.AvatarId == actor && row.IsActive == credential.IsActive && row.IpAddress == credential.CreatedByIp &&
                row.LoginTime == credential.Created && row.ExpiresAt == credential.Expires && row.SecurityToken == null &&
                row.DeviceName == null && row.Location == null, "Session-list invented or changed stored credential evidence.");
            Require(!body.Contains(credential.Token, StringComparison.Ordinal), "Session-list leaked a refresh credential.");
        }
        Require(view.Value<int?>("failedLoginAttempts") == null && view.Value<int?>("sessionTimeoutMinutes") == null &&
            view.Value<string>("securityStatus") == "Not assessed", "Session-list invented security assessment or counters.");
        using var reload = await http.GetAsync(path);
        var repeated = JObject.Parse(await ReadSessionJson(reload, "repeat-list"))["result"]!["sessions"]!;
        Require(reload.IsSuccessStatusCode && repeated.Children<JObject>().Select(item => item.Value<string>("id"))
            .SequenceEqual(sessions.Select(item => item.Id)), "Persisted session public IDs changed across reads.");
        using var foreignRead = await http.GetAsync("/api/avatar/" + accounts[1].Id.ToString("D") + "/sessions");
        var foreignEnvelope = JObject.Parse(await ReadSessionJson(foreignRead, "foreign-refusal"));
        Require(foreignEnvelope.Value<bool>("isError") && foreignEnvelope["result"]?.Type is null or JTokenType.Null,
            "A non-Wizard read another avatar's session records.");
        var stored = await provider.LoadAvatarAsync(actor);
        Require(!stored.IsError && stored.Result!.VersionId == initialVersion && stored.Result.RefreshTokens.Count == 3,
            "Session reads mutated the native avatar.");
        Console.WriteLine("SESSION_LIST_HTTP_NATIVE_PROOF: actual signed MVC/manager/native Mongo read returned only persisted active/revoked/expired credentials, stable public hashes, no raw tokens/invented device/security values and unchanged native avatar. Not device tracking, create/update/revocation/JWT/other-provider/hosted acceptance.");
    }
    else if (mode == "billing-warning-provider")
    {
        Require(Environment.GetEnvironmentVariable("OASIS_WARNING_TEST_RESEND_ENABLED") == "true",
            "Real provider qualification requires the explicit isolated test gate.");
        var warningTestKey = Environment.GetEnvironmentVariable("OASIS_WARNING_TEST_RESEND_KEY");
        var warningSender = Environment.GetEnvironmentVariable("OASIS_WARNING_TEST_EMAIL_FROM");
        Require(!string.IsNullOrWhiteSpace(warningTestKey) && !string.IsNullOrWhiteSpace(warningSender),
            "Dedicated warning test credential and configured sender are required.");
        Require(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OASIS_RESEND_KEY")) &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RESEND_API_KEY")),
            "An ambient deployment email credential must not override the dedicated test credential.");
        const string recipient = "delivered@resend.dev";
        // Populate only the already-booted in-memory fixture DNA, never its serialized file.
        dna.OASIS.UsageVisibility = new UsageVisibilitySettings { EmailEnabled = true };
        dna.OASIS.Email.DisableAllEmails = false;
        var month = DateTime.UtcNow.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        var delivery = await billingRepository!.PrepareWarningEmailAsync("user:" + actor.ToString("D"), month, 80,
            warningSender, recipient, "OASIS isolated warning qualification", "<p>Isolated OASIS warning submission qualification.</p>");
        using var providerHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var gateway = new UsageWarningEmailGateway(providerHttp, TimeProvider.System);
        try
        {
            dna.OASIS.Email.ResendKey = warningTestKey;
            var accepted = await gateway.SendAsync(delivery.Email, CancellationToken.None);
            Require(!accepted.IsError && accepted.IsSaved && accepted.Result != Guid.Empty,
                "Real email provider did not acknowledge the isolated warning submission.");
            await billingRepository.AcknowledgeWarningEmailAsync(delivery.Email.EventId, accepted.Result);
            var replay = await gateway.SendAsync(delivery.Email, CancellationToken.None);
            Require(!replay.IsError && replay.IsSaved && replay.Result == accepted.Result,
                "Real provider idempotent replay did not return the original acceptance ID.");
            var stored = await billingRepository.GetWarningEmailAsync(delivery.Email.EventId);
            Require(stored != null && stored.ProviderAcknowledgement == accepted.Result && stored.Email == delivery.Email,
                "Native warning payload/provider acceptance readback differs.");
            Console.WriteLine("WARNING_PROVIDER_ACCEPTANCE_PROOF: real pinned Resend SDK submitted one owned event to the fixed provider-controlled test recipient; identical replay returned the same actual provider ID and Mongo persisted it. Two bounded API calls, no real recipient, no inbox/delivery-event/Portal/hosted acceptance.");
        }
        finally { dna.OASIS.Email.ResendKey = null; }
    }
    else if (mode == "billing-warning-http")
    {
        var notifications = client.GetDatabase(database).GetCollection<MongoDB.Bson.BsonDocument>("usage_notification_settings");
        Require(await notifications.CountDocumentsAsync(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty) == 0,
            "Warning HTTP fixture started with notification records.");
        using var preferences = await http.GetAsync("/api/subscription/usage/notifications");
        Require(preferences.IsSuccessStatusCode, "Warning capability GET failed.");
        var view = JObject.Parse(await preferences.Content.ReadAsStringAsync())["result"]!;
        Require(view.Value<bool>("emailSubmissionAvailable") && !view.Value<bool>("emailEnabled"),
            "Warning capability did not distinguish implemented action from disabled delivery.");
        using var refused = await http.PostAsJsonAsync("/api/subscription/usage/alerts/email", new { });
        var error = JObject.Parse(await refused.Content.ReadAsStringAsync());
        Require(refused.StatusCode == HttpStatusCode.ServiceUnavailable && error.Value<bool>("isError") &&
            !error.Value<bool>("isSaved"), "Disabled warning action claimed acceptance.");
        using var anonymous = new HttpClient { BaseAddress = http.BaseAddress, Timeout = TimeSpan.FromSeconds(15) };
        using var unsigned = await anonymous.PostAsJsonAsync("/api/subscription/usage/alerts/email", new { });
        Require(unsigned.StatusCode == HttpStatusCode.Unauthorized, "Anonymous warning action was not refused.");
        Require(await notifications.CountDocumentsAsync(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty) == 0,
            "Disabled or anonymous warning action created delivery/preferences records.");
        Console.WriteLine("WARNING_ACTION_HTTP_PROOF: actual MVC/repository/native Mongo capability GET, disabled503 and anonymous401 passed with zero notification/event writes. No real email/provider acceptance, persistence/admin/notification replay, shared cluster or hosted acceptance.");
    }
    else if (mode == "billing-warning-store")
    {
        var month = DateTime.UtcNow.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        var subject = "user:" + actor.ToString("D");
        var first = await billingRepository!.PrepareWarningEmailAsync(subject, month, 80,
            "warnings@example.test", accounts[0].Email, "Owned usage warning", "<p>Owned warning</p>");
        var repeated = await billingRepository.PrepareWarningEmailAsync(subject, month, 80,
            "changed@example.test", "changed@example.test", "Changed payload", "<p>Changed</p>");
        Require(first.Email == repeated.Email && first.Email.EventId != Guid.Empty &&
            first.ProviderAcknowledgement == null && repeated.ProviderAcknowledgement == null,
            "Repeated warning preparation replaced its immutable payload or claimed acceptance.");
        var reloaded = await billingRepository.GetWarningEmailAsync(first.Email.EventId);
        Require(reloaded == first, "Native warning event reload differs from its acknowledged persistence.");
        bool invalidAckRefused = false;
        try { await billingRepository.AcknowledgeWarningEmailAsync(first.Email.EventId, Guid.Empty); }
        catch (ArgumentException) { invalidAckRefused = true; }
        Require(invalidAckRefused && (await billingRepository.GetWarningEmailAsync(first.Email.EventId))!.ProviderAcknowledgement == null,
            "Empty provider acknowledgement marked the warning as accepted.");
        dna.OASIS.UsageVisibility = new UsageVisibilitySettings { EmailEnabled = false };
        using var emailHttp = new HttpClient();
        var disabled = await new UsageWarningEmailGateway(emailHttp, TimeProvider.System).SendAsync(first.Email, CancellationToken.None);
        Require(disabled.IsError && !disabled.IsSaved && disabled.Result == Guid.Empty,
            "Disabled email submission claimed provider acceptance.");
        Console.WriteLine("WARNING_EVENT_NATIVE_PROOF: real Mongo immutable repeated prepare/reload and empty-ack refusal passed; disabled real gateway refused before transport. No email provider acknowledgement/delivery acceptance, fake provider, shared backend, or prior HTTP replay.");
    }
    else if (mode == "billing-admin-http")
    {
        const string path = "/api/subscription/admin/app-accounts/owned-billing/owners";
        var request = new { ownerAvatarIds = new[] { wizardActor.ToString("D") }, reason = "Owned HTTP ownership replacement" };
        using var refused = await http.PostAsJsonAsync(path, request);
        Require(refused.StatusCode == HttpStatusCode.Forbidden, "Ordinary actor changed app ownership.");
        var unchanged = await billingRepository!.GetAppForOwnerAsync("owned-billing", actor.ToString("D"), false);
        Require(unchanged.OwnerAvatarIds.SequenceEqual(new[] { actor.ToString("D") }), "Refused owner command mutated app ownership.");
        var adminJwt = new JwtSecurityToken(claims: new[] { new Claim("sub", wizardActor.ToString("D")) },
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        using var command = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(request) };
        command.Headers.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(adminJwt));
        using var saved = await http.SendAsync(command);
        Require(saved.IsSuccessStatusCode, "Wizard owner command failed.");
        var reply = JObject.Parse(await saved.Content.ReadAsStringAsync());
        Require(!reply.Value<bool>("isError") && reply["result"]!["ownerAvatarIds"]!.Values<string>()
            .SequenceEqual(new[] { wizardActor.ToString("D") }), "Owner acknowledgement differs from the requested canonical IDs.");
        var stored = await billingRepository.GetAppForOwnerAsync("owned-billing", wizardActor.ToString("D"), false);
        Require(stored.OwnerAvatarIds.SequenceEqual(new[] { wizardActor.ToString("D") }) &&
            stored.UpdatedBy == wizardActor.ToString("D") && stored.Reason == request.reason,
            "Native owner mutation did not persist the authenticated administrator/reason.");
        Console.WriteLine("BILLING_ADMIN_RESTORATION_HTTP_PROOF: real native Wizard owner replacement acknowledged and persisted actor/reason; ordinary403 left ownership unchanged. No Stripe/shared cluster/full Startup/hosted acceptance.");
    }
    else if (mode == "billing-http")
    {
        const string path = "/api/subscription/app-owner/owned-billing/notifications";
        using var initial = await http.GetAsync(path);
        Require(initial.IsSuccessStatusCode, "Owned notification initial GET failed.");
        using var save = await http.PostAsJsonAsync(path, new { thresholds = new[] { 75, 95 },
            emailRequested = false, warningsEnabled = true });
        Require(save.IsSuccessStatusCode, "Owned notification POST failed.");
        var acknowledgement = Newtonsoft.Json.Linq.JObject.Parse(await save.Content.ReadAsStringAsync());
        Require(acknowledgement.Value<bool>("isSaved") && acknowledgement.Value<int>("savedCount") == 1,
            "Notification save did not acknowledge the persisted command.");
        using var reload = await http.GetAsync(path);
        Require(reload.IsSuccessStatusCode, "Owned notification reload failed.");
        var view = Newtonsoft.Json.Linq.JObject.Parse(await reload.Content.ReadAsStringAsync())["result"]!;
        Require(view["thresholds"]!.Values<int>().SequenceEqual(new[] { 75, 95 }) &&
            view.Value<bool>("warningsEnabled") && !view.Value<bool>("emailRequested"), "Notification HTTP readback differs.");
        var subject = "app:owned-billing:owner:" + actor.ToString("D");
        var stored = await billingRepository!.GetNotificationSettingsAsync(subject);
        Require(stored != null && stored.Thresholds.SequenceEqual(new[] { 75, 95 }) && stored.WarningsOverride == true &&
            !stored.EmailRequested && stored.UpdatedBy == actor.ToString("D"), "Native notification writer/readback differs.");
        var foreignJwt = new JwtSecurityToken(claims: new[] { new Claim("sub", wizardActor.ToString("D")) },
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        using var foreign = new HttpRequestMessage(HttpMethod.Post, path) {
            Content = JsonContent.Create(new { thresholds = new[] { 10 }, emailRequested = true, warningsEnabled = false }) };
        foreign.Headers.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(foreignJwt));
        using var refused = await http.SendAsync(foreign);
        Require(refused.StatusCode == HttpStatusCode.Forbidden, "Foreign app notification command was not refused.");
        Require(await billingRepository.GetNotificationSettingsAsync("app:owned-billing:owner:" + wizardActor.ToString("D")) == null,
            "Refused actor created notification preferences.");
        stored = await billingRepository.GetNotificationSettingsAsync(subject);
        Require(stored!.Thresholds.SequenceEqual(new[] { 75, 95 }) && stored.UpdatedBy == actor.ToString("D"),
            "Refused actor mutated owner notification preferences.");
        Console.WriteLine("BILLING_RESTORATION_HTTP_PROOF: actual signed MVC/repository/native Mongo notification GET-POST-GET acknowledged owner-bound preferences; foreign write403 left both subjects unchanged. No Stripe/email/shared cluster/full Startup/hosted acceptance.");
    }
    else if (mode == "profile-http")
    {
        async Task<JsonDocument> ReadSelfProfile()
        {
            using var response = await http.GetAsync("/api/avatar/profile?providerType=MongoDBOASIS");
            var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Require(response.IsSuccessStatusCode && !document.RootElement.GetProperty("isError").GetBoolean(),
                "Restored profile GET failed: " + response.StatusCode);
            return document;
        }
        using var opened = await ReadSelfProfile();
        var initial = opened.RootElement.GetProperty("result").GetProperty("profile");
        Require(initial.GetProperty("avatarId").GetGuid() == actor, "Profile GET returned a different actor.");
        var originalUsername = initial.GetProperty("username").GetString();
        var originalEmail = initial.GetProperty("email").GetString();
        var body = System.Text.Json.JsonSerializer.Serialize(new {
            avatarRevision = initial.GetProperty("avatarRevision"), detailRevision = initial.GetProperty("detailRevision"),
            changes = new { firstName = "Owned profile updated", address = "Owned profile address" }, providerType = "MongoDBOASIS" });
        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/avatar/profile") {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        using var response = await http.SendAsync(request);
        using var saved = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Require(response.IsSuccessStatusCode && !saved.RootElement.GetProperty("isError").GetBoolean() &&
            saved.RootElement.GetProperty("isSaved").GetBoolean(), "Restored atomic profile PATCH failed: " + response.StatusCode);
        var committed = saved.RootElement.GetProperty("result").GetProperty("profile");
        using var reloaded = await ReadSelfProfile();
        var observed = reloaded.RootElement.GetProperty("result").GetProperty("profile");
        Require(observed.GetProperty("avatarId").GetGuid() == actor &&
            observed.GetProperty("firstName").GetString() == "Owned profile updated" &&
            observed.GetProperty("address").GetString() == "Owned profile address" &&
            observed.GetProperty("username").GetString() == originalUsername && observed.GetProperty("email").GetString() == originalEmail,
            "Restored profile reload lost edited or unselected values.");
        var nativeAvatar = await provider.LoadAvatarAsync(actor);
        var nativeDetail = await provider.LoadAvatarDetailAsync(actor);
        Require(!nativeAvatar.IsError && !nativeDetail.IsError && nativeAvatar.Result != null && nativeDetail.Result != null &&
            nativeAvatar.Result.FirstName == "Owned profile updated" && nativeDetail.Result.Address == "Owned profile address" &&
            nativeAvatar.Result.Username == originalUsername && nativeAvatar.Result.Email == originalEmail &&
            nativeAvatar.Result.VersionId == committed.GetProperty("avatarRevision").GetProperty("versionId").GetGuid() &&
            nativeDetail.Result.VersionId == committed.GetProperty("detailRevision").GetProperty("versionId").GetGuid(),
            "Atomic profile acknowledgement differs from native account/detail persistence.");
        Console.WriteLine("PROFILE_RESTORATION_HTTP_PROOF: current restored controller/manager/router/native Mongo GET-PATCH-GET saved both selected fields, retained username/email and acknowledged exact native account/detail revisions. Prior provider matrix/browser cases not replayed; full Startup/paid admission/SSO/hosted excluded.");
    }
    else if (mode == "competition-publisher")
    {
        var leagueId = Guid.NewGuid(); var tournamentId = Guid.NewGuid(); var now = DateTime.UtcNow;
        var leaguePayload = new[] { new NextGenSoftware.OASIS.API.Core.Objects.League {
            Id = leagueId, Name = "Owned HTTP published league", MinScore = 0, MaxScore = 100, IsActive = true } };
        var tournamentPayload = new NextGenSoftware.OASIS.API.Core.Objects.Tournament {
            Id = tournamentId, Name = "Owned HTTP published tournament", CompetitionType = CompetitionType.Karma,
            Status = NextGenSoftware.OASIS.API.Core.Objects.TournamentStatus.Registration,
            StartDate = now.AddHours(2), EndDate = now.AddHours(3),
            RegistrationStart = now.AddHours(-1), RegistrationEnd = now.AddHours(1), MaxParticipants = 2,
            CreatedBy = actor };
        async Task PutPublication(string path, object payload, HttpStatusCode expectedStatus)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, path) {
                Content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json") };
            using var response = await http.SendAsync(request);
            Require(response.StatusCode == expectedStatus, "Publisher permission outcome differed: " + path + ": " + response.StatusCode);
            using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Require(result.RootElement.GetProperty("isError").GetBoolean() == (expectedStatus != HttpStatusCode.OK),
                "Publisher response did not preserve its actual permission outcome.");
            if (expectedStatus == HttpStatusCode.OK)
                Require(result.RootElement.GetProperty("isSaved").GetBoolean(), "Publisher save was not acknowledged.");
        }
        foreach (var status in new[] { HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized })
        {
            if (status == HttpStatusCode.Unauthorized) http.DefaultRequestHeaders.Authorization = null;
            await PutPublication("/api/competition/leagues/Karma/Yearly", leaguePayload, status);
            await PutPublication("/api/competition/tournaments", tournamentPayload, status);
        }
        var data = new HolonManager(provider, dna, ProviderManager.Instance);
        var deniedLeagues = await CompetitionManager.ForData(data).GetAvailableLeaguesAsync(CompetitionType.Karma, SeasonType.Yearly);
        var deniedTournaments = await CompetitionManager.ForData(data).GetActiveTournamentsAsync(CompetitionType.Karma);
        Require(!deniedLeagues.IsError && deniedLeagues.Result.Count == 0 && !deniedTournaments.IsError && deniedTournaments.Result.Count == 0,
            "Rejected publisher calls changed the stored catalogues.");
        var wizardJwt = new JwtSecurityToken(claims: new[] { new Claim("sub", wizardActor.ToString("D")) },
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(wizardJwt));
        await PutPublication("/api/competition/leagues/Karma/Yearly", leaguePayload, HttpStatusCode.OK);
        await PutPublication("/api/competition/tournaments", tournamentPayload, HttpStatusCode.OK);
        var leagues = await CompetitionManager.ForData(data).GetAvailableLeaguesAsync(CompetitionType.Karma, SeasonType.Yearly);
        var tournaments = await CompetitionManager.ForData(data).GetActiveTournamentsAsync(CompetitionType.Karma);
        Require(!leagues.IsError && leagues.Result.Count == 1 && leagues.Result[0].Id == leagueId,
            "Wizard league acknowledgement did not reach native storage.");
        Require(!tournaments.IsError && tournaments.Result.Count == 1 && tournaments.Result[0].Id == tournamentId &&
            tournaments.Result[0].CreatedBy == wizardActor, "Wizard tournament writer was not bound to the authenticated actor.");
        Console.WriteLine("COMPETITION_PUBLISHER_HTTP_PROOF: real MVC/native Mongo two anonymous401, two non-Wizard403, two Wizard acknowledged publications; denied calls left native catalogues empty and actual Wizard writer persisted. Browser/full Startup/SSO/hosted excluded.");
    }
    else if (mode == "competition-browser")
    {
        var data = new HolonManager(provider, dna, ProviderManager.Instance);
        var competition = CompetitionManager.ForData(data);
        const string leagueName = "Owned Competition league";
        const string tournamentName = "Owned Competition tournament";
        var leagues = await competition.SaveLeaguesAsync(CompetitionType.Karma, SeasonType.Yearly,
            new List<NextGenSoftware.OASIS.API.Core.Objects.League> {
                new() { Name = leagueName, MinScore = 0, MaxScore = 100, IsActive = true } });
        Require(!leagues.IsError && leagues.IsSaved, "Owned Competition league seed failed: " + leagues.Message);
        var score = await competition.UpdateAvatarScoreAsync(actor, CompetitionType.Karma, SeasonType.Yearly, 0);
        Require(!score.IsError && score.Result, "Owned Competition score seed failed: " + score.Message);
        var now = DateTime.UtcNow;
        var tournamentId = Guid.NewGuid();
        var tournament = await competition.SaveTournamentAsync(new NextGenSoftware.OASIS.API.Core.Objects.Tournament {
            Id = tournamentId, Name = tournamentName, CompetitionType = CompetitionType.Karma,
            Status = NextGenSoftware.OASIS.API.Core.Objects.TournamentStatus.Registration,
            StartDate = now.AddHours(2), EndDate = now.AddHours(3),
            RegistrationStart = now.AddHours(-1), RegistrationEnd = now.AddHours(1),
            MaxParticipants = 2, CreatedBy = actor });
        Require(!tournament.IsError && tournament.IsSaved, "Owned Competition tournament seed failed: " + tournament.Message);
        var before = await provider.LoadAvatarAsync(actor);
        Require(!before.IsError && before.Result != null, "Owned Competition actor read failed.");
        var actorBefore = JsonConvert.SerializeObject(before.Result);
        await RunOwnedBrowserAsync(new { baseUrl = address,
            token = new JwtSecurityTokenHandler().WriteToken(jwt), avatarId = actor,
            tournamentId, leagueName, tournamentName }, "competitionPopupVerified");
        var stored = await CompetitionManager.ForData(data).GetActiveTournamentsAsync(CompetitionType.Karma);
        Require(!stored.IsError && stored.Result != null, "Owned Competition native readback failed: " + stored.Message);
        var joined = stored.Result.Single(row => row.Id == tournamentId);
        Require(joined.CurrentParticipants == 1 && joined.Participants.Count == 1 && joined.Participants[0].AvatarId == actor,
            "Browser registration was not persisted exactly once for the signed actor.");
        var after = await provider.LoadAvatarAsync(actor);
        Require(!after.IsError && after.Result != null && JsonConvert.SerializeObject(after.Result) == actorBefore,
            "Competition browser changed the native avatar account.");
        Console.WriteLine("COMPETITION_POPUP_BROWSER_PROOF: actual Portal SDK/component and MVC/native Mongo leaderboard, rank/league, tournament counter, one persisted actor-bound join, refresh/reopen/logout verified; native avatar unchanged. Full Startup/SSO/hosted/publisher authorization excluded.");
    }
    else if (mode is "catalogue" or "catalogue-browser")
    {
        async Task<JsonDocument> ReadCatalogue(string route)
        {
            using var reply = await http.GetAsync(route);
            var document = JsonDocument.Parse(await reply.Content.ReadAsStringAsync());
            Require(reply.IsSuccessStatusCode && !document.RootElement.GetProperty("isError").GetBoolean(),
                "Real catalogue read failed: " + route + " HTTP " + (int)reply.StatusCode + ": " +
                (document.RootElement.TryGetProperty("message", out var message) ? message.GetString() : "No API error message."));
            return document;
        }
        if (mode == "catalogue")
        {
        foreach (var kind in new[] { "missions", "chapters" })
        {
            using var empty = await ReadCatalogue("/api/" + kind);
            Require(empty.RootElement.GetProperty("result").ValueKind == JsonValueKind.Array &&
                empty.RootElement.GetProperty("result").GetArrayLength() == 0, "Empty native catalogue was fabricated: " + kind);
        }
        }
        var data = new HolonManager(provider, dna, ProviderManager.Instance);
        var missionId = Guid.NewGuid(); var chapterId = Guid.NewGuid();
        var chapter = new Chapter { Id = chapterId, Name = "Owned catalogue chapter", Description = "Native chapter detail", ChapterDisplayName = "Act",
            STARNETDNA = new STARNETDNA { Id = chapterId, CreatedByAvatarId = actor, Version = "1.0.0", VersionSequence = 1 } };
        var mission = new Mission { Id = missionId, Name = "Owned catalogue mission", Description = "Native mission detail",
            STARNETDNA = new STARNETDNA { Id = missionId, CreatedByAvatarId = actor, Version = "1.0.0", VersionSequence = 1 } };
        var physicalChildId = Guid.NewGuid();
        mission.Children.Add(new Holon { Id = physicalChildId, Name = "Owned physical mission child", HolonType = HolonType.Holon });
        ((NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons.IMission)mission).Chapters.Add(chapter);
        Require(mission.Chapters.Count == 1 && mission.Chapters[0].Id == chapterId,
            "Existing IMission chapter mutation did not reach the persisted collection.");
        foreach (var holon in new NextGenSoftware.OASIS.API.Core.Interfaces.IHolon[] { mission, chapter })
        {
            holon.MetaData["STARNETHolonId"] = holon.Id.ToString();
            holon.MetaData["CreatedByAvatarId"] = actor.ToString();
            holon.MetaData["Active"] = "1";
        }
        var savedMission = await data.SaveHolonAsync<Mission>(mission, actor, saveChildren: true, recursive: true, providerType: ProviderType.MongoDBOASIS);
        Require(!savedMission.IsError && savedMission.IsSaved,
            "Owned mission persistence failed: " + savedMission.Message +
            "; code=" + savedMission.ErrorCode +
            "; cause=" + savedMission.Exception?.GetBaseException().GetType().FullName +
            ": " + savedMission.Exception?.GetBaseException().Message);
        if (mode == "catalogue")
        {
        string MissionReadbackFacts(NextGenSoftware.OASIS.Common.OASISResult<Mission> value)
            => System.Text.Json.JsonSerializer.Serialize(new { value.IsError, value.IsLoaded,
                id = value.Result?.Id, name = value.Result?.Name,
                physicalChildren = value.Result?.Children?.Select(member => member?.Id).ToArray(),
                chapters = value.Result?.Chapters?.Select(member => new { id = member?.Id, display = member?.ChapterDisplayName }).ToArray(),
                manifestPresent = (value.Result as Holon)?.ChildRoleManifestJson != null });
        var storedMission = await data.LoadHolonAsync<Mission>(missionId, loadChildren: false, providerType: ProviderType.MongoDBOASIS);
        Require(!storedMission.IsError && storedMission.Result != null && storedMission.Result.Name == mission.Name &&
            storedMission.Result.Chapters.Count == 0 && storedMission.Result.Children.Count == 0 &&
            (storedMission.Result as Holon)?.ChildRoleManifestJson != null,
            "Owned shallow mission read failed: " + storedMission.Message + "; " + storedMission.Exception?.GetBaseException().Message +
            "; observed=" + MissionReadbackFacts(storedMission));
        var detailedMission = await data.LoadHolonAsync<Mission>(missionId, loadChildren: true, recursive: true, providerType: ProviderType.MongoDBOASIS);
        Require(!detailedMission.IsError && detailedMission.Result != null && detailedMission.Result.Chapters.Count == 1 &&
            detailedMission.Result.Chapters[0].Id == chapterId && detailedMission.Result.Chapters[0].ChapterDisplayName == "Act" &&
            detailedMission.Result.Children.Count == 1 && detailedMission.Result.Children[0].Id == physicalChildId,
            "Owned explicit mission graph read failed: " + detailedMission.Message + "; " + detailedMission.Exception?.GetBaseException().Message +
            "; observed=" + MissionReadbackFacts(detailedMission));
        var storedChapter = await data.LoadHolonAsync<Chapter>(chapterId, loadChildren: false, providerType: ProviderType.MongoDBOASIS);
        Require(!storedChapter.IsError && storedChapter.Result != null && storedChapter.Result.Name == chapter.Name &&
            storedChapter.Result.ParentHolonId == missionId && storedChapter.Result.ChapterDisplayName == "Act" && ((NextGenSoftware.OASIS.API.Core.Interfaces.STAR.IQuestBase)storedChapter.Result).Status == QuestStatus.NotStarted &&
            Newtonsoft.Json.Linq.JObject.FromObject(storedChapter.Result).Value<string>("StatusDisplay") == "Act 0/0",
            "Owned typed chapter readback failed: " + storedChapter.Message + "; " +
            storedChapter.Exception?.GetBaseException().GetType().FullName + ": " + storedChapter.Exception?.GetBaseException().Message);
        foreach (var item in new[] { (kind: "missions", id: missionId, name: mission.Name), (kind: "chapters", id: chapterId, name: chapter.Name) })
        {
            foreach (var suffix in new[] { "", "/load-all-for-avatar" })
            {
                using var list = await ReadCatalogue("/api/" + item.kind + suffix);
                var records = list.RootElement.GetProperty("result");
                Require(records.ValueKind == JsonValueKind.Array && records.GetArrayLength() == 1 &&
                    records[0].GetProperty("id").GetGuid() == item.id && records[0].GetProperty("name").GetString() == item.name,
                    "Native catalogue identity/typed name was not preserved: " + item.kind + suffix);
                if (item.kind == "missions")
                    Require(records[0].GetProperty("chapters").GetArrayLength() == 0,
                        "A mission catalogue unexpectedly loaded chapter roles.");
            }
            using var detail = await ReadCatalogue("/api/" + item.kind + "/" + item.id);
            Require(detail.RootElement.GetProperty("result").GetProperty("id").GetGuid() == item.id &&
                detail.RootElement.GetProperty("result").GetProperty("name").GetString() == item.name,
                "Native detail identity/typed name was not preserved: " + item.kind);
            if (item.kind == "missions")
                Require(detail.RootElement.GetProperty("result").GetProperty("chapters").GetArrayLength() == 1 &&
                    detail.RootElement.GetProperty("result").GetProperty("chapters")[0].GetProperty("id").GetGuid() == chapterId,
                    "Real mission detail lost its persisted chapter collection.");
            else
                Require(detail.RootElement.GetProperty("result").GetProperty("statusDisplay").GetString() == "Act 0/0",
                    "Real chapter detail lost its persisted display name.");
            using var absent = await http.GetAsync("/api/" + item.kind + "/" + Guid.NewGuid());
            using var missing = JsonDocument.Parse(await absent.Content.ReadAsStringAsync());
            Require(missing.RootElement.GetProperty("isError").GetBoolean(), "Missing native detail became generated success: " + item.kind);
        }
        Console.WriteLine("CATALOGUE_HTTP_PROOF: real boot/JWT/MVC/native Mongo mission/chapter graph save, shallow catalogue/avatar reads, explicit typed child detail identities and missing-record errors verified. Browser/create/update/production Startup excluded.");
        }
        else
        {
            var questId = Guid.NewGuid();
            var quest = new Quest { Id = questId, Name = "Owned catalogue quest", Description = "Native popup quest",
                STARNETDNA = new STARNETDNA { Id = questId, CreatedByAvatarId = actor, Version = "1.0.0", VersionSequence = 1 },
                Objectives = new List<Objective> { new() { Id = Guid.NewGuid(), Title = "Owned catalogue objective" } } };
            quest.MetaData["STARNETHolonId"] = questId.ToString();
            quest.MetaData["CreatedByAvatarId"] = actor.ToString(); quest.MetaData["Active"] = "1";
            var savedQuest = await data.SaveHolonAsync<Quest>(quest, actor, saveChildren: false, providerType: ProviderType.MongoDBOASIS);
            Require(!savedQuest.IsError && savedQuest.IsSaved, "Owned popup quest seed failed: " + savedQuest.Message);
            await RunOwnedBrowserAsync(new { baseUrl = address,
                token = new JwtSecurityTokenHandler().WriteToken(jwt), avatarId = actor, missionId, questId, chapterId },
                "cataloguePopupsVerified");
            Console.WriteLine("CATALOGUE_POPUP_BROWSER_PROOF: actual Portal WEB5 SDK and Quests/Missions popups read real native catalogues, explicit details, search/refresh and clear on logout; ten GETs, zero browser mutations. Full Startup/SSO/hosted acceptance excluded.");
        }
    }
    else if (mode == "quest-empty")
    {
        foreach (var route in new[] { "/api/quests", "/api/quests/all-for-avatar", "/api/quests/all-for-avatar/game",
            "/api/quests/by-status/NotStarted", "/api/quests/by-status/NotStarted/game" })
        {
            using var reply = await http.GetAsync(route);
            using var empty = JsonDocument.Parse(await reply.Content.ReadAsStringAsync());
            Require(reply.IsSuccessStatusCode && !empty.RootElement.GetProperty("isError").GetBoolean() &&
                empty.RootElement.GetProperty("result").ValueKind == JsonValueKind.Array &&
                empty.RootElement.GetProperty("result").GetArrayLength() == 0,
                "Real empty quest catalogue was not preserved: " + route);
        }
        Console.WriteLine("QUEST_EMPTY_HTTP_PROOF: real boot/JWT/MVC/native empty quest catalogue and four avatar/status read actions remain empty; no generated records. Browser/nonempty quest/production Startup excluded.");
    }
    else
    {
    using var initial = await http.GetAsync("/api/GeoHotSpots/nearby?latitude=0&longitude=0&radiusKm=1");
    using var body = JsonDocument.Parse(await initial.Content.ReadAsStringAsync());
    Require(initial.IsSuccessStatusCode && !body.RootElement.GetProperty("isError").GetBoolean(),
        "First authenticated nearby HTTP read failed: " + (int)initial.StatusCode + ": " + body.RootElement.GetProperty("message").GetString());
    Require(body.RootElement.GetProperty("result").ValueKind == JsonValueKind.Array, "Nearby HTTP response is not a catalogue.");
    if (mode is "populated" or "browser")
    {
        var rows = body.RootElement.GetProperty("result").EnumerateArray().ToArray();
        Require(rows.Select(row => row.GetProperty("id").GetGuid()).SequenceEqual(expected),
            "Populated HTTP radius/visibility/activity/distance ordering returned unexpected canonical IDs: " +
            System.Text.Json.JsonSerializer.Serialize(new { expected, actual = rows.Select(row => new {
                id = row.GetProperty("id").GetGuid(), lat = row.GetProperty("lat").GetDouble(),
                active = row.GetProperty("isActive").GetBoolean(), visible = row.GetProperty("isVisibleOnMap").GetBoolean() }) }));
        Require(rows[0].GetProperty("lat").GetDouble() == 0 && rows[1].GetProperty("lat").GetDouble() == .001 &&
            rows.All(row => row.GetProperty("long").GetDouble() == 0 && row.GetProperty("hotSpotRadiusInMetres").GetInt32() == 17 &&
                row.GetProperty("textContent").GetString() == "Native HTTP geometry"), "Derived typed hotspot fields changed across HTTP serialization.");
        Console.WriteLine("HOTSPOT_POPULATED_HTTP_PROOF: five native hotspot roots through real boot/MVC/JWT; two nearby active visible records returned in distance order with exact typed fields; far/hidden/inactive excluded. Browser/production Startup excluded.");
        if (mode == "browser")
        {
            await RunOwnedBrowserAsync(new { baseUrl = address,
                token = new JwtSecurityTokenHandler().WriteToken(jwt), avatarId = actor,
                expectedGeoNFTs = 1, expectedHotspots = 2, expectedQuestLocations = 3 }, "geoAssetsBrowserVerified");
            Console.WriteLine("GEO_ASSETS_BROWSER_PROOF: actual Portal WEB4/WEB5 bundles, three native API catalogues, independent layer completion, quest requirement join and logout clearing passed; zero browser mutations. Renderer/full Startup/SSO/deployment excluded.");
        }
    }
    else
    {
    foreach (var query in new[] { "latitude=0", "longitude=0", "latitude=91&longitude=0", "latitude=0&longitude=0&radiusKm=0" })
    {
        using var invalid = await http.GetAsync("/api/GeoHotSpots/nearby?" + query);
        Require(invalid.StatusCode == HttpStatusCode.BadRequest, "Invalid coordinates were not rejected by real MVC: " + query);
    }
    http.DefaultRequestHeaders.Authorization = null;
    using var anonymous = await http.GetAsync("/api/GeoHotSpots/nearby?latitude=0&longitude=0");
    Require(anonymous.StatusCode == HttpStatusCode.Unauthorized, "Anonymous nearby read was not rejected.");
    Console.WriteLine("HOTSPOT_HTTP_NATIVE_PROOF: real isolated boot, MVC nearby action, signed native actor, zero coordinates, required/bounded coordinates, radius and anonymous rejection passed. Full production Startup/SSO/browser excluded.");
    }
    }
}
finally
{
    AvatarManager.SecondFactorGate = previousMfaGate;
    try { if (app != null) { await app.StopAsync(); await app.DisposeAsync(); } }
    finally
    {
        try { await provider.DeActivateProviderAsync(); }
        finally
        {
            try
            {
                await client.DropDatabaseAsync(database);
                Require(!(await (await client.ListDatabaseNamesAsync()).ToListAsync()).Contains(database), "Owned HTTP database cleanup failed.");
                Console.WriteLine("Owned hotspot HTTP database absence verified.");
            }
            finally
            {
                Require(Path.GetFullPath(dnaPath) == Path.Combine(Path.GetFullPath(Path.GetTempPath()), database + ".json"), "Owned DNA cleanup scope invalid.");
                if (File.Exists(dnaPath)) File.Delete(dnaPath);
                Require(!File.Exists(dnaPath), "Owned DNA cleanup failed.");
                Console.WriteLine("Owned hotspot HTTP DNA absence verified.");
            }
        }
    }
}

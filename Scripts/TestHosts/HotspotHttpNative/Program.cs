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
Require(mode is "binding" or "populated" or "browser" or "quest-empty" or "catalogue" or "catalogue-browser" or "competition-browser" or "competition-publisher" or "profile-http" or "billing-http", "Unsupported hotspot HTTP check mode.");
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
try
{
    var active = await provider.ActivateProviderAsync(); Require(!active.IsError, active.Message);
    var actor = Guid.NewGuid();
    var wizardActor = Guid.NewGuid();
    var accounts = new List<Avatar> {
        new Avatar { Id = actor, Username = "http-" + actor.ToString("N"), Email = actor.ToString("N") + "@example.test", AvatarType = new EnumValue<AvatarType>(AvatarType.User) },
        new Avatar { Id = Guid.NewGuid(), FirstName = "OASIS", LastName = "SYSTEM", Username = "root", Email = "anorak@oasisomniverse.one", AvatarType = new EnumValue<AvatarType>(AvatarType.System) } };
    if (mode is "competition-publisher" or "billing-http") accounts.Add(new Avatar { Id = wizardActor,
        Username = "wizard-" + wizardActor.ToString("N"), Email = wizardActor.ToString("N") + "@example.test",
        AvatarType = new EnumValue<AvatarType>(mode == "billing-http" ? AvatarType.User : AvatarType.Wizard) });
    foreach (var account in accounts)
    {
        account.IsActive = true; account.Version = 1; account.VersionId = Guid.NewGuid(); account.CreatedDate = DateTime.UtcNow;
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
    MongoSubscriptionUsageRepository? billingRepository = null;
    if (mode == "billing-http")
    {
        billingRepository = new MongoSubscriptionUsageRepository(client, database, TimeProvider.System);
        await billingRepository.CreateAppAccountAsync("owned-billing", "Owned billing route fixture", "free",
            actor.ToString("D"), "Owned isolated fixture", false, null, CancellationToken.None);
        await billingRepository.SetAppOwnersAsync("owned-billing", new[] { actor.ToString("D") },
            actor.ToString("D"), "Owned isolated fixture", CancellationToken.None);
        builder.Services.AddSingleton(billingRepository);
        builder.Services.AddSingleton<IUsageSeriesSource>(billingRepository);
    }
    var key = new SymmetricSecurityKey(secret);
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new() { ValidateIssuer = false, ValidateAudience = false,
            ValidateIssuerSigningKey = true, IssuerSigningKey = key, ValidateLifetime = true, ClockSkew = TimeSpan.Zero };
    });
    builder.Services.AddControllers().AddApplicationPart(typeof(GeoHotSpotsController).Assembly)
        .AddApplicationPart(typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.NftController).Assembly);
    app = builder.Build(); app.UseAuthentication();
    if (mode is "browser" or "catalogue-browser" or "competition-browser")
    {
        var portal = Environment.GetEnvironmentVariable("OASIS_GEO_PORTAL_ROOT") ?? throw new InvalidOperationException("Pinned Portal root required.");
        var browserHost = Environment.GetEnvironmentVariable("OASIS_GEO_BROWSER_HOST") ?? throw new InvalidOperationException("Geo browser host required.");
        Require(File.Exists(Path.Combine(portal, mode switch { "catalogue-browser" => "assets/js/missions-modal.js", "competition-browser" => "assets/js/competition-modal.js", _ => "assets/js/map-modal.js" })) && File.Exists(browserHost), "Actual Portal/browser assets missing.");
        app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(Path.Combine(portal, "assets")), RequestPath = "/assets" });
        app.MapGet(mode switch { "catalogue-browser" => "/__catalogue_browser", "competition-browser" => "/__competition_browser", _ => "/__geo_browser" }, () => Results.File(browserHost, "text/html"));
    }
    app.Use(async (context, next) => {
        if (context.User.Identity?.IsAuthenticated == true && Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id))
        {
            var loaded = await provider.LoadAvatarAsync(id);
            Require(!loaded.IsError && loaded.Result != null, "Signed fixture actor unavailable.");
            context.Items["Avatar"] = loaded.Result; context.Items["AvatarId"] = id;
            OASISRequestContext.CurrentAvatar = loaded.Result; OASISRequestContext.CurrentAvatarId = id;
        }
        await next(context);
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
    if (mode == "billing-http")
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

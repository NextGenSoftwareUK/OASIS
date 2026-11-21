using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using MongoDB.Driver;
using Newtonsoft.Json;
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
Require(mode is "binding" or "populated" or "browser" or "quest-empty" or "catalogue", "Unsupported hotspot HTTP check mode.");
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
    foreach (var account in new[] {
        new Avatar { Id = actor, Username = "http-" + actor.ToString("N"), Email = actor.ToString("N") + "@example.test", AvatarType = new EnumValue<AvatarType>(AvatarType.User) },
        new Avatar { Id = Guid.NewGuid(), FirstName = "OASIS", LastName = "SYSTEM", Username = "root", Email = "anorak@oasisomniverse.one", AvatarType = new EnumValue<AvatarType>(AvatarType.System) } })
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
    var key = new SymmetricSecurityKey(secret);
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new() { ValidateIssuer = false, ValidateAudience = false,
            ValidateIssuerSigningKey = true, IssuerSigningKey = key, ValidateLifetime = true, ClockSkew = TimeSpan.Zero };
    });
    builder.Services.AddControllers().AddApplicationPart(typeof(GeoHotSpotsController).Assembly)
        .AddApplicationPart(typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.NftController).Assembly);
    app = builder.Build(); app.UseAuthentication();
    if (mode == "browser")
    {
        var portal = Environment.GetEnvironmentVariable("OASIS_GEO_PORTAL_ROOT") ?? throw new InvalidOperationException("Pinned Portal root required.");
        var browserHost = Environment.GetEnvironmentVariable("OASIS_GEO_BROWSER_HOST") ?? throw new InvalidOperationException("Geo browser host required.");
        Require(File.Exists(Path.Combine(portal, "assets/js/map-modal.js")) && File.Exists(browserHost), "Actual Portal/browser assets missing.");
        app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(Path.Combine(portal, "assets")), RequestPath = "/assets" });
        app.MapGet("/__geo_browser", () => Results.File(browserHost, "text/html"));
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
    if (mode == "catalogue")
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
        foreach (var kind in new[] { "missions", "chapters" })
        {
            using var empty = await ReadCatalogue("/api/" + kind);
            Require(empty.RootElement.GetProperty("result").ValueKind == JsonValueKind.Array &&
                empty.RootElement.GetProperty("result").GetArrayLength() == 0, "Empty native catalogue was fabricated: " + kind);
        }
        var data = new HolonManager(provider, dna, ProviderManager.Instance);
        var missionId = Guid.NewGuid(); var chapterId = Guid.NewGuid();
        var mission = new Mission { Id = missionId, Name = "Owned catalogue mission", Description = "Native mission detail",
            STARNETDNA = new STARNETDNA { Id = missionId, CreatedByAvatarId = actor, Version = "1.0.0", VersionSequence = 1 } };
        var chapter = new Chapter { Id = chapterId, Name = "Owned catalogue chapter", Description = "Native chapter detail",
            STARNETDNA = new STARNETDNA { Id = chapterId, CreatedByAvatarId = actor, Version = "1.0.0", VersionSequence = 1 } };
        foreach (var holon in new NextGenSoftware.OASIS.API.Core.Interfaces.IHolon[] { mission, chapter })
        {
            holon.MetaData["STARNETHolonId"] = holon.Id.ToString();
            holon.MetaData["CreatedByAvatarId"] = actor.ToString();
            holon.MetaData["Active"] = "1";
        }
        var savedMission = await data.SaveHolonAsync<Mission>(mission, actor, saveChildren: false, providerType: ProviderType.MongoDBOASIS);
        Require(!savedMission.IsError && savedMission.IsSaved, "Owned mission persistence failed: " + savedMission.Message);
        var savedChapter = await data.SaveHolonAsync<Chapter>(chapter, actor, saveChildren: false, providerType: ProviderType.MongoDBOASIS);
        Require(!savedChapter.IsError && savedChapter.IsSaved, "Owned chapter persistence failed: " + savedChapter.Message);
        var storedMission = await data.LoadHolonAsync<Mission>(missionId, loadChildren: false, providerType: ProviderType.MongoDBOASIS);
        Require(!storedMission.IsError && storedMission.Result != null && storedMission.Result.Name == mission.Name,
            "Owned typed mission readback failed: " + storedMission.Message + "; " +
            storedMission.Exception?.GetBaseException().GetType().FullName + ": " + storedMission.Exception?.GetBaseException().Message);
        var storedChapter = await data.LoadHolonAsync<Chapter>(chapterId, loadChildren: false, providerType: ProviderType.MongoDBOASIS);
        Require(!storedChapter.IsError && storedChapter.Result != null && storedChapter.Result.Name == chapter.Name,
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
            }
            using var detail = await ReadCatalogue("/api/" + item.kind + "/" + item.id);
            Require(detail.RootElement.GetProperty("result").GetProperty("id").GetGuid() == item.id &&
                detail.RootElement.GetProperty("result").GetProperty("name").GetString() == item.name,
                "Native detail identity/typed name was not preserved: " + item.kind);
            using var absent = await http.GetAsync("/api/" + item.kind + "/" + Guid.NewGuid());
            using var missing = JsonDocument.Parse(await absent.Content.ReadAsStringAsync());
            Require(missing.RootElement.GetProperty("isError").GetBoolean(), "Missing native detail became generated success: " + item.kind);
        }
        Console.WriteLine("CATALOGUE_HTTP_PROOF: real boot/JWT/MVC/native Mongo mission/chapter empty and populated catalogue, avatar lists, typed detail identities and missing-record errors verified. Browser/create/update/production Startup excluded.");
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
            var script = Environment.GetEnvironmentVariable("OASIS_GEO_BROWSER_SCRIPT") ?? throw new InvalidOperationException("Geo browser verification script required.");
            Require(File.Exists(script), "Geo browser verification script missing.");
            var start = new ProcessStartInfo("node") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("-e");
            start.ArgumentList.Add("const fs=require('node:fs');require(process.argv[1]).verify(JSON.parse(fs.readFileSync(0,'utf8'))).then(x=>console.log(JSON.stringify(x))).catch(e=>{console.error(e.message);process.exitCode=1;});");
            start.ArgumentList.Add(script);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Geo browser process did not start.");
            var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            await process.StandardInput.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { baseUrl = address,
                token = new JwtSecurityTokenHandler().WriteToken(jwt), avatarId = actor, expectedGeoNFTs = 1, expectedHotspots = 2, expectedQuestLocations = 3 }));
            process.StandardInput.Close();
            try { using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90)); await process.WaitForExitAsync(timeout.Token); }
            finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
            var resultText = await output; var errorText = await errors;
            errorText = System.Text.RegularExpressions.Regex.Replace(errorText, @"[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}", "[redacted fixture bearer]");
            Require(process.ExitCode == 0, "Actual geo browser failed: " + errorText);
            using var result = JsonDocument.Parse(resultText);
            Require(result.RootElement.GetProperty("geoAssetsBrowserVerified").GetBoolean(), "Geo browser completion marker missing.");
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

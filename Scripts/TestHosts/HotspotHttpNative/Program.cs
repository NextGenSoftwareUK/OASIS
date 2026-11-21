using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
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
var dna = new OASISDNA { OASIS = new() {
    HyperDriveMode = HyperDriveModes.V2, OASISHyperDriveConfig = new(),
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
    var builder = WebApplication.CreateBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
    var key = new SymmetricSecurityKey(secret);
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new() { ValidateIssuer = false, ValidateAudience = false,
            ValidateIssuerSigningKey = true, IssuerSigningKey = key, ValidateLifetime = true, ClockSkew = TimeSpan.Zero };
    });
    builder.Services.AddControllers().AddApplicationPart(typeof(GeoHotSpotsController).Assembly);
    app = builder.Build(); app.UseAuthentication();
    app.Use(async (context, next) => {
        if (context.User.Identity?.IsAuthenticated == true && Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id))
        {
            var loaded = await provider.LoadAvatarAsync(id);
            Require(!loaded.IsError && loaded.Result != null, "Signed fixture actor unavailable.");
            context.Items["Avatar"] = loaded.Result; context.Items["AvatarId"] = id;
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
    using var initial = await http.GetAsync("/api/GeoHotSpots/nearby?latitude=0&longitude=0&radiusKm=1");
    using var body = JsonDocument.Parse(await initial.Content.ReadAsStringAsync());
    Require(initial.IsSuccessStatusCode && !body.RootElement.GetProperty("isError").GetBoolean(),
        "First authenticated nearby HTTP read failed: " + (int)initial.StatusCode + ": " + body.RootElement.GetProperty("message").GetString());
    Require(body.RootElement.GetProperty("result").ValueKind == JsonValueKind.Array, "Nearby HTTP response is not a catalogue.");
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

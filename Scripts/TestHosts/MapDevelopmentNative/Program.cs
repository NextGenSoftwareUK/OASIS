using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.Utilities;

const string ownedUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != ownedUri)
    throw new InvalidOperationException("Only the exact owned loopback fixture may be used.");
static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
var client = new MongoClient(ownedUri);
string database = "map_development_" + Guid.NewGuid().ToString("N");
Require(!(await (await client.ListDatabaseNamesAsync()).ToListAsync()).Contains(database), "Owned database already exists.");
var provider = new MongoDBOASIS(ownedUri, database);
try
{
    var active = await provider.ActivateProviderAsync(); Require(!active.IsError, active.Message);
    var owner = Guid.NewGuid(); var reader = Guid.NewGuid();
    foreach (var actor in new[] { owner, reader })
    {
        var account = await provider.SaveAvatarAsync(new Avatar { Id = actor, Username = "map-" + actor.ToString("N"),
            Email = actor.ToString("N") + "@example.test", IsActive = true, Version = 1, VersionId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow, AvatarType = new EnumValue<AvatarType>(AvatarType.User) });
        Require(!account.IsError && account.Result != null, "Owned account seed failed: " + account.Message);
        var detail = await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = actor, Username = account.Result.Username,
            Email = account.Result.Email, Version = 1, VersionId = Guid.NewGuid(), CreatedDate = DateTime.UtcNow });
        Require(!account.IsError && !detail.IsError, "Owned actor seed failed.");
    }
    async Task<string> NativeAccount(Guid actor) => (await client.GetDatabase(database)
        .GetCollection<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar>("Avatar")
        .Find(a => a.HolonId == actor).Project<BsonDocument>(new BsonDocument()).SingleAsync()).ToJson();
    var ownerBefore = await NativeAccount(owner); var readerBefore = await NativeAccount(reader);
    var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
        HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings(),
        SubscriptionConfig = new() { PlanType = "Gold" } } };
    var routing = new ProviderManager(null, dna); Require(routing.RegisterProvider(provider), "Provider registration failed.");
    var configured = routing.PublishRoutingOverrides(new ProviderRoutingOverrides(autoFailoverEnabled: false,
        autoReplicationEnabled: false, autoLoadBalancingEnabled: false), ProviderType.MongoDBOASIS);
    Require(!configured.IsError, configured.Message);
    HolonManager Data() => new(provider, dna, routing);
    var location = new MapLocation { Id = Guid.NewGuid(), Name = "Development candidate destination",
        Latitude = 0, Longitude = 0, IsActive = true };
    var record = location.ToHolon(); record.IsPublic = true;
    var stored = await Data().SaveHolonAsync(record, owner, saveChildren: false);
    Require(!stored.IsError && stored.IsSaved, stored.Message);
    var map = new MapManager(Data(), owner);
    var nearby = await map.GetNearbyLocationsAsync(owner, 0, 0, 1);
    Require(!nearby.IsError && nearby.Result.Count == 1 && nearby.Result[0].Id == location.Id,
        "Combined Development catalogue did not return the native persisted location: " + nearby.Message);
    var visited = await map.VisitLocationAsync(owner, location.Id, "Scoped Development integration visit");
    Require(!visited.IsError && visited.Result && visited.IsSaved && !visited.IsWarning,
        "Combined Development visit/scoring failed: " + visited.Message);
    foreach (var season in new[] { SeasonType.Daily, SeasonType.Weekly, SeasonType.Monthly })
    {
        var scores = await CompetitionManager.ForData(Data()).GetLeaderboardAsync(CompetitionType.SocialActivity, season);
        Require(!scores.IsError && scores.Result.Count == 1 && scores.Result[0].AvatarId == owner && scores.Result[0].Score == 5,
            "Combined Development score did not persist: " + season + ": " + scores.Message);
    }
    async Task<MapRoutesController> Routes(Guid actor, Guid? operation = null)
    {
        var controller = new MapRoutesController();
        typeof(MapRoutesController).GetField("data", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(controller, Data());
        var context = new DefaultHttpContext(); var avatar = await provider.LoadAvatarAsync(actor);
        Require(!avatar.IsError && avatar.Result != null, "Owned actor lookup failed."); context.Items["Avatar"] = avatar.Result;
        if (operation.HasValue) context.Request.Headers["X-COSMIC-Operation-Id"] = operation.Value.ToString("D");
        controller.ControllerContext = new ControllerContext { HttpContext = context }; return controller;
    }
    var id = Guid.NewGuid();
    var geometryId = (Guid)typeof(MapRoutesController).GetMethod("Geometry", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [id])!;
    var operation = Guid.NewGuid();
    var create = new MapRoutesController.CreateRouteRequest { OperationId = operation,
        ExpectedRevisions = new Dictionary<Guid, Guid?> { [id] = null, [geometryId] = null },
        Route = new MapRoutesController.RecordedRoute { Id = id, Name = "Development private walk", SourceApp = "Our World",
            SourceRouteId = "combined-development-walk", IsPublic = false,
            Segments = [[new() { Latitude = 0, Longitude = 0, Utc = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc).AddTicks(1234567) },
                new() { Latitude = 0, Longitude = .001 }]] } };
    var created = await (await Routes(owner, operation)).Create(create);
    Require(!created.IsError && created.IsSaved && created.Result.Count() == 2, "Combined UTC helper create failed: " + created.Message);
    var detailResult = await (await Routes(owner)).Get(id); Require(!detailResult.IsError, detailResult.Message);
    var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    var detail = JsonSerializer.SerializeToElement(detailResult.Result, json);
    var revisions = new Dictionary<Guid, Guid?> { [id] = detail.GetProperty("expectedRevisions").GetProperty(id.ToString("D")).GetGuid(),
        [geometryId] = detail.GetProperty("expectedRevisions").GetProperty(geometryId.ToString("D")).GetGuid() };
    Require((await (await Routes(reader)).Get(id)).IsError, "Private route leaked to another actor.");
    var change = Guid.NewGuid();
    var updated = await (await Routes(owner, change)).UpdateAccess(id, new MapRoutesController.RouteAccessRequest {
        OperationId = change, ExpectedRevisions = revisions, IsPublic = true, Name = "Development shared walk" });
    Require(!updated.IsError && updated.IsSaved, "Combined UTC helper access update failed: " + updated.Message);
    var shared = await (await Routes(reader)).Get(id); Require(!shared.IsError, shared.Message);
    var current = JsonSerializer.SerializeToElement(shared.Result, json);
    Require(current.GetProperty("geometry").GetProperty("metaData").GetProperty("payload").GetString() ==
        detail.GetProperty("geometry").GetProperty("metaData").GetProperty("payload").GetString(), "Access edit changed geometry.");
    Require(ownerBefore == await NativeAccount(owner) && readerBefore == await NativeAccount(reader), "Map integration altered actor accounts.");
    Console.WriteLine("MAP_DEVELOPMENT_NATIVE_PROOF: one new combined-source case; actual Mongo catalogue/visit/three score periods and current UTC batch helper private/public route writes passed; actor accounts unchanged. HTTP/browser/Unity/deployed acceptance excluded.");
}
finally
{
    try { await provider.DeActivateProviderAsync(); }
    finally
    {
        Require(database.StartsWith("map_development_", StringComparison.Ordinal), "Cleanup scope invalid.");
        await client.DropDatabaseAsync(database);
        Require(!(await (await client.ListDatabaseNamesAsync()).ToListAsync()).Contains(database), "Owned database cleanup failed.");
        Console.WriteLine("Owned Map Development database absence verified.");
    }
}

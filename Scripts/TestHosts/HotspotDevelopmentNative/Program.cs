using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.Utilities;

const string ownedUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != ownedUri)
    throw new InvalidOperationException("Only the exact owned loopback fixture may be used.");
static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
var client = new MongoClient(ownedUri);
string database = "hotspot_development_" + Guid.NewGuid().ToString("N");
Require(!(await (await client.ListDatabaseNamesAsync()).ToListAsync()).Contains(database), "Owned database already exists.");
var provider = new MongoDBOASIS(ownedUri, database);
try
{
    var active = await provider.ActivateProviderAsync(); Require(!active.IsError, active.Message);
    var owner = Guid.NewGuid(); var reader = Guid.NewGuid();
    foreach (var actor in new[] { owner, reader })
    {
        var account = await provider.SaveAvatarAsync(new Avatar { Id = actor,
            Username = "hotspot-" + actor.ToString("N"), Email = actor.ToString("N") + "@example.test",
            IsActive = true, Version = 1, VersionId = Guid.NewGuid(), CreatedDate = DateTime.UtcNow,
            AvatarType = new EnumValue<AvatarType>(AvatarType.User) });
        Require(!account.IsError && account.Result != null, "Owned actor seed failed: " + account.Message);
        var detail = await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = actor,
            Username = account.Result.Username, Email = account.Result.Email, Version = 1,
            VersionId = Guid.NewGuid(), CreatedDate = DateTime.UtcNow });
        Require(!detail.IsError && detail.Result != null, "Owned actor detail seed failed: " + detail.Message);
    }
    var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
        HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings(),
        SubscriptionConfig = new() { PlanType = "Gold" } } };
    var routing = new ProviderManager(null, dna); Require(routing.RegisterProvider(provider), "Provider registration failed.");
    var configured = routing.PublishRoutingOverrides(new ProviderRoutingOverrides(autoFailoverEnabled: false,
        autoReplicationEnabled: false, autoLoadBalancingEnabled: false), ProviderType.MongoDBOASIS);
    Require(!configured.IsError, configured.Message);
    HolonManager Data() => new(provider, dna, routing);
    async Task<Guid> Seed(Guid creator, bool published, GeoHotSpotType category, double latitude)
    {
        var id = Guid.NewGuid();
        var hotspot = new GeoHotSpot { Id = id, Name = "Owned hotspot " + id.ToString("N"),
            Lat = latitude, Long = -86.8515, IsActive = true, IsVisibleOnMap = true,
            HotSpotRadiusInMetres = 17, TextContent = "Native typed hydration",
            STARNETDNA = new STARNETDNA { Id = id, CreatedByAvatarId = creator, Version = "1.0.0",
                VersionSequence = 1, PublishedOnSTARNET = published,
                PublishedOn = published ? DateTime.UtcNow : DateTime.MinValue } };
        hotspot.MetaData["STARNETHolonId"] = id.ToString("D");
        hotspot.MetaData["GeoHotSpotType"] = category.ToString();
        var saved = await Data().SaveHolonAsync<GeoHotSpot>(hotspot, creator, saveChildren: false,
            providerType: ProviderType.MongoDBOASIS);
        Require(!saved.IsError && saved.IsSaved, "Owned typed hotspot seed failed: " + saved.Message);
        return id;
    }
    var own = await Seed(owner, false, GeoHotSpotType.Map, 21.1619);
    var hidden = await Seed(reader, false, GeoHotSpotType.AR, 21.1620);
    var published = await Seed(reader, true, GeoHotSpotType.Map, 21.1621);
    var manager = new GeoHotSpotManager(provider, owner, new STARDNA(), dna) { Data = Data() };
    var catalogue = await manager.LoadAllShallowAsync(owner, GeoHotSpotType.Map, providerType: ProviderType.MongoDBOASIS);
    Require(!catalogue.IsError && catalogue.Result != null, "Typed catalogue failed: " + catalogue.Message);
    var rows = catalogue.Result.ToList();
    Require(rows.Select(x => x.Id).ToHashSet().SetEquals(new[] { own, published }), "Owner/published visibility is incorrect.");
    var hydrated = rows.Single(x => x.Id == own);
    Require(hydrated.Lat == 21.1619 && hydrated.Long == -86.8515 && hydrated.IsVisibleOnMap &&
        hydrated.HotSpotRadiusInMetres == 17 && hydrated.TextContent == "Native typed hydration",
        "Derived hotspot properties were not hydrated from native persisted metadata.");
    var subtype = await manager.LoadAllShallowAsync(owner, GeoHotSpotType.Map, loadAllTypes: false,
        providerType: ProviderType.MongoDBOASIS);
    Require(!subtype.IsError && subtype.Result != null && subtype.Result.Select(x => x.Id).ToHashSet().SetEquals(new[] { own, published }),
        "Requested subtype/provider query failed: " + subtype.Message);
    var anonymous = await manager.LoadAllShallowAsync(Guid.Empty, GeoHotSpotType.Map, providerType: ProviderType.MongoDBOASIS);
    Require(!anonymous.IsError && anonymous.Result != null && anonymous.Result.Select(x => x.Id).ToHashSet().SetEquals(new[] { published }),
        "Anonymous catalogue exposed an unpublished hotspot.");
    Require(!rows.Any(x => x.Id == hidden), "Another owner's unpublished hotspot leaked.");
    Console.WriteLine("HOTSPOT_DEVELOPMENT_NATIVE_PROOF: real Mongo typed metadata hydration, owner/published visibility, subtype selection and explicit Mongo provider passed. Descendant fetch, HTTP binding, browser and deployed acceptance excluded.");
}
finally
{
    try { await provider.DeActivateProviderAsync(); }
    finally
    {
        Require(database.StartsWith("hotspot_development_", StringComparison.Ordinal), "Cleanup scope invalid.");
        await client.DropDatabaseAsync(database);
        Require(!(await (await client.ListDatabaseNamesAsync()).ToListAsync()).Contains(database), "Owned database cleanup failed.");
        Console.WriteLine("Owned hotspot Development database absence verified.");
    }
}

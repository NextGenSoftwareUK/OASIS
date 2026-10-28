using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// WEB4 sandbox controls, on nodes that enable them (OASIS_SANDBOX=true); answers 404 everywhere else. Every
    /// control is scoped to the calling avatar, so the sandbox can run on a shared node without affecting other users.
    /// </summary>
    [Route("api/sandbox")]
    [ApiController]
    public class SandboxController : OASISControllerBase
    {
        /// <summary>How long a provider stays offline before it restores itself, so a demo can never leave it down.</summary>
        public static readonly TimeSpan MaxOfflineDuration = TimeSpan.FromMinutes(2);

        /// <summary>The databases sandbox demo avatars use for failover, replication and load balancing.</summary>
        public const string SandboxProviders = "MongoDBOASIS, SQLLiteDBOASIS, Neo4jOASIS";

        /// <summary>
        /// Creates a fresh, verified demo avatar for this visitor and signs it in. Sandbox avatars live only in the
        /// sandbox's own databases, so visitors can try everything without an OASIS account.
        /// </summary>
        [HttpPost("session")]
        public async Task<ActionResult<OASISResult<IAvatar>>> StartSession()
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            string suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
            string username = $"sandbox-{suffix}";
            string password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));

            OASISResult<IAvatar> registerResult = await Program.AvatarManager.RegisterAsync("Explorer", "Sandbox", suffix,
                $"{username}@sandbox.oasisomniverse.one", password, username, AvatarType.User, OASISType.OASISAPIREST, suppressVerificationEmail: true);

            if (registerResult.IsError || registerResult.Result == null)
                return BadRequest(new OASISResult<IAvatar> { IsError = true, Message = $"Could not create a sandbox avatar: {registerResult.Message}" });

            IAvatar avatar = registerResult.Result;
            // Demo avatars route only across the sandbox's three databases (their own HyperDrive settings), so a
            // visitor's saves and failovers never touch the chains or other providers in the node's DNA lists.
            OASISResult<Dictionary<string, object>> routing = AvatarHyperDriveSettings.Normalize(new Dictionary<string, object>
            {
                [AvatarHyperDriveSettings.AutoFailoverEnabled] = true,
                [AvatarHyperDriveSettings.AutoReplicationEnabled] = true,
                [AvatarHyperDriveSettings.AutoLoadBalancingEnabled] = true,
                [AvatarHyperDriveSettings.AutoFailoverProviders] = SandboxProviders,
                [AvatarHyperDriveSettings.AutoReplicationProviders] = SandboxProviders,
                [AvatarHyperDriveSettings.LoadBalancingProviders] = SandboxProviders
            }, provider => ProviderManager.Instance.GetStorageProvider(provider) != null);
            if (routing.IsError)
                return BadRequest(new OASISResult<IAvatar> { IsError = true, Message = $"This node cannot host the sandbox: {routing.Message}" });
            avatar.MetaData ??= new Dictionary<string, object>();
            avatar.MetaData[AvatarHyperDriveSettings.PreferencesKey] = new Dictionary<string, object> { [AvatarHyperDriveSettings.Category] = routing.Result };
            avatar.Verified = DateTime.UtcNow;
            avatar.VerificationToken = null;
            avatar.IsActive = true;
            OASISResult<IAvatar> saveResult = await Program.AvatarManager.SaveAvatarAsync(avatar);

            if (saveResult.IsError)
                return BadRequest(new OASISResult<IAvatar> { IsError = true, Message = $"Could not verify the sandbox avatar: {saveResult.Message}" });

            OASISResult<IAvatar> authResult = await Program.AvatarManager.AuthenticateAsync(username, password, HttpContext.Connection.RemoteIpAddress?.ToString());

            if (authResult.IsError || authResult.Result == null)
                return BadRequest(new OASISResult<IAvatar> { IsError = true, Message = $"Could not sign in the sandbox avatar: {authResult.Message}" });

            return Ok(authResult);
        }

        /// <summary>Lists providers this avatar has taken offline and when each comes back.</summary>
        [Authorize]
        [HttpGet("faults")]
        public ActionResult<OASISResult<Dictionary<string, DateTime>>> GetFaults()
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            return Ok(new OASISResult<Dictionary<string, DateTime>>
            {
                Result = ProviderFaultGate.GetFaulted(AvatarId).ToDictionary(x => x.Key.ToString(), x => x.Value),
                Message = "Sandbox provider faults retrieved."
            });
        }

        /// <summary>Counts of this avatar's requests each offline provider has refused.</summary>
        [Authorize]
        [HttpGet("refusals")]
        public ActionResult<OASISResult<Dictionary<string, long>>> GetRefusals()
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            return Ok(new OASISResult<Dictionary<string, long>>
            {
                Result = ProviderFaultGate.GetRefusals(AvatarId).ToDictionary(x => x.Key.ToString(), x => x.Value),
                Message = "Sandbox provider refusals retrieved."
            });
        }

        /// <summary>Takes a provider offline for up to two minutes so HyperDrive must fail over.</summary>
        [Authorize]
        [HttpPost("providers/{providerType}/offline")]
        public ActionResult<OASISResult<DateTime>> TakeOffline(ProviderType providerType, [FromQuery] int seconds = 60)
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            if (!ProviderManager.Instance.GetAllRegisteredProviders().Any(x => x.ProviderType.Value == providerType))
                return BadRequest(new OASISResult<DateTime> { IsError = true, Message = $"{providerType} is not registered on the sandbox." });

            TimeSpan duration = TimeSpan.FromSeconds(Math.Clamp(seconds, 5, (int)MaxOfflineDuration.TotalSeconds));
            ProviderFaultGate.Fault(AvatarId, providerType, duration);

            return Ok(new OASISResult<DateTime>
            {
                Result = DateTime.UtcNow.Add(duration),
                Message = $"{providerType} is offline for your requests for {duration.TotalSeconds:0} seconds. Nobody else is affected."
            });
        }

        /// <summary>Brings a provider back online immediately.</summary>
        [Authorize]
        [HttpPost("providers/{providerType}/online")]
        public ActionResult<OASISResult<bool>> BringOnline(ProviderType providerType)
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            ProviderFaultGate.Restore(AvatarId, providerType);
            return Ok(new OASISResult<bool> { Result = true, Message = $"{providerType} is back online." });
        }
        /// <summary>
        /// Writes a small fixed dataset into the sandbox's native-apps Mongo database, standing in for an existing app that
        /// has never heard of OASIS: customers keyed by ObjectId, products by SKU string, orders by number with nested items.
        /// Idempotent (records are replaced by id), so visitors can call it any time before importing.
        /// </summary>
        [Authorize]
        [HttpPost("native-demo")]
        public async Task<ActionResult<OASISResult<Dictionary<string, long>>>> SeedNativeDemo()
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            OASISResult<IOASISStorageProvider> resolved = ProviderManager.Instance.ResolveStorageProvider(ProviderType.MongoDBOASIS);
            if (resolved.IsError || resolved.Result is not MongoDBOASIS mongo || mongo.Database?.MongoClient == null)
                return BadRequest(new OASISResult<Dictionary<string, long>> { IsError = true, Message = $"MongoDBOASIS is not available: {resolved.Message}" });
            if (string.IsNullOrWhiteSpace(mongo.NativeImportDatabaseName) || mongo.NativeImportDatabaseName == mongo.DBName)
                return BadRequest(new OASISResult<Dictionary<string, long>> { IsError = true, Message = "The sandbox DNA must set MongoDBOASIS.NativeImportDBName to a database other than the OASIS one." });

            IMongoDatabase database = mongo.Database.MongoClient.GetDatabase(mongo.NativeImportDatabaseName);
            var counts = new Dictionary<string, long>
            {
                ["customers"] = await Upsert(database, "customers", NativeDemoCustomers()),
                ["products"] = await Upsert(database, "products", NativeDemoProducts()),
                ["orders"] = await Upsert(database, "orders", NativeDemoOrders())
            };
            return Ok(new OASISResult<Dictionary<string, long>>(counts) { Message = $"Demo shop data is ready in '{mongo.NativeImportDatabaseName}'." });
        }

        private static async Task<long> Upsert(IMongoDatabase database, string collection, IEnumerable<BsonDocument> documents)
        {
            IMongoCollection<BsonDocument> target = database.GetCollection<BsonDocument>(collection);
            var writes = documents.Select(document => new ReplaceOneModel<BsonDocument>(
                Builders<BsonDocument>.Filter.Eq("_id", document["_id"]), document) { IsUpsert = true }).ToList();
            await target.BulkWriteAsync(writes);
            return writes.Count;
        }

        private static IEnumerable<BsonDocument> NativeDemoCustomers() => new[]
        {
            ("665f1c2a9b1e8a0001a1c001", "Ada Lovelace", "London", "gold", 1842),
            ("665f1c2a9b1e8a0001a1c002", "Alan Turing", "Manchester", "silver", 1936),
            ("665f1c2a9b1e8a0001a1c003", "Grace Hopper", "New York", "gold", 1952),
            ("665f1c2a9b1e8a0001a1c004", "Katherine Johnson", "Hampton", "bronze", 1961),
            ("665f1c2a9b1e8a0001a1c005", "Tim Berners-Lee", "Geneva", "silver", 1989)
        }.Select(c => new BsonDocument
        {
            { "_id", ObjectId.Parse(c.Item1) }, { "name", c.Item2 }, { "city", c.Item3 }, { "tier", c.Item4 },
            { "customerSince", new DateTime(c.Item5, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, { "marketingOptIn", c.Item5 % 2 == 0 }
        });

        private static IEnumerable<BsonDocument> NativeDemoProducts() => new[]
        {
            ("SKU-ENGINE", "Analytical Engine kit", "Brass gears, punch cards included.", 249.00m, new[] { "hardware", "classic" }),
            ("SKU-BOMBE", "Bombe replica", "Rotor drums for the patient codebreaker.", 499.50m, new[] { "hardware", "crypto" }),
            ("SKU-COBOL", "COBOL field guide", "Readable by managers, they said.", 19.99m, new[] { "book" }),
            ("SKU-WWW", "World Wide Web starter", "One server, one browser, endless links.", 0m, new[] { "software", "open" })
        }.Select(p => new BsonDocument
        {
            { "_id", p.Item1 }, { "title", p.Item2 }, { "summary", p.Item3 }, { "price", new BsonDecimal128(p.Item4) },
            { "tags", new BsonArray(p.Item5) }, { "inStock", p.Item4 > 0 }
        });

        private static IEnumerable<BsonDocument> NativeDemoOrders() => new[]
        {
            (1001, "665f1c2a9b1e8a0001a1c001", "SKU-ENGINE", 1, 249.00m, 3),
            (1002, "665f1c2a9b1e8a0001a1c003", "SKU-COBOL", 3, 59.97m, 5),
            (1003, "665f1c2a9b1e8a0001a1c002", "SKU-BOMBE", 1, 499.50m, 9),
            (1004, "665f1c2a9b1e8a0001a1c005", "SKU-WWW", 2, 0m, 12),
            (1005, "665f1c2a9b1e8a0001a1c001", "SKU-COBOL", 1, 19.99m, 20)
        }.Select(o => new BsonDocument
        {
            { "_id", o.Item1 }, { "customerId", ObjectId.Parse(o.Item2) }, { "status", o.Item6 > 10 ? "delivered" : "shipped" },
            { "total", new BsonDecimal128(o.Item5) }, { "placedAt", new DateTime(2026, 10, o.Item6, 9, 30, 0, DateTimeKind.Utc) },
            { "items", new BsonArray { new BsonDocument { { "sku", o.Item3 }, { "quantity", o.Item4 } } } }
        });
    }
}

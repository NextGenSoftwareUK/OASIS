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
        private readonly Services.Sandbox.SandboxSubscriptionLifecycle subscriptions;

        public SandboxController(Services.Sandbox.SandboxSubscriptionLifecycle subscriptions) => this.subscriptions = subscriptions;
        /// <summary>How long a provider stays offline before it restores itself, so a demo can never leave it down.</summary>

        /// <summary>The databases sandbox demo avatars use for failover, replication and load balancing.</summary>

        /// <summary>The providers a demo avatar routes across (DNA OASIS.Sandbox.Providers), in the comma-separated form HyperDrive settings use.</summary>
        private static string SandboxProviders() => string.Join(", ", SandboxOptions.Providers());

        private static readonly Lazy<SandboxSessionLimiter> SessionLimiter = new Lazy<SandboxSessionLimiter>(SandboxOptions.CreateSessionLimiter);

        /// <summary>
        /// Creates a fresh, verified demo avatar for this visitor and signs it in. Sandbox avatars live only in the
        /// sandbox's own databases, so visitors can try everything without an OASIS account.
        /// </summary>
        [HttpPost("session")]
        public async Task<ActionResult<OASISResult<IAvatar>>> StartSession()
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            // The demo button is public, so cap how many avatars one address and the whole sandbox can create.
            if (!SessionLimiter.Value.TryAcquire(HttpContext.Connection.RemoteIpAddress?.ToString(), out var limit))
                return StatusCode(429, new OASISResult<IAvatar> { IsError = true, Message = limit });

            string suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
            string username = $"{SandboxOptions.Current.DemoAvatarUsernamePrefix}{suffix}";
            string password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));

            OASISResult<IAvatar> registerResult = await Program.AvatarManager.RegisterAsync("Explorer", "Sandbox", suffix,
                $"{username}@{SandboxOptions.Current.DemoAvatarEmailDomain}", password, username, AvatarType.User, OASISType.OASISAPIREST, suppressVerificationEmail: true);

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
                [AvatarHyperDriveSettings.AutoFailoverProviders] = SandboxProviders(),
                [AvatarHyperDriveSettings.AutoReplicationProviders] = SandboxProviders(),
                [AvatarHyperDriveSettings.LoadBalancingProviders] = SandboxProviders()
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

            // Do not release a demo bearer before its configured CQL entitlement is durably recorded.
            // A failed issuance leaves the temporary avatar for the existing expiry sweep, with no token published.
            await subscriptions.ProvisionAsync(avatar, HttpContext.RequestAborted);

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
        public ActionResult<OASISResult<DateTime>> TakeOffline(ProviderType providerType, [FromQuery] int? seconds = null)
        {
            if (!ProviderFaultGate.IsEnabled)
                return NotFound();

            if (!ProviderManager.Instance.GetAllRegisteredProviders().Any(x => x.ProviderType.Value == providerType))
                return BadRequest(new OASISResult<DateTime> { IsError = true, Message = $"{providerType} is not registered on the sandbox." });

            var sandbox = SandboxOptions.Current;
            TimeSpan duration = TimeSpan.FromSeconds(Math.Clamp(seconds ?? sandbox.ProviderOfflineDefaultSeconds,
                SandboxOptions.AtLeastOne(sandbox.ProviderOfflineMinimumSeconds, nameof(sandbox.ProviderOfflineMinimumSeconds)),
                SandboxOptions.AtLeastOne(sandbox.ProviderOfflineMaximumSeconds, nameof(sandbox.ProviderOfflineMaximumSeconds))));
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

            // The same pretend shop keeps its suppliers in its own SQLite file and its loyalty graph in Neo4j, so the
            // import tab can holonize three different kinds of store. Each is optional: a node without the setting skips it.
            var sqliteSettings = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.StorageProviders?.SQLLiteDBOASIS;
            if (!string.IsNullOrWhiteSpace(sqliteSettings?.NativeImportConnectionString))
                counts["sqlite:suppliers"] = await SeedSqliteSuppliersAsync(sqliteSettings.NativeImportConnectionString);
            var neo4jSettings = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.StorageProviders?.Neo4jOASIS;
            if ((neo4jSettings?.NativeImportLabels ?? "").Split(',').Select(label => label.Trim()).Contains(SandboxOptions.Current.DemoNeo4jLabel))
                counts["neo4j:" + SandboxOptions.Current.DemoNeo4jLabel] = await SeedNeo4jLoyaltyAsync(neo4jSettings);

            return Ok(new OASISResult<Dictionary<string, long>>(counts) { Message = "Demo shop data is ready." });
        }


        private static async Task<long> SeedSqliteSuppliersAsync(string connectionString)
        {
            var rows = new[]
            {
                (1, "Babbage & Sons", "United Kingdom", 4.8, "Brass gears and difference engines"),
                (2, "Bletchley Rotors", "United Kingdom", 4.5, "Precision rotor drums"),
                (3, "Flow-Matic Press", "United States", 4.2, "Technical manuals"),
                (4, "CERN Software Co-op", "Switzerland", 5.0, "Open web software")
            };
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await connection.OpenAsync();
            using (var create = connection.CreateCommand())
            {
                create.CommandText = "CREATE TABLE IF NOT EXISTS suppliers (id INTEGER PRIMARY KEY, name TEXT NOT NULL, country TEXT, rating REAL, description TEXT)";
                await create.ExecuteNonQueryAsync();
            }
            foreach (var row in rows)
            {
                using var upsert = connection.CreateCommand();
                upsert.CommandText = "INSERT OR REPLACE INTO suppliers (id, name, country, rating, description) VALUES ($id, $name, $country, $rating, $description)";
                upsert.Parameters.AddWithValue("$id", row.Item1); upsert.Parameters.AddWithValue("$name", row.Item2);
                upsert.Parameters.AddWithValue("$country", row.Item3); upsert.Parameters.AddWithValue("$rating", row.Item4);
                upsert.Parameters.AddWithValue("$description", row.Item5);
                await upsert.ExecuteNonQueryAsync();
            }
            return rows.Length;
        }

        private static async Task<long> SeedNeo4jLoyaltyAsync(NextGenSoftware.OASIS.API.DNA.Neo4jOASISSettings settings)
        {
            var members = new[] { ("ada", "Ada Lovelace", "gold", 1840), ("grace", "Grace Hopper", "gold", 1210), ("alan", "Alan Turing", "silver", 760) };
            using var driver = Neo4j.Driver.GraphDatabase.Driver(settings.ConnectionString, Neo4j.Driver.AuthTokens.Basic(settings.Username, settings.Password));
            await using var session = driver.AsyncSession();
            foreach (var member in members)
                await session.RunAsync($"MERGE (m:{SandboxOptions.Current.DemoNeo4jLabel} {{handle: $handle}}) SET m.name = $name, m.tier = $tier, m.points = $points",
                    new { handle = member.Item1, name = member.Item2, tier = member.Item3, points = member.Item4 });
            await session.RunAsync($"MATCH (a:{SandboxOptions.Current.DemoNeo4jLabel} {{handle: 'ada'}}), (g:{SandboxOptions.Current.DemoNeo4jLabel} {{handle: 'grace'}}) MERGE (a)-[:REFERRED]->(g)");
            return members.Length;
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

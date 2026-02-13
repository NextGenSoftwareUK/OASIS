using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public HealthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>Returns null if value is null or whitespace so fallback config (e.g. OASIS_DNA) is used when appsettings has "".</summary>
        private static string GetConfigOrNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

        /// <summary>
        /// Health check endpoint for Railway deployment
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "OASIS API ONODE WebAPI"
            });
        }

        /// <summary>
        /// Health check endpoint for Railway deployment
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "OASIS API ONODE WebAPI"
            });
        }

        /// <summary>
        /// Verifies MongoDB connection and that the API can read from the same database used for avatars.
        /// Uses the same config as the rest of the API: SubscriptionStore:ConnectionString or OASIS:StorageProviders:MongoDBOASIS:ConnectionString (from OASIS_DNA.json or appsettings).
        /// </summary>
        /// <returns>MongoDB connection and read status</returns>
        [HttpGet("health/mongo")]
        public async Task<IActionResult> MongoHealth()
        {
            // Same sources as SubscriptionStore + OASIS DNA (used by MongoDBOASIS provider after BootOASIS).
            // Treat empty string as unset so appsettings "SubscriptionStore:ConnectionString": "" still falls back to OASIS_DNA.
            var conn = GetConfigOrNull(_configuration["SubscriptionStore:ConnectionString"])
                ?? GetConfigOrNull(_configuration["OASIS:StorageProviders:MongoDBOASIS:ConnectionString"])
                ?? OASISDNAManager.OASISDNA?.OASIS?.StorageProviders?.MongoDBOASIS?.ConnectionString;
            var dbName = GetConfigOrNull(_configuration["SubscriptionStore:DatabaseName"])
                ?? GetConfigOrNull(_configuration["OASIS:StorageProviders:MongoDBOASIS:DBName"])
                ?? OASISDNAManager.OASISDNA?.OASIS?.StorageProviders?.MongoDBOASIS?.DBName
                ?? "OASISAPI_DEV";

            if (string.IsNullOrWhiteSpace(conn))
            {
                return Ok(new
                {
                    status = "notConfigured",
                    message = "MongoDB connection string not set. Set SubscriptionStore:ConnectionString or OASIS:StorageProviders:MongoDBOASIS:ConnectionString (e.g. in OASIS_DNA.json or appsettings).",
                    timestamp = DateTime.UtcNow
                });
            }

            try
            {
                var client = new MongoClient(conn);
                var db = client.GetDatabase(dbName);
                // Prove we can read from the Avatar collection (same data the API uses for auth/avatars)
                var avatarCollection = db.GetCollection<BsonDocument>("Avatar");
                await avatarCollection.Find(FilterDefinition<BsonDocument>.Empty).Limit(1).FirstOrDefaultAsync();

                return Ok(new
                {
                    status = "ok",
                    mongo = "connected",
                    read = "ok",
                    database = dbName,
                    avatarCollectionRead = true,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    status = "error",
                    message = "MongoDB connection or read failed.",
                    detail = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}

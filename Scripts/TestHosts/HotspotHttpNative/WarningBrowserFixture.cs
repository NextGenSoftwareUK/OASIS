using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

internal static class WarningBrowserFixture
{
    // This is an owned historical quota fixture, not payment or provider-execution evidence.
    public static async Task SeedAsync(MongoSubscriptionUsageRepository repository, string database,
        Guid subscriber, Guid fixtureAdministrator)
    {
        if (!database.StartsWith("hotspot_http_", StringComparison.Ordinal) || subscriber == Guid.Empty ||
            fixtureAdministrator == Guid.Empty || subscriber == fixtureAdministrator)
            throw new InvalidOperationException("Distinct owned subscriber/administrator and fixture database required.");
        var inventory = await repository.GetMigrationInventoryAsync(CancellationToken.None);
        if (inventory.AlreadyImported) throw new InvalidOperationException("Owned warning fixture ledger was already initialized.");
        var now = DateTime.UtcNow;
        var user = subscriber.ToString("D");
        var actor = fixtureAdministrator.ToString("D");
        var day = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).ToString("yyyy-MM-dd");
        var evidence = Encoding.UTF8.GetBytes("owned-warning-browser-quota-v1|" + database + "|" + user + "|" + day + "|24000");
        var manifest = new UsageOpeningBalanceManifest {
            MigrationId = Guid.NewGuid().ToString("D"), ApprovedBy = actor, SourceDigest = inventory.SourceDigest,
            EvidenceSha256 = Convert.ToHexStringLower(SHA256.HashData(evidence)),
            Balances = new() { new UsageOpeningBalance { UserId = user, Day = day, Requests = 24000 } },
            Subscriptions = new() { new SubscriptionRecord { UserId = user, PlanId = "free", Status = "free",
                CreatedAt = now, UpdatedAt = now } }
        };
        var approvalKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["SUBSCRIPTION_MIGRATION_APPROVAL_KEY"] = approvalKey
        }).Build();
        manifest.Signature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(approvalKey),
            Encoding.UTF8.GetBytes(UsageMigrationSignature.CanonicalJson(manifest))));
        UsageMigrationSignature.RequireApproval(manifest, actor, settings);
        var acknowledged = await repository.ImportOpeningBalanceAsync(actor, manifest, CancellationToken.None);
        if (acknowledged != manifest.MigrationId) throw new InvalidOperationException("Owned opening balance was not acknowledged.");
        var usage = await repository.GetAggregateAsync(user, CancellationToken.None);
        var subscription = await repository.GetSubscriptionAsync(user);
        if (usage.MonthlyRequests != 24000 || subscription?.PlanId != "free")
            throw new InvalidOperationException("Owned warning quota snapshot was not persisted.");
        Console.WriteLine("WARNING_BROWSER_FIXTURE_PROOF: owned approved historical quota snapshot and free subscription imported through shipping repository and native readback. Not payment/provider-execution evidence.");
    }
}

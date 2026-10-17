using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

if (args.Length != 3 || args[0] != "sign")
    throw new ArgumentException("Usage: dotnet run --project Scripts/SubscriptionLedger.Migration -- sign reviewed-manifest.json signed-manifest.json. Set SUBSCRIPTION_MIGRATION_APPROVAL_KEY in the environment; never pass it on the command line.");
string key = Environment.GetEnvironmentVariable("SUBSCRIPTION_MIGRATION_APPROVAL_KEY");
if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
    throw new InvalidOperationException("A signing key of at least 32 bytes is required.");
var manifest = JsonSerializer.Deserialize<UsageOpeningBalanceManifest>(await File.ReadAllTextAsync(args[1]), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Opening-balance manifest is empty.");
manifest.Signature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(UsageMigrationSignature.CanonicalJson(manifest))));
await File.WriteAllTextAsync(args[2], JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("Signed manifest saved. Submit it to POST /api/subscription/usage/admin/migration/opening-balance with the approved administrator JWT.");

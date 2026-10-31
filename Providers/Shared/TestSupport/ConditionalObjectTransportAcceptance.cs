using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

/// <summary>One bounded native object probe, shared by OCI/S3. No replay or error cleanup.</summary>
public static class ConditionalObjectTransportAcceptance
{
    public static async Task RunAsync(IConditionalKeyValueBackend backend, string bucket, string prefix,
        string work, string stem, string? namespaceName = null, string? region = null)
    {
        ArgumentNullException.ThrowIfNull(backend); ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        if (prefix != "oasis-acceptance/") throw new ArgumentException("Use the fixed isolated acceptance prefix.", nameof(prefix));
        if (stem is not ("oci-live-transport" or "s3-live-transport")) throw new ArgumentException("Unknown evidence scope.", nameof(stem));
        if (!Directory.Exists(work)) throw new DirectoryNotFoundException("Reuse the existing acceptance evidence directory.");
        var pending = Path.Combine(work, stem + ".pending.json"); var report = Path.Combine(work, stem + ".current.json");
        void RefuseExisting()
        {
            if (new[] { pending, report, pending + ".next", report + ".next" }.Any(File.Exists))
                throw new InvalidOperationException("Review existing or incomplete evidence before another run; never replay an uncertain mutation.");
        }
        RefuseExisting();
        await using var lease = new FileStream(Path.Combine(work, stem + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        RefuseExisting();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45)); var token = timeout.Token;
        const string key = "transport-probe"; var operation = Guid.NewGuid();
        var first = "owned object transport acceptance " + operation.ToString("N"); var replacement = first + " replacement";
        static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        async Task Durable(string path, object value, bool replace)
        {
            var next = path + ".next";
            await using (var file = new FileStream(next, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, value, cancellationToken: token);
                await file.FlushAsync(token); file.Flush(flushToDisk: true);
            }
            File.Move(next, path, overwrite: replace);
        }
        Task Journal(string state, string? etag = null) => Durable(pending, new
        {
            state, operation, namespaceName, region, bucket, objectName = prefix + key,
            firstHash = Hash(first), replacementHash = Hash(replacement), etag
        }, File.Exists(pending));
        static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
        await backend.VerifyAsync(token);
        Require(await backend.ReadObservedAsync(key, token) == null, "Fixed acceptance object is occupied; do not adopt/delete it.");
        await Journal("before-create");
        await backend.CreateOnlyAsync(key, first, token); await Journal("create-acknowledged");
        var created = await backend.ReadObservedAsync(key, token);
        Require(created?.Value == first, "Created object readback does not match this operation.");
        Require((await backend.ListKeysAsync(key, token)).Contains(key, StringComparer.Ordinal), "Created object is missing from scoped native listing.");
        await Journal("before-replace", created!.Revision);
        await backend.ReplaceUnchangedAsync(key, replacement, created.Revision, token); await Journal("replace-acknowledged");
        var updated = await backend.ReadObservedAsync(key, token);
        Require(updated?.Value == replacement && updated.Revision != created.Revision, "Replacement readback/value/revision did not advance.");
        await Journal("before-stale-conflict", updated!.Revision);
        var conflicted = false;
        try { await backend.ReplaceUnchangedAsync(key, "must not be stored", created.Revision, token); }
        catch (StorageLifecycleConflictException) { conflicted = true; }
        Require(conflicted, "Stale native revision did not reject replacement.");
        var unchanged = await backend.ReadObservedAsync(key, token);
        Require(unchanged?.Value == replacement && unchanged.Revision == updated.Revision, "Conflict changed the owned object.");
        await Journal("before-conditional-delete", updated.Revision);
        await backend.DeleteUnchangedAsync(key, updated.Revision, token); await Journal("delete-acknowledged");
        Require(await backend.ReadObservedAsync(key, token) == null, "Acknowledged deletion did not read back as explicit absence.");
        Require(!(await backend.ListKeysAsync(key, token)).Contains(key, StringComparer.Ordinal), "Deleted object remains in scoped native listing.");
        await Durable(report, new
        {
            state = "verified", operation, namespaceName, region, bucket, objectName = prefix + key,
            firstHash = Hash(first), replacementHash = Hash(replacement), etag = updated.Revision,
            scope = "native signed SDK transport only", fullProviderVerified = false, hyperDriveVerified = false,
            nominalSdkCalls = 12, listPaginationMayIncreaseRequests = true, cloudCostMeasured = false, completedUtc = DateTime.UtcNow
        }, false);
        File.Delete(pending); // Only this completed operation; compact success proof remains.
    }
}

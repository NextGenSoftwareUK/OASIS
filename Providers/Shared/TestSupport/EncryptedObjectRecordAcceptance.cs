using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

/// <summary>A bounded public-record gate, not every interface/options/IAM/fee/HyperDrive certification.
/// No error-path cleanup: original intent and native observations survive uncertain writes.</summary>
public static class EncryptedObjectRecordAcceptance
{
    public const string Prefix = "oasis-provider-acceptance/";
    private sealed record Observation(string Key, string Revision, string Sha256);
    private sealed class FrozenKey : IDisposable
    {
        private readonly byte[] _bytes;
        internal FrozenKey(Func<byte[]> resolver)
        {
            _bytes = resolver() ?? throw new InvalidOperationException("Configured persisted storage key is missing.");
            if (_bytes.Length != 32)
            {
                CryptographicOperations.ZeroMemory(_bytes);
                throw new InvalidOperationException("Configured storage key must contain exactly 32 bytes.");
            }
        }
        internal byte[] Copy() => _bytes.ToArray();
        public void Dispose() => CryptographicOperations.ZeroMemory(_bytes);
    }

    public static async Task RunAsync(IConditionalKeyValueBackend native,
        Func<Func<byte[]>, KeyValueStorageProviderBase> providerFactory,
        Func<byte[]> keyResolver, string work, string evidenceStem, string service, string bucket,
        string? namespaceName = null, string? region = null)
    {
        ArgumentNullException.ThrowIfNull(native); ArgumentNullException.ThrowIfNull(providerFactory);
        ArgumentNullException.ThrowIfNull(keyResolver); ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        if (string.IsNullOrWhiteSpace(evidenceStem) || !Regex.IsMatch(evidenceStem, "^[a-z0-9][a-z0-9-]*$", RegexOptions.CultureInvariant))
            throw new ArgumentException("A literal acceptance evidence identity is required.", nameof(evidenceStem));
        if (!Directory.Exists(work)) throw new DirectoryNotFoundException("Reuse the existing acceptance evidence directory.");
        var pending = Path.Combine(work, evidenceStem + ".pending.json");
        var report = Path.Combine(work, evidenceStem + ".current.json");
        void RefuseExisting()
        {
            if (new[] { pending, report, pending + ".next", report + ".next" }.Any(File.Exists))
                throw new InvalidOperationException("Review original " + service + " record evidence; never replay an unresolved operation.");
        }
        RefuseExisting();
        await using var lease = new FileStream(Path.Combine(work, evidenceStem + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        RefuseExisting();
        using var frozenStorageKey = new FrozenKey(keyResolver); // Resolve once before any SDK access; never generate a replacement.
        await native.VerifyAsync();
        Require((await native.ListKeysAsync("")).Count == 0, "Isolated record-acceptance prefix is occupied; do not adopt or delete its objects.");
        var provider = providerFactory(frozenStorageKey.Copy) ?? throw new InvalidOperationException("Record provider factory returned no provider.");
        using var providerLifetime = provider as IDisposable ?? throw new InvalidOperationException("Record acceptance requires disposable key ownership.");
        provider.IsVersionControlEnabled = true;
        var activated = await provider.ActivateProviderAsync();
        if (activated.IsError) throw new InvalidDataException(service + " record provider activation failed; inspect the original exception.", activated.Exception);
        Require(!activated.IsError && activated.Result, service + " record provider activation failed.");
        var operation = Guid.NewGuid(); var avatarId = Guid.NewGuid(); var rootId = Guid.NewGuid(); var childId = Guid.NewGuid();
        var marker = "owned-" + service.ToLowerInvariant() + "-" + operation.ToString("N");
        var avatar = new Avatar { Id = avatarId, Username = marker, Email = marker + "@example.invalid", IsActive = true };
        var detail = new AvatarDetail { Id = avatarId, Username = avatar.Username, Email = avatar.Email, Karma = 11, XP = 22 };
        var root = new Holon { Id = rootId, Name = marker + "-root", CreatedByAvatarId = avatarId, MetaData = new() { ["acceptance"] = marker } };
        var child = new Holon { Id = childId, Name = marker + "-child", CreatedByAvatarId = avatarId };
        root.Children.Add(child);
        var observed = Array.Empty<Observation>(); var mutations = 0;
        var deadline = DateTime.UtcNow.AddMinutes(5);
        string? ownedIntentHash = null;
        void RequireOwnedIntent() => Require(ownedIntentHash != null && File.Exists(pending) &&
            HashBytes(File.ReadAllBytes(pending)) == ownedIntentHash, "Original record intent changed; preserve it without replay or cleanup.");
        async Task Durable(string path, object value, bool replace)
        {
            if (path == pending) { if (replace) RequireOwnedIntent(); else Require(!File.Exists(pending), "Do not adopt a newly appeared record intent."); }
            var next = path + ".next";
            await using (var file = new FileStream(next, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, value); await file.FlushAsync(); file.Flush(true);
            }
            if (path == pending) { if (replace) RequireOwnedIntent(); else Require(!File.Exists(pending), "Do not adopt a newly appeared record intent."); }
            File.Move(next, path, overwrite: replace);
            if (path == pending) ownedIntentHash = HashBytes(File.ReadAllBytes(pending));
        }
        Task Journal(string state) => Durable(pending, new
        {
            state, operation, namespaceName, region, bucket, prefix = Prefix, avatarId, rootId, childId,
            acknowledgedMutations = mutations, observations = observed
        }, ownedIntentHash != null);
        async Task<Observation[]> Observe()
        {
            var result = new List<Observation>();
            foreach (var key in (await native.ListKeysAsync("")).OrderBy(value => value, StringComparer.Ordinal))
            {
                Require(Regex.IsMatch(key, "^oasis/private/v1/[a-f0-9]{64}$", RegexOptions.CultureInvariant), "Record object name is not an opaque private index.");
                var value = await native.ReadObservedAsync(key) ?? throw new InvalidDataException("Native record disappeared during observation.");
                foreach (var text in new[] { marker, avatarId.ToString(), rootId.ToString(), childId.ToString() })
                    Require(!value.Value.Contains(text, StringComparison.Ordinal), "Native record contains an unencrypted synthetic identity/value.");
                result.Add(new(key, value.Revision, Hash(value.Value)));
            }
            return result.ToArray();
        }
        async Task Mutate<T>(string phase, Func<Task<OASISResult<T>>> action, Func<OASISResult<T>, bool> acknowledged)
        {
            // Admission deadline only: never abandon/dispose an in-flight write on a timer.
            Require(DateTime.UtcNow < deadline, "Record acceptance deadline reached before the next mutation.");
            await Journal("before-" + phase);
            var result = await action();
            if (result.IsError) throw new InvalidDataException("Public record " + phase + " failed; original intent is preserved. Code=" + result.ErrorCode, result.Exception);
            Require(!result.IsError && acknowledged(result), "Public record mutation did not acknowledge " + phase + ".");
            mutations++; observed = await Observe(); await Journal("acknowledged-" + phase);
        }
        static T Loaded<T>(OASISResult<T> result) where T : class
        {
            if (result.IsError) throw new InvalidDataException("Public record read failed; original intent is preserved. Code=" + result.ErrorCode, result.Exception);
            Require(!result.IsError && result.IsLoaded && result.LoadedCount == 1 && result.Result != null, "Expected one acknowledged public record read.");
            return result.Result!;
        }
        await Mutate("avatar-create", () => provider.SaveAvatarAsync(avatar), r => r.IsSaved && r.SavedCount == 1);
        Require(avatar.Version == 1 && observed.Length > 0, "Avatar creation did not publish version one and native objects.");
        avatar.Description = marker + "-updated";
        await Mutate("avatar-update", () => provider.SaveAvatarAsync(avatar), r => r.IsSaved && r.SavedCount == 1);
        Require(avatar.Version == 2, "Avatar update did not advance history.");
        await Mutate("detail-create", () => provider.SaveAvatarDetailAsync(detail), r => r.IsSaved && r.SavedCount == 1);
        detail.Karma = 33; detail.XP = 44;
        await Mutate("detail-update", () => provider.SaveAvatarDetailAsync(detail), r => r.IsSaved && r.SavedCount == 1);
        await Mutate("graph-create", () => provider.SaveHolonAsync(root), r => r.IsSaved && r.SavedCount == 2);
        Require(root.Version == 1 && child.Version == 1 && child.ParentHolonId == rootId, "Graph acknowledgement did not publish independent records/parent.");
        root.Name = marker + "-root-updated";
        await Mutate("root-only-update", () => provider.SaveHolonAsync(root, saveChildren: false), r => r.IsSaved && r.SavedCount == 1);
        Require(root.Version == 2 && child.Version == 1, "Root-only update changed excluded child version.");

        foreach (var result in new[] { await provider.LoadAvatarAsync(avatarId), await provider.LoadAvatarByUsernameAsync(avatar.Username), await provider.LoadAvatarByEmailAsync(avatar.Email) })
        {
            var row = Loaded(result); Require(row.Id == avatarId && row.Version == 2 && row.Description == avatar.Description, "Avatar selector returned a different record/version.");
        }
        var historicalAvatar = Loaded(await provider.LoadAvatarAsync(avatarId, 1));
        Require(historicalAvatar.Version == 1 && historicalAvatar.Description == null, "Historical avatar does not match original version.");
        var historicalDetail = Loaded(await provider.LoadAvatarDetailByUsernameAsync(avatar.Username, 1));
        Require(historicalDetail.Id == avatarId && historicalDetail.Karma == 11 && historicalDetail.XP == 22, "Detail history did not remain independent of avatar history.");
        var currentDetail = Loaded(await provider.LoadAvatarDetailByEmailAsync(avatar.Email));
        Require(currentDetail.Id == avatarId && currentDetail.Version == 2 && currentDetail.Karma == 33 && currentDetail.XP == 44, "Current detail selector returned different values.");
        var graph = Loaded(await provider.LoadHolonAsync(rootId, loadChildrenFromProvider: true));
        Require(graph.Version == 2 && graph.Children.Count == 1 && graph.Children.Single().Id == childId && graph.Children.Single().Version == 1, "Provider-linked graph does not retain independent child version.");
        var selected = await provider.LoadHolonsByMetaDataAsync("acceptance", marker, loadChildren: false);
        Require(!selected.IsError && selected.IsLoaded && selected.LoadedCount == 1 && selected.Result.Single().Id == rootId, "Metadata selection did not isolate the owned root.");
        var afterQueries = await Observe();
        Require(observed.SequenceEqual(afterQueries), "Public reads changed native objects or their revisions.");
        await Journal("queries-verified");

        // Delete only this operation's verified public identities. Provider cleanup uses
        // observed native preconditions; never sweep raw objects/prefixes after a failure.
        var ownedRoot = Loaded(await provider.LoadHolonAsync(rootId, loadChildren: false));
        Require(ownedRoot.Name == root.Name && ownedRoot.CreatedByAvatarId == avatarId && ownedRoot.Version == 2, "Root changed before cleanup.");
        await Mutate("root-delete", () => provider.DeleteHolonAsync(rootId), r => r.IsDeleted);
        var ownedChild = Loaded(await provider.LoadHolonAsync(childId, loadChildren: false));
        Require(ownedChild.Name == child.Name && ownedChild.ParentHolonId == rootId && ownedChild.Version == 1, "Child changed before cleanup.");
        await Mutate("child-delete", () => provider.DeleteHolonAsync(childId), r => r.IsDeleted);
        Require((await provider.LoadHolonAsync(rootId, loadChildren: false)).IsError && (await provider.LoadHolonAsync(childId, loadChildren: false)).IsError, "Deleted graph identities are still live.");
        var ownedAvatar = Loaded(await provider.LoadAvatarAsync(avatarId));
        Require(ownedAvatar.Username == avatar.Username && ownedAvatar.Email == avatar.Email && ownedAvatar.Description == avatar.Description && ownedAvatar.Version == 2, "Avatar changed before cleanup.");
        var ownedDetail = Loaded(await provider.LoadAvatarDetailAsync(avatarId));
        Require(ownedDetail.Id == avatarId && ownedDetail.Karma == 33 && ownedDetail.XP == 44 && ownedDetail.Version == 2, "Detail changed before cleanup.");
        await Mutate("avatar-detail-delete", () => provider.DeleteAvatarAsync(avatarId, softDelete: false), r => r.IsDeleted && r.Result);
        Require((await provider.LoadAvatarAsync(avatarId)).IsError && (await provider.LoadAvatarDetailAsync(avatarId)).IsError, "Deleted avatar/detail are still live.");
        Require(observed.Length == 0 && (await native.ListKeysAsync("")).Count == 0, "Owned record/index/archive objects remain; preserve intent, do not sweep.");
        RequireOwnedIntent();
        await Durable(report, new
        {
            state = "verified", operation, namespaceName, region, bucket, prefix = Prefix, avatarId, rootId, childId,
            acknowledgedMutations = mutations, currentNativeObjectsRemaining = 0, completedUtc = DateTime.UtcNow,
            scope = "bounded public record lifecycle over signed SDK", fullProviderVerified = false,
            hyperDriveVerified = false, cloudCostMeasured = false, sdkRequestsMeasured = false,
            bucketVersionsVerified = false, retainedVersionsMayIncurCharges = true
        }, false);
        RequireOwnedIntent(); File.Delete(pending);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string HashBytes(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
}

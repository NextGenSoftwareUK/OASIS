using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Explicit host-owned persistent folder; configure outside temporary
/// test/build outputs. Write-once flushed evidence, atomic same-directory rename,
/// exclusive writers and strict bounded reads. This is not a distributed lock,
/// permission store, power-loss guarantee or migration commit decision.</summary>
public sealed class FileHolonMigrationMutationPreparationStore
{
    private readonly string folder;
    public FileHolonMigrationMutationPreparationStore(string persistentFolder)
    {
        if (string.IsNullOrWhiteSpace(persistentFolder)) throw new ArgumentException("An explicit host-owned persistent folder is required.");
        folder = Path.GetFullPath(persistentFolder);
    }
    private string PathFor(Guid id) => Path.Combine(folder, id.ToString("D") + ".mutation-preparation.json");
    private static OASISResult<HolonMigrationMutationPreparation> Failure(string code, Exception error)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Exception = error,
            Message = "Retain the original host evidence and reconcile the exact file; no provider operation was issued by the plan store." };
    private static async Task<HolonMigrationMutationPreparation> ReadFile(string path,
        HolonReplicationMigrationBarrierPlan plan, string approvedFingerprint, CancellationToken token)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (file.Length == 0 || file.Length > 16 * 1024 * 1024)
            throw new InvalidDataException("Recovery evidence length is invalid.");
        var bytes = new byte[checked((int)file.Length)];
        await file.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        if (file.Position != file.Length) throw new InvalidDataException("Recovery evidence changed while being read.");
        return HolonMigrationMutationPreparation.ReadCanonical(new UTF8Encoding(false, true).GetString(bytes), plan, approvedFingerprint);
    }
    public async Task<OASISResult<HolonMigrationMutationPreparation>> ReadAsync(
        HolonReplicationMigrationBarrierPlan plan, string approvedFingerprint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        try
        {
            cancellationToken.ThrowIfCancellationRequested(); var path = PathFor(plan.Context.MigrationId);
            if (File.Exists(path + ".pending")) throw new InvalidDataException("A pending plan requires explicit recovery before use.");
            return new(await ReadFile(path, plan, approvedFingerprint, cancellationToken).ConfigureAwait(false)) { IsLoaded = true };
        }
        catch (Exception error)
        { return Failure(error is FileNotFoundException or DirectoryNotFoundException ? "COSMIC_MIGRATION_MUTATION_PREPARATION_NOT_FOUND" : "COSMIC_MIGRATION_MUTATION_PREPARATION_UNVERIFIED", error); }
    }
    public async Task<OASISResult<HolonMigrationMutationPreparation>> SaveAsync(
        HolonMigrationMutationPreparation preparation, HolonReplicationMigrationBarrierPlan plan, string approvedFingerprint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = HolonMigrationMutationPreparation.ReadCanonical(preparation.CanonicalJson, plan, approvedFingerprint);
            Directory.CreateDirectory(folder);
            var path = PathFor(plan.Context.MigrationId);
            using var lease = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (File.Exists(path + ".pending")) throw new InvalidDataException("Pending evidence must be reviewed explicitly; it was not replaced.");
            if (File.Exists(path))
            {
                var original = await ReadFile(path, plan, approvedFingerprint, cancellationToken).ConfigureAwait(false);
                if (original.CanonicalJson != preparation.CanonicalJson) throw new InvalidDataException("The original migration plan cannot be replaced.");
                return new(original) { IsLoaded = true };
            }
            var bytes = new UTF8Encoding(false, true).GetBytes(preparation.CanonicalJson);
            using (var pending = new FileStream(path + ".pending", FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await pending.WriteAsync(bytes, cancellationToken).ConfigureAwait(false); pending.Flush(true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(path + ".pending", path, false);
            // Confirm the persisted original before the coordinator can touch providers.
            return new(await ReadFile(path, plan, approvedFingerprint, cancellationToken).ConfigureAwait(false)) { IsLoaded = true, IsSaved = true, SavedCount = 1 };
        }
        catch (Exception error) { return Failure("COSMIC_MIGRATION_MUTATION_PREPARATION_WRITE_UNVERIFIED", error); }
    }
    /// <summary>Explicit recovery of a complete pending write after interruption.
    /// Validate against the independently approved context before atomic publish.
    /// Partial/corrupt evidence and simultaneous final/pending files are retained.</summary>
    public async Task<OASISResult<HolonMigrationMutationPreparation>> RecoverPendingAsync(
        HolonReplicationMigrationBarrierPlan plan, string approvedFingerprint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        try
        {
            cancellationToken.ThrowIfCancellationRequested(); var path = PathFor(plan.Context.MigrationId);
            using var lease = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (File.Exists(path)) throw new InvalidDataException("Existing final evidence cannot be replaced by a pending write.");
            _ = await ReadFile(path + ".pending", plan, approvedFingerprint, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested(); File.Move(path + ".pending", path, false);
            return new(await ReadFile(path, plan, approvedFingerprint, cancellationToken).ConfigureAwait(false)) { IsLoaded = true, IsSaved = true, SavedCount = 1 };
        }
        catch (Exception error) { return Failure("COSMIC_MIGRATION_MUTATION_PREPARATION_RECOVERY_UNVERIFIED", error); }
    }
}

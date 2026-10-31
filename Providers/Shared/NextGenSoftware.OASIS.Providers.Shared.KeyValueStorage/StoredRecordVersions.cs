using System;
using System.IO;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Shared record-version invariants; callers own storage layout, encryption and durable I/O.</summary>
public static class StoredRecordVersions
{
    public static async Task<T> ReadAsync<T>(Guid expectedId, T current, int version,
        Func<int, Task<T>> readHistorical) where T : class, IHolonBase
    {
        if (version < 0) throw new ArgumentOutOfRangeException(nameof(version), "Version must be zero (latest) or a positive historical version.");
        if (current != null && current.Id != expectedId)
            throw new InvalidDataException("Stored record ID does not match its storage key.");
        if (current == null || version == 0 || current.Version == version) return current;
        var historical = await readHistorical(version);
        if (historical != null && (historical.Id != expectedId || historical.Version != version))
            throw new InvalidDataException("Stored historical record ID or version does not match its storage key.");
        return historical;
    }

    public static async Task StampAsync(Guid expectedId, IHolonBase entity, IHolonBase previous,
        Func<IHolonBase, Task> archivePrevious)
    {
        if (entity.Id != expectedId || previous != null && previous.Id != expectedId)
            throw new InvalidDataException("Stored record ID does not match its storage key.");
        if (entity.VersionId == Guid.Empty) entity.VersionId = Guid.NewGuid();
        if (previous == null)
        {
            if (entity.Version < 1) entity.Version = 1;
            return;
        }
        int next;
        try { next = checked(previous.Version + 1); }
        catch (OverflowException cause)
        { throw new InvalidDataException("Stored record version is exhausted; refusing rollover or another-provider retry.", cause); }
        if (archivePrevious != null) await archivePrevious(previous);
        entity.Version = next;
        entity.PreviousVersionId = previous.VersionId;
        entity.VersionId = Guid.NewGuid();
    }
}

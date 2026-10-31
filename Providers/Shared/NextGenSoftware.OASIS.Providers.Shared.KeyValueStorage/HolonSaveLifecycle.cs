using System;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Pure save admission against an authoritative, provider-protected observation.
/// Native locking/transactions, replica lineage and explicit restore authorization belong to their owners.</summary>
public static class HolonSaveLifecycle
{
    public static void RequireCompatibleSave(IHolonBase previous,IHolonBase proposed)
    {
        ArgumentNullException.ThrowIfNull(proposed);
        if(previous==null)return;
        RequireCompatibleSave(previous.DeletedDate,previous.DeletedByAvatarId.ToString("D"),proposed.IsActive,proposed.DeletedDate,proposed.DeletedByAvatarId.ToString("D"));
    }
    public static void RequireCompatibleSave(DateTime previousDeletion,string previousActor,bool proposedActive,DateTime proposedDeletion,string proposedActor)
    {
        if(previousDeletion==DateTime.MinValue)return;
        if(proposedActive||proposedDeletion!=previousDeletion||Actor(previousActor)!=Actor(proposedActor))
            throw new StorageLifecycleConflictException("Save cannot clear or replace a committed tombstone.");
    }
    private static Guid Actor(string value)
    {
        // Nullable native audit columns historically represent Guid.Empty.
        if(string.IsNullOrEmpty(value))return Guid.Empty;
        return Guid.TryParse(value,out var actor)?actor:throw new InvalidDataException("Stored deletion actor is not a valid identity.");
    }
}

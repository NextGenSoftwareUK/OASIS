using System;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Admission for a managed account's declared successor revision.
/// Call against the authoritative observation protected by native CAS/transaction.
/// Unversioned native replacements retain their existing contract.</summary>
public static class AccountSaveRevision
{
    public static void RequireDeclaredSuccessor(int currentVersion,Guid currentId,
        int proposedVersion,Guid proposedId,Guid predecessor)
    {
        if(predecessor==Guid.Empty || proposedId==currentId)return;
        if(proposedId==Guid.Empty || predecessor!=currentId || currentVersion==int.MaxValue ||
            proposedVersion!=currentVersion+1)
            throw new StorageLifecycleConflictException("The account changed after it was opened. Reload it before saving.");
    }
}

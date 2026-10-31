using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

public sealed class AzureAvatarLifecycleException : Exception, INativeMutationReconciliationRequired
{
    public Guid AvatarId {get;}
    public IReadOnlyList<string> AcknowledgedBlobPaths {get;}
    public string OriginalTransactionId => string.Empty; // Two blobs have no common native transaction receipt.
    public AzureAvatarLifecycleException(Guid id,IEnumerable<string> acknowledged,Exception cause)
        :base("Azure avatar lifecycle partially acknowledged; reconcile the recorded blobs before replay.",cause)
    {AvatarId=id;AcknowledgedBlobPaths=Array.AsReadOnly(acknowledged.ToArray());}
}

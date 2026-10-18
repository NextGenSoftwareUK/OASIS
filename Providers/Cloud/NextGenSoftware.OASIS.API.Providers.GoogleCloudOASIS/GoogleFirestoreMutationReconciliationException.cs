using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public sealed class GoogleFirestoreMutationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string DocumentPath {get;}
    public string OriginalTransactionId => string.Empty; // No native commit receipt observed.
    public GoogleFirestoreMutationReconciliationException(string path,Exception cause)
        :base("Firestore mutation attempt requires reconciliation before replay.",cause) => DocumentPath=path;
}

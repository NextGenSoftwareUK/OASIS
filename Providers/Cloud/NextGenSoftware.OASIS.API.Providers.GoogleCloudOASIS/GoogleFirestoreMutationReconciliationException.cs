using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public sealed class GoogleFirestoreMutationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string DocumentPath {get;}
    public string OriginalTransactionId {get;} // Transaction identity is not proof of commit acknowledgement.
    public GoogleFirestoreMutationReconciliationException(string path,Exception cause,string transactionId="")
        :base("Firestore mutation attempt requires reconciliation before replay.",cause){DocumentPath=path;OriginalTransactionId=transactionId;}
}

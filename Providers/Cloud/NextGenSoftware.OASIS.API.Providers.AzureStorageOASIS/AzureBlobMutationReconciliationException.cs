using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

public sealed class AzureBlobMutationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string ContainerName {get;}
    public string BlobName {get;}
    public string OriginalTransactionId => string.Empty; // No native commit receipt observed; never fabricate one.
    public AzureBlobMutationReconciliationException(string container,string blob,Exception cause)
        :base("Azure blob mutation attempt requires reconciliation before replay.",cause)
    {ContainerName=container;BlobName=blob;}
}

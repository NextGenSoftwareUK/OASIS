using Oci.ObjectstorageService;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests;

public static class OracleLiveProviderAcceptance
{
    public const string Prefix = EncryptedObjectRecordAcceptance.Prefix;
    public static Task RunAsync(ObjectStorageClient client, string namespaceName, string bucket, string encryptionKeyVariable, string work)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptionKeyVariable);
        return RunAsync(client, namespaceName, bucket, () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(encryptionKeyVariable), work);
    }
    internal static Task RunAsync(ObjectStorageClient client, string namespaceName, string bucket, Func<byte[]> resolver, string work)
    {
        ArgumentNullException.ThrowIfNull(client); ArgumentException.ThrowIfNullOrWhiteSpace(namespaceName);
        return EncryptedObjectRecordAcceptance.RunAsync(new OracleObjectStorageBackend(client, namespaceName, bucket, Prefix),
            key => new OracleObjectStorageOASIS(client, namespaceName, bucket, key, Prefix),
            resolver, work, "oci-live-records", "OCI", bucket, namespaceName: namespaceName);
    }
}

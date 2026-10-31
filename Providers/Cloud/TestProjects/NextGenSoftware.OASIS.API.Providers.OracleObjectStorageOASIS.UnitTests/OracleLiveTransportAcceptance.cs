using NextGenSoftware.OASIS.Providers.TestSupport;
using Oci.ObjectstorageService;

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests;

/// <summary>Native transport only; no full provider/IAM/cost/HyperDrive certification.</summary>
public static class OracleLiveTransportAcceptance
{
    public static Task RunAsync(ObjectStorageClient client, string namespaceName, string bucket, string prefix, string work)
        => ConditionalObjectTransportAcceptance.RunAsync(new OracleObjectStorageBackend(client, namespaceName, bucket, prefix),
            bucket, prefix, work, "oci-live-transport", namespaceName);
}

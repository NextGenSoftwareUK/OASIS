using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Oci.Common;
using Oci.Common.Auth;
using Oci.ObjectstorageService;
using Provider=NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.OracleObjectStorageOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class OracleObjectStorageProviderFactory
{
    internal static Provider Create(OracleObjectStorageOASISSettings settings,string overrideConnectionString=null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if(overrideConnectionString!=null)throw new ArgumentException("OCI Object Storage requires structured provider settings, not a SQL connection-string override.",nameof(overrideConnectionString));
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.ConfigFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.ProfileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.RegionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.NamespaceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.BucketName);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.KeyPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.StorageEncryptionKeyEnvironmentVariable);
        // Freeze settings before callbacks/I/O. Never generate a replacement encryption/signing key.
        var keyVariable=settings.StorageEncryptionKeyEnvironmentVariable;
        var keyPrefix=settings.KeyPrefix;var history=settings.IsVersionControlEnabled;
        var namespaceName=settings.NamespaceName;var bucketName=settings.BucketName;
        var region=settings.RegionId;var configPath=settings.ConfigFilePath;var profile=settings.ProfileName;
        var auth=new ConfigFileAuthenticationDetailsProvider(configPath,profile);
        var client=new ObjectStorageClient(auth,new ClientConfiguration{TimeoutMillis=30000});
        try {
            client.SetRegion(region);
            return new Provider(client,namespaceName,bucketName,()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(keyVariable),keyPrefix,ownsClient:true)
                {IsVersionControlEnabled=history};
        }catch{client.Dispose();throw;}
    }
}

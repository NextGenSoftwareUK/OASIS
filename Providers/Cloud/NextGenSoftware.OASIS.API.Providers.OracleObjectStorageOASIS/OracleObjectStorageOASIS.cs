using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using Oci.ObjectstorageService;

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS;

/// <summary>OASIS records over OCI objects. Shared storage owns record/identity/query/history
/// behavior; private storage owns encryption and opaque keyed indexes. No bucket is created.</summary>
public sealed class OracleObjectStorageOASIS : KeyValueStorageProviderBase,IDisposable
{
    private readonly PersistedPrivateKeyValueBackend _private;
    private readonly ObjectStorageClient _client;
    private readonly bool _ownsClient;
    private bool _disposed;

    public OracleObjectStorageOASIS(ObjectStorageClient client,string namespaceName,string bucketName,
        Func<byte[]> storageKeyResolver,string keyPrefix="oasis/",bool ownsClient=false)
        :this(client,new PersistedPrivateKeyValueBackend(new OracleObjectStorageBackend(client,namespaceName,bucketName,keyPrefix),storageKeyResolver),keyPrefix,ownsClient){}

    private OracleObjectStorageOASIS(ObjectStorageClient client,PersistedPrivateKeyValueBackend backend,string prefix,bool ownsClient)
        :base(backend,prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        _client=client;_private=backend;_ownsClient=ownsClient;
        ProviderName="OracleObjectStorageOASIS";
        ProviderDescription="Oracle OCI Object Storage with encrypted OASIS records and opaque keyed indexes";
        ProviderType=new EnumValue<ProviderType>(Core.Enums.ProviderType.OracleObjectStorageOASIS);
        ProviderCategory=new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
    }

    public void Dispose()
    {
        if(_disposed)return;
        _disposed=true;IsProviderActivated=false;
        _private.Dispose();
        if(_ownsClient)_client.Dispose();
    }

    protected override Task<OASISResult<IEnumerable<IAvatar>>> LoadAvatarsForCredentialSelectionAsync(int version)
    {
        if(version!=0)throw new NotSupportedException("OCI credential authentication requires latest version zero; archived credentials cannot authenticate an avatar.");
        return base.LoadAvatarsForCredentialSelectionAsync(version);
    }
}

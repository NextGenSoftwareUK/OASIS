using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using SharedRedis=NextGenSoftware.OASIS.API.Providers.RedisOASIS.RedisOASIS;

namespace NextGenSoftware.OASIS.API.Providers.GarnetOASIS;

/// <summary>Thin Redis-protocol transport facade. Native product conformance is a separate gate.</summary>
public partial class GarnetOASIS : SharedRedis
{
    public new const string StorageKeyEnvironmentVariable="OASIS_GARNET_STORAGE_ENCRYPTION_KEY";
    private const string Prefix="oasis/garnet/";
    public GarnetOASIS(string connectionString="localhost:3278")
        :this(connectionString,()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(StorageKeyEnvironmentVariable)){}
    public GarnetOASIS(string connectionString,Func<byte[]> storageKeyResolver)
        :base(connectionString,storageKeyResolver,Prefix)=>SetIdentity();
    internal GarnetOASIS(IKeyValueBackend backend,Func<byte[]> storageKeyResolver)
        :base(backend,storageKeyResolver,Prefix)=>SetIdentity();
    private void SetIdentity()
    {
        ProviderName="GarnetOASIS";
        ProviderType=new EnumValue<ProviderType>(Core.Enums.ProviderType.GarnetOASIS);
        ProviderDescription="GarnetOASIS using the shared Redis SDK transport and encrypted OASIS record implementation";
    }
}

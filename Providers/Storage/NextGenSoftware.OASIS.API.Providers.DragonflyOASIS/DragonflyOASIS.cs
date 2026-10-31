using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using SharedRedis=NextGenSoftware.OASIS.API.Providers.RedisOASIS.RedisOASIS;

namespace NextGenSoftware.OASIS.API.Providers.DragonflyOASIS;

/// <summary>Thin Redis-protocol transport facade. Native product conformance is a separate gate.</summary>
public class DragonflyOASIS : SharedRedis
{
    public new const string StorageKeyEnvironmentVariable="OASIS_DRAGONFLY_STORAGE_ENCRYPTION_KEY";
    private const string Prefix="oasis/dragonfly/";
    public DragonflyOASIS(string connectionString="localhost:6379")
        :this(connectionString,()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(StorageKeyEnvironmentVariable)){}
    public DragonflyOASIS(string connectionString,Func<byte[]> storageKeyResolver)
        :base(connectionString,storageKeyResolver,Prefix)=>SetIdentity();
    internal DragonflyOASIS(IKeyValueBackend backend,Func<byte[]> storageKeyResolver)
        :base(backend,storageKeyResolver,Prefix)=>SetIdentity();
    private void SetIdentity()
    {
        ProviderName="DragonflyOASIS";
        ProviderType=new EnumValue<ProviderType>(Core.Enums.ProviderType.DragonflyOASIS);
        ProviderDescription="DragonflyOASIS using the shared Redis SDK transport and encrypted OASIS record implementation";
    }
}

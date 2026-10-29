using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using SharedRedis=NextGenSoftware.OASIS.API.Providers.RedisOASIS.RedisOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ValKeyOASIS;

/// <summary>Thin Redis-protocol transport facade. Native product conformance is a separate gate.</summary>
public partial class ValKeyOASIS : SharedRedis
{
    public new const string StorageKeyEnvironmentVariable="OASIS_VALKEY_STORAGE_ENCRYPTION_KEY";
    private const string Prefix="oasis/valkey/";
    public ValKeyOASIS(string connectionString="localhost:6379")
        :this(connectionString,()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(StorageKeyEnvironmentVariable)){}
    public ValKeyOASIS(string connectionString,Func<byte[]> storageKeyResolver)
        :base(connectionString,storageKeyResolver,Prefix)=>SetIdentity();
    internal ValKeyOASIS(IKeyValueBackend backend,Func<byte[]> storageKeyResolver)
        :base(backend,storageKeyResolver,Prefix)=>SetIdentity();
    private void SetIdentity()
    {
        ProviderName="ValKeyOASIS";
        ProviderType=new EnumValue<ProviderType>(Core.Enums.ProviderType.ValKeyOASIS);
        ProviderDescription="ValKeyOASIS using the shared Redis SDK transport and encrypted OASIS record implementation";
    }
}

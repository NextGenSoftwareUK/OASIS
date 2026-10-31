using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using SharedRedis=NextGenSoftware.OASIS.API.Providers.RedisOASIS.RedisOASIS;

namespace NextGenSoftware.OASIS.API.Providers.KeyDBOASIS;

/// <summary>Thin Redis-protocol transport facade. Native product conformance is a separate gate.</summary>
public class KeyDBOASIS : SharedRedis
{
    public new const string StorageKeyEnvironmentVariable="OASIS_KEYDB_STORAGE_ENCRYPTION_KEY";
    private const string Prefix="oasis/keydb/";
    public KeyDBOASIS(string configuration="localhost:6379")
        :this(configuration,()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(StorageKeyEnvironmentVariable)){}
    public KeyDBOASIS(string configuration,Func<byte[]> storageKeyResolver)
        :base(configuration,storageKeyResolver,Prefix)=>SetIdentity();
    internal KeyDBOASIS(IKeyValueBackend backend,Func<byte[]> storageKeyResolver)
        :base(backend,storageKeyResolver,Prefix)=>SetIdentity();
    private void SetIdentity()
    {
        ProviderName="KeyDBOASIS";
        ProviderType=new EnumValue<ProviderType>(Core.Enums.ProviderType.KeyDBOASIS);
        ProviderDescription="KeyDBOASIS using the shared Redis SDK transport and encrypted OASIS record implementation";
    }
    // Preserve the original KeyDB alias parameter names for named callers.
    public new System.Threading.Tasks.Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>> LoadHolonByProviderKeyAsync(string pk,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        =>base.LoadHolonByProviderKeyAsync(pk,loadChildren,recursive,maxChildDepth,continueOnError,loadChildrenFromProvider,version);
    public new NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> LoadHolonByProviderKey(string pk,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        =>base.LoadHolonByProviderKey(pk,loadChildren,recursive,maxChildDepth,continueOnError,loadChildrenFromProvider,version);
    public new System.Threading.Tasks.Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> DeleteHolonSoftAsync(string pk,bool softDelete=true)
        =>base.DeleteHolonSoftAsync(pk,softDelete);
    public new NextGenSoftware.OASIS.Common.OASISResult<bool> DeleteHolonSoft(string pk,bool softDelete=true)
        =>base.DeleteHolonSoft(pk,softDelete);
    public new System.Threading.Tasks.Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> DeleteHolonSoftAsync(Guid id,bool softDelete=true)
        =>base.DeleteHolonSoftAsync(id,softDelete);
    public new NextGenSoftware.OASIS.Common.OASISResult<bool> DeleteHolonSoft(Guid id,bool softDelete=true)
        =>base.DeleteHolonSoft(id,softDelete);

}

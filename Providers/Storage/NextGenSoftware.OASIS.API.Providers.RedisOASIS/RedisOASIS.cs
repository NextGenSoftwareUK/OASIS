#nullable enable
using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS;

/// <summary>Thin Redis transport over shared canonical storage/history/graphs/queries and private-record policy.
/// An existing persisted32-byte key is required; no key generation, legacy fallback or automatic migration.</summary>
public class RedisOASIS : KeyValueStorageProviderBase, IDisposable
{
    public const string StorageKeyEnvironmentVariable="OASIS_REDIS_STORAGE_ENCRYPTION_KEY";
    private const string DefaultPrefix="oasis/redis/";
    private const string PrivatePhysicalPrefix="oasis/private/v1/";
    private readonly PersistedPrivateKeyValueBackend _private;
    private readonly RedisConnectionOwner? _owner;

    public RedisOASIS(string configuration="localhost:6379")
        :this(configuration,()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(StorageKeyEnvironmentVariable)){}
    public RedisOASIS(string configuration,Func<byte[]> storageKeyResolver,string keyPrefix=DefaultPrefix)
        :this(new RedisConnectionOwner(configuration),storageKeyResolver,keyPrefix){}
    private RedisOASIS(RedisConnectionOwner owner,Func<byte[]> resolver,string prefix)
        :this(new RedisKeyValueBackend(owner.DatabaseAsync,owner.PrimariesAsync,PrivatePhysicalPrefix){VerifyAdmission=owner.RequireReviewedLayoutAsync},resolver,prefix)
        =>_owner=owner;
    internal RedisOASIS(IKeyValueBackend nativeBackend,Func<byte[]> resolver,string keyPrefix=DefaultPrefix)
        :this(PersistedPrivateKeyValueBackend.Create(nativeBackend,resolver),keyPrefix){}
    private RedisOASIS(PersistedPrivateKeyValueBackend backend,string prefix):base(backend,prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        _private=backend;ProviderName="RedisOASIS";
        ProviderDescription="Redis native conditional transport with shared encrypted OASIS records, history and queries";
        ProviderType=new EnumValue<ProviderType>(Core.Enums.ProviderType.RedisOASIS);
        ProviderCategory=new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
    }

    protected override Task<OASISResult<System.Collections.Generic.IEnumerable<IAvatar>>> LoadAvatarsForCredentialSelectionAsync(int version)
    {
        if(version!=0)throw new NotSupportedException("Redis credential authentication requires latest version zero; archived credentials cannot authenticate.");
        return base.LoadAvatarsForCredentialSelectionAsync(version);
    }
    public override async Task<OASISResult<bool>> ActivateProviderAsync()
    {
        var result=await base.ActivateProviderAsync().ConfigureAwait(false);
        if(result.IsError && _owner!=null)
            try{await _owner.CloseAsync().ConfigureAwait(false);}
            catch(Exception cleanup){OASISErrorHandling.HandleError(ref result,"Redis activation failed and connection cleanup also failed.",new AggregateException(result.Exception??new InvalidOperationException(result.Message),cleanup));}
        return result;
    }
    public override async Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        IsProviderActivated=false;var result=new OASISResult<bool>();
        try{if(_owner!=null)await _owner.CloseAsync().ConfigureAwait(false);result.Result=true;}
        catch(Exception cause){OASISErrorHandling.HandleError(ref result,"Redis connection shutdown failed.",cause);}
        return result;
    }

    // Compatibility aliases delegate to one shared soft/hard lifecycle owner.
    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)=>DeleteHolonRecordAsync(id,true);
    public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id,bool softDelete=true)
        =>BooleanDeletion(await DeleteHolonRecordAsync(id,softDelete).ConfigureAwait(false));
    private static OASISResult<bool> BooleanDeletion(OASISResult<IHolon> deleted)
    {
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,bool>(deleted);
        result.Result=!deleted.IsError && deleted.IsDeleted;return result;
    }
    public OASISResult<bool> DeleteHolonSoft(Guid id,bool softDelete=true)=>DeleteHolonSoftAsync(id,softDelete).GetAwaiter().GetResult();
    public async Task<OASISResult<bool>> DeleteHolonSoftAsync(string key,bool softDelete=true)
        =>BooleanDeletion(await DeleteHolonRecordAsync(key,softDelete).ConfigureAwait(false));
    public OASISResult<bool> DeleteHolonSoft(string key,bool softDelete=true)=>DeleteHolonSoftAsync(key,softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string key,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        =>LoadHolonAsync(key,loadChildren,recursive,maxChildDepth,continueOnError,loadChildrenFromProvider,version);
    public OASISResult<IHolon> LoadHolonByProviderKey(string key,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        =>LoadHolonByProviderKeyAsync(key,loadChildren,recursive,maxChildDepth,continueOnError,loadChildrenFromProvider,version).GetAwaiter().GetResult();
    public string GetProviderVersion()=>"2.0.0";
    public Task<string> GetProviderVersionAsync()=>Task.FromResult(GetProviderVersion());
    public void Dispose()
    {
        try{_owner?.Dispose();}
        finally{_private.Dispose();IsProviderActivated=false;}
    }
}

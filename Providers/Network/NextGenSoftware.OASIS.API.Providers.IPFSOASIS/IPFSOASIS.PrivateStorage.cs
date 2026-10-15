using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public partial class IPFSOASIS
{
    private const string PrivateIndexContext="oasis/ipfs/private-index/v1";
    private Func<byte[]> _storageKeyResolver=null;

    private EncryptedRecordCodec OpenPrivateCodec()
    {
        var key=_storageKeyResolver != null ? _storageKeyResolver()
            :PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_IPFS_STORAGE_ENCRYPTION_KEY");
        try {return new EncryptedRecordCodec(key,compress:true);}
        finally {if(key != null) CryptographicOperations.ZeroMemory(key);}
    }

    private string ProtectIndex(string json)
    {
        using var codec=OpenPrivateCodec();
        return codec.Encrypt(PrivateIndexContext,json);
    }

    private string UnprotectIndex(string envelope)
    {
        using var codec=OpenPrivateCodec();
        return codec.Decrypt(PrivateIndexContext,envelope);
    }

    private static string RecordContext(string kind,Guid id)
    {
        if(id==Guid.Empty || kind is not ("avatar" or "avatar-detail" or "holon"))
            throw new ArgumentException("A canonical IPFS record kind and nonempty identity are required.");
        return "oasis/ipfs/private-record/v1/"+kind+"/"+id.ToString("D");
    }

    private string ProtectRecord(string kind,Guid id,string json)
    {
        var context=RecordContext(kind,id);
        using var codec=OpenPrivateCodec();
        return codec.Encrypt(context,json);
    }

    private string UnprotectRecord(string kind,Guid id,string envelope)
    {
        var context=RecordContext(kind,id);
        using var codec=OpenPrivateCodec();
        return codec.Decrypt(context,envelope);
    }

    private async Task<T> ReadPrivateRecordAsync<T>(string cid,string kind,Guid id) where T:class,IHolonBase
    {
        var context=RecordContext(kind,id);
        using var codec=OpenPrivateCodec();
        var json=codec.Decrypt(context,await LoadStringToJson(cid));
        var record=JsonConvert.DeserializeObject<T>(json) ?? throw new InvalidDataException("IPFS private blob has no entity content.");
        if(record.Id!=id) throw new InvalidDataException("IPFS private blob identity disagrees with its authenticated index.");
        record.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
        record.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS]=cid;
        return record;
    }

    private async Task<List<T>> ReadIndexedCollectionAsync<T>(string kind,Func<HolonResume,bool> predicate,int version=0) where T:class,IHolonBase
    {
        if(version!=0) throw new NotSupportedException("IPFS current index collection requires version 0; historical traversal is not implemented.");
        var lookup=await LoadLookupToJson();
        var selected=lookup.Where(entry=>entry.Value?.RecordKind==kind && predicate(entry.Value)).ToList();
        if(selected.GroupBy(entry=>entry.Value.Id).Any(group=>group.Count()>1)) throw new StorageIdentityAmbiguousException();
        var records=new List<T>();
        foreach(var entry in selected) records.Add(await ReadPrivateRecordAsync<T>(entry.Key,kind,entry.Value.Id));
        return records;
    }
}

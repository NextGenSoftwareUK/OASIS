using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public partial class IPFSOASIS
{
    private static void ValidateCanonicalSelector(IHolonBase record,string kind,Func<HolonResume,bool> selector)
    {
        var canonical=new HolonResume{Id=record.Id,RecordKind=kind,HolonType=record.HolonType,
            ProviderUniqueStorageKey=record.ProviderUniqueStorageKey,
            ParentHolonId=record is IHolon holon?holon.ParentHolonId:Guid.Empty,
            login=record is IAvatar avatar?avatar.Username:record is IAvatarDetail detail?detail.Username:null,
            email=record is IAvatar profile?profile.Email:record is IAvatarDetail identity?identity.Email:null};
        if(!selector(canonical))throw new InvalidDataException("IPFS index selector differs from the canonical current record.");
    }
    private KeyValuePair<string,HolonResume> FindIndexEntry(string kind,Func<HolonResume,bool> predicate,bool allowMissing=false)
    {
        var matches=_idLookup.Where(entry=>entry.Value?.RecordKind==kind && predicate(entry.Value)).Take(2).ToList();
        if(matches.Count>1) throw new StorageIdentityAmbiguousException();
        if(matches.Count==0 && !allowMissing) throw new KeyNotFoundException("No IPFS index record matches the requested kind and identity.");
        return matches.SingleOrDefault();
    }

    private void ReplaceIndexEntry(string cid,HolonResume record)
    {
        if(string.IsNullOrWhiteSpace(cid) || record == null || record.Id==Guid.Empty
            || record.RecordKind is not ("avatar" or "avatar-detail" or "holon"))
            throw new ArgumentException("A valid CID, identity and canonical record kind are required.");
        if(_idLookup.TryGetValue(cid,out var existing) && (existing?.Id!=record.Id || existing.RecordKind!=record.RecordKind))
            throw new InvalidOperationException("IPFS CID already belongs to another indexed identity/kind.");
        foreach(var previous in _idLookup.Where(entry=>entry.Value?.RecordKind==record.RecordKind && entry.Value.Id==record.Id).Select(entry=>entry.Key).ToArray())
            _idLookup.Remove(previous);
        _idLookup.Add(cid,record);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public partial class IPFSOASIS
{
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

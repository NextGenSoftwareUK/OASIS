using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Strict SDK storage-response boundary; malformed data must not become a transient provider failure.</summary>
public static class StorageJsonResponse
{
    public static string NullableString(JsonElement value)
    {
        if(value.ValueKind==JsonValueKind.Null) return null;
        if(value.ValueKind!=JsonValueKind.String) throw new InvalidDataException("Storage get response must be a string or explicit null.");
        return value.GetString();
    }

    public static IReadOnlyList<string> KeyPage(JsonElement response,string prefix,string startAfter=null)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        if(response.ValueKind!=JsonValueKind.Array) throw new InvalidDataException("Storage list response must be an array.");
        var keys=new List<string>();var previous=startAfter;
        foreach(var item in response.EnumerateArray())
        {
            if(item.ValueKind!=JsonValueKind.String) throw new InvalidDataException("Storage list entries must be strings.");
            var key=item.GetString();
            if(!key.StartsWith(prefix,StringComparison.Ordinal) || (previous!=null && string.CompareOrdinal(previous,key)>=0))
                throw new InvalidDataException("Storage list returned duplicate, unordered or out-of-prefix keys.");
            keys.Add(key);previous=key;
        }
        return keys;
    }
}

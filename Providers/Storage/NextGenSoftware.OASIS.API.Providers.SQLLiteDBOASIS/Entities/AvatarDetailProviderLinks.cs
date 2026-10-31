using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;

// One native JSON column owns these inherited provider dictionaries, not profile metadata.
internal sealed class AvatarDetailProviderLinks
{
    public Dictionary<ProviderType,string> Current {get;set;}=new();
    public Dictionary<ProviderType,string> PreviousVersion {get;set;}=new();
    public Dictionary<ProviderType,Dictionary<string,string>> Metadata {get;set;}=new();

    internal static string Encode(IHolonBase source)
    {
        var links=new AvatarDetailProviderLinks{Current=source.ProviderUniqueStorageKey??new(),
            PreviousVersion=source.PreviousVersionProviderUniqueStorageKey??new(),Metadata=source.ProviderMetaData??new()};
        links.Validate();return JsonSerializer.Serialize(links);
    }
    internal static void Apply(string json,IHolonBase target)
    {
        AvatarDetailProviderLinks links;
        try{links=JsonSerializer.Deserialize<AvatarDetailProviderLinks>(json);}
        catch(JsonException cause){throw new InvalidDataException("Stored avatar-detail provider links are invalid JSON.",cause);}
        if(links==null)throw new InvalidDataException("Stored avatar-detail provider links must be an object.");
        links.Validate();
        target.ProviderUniqueStorageKey=links.Current;target.PreviousVersionProviderUniqueStorageKey=links.PreviousVersion;target.ProviderMetaData=links.Metadata;
    }
    private void Validate()
    {
        if(Current==null||PreviousVersion==null||Metadata==null||Metadata.Values.Any(value=>value==null)
            ||Current.Keys.Concat(PreviousVersion.Keys).Concat(Metadata.Keys).Any(key=>!Enum.IsDefined(typeof(ProviderType),key)))
            throw new InvalidDataException("Avatar-detail provider dictionaries require valid provider identities and nonnull maps.");
    }
}

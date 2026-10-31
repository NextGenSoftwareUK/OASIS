using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;

internal static class AvatarDetailDimensionLinks
{
    internal static string Encode(IAvatarDetail source)
    {
        var links=source.DimensionLevelIds==null?new Dictionary<DimensionLevel,Guid>():new Dictionary<DimensionLevel,Guid>(source.DimensionLevelIds);
        foreach(var level in links.Keys)if(!Enum.IsDefined(typeof(DimensionLevel),level))throw new InvalidDataException("Unknown dimension identity key.");
        return JsonSerializer.Serialize(links);
    }
    internal static Dictionary<DimensionLevel,Guid> Decode(string json)
    {
        try{
            using var document=JsonDocument.Parse(json);
            if(document.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Stored dimension identities must be a JSON object.");
            var links=new Dictionary<DimensionLevel,Guid>();
            foreach(var property in document.RootElement.EnumerateObject()){
                if(!Enum.TryParse<DimensionLevel>(property.Name,out var level)||!Enum.IsDefined(typeof(DimensionLevel),level)
                    ||!string.Equals(property.Name,level.ToString(),StringComparison.Ordinal))throw new InvalidDataException("Unknown or noncanonical stored dimension identity key.");
                if(!links.TryAdd(level,property.Value.GetGuid()))throw new InvalidDataException("Duplicate stored dimension identity key.");
            }
            return links;
        }catch(Exception cause)when(cause is JsonException or FormatException or InvalidOperationException){
            throw new InvalidDataException("Invalid stored dimension identity map.",cause);
        }
    }
}

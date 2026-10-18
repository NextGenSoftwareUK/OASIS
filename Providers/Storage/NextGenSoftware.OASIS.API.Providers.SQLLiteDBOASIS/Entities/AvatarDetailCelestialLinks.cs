using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;

internal static class AvatarDetailCelestialLinks
{
    private static readonly JsonSerializerOptions Options=new(){UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
    internal static string Encode(IHolon source)=>JsonSerializer.Serialize(HolonCelestialParentIds.Capture(source),Options);
    internal static void Apply(string json,IHolon target)
    {
        using var document=JsonDocument.Parse(json);
        if(document.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Stored celestial identities must be an object.");
        var fields=new HashSet<string>(StringComparer.Ordinal);
        foreach(var property in document.RootElement.EnumerateObject())
            if(!fields.Add(property.Name))throw new InvalidDataException("Duplicate stored celestial identity field.");
        // {} is the explicit legacy default. Nonempty snapshots must be complete, not partly missing identities.
        if(fields.Count>0&&typeof(HolonCelestialParentIds).GetProperties().Where(property=>property.CanWrite).Any(property=>!fields.Contains(property.Name)))
            throw new InvalidDataException("Stored celestial identity snapshot is incomplete.");
        var links=JsonSerializer.Deserialize<HolonCelestialParentIds>(json,Options)??throw new InvalidDataException("Missing celestial identity snapshot.");
        links.Apply(target);
    }
}

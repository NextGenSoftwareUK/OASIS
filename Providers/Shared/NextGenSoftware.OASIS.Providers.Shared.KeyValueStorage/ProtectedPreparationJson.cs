using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Opaque id/envelope transport only. Authentication and logical comparison remain Core's responsibility.</summary>
public static class ProtectedPreparationJson
{
    public const long MaximumPayloadCharacters=HolonWritePreparation.MaximumTotalEnvelopeCharacters*6L+
        1024L*HolonWritePreparation.MaximumRecords;
    public static string Write(HolonWritePreparation candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return JsonSerializer.Serialize(candidate.Records.Select(record=>new {
            id=record.HolonId.ToString("D"),envelope=record.Envelope
        }));
    }
    public static HolonWritePreparation Read(HolonWritePreparation owner,string payload)
    {
        ArgumentNullException.ThrowIfNull(owner);ArgumentNullException.ThrowIfNull(payload);
        if(payload.Length>MaximumPayloadCharacters)throw new InvalidDataException("Stored preparation exceeds its transport limit.");
        using var document=JsonDocument.Parse(payload,new JsonDocumentOptions{MaxDepth=4});
        var root=document.RootElement;
        if(root.ValueKind!=JsonValueKind.Array||root.GetArrayLength()<1||root.GetArrayLength()>HolonWritePreparation.MaximumRecords)
            throw new InvalidDataException("Stored preparation record count is invalid.");
        var records=new List<ProtectedHolonPreparation>();
        foreach(var item in root.EnumerateArray()) {
            if(item.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Stored preparation record must be an object.");
            var fields=new HashSet<string>(StringComparer.Ordinal);
            foreach(var field in item.EnumerateObject())
                if(!fields.Add(field.Name))throw new InvalidDataException("Stored preparation has duplicate fields.");
            if(!fields.SetEquals(new[]{"id","envelope"})||item.GetProperty("id").ValueKind!=JsonValueKind.String
                ||item.GetProperty("envelope").ValueKind!=JsonValueKind.String)
                throw new InvalidDataException("Stored preparation schema is invalid.");
            var text=item.GetProperty("id").GetString();
            if(!Guid.TryParseExact(text,"D",out var id)||text!=id.ToString("D"))
                throw new InvalidDataException("Stored preparation identity is invalid.");
            records.Add(new ProtectedHolonPreparation(id,item.GetProperty("envelope").GetString()!));
        }
        return new HolonWritePreparation(owner.OperationId,owner.ActorId,owner.Authority,records);
    }
}

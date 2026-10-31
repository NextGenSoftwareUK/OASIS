using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    private static T ParseCanonicalPolkadotRecord<T>(string json) where T:class,IHolonBase
    {
        using var document=JsonDocument.Parse(json);
        if(document.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Stored record must be an object.");
        var identities=document.RootElement.EnumerateObject().Where(field=>string.Equals(field.Name,"Id",StringComparison.OrdinalIgnoreCase)).ToArray();
        if(identities.Length!=1 || identities[0].Value.ValueKind!=JsonValueKind.String || !Guid.TryParse(identities[0].Value.GetString(),out var id) || id==Guid.Empty)
            throw new InvalidDataException("Stored record requires one explicit nonempty GUID identity.");
        var record=OasisJson.DeserializeStored<T>(json);
        if(record==null || record.Id!=id)throw new InvalidDataException("Decoded stored identity differs from its explicit identity.");
        return record;
    }

    private static IEnumerable<T> ParseCanonicalPolkadotRecords<T>(string json,string member) where T:class,IHolonBase
    {
        using var document=JsonDocument.Parse(json);
        var root=document.RootElement;
        if(root.ValueKind==JsonValueKind.Object && root.TryGetProperty(member,out var rows))root=rows;
        if(root.ValueKind!=JsonValueKind.Array)throw new InvalidDataException("Stored record collection must be an array.");
        return root.EnumerateArray().Select(row=>ParseCanonicalPolkadotRecord<T>(row.GetRawText())).ToArray();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    private static Holon DecodeHolon(Dictionary<string,object> fields,string documentId)
    {
        if(!fields.TryGetValue("data_json",out var value) || value is not string json)
            throw new InvalidDataException("Firestore holon lacks canonical data_json; old mappings require explicit review.");
        var holon=OasisJson.Deserialize<Holon>(json) ?? throw new InvalidDataException("Firestore holon payload is null.");
        if(holon.Id==Guid.Empty || holon.Id.ToString()!=documentId || !fields.TryGetValue("id",out var id) || !Equals(id,documentId))
            throw new InvalidDataException("Firestore holon identity does not match document key.");
        return holon;
    }
}

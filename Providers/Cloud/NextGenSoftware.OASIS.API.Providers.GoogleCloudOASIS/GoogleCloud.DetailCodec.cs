using System;
using System.Collections.Generic;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    private static Dictionary<string,object> EncodeAvatarDetail(IAvatarDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        if(detail.Id==Guid.Empty) throw new ArgumentException("A nonempty avatar detail identity is required.");
        return new Dictionary<string,object> {
            ["id"]=detail.Id.ToString(),["username"]=detail.Username,["email"]=detail.Email,
            ["isActive"]=detail.IsActive,["version"]=detail.Version,
            ["createdDate"]=ToFirestoreTimestamp(detail.CreatedDate),["modifiedDate"]=ToFirestoreTimestamp(detail.ModifiedDate),
            ["savedAt"]=ToFirestoreTimestamp(DateTime.UtcNow),["data_json"]=OasisJson.Serialize(detail)
        };
    }

    private static AvatarDetail DecodeAvatarDetail(Dictionary<string,object> fields,string documentId)
    {
        if(!fields.TryGetValue("data_json",out var value) || value is not string json)
            throw new InvalidDataException("Firestore detail lacks canonical data_json; legacy manual mappings require explicit review.");
        var detail=OasisJson.Deserialize<AvatarDetail>(json) ?? throw new InvalidDataException("Firestore detail payload is null.");
        if(detail.Id==Guid.Empty || detail.Id.ToString()!=documentId || !fields.TryGetValue("id",out var id) || !Equals(id,documentId))
            throw new InvalidDataException("Firestore detail identity does not match document key.");
        return detail;
    }
}

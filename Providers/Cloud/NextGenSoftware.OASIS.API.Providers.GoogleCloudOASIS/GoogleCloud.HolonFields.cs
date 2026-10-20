using System.Collections.Generic;
using Google.Cloud.Firestore;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    // One existing projection owner for ordinary and atomic graph saves.
    private Dictionary<string,object> CreateFirestoreHolonFields(IHolon holon,string json)=>new() {
        ["id"]=holon.Id.ToString(),["name"]=holon.Name,["description"]=holon.Description,
        ["holonType"]=holon.HolonType.ToString(),["createdDate"]=ToFirestoreTimestamp(holon.CreatedDate),
        ["modifiedDate"]=ToFirestoreTimestamp(holon.ModifiedDate),["version"]=holon.Version,["isActive"]=holon.IsActive,
        ["parentId"]=holon.ParentHolonId.ToString(),
        ["providerKey"]=holon.ProviderUniqueStorageKey!=null&&holon.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.GoogleCloudOASIS,out var key)?key:"",
        ["previousVersionId"]=holon.PreviousVersionId.ToString(),["nextVersionId"]=holon.VersionId.ToString(),
        ["isChanged"]=holon.IsChanged,["isNew"]=holon.IsNewHolon,["isDeleted"]=holon.IsDeleted,
        ["deletedByAvatarId"]=holon.DeletedByAvatarId.ToString(),["deletedDate"]=ToFirestoreTimestamp(holon.DeletedDate),
        ["createdByAvatarId"]=holon.CreatedByAvatarId.ToString(),["modifiedByAvatarId"]=holon.ModifiedByAvatarId.ToString(),
        ["googleCloudProjectId"]=_projectId,["googleCloudBucketName"]=_bucketName,
        ["googleCloudFirestoreDatabaseId"]=_firestoreDatabaseId,["googleCloudBigQueryDatasetId"]=_bigQueryDatasetId,
        ["savedAt"]=Timestamp.GetCurrentTimestamp(),["data_json"]=json
    };
}

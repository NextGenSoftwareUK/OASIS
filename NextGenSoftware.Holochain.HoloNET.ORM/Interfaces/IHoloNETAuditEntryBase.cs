using NextGenSoftware.Holochain.HoloNET.Client;

namespace NextGenSoftware.Holochain.HoloNET.ORM.Interfaces
{
    public interface IHoloNETAuditEntryBase : IHoloNETEntryBase
    {
        List<IHoloNETAuditEntry> AuditEntries { get; set; }
        string CreatedBy { get; set; }
        DateTime CreatedDate { get; set; }
        string DeletedBy { get; set; }
        DateTime DeletedDate { get; set; }
        Guid Id { get; set; }
        bool IsActive { get; set; }
        bool IsActiveFlagEnabled { get; set; }
        bool IsAuditAgentCreateModifyDeleteFieldsEnabled { get; set; }
        bool IsAuditTrackingEnabled { get; set; }
        bool IsGenerateUniqueGuidIdEnabled { get; set; }
        bool IsVersionTrackingEnabled { get; set; }
        string ModifiedBy { get; set; }
        DateTime ModifiedDate { get; set; }
        int Version { get; set; }

        ZomeFunctionCallBackEventArgs Delete(Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs Delete(string entryHash, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs DeleteByCustomField(string customFieldToDeleteByValue, string customFieldToDeleteByKey = "", Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> DeleteAsync(Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> DeleteAsync(string entryHash, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> DeleteByCustomFieldAsync(string customFieldToDeleteByValue, string customFieldToDeleteByKey = "", Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs Load(int version = 0, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs Load(string entryHash, int version = 0, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> LoadAsync(int version = 0, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> LoadAsync(string entryHash, int version = 0, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs LoadByCustomField(string customFieldToLoadByValue, string customFieldToLoadByKey = "", int version = 0, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> LoadByCustomFieldAsync(string customFieldToLoadByValue, string customFieldToLoadByKey = "", int version = 0, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs Save(Dictionary<string, string> customDataKeyValuePair = null, Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePair = null, bool cachePropertyInfos = true, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs Save(dynamic paramsObject, bool autoGeneratUpdatedEntryObjectAndOriginalEntryHashRustParams = true, string updatedEntryRustParamName = "updated_entry", string originalEntryHashRustParamName = "original_action_hash", bool addAuditInfoToParams = true, bool addVersionToParams = true, bool addUniqueGuidIdToParams = true, bool addIsActiveFlagToParams = true, string createdDateRustParamName = "created_date", string createdByRustParamName = "created_by", string modifiedDateRustParamName = "modified_date", string modifiedByRustParamName = "modified_by", string deletedDateRustParamName = "deleted_date", string deletedByRustParamName = "deleted_by", string versionRustParamName = "version", string idRustParamName = "id", string isActiveRustParamName = "is_active", bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> SaveAsync(Dictionary<string, string> customDataKeyValuePair = null, Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePair = null, bool cachePropertyInfos = true, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> SaveAsync(dynamic paramsObject, bool autoGeneratUpdatedEntryObjectAndOriginalEntryHashRustParams = true, string updatedEntryRustParamName = "updated_entry", string originalEntryHashRustParamName = "original_action_hash", bool addAuditInfoToParams = true, bool addVersionToParams = true, bool addUniqueGuidIdToParams = true, bool addIsActiveFlagToParams = true, string createdDateRustParamName = "created_date", string createdByRustParamName = "created_by", string modifiedDateRustParamName = "modified_date", string modifiedByRustParamName = "modified_by", string deletedDateRustParamName = "deleted_date", string deletedByRustParamName = "deleted_by", string versionRustParamName = "version", string idRustParamName = "id", string isActiveRustParamName = "is_active", bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
    }
}
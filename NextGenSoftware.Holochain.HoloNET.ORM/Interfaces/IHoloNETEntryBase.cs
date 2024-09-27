using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.Holochain.HoloNET.ORM.Entries;
using NextGenSoftware.Holochain.HoloNET.ORM.Enums;

namespace NextGenSoftware.Holochain.HoloNET.ORM.Interfaces
{
    public interface IHoloNETEntryBase
    {
        IRecord Record { get; set; }
        string EntryHash { get; set; }
        string ActionHash { get; set; }
        IHoloNETClientAppBase HoloNETClient { get; set; }
        bool IsChanged { get; set; }
        bool IsInitialized { get; }
        bool IsInitializing { get; }
        Dictionary<string, object> OrginalDataKeyValuePairs { get; }
        IHoloNETEntryBase OrginalEntry { get; }
        Dictionary<string, string> OrginalKeyValuePairs { get; }
        string PreviousVersionActionHash { get; set; }
        HoloNETEntryState State { get; set; }
        string ZomeCreateEntryFunction { get; set; }
        string ZomeDeleteEntryFunction { get; set; }
        string ZomeLoadEntryFunction { get; set; }
        string ZomeName { get; set; }
        string ZomeUpdateEntryFunction { get; set; }

        event HoloNETEntryBase.Closed OnClosed;
        event HoloNETEntryBase.Deleted OnDeleted;
        event HoloNETEntryBase.Error OnError;
        event HoloNETEntryBase.Initialized OnInitialized;
        event HoloNETEntryBase.Loaded OnLoaded;
        event HoloNETEntryBase.Saved OnSaved;

        dynamic BuildDynamicParamsObject(Dictionary<string, string> customDataKeyValuePairs = null, Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePairs = null, bool cachePropertyInfos = true);
        HoloNETShutdownEventArgs Close(ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings);
        Task<HoloNETShutdownEventArgs> CloseAsync(DisconnectedCallBackMode disconnectedCallBackMode = DisconnectedCallBackMode.WaitForHolochainConductorToDisconnect, ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings);
        ZomeFunctionCallBackEventArgs Delete(Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs Delete(string entryHash, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs DeleteByCustomField(string customFieldToDeleteByValue, string customFieldToDeleteByKey = "", Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> DeleteAsync(Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> DeleteAsync(string entryHash, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> DeleteByCustomFieldAsync(string customFieldToDeleteByValue, string customFieldToDeleteByKey = "", Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        bool HasEntryChanged(Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePairs = null, bool cachePropertyInfos = true);
        void Initialize(bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        Task InitializeAsync(ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        ZomeFunctionCallBackEventArgs Load(Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs Load(string entryHash, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> LoadAsync(Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> LoadAsync(string entryHash, Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs LoadByCustomField(string customFieldToLoadByValue, string customFieldToLoadByKey = "", Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> LoadByCustomFieldAsync(string customFieldToLoadByValue, string customFieldToLoadByKey = "", Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs Save(Dictionary<string, string> customDataKeyValuePairs = null, Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePairs = null, bool cachePropertyInfos = true, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs Save(dynamic paramsObject, bool autoGeneratUpdatedEntryObjectAndOriginalEntryHashRustParams = true, string updatedEntryRustParamName = "updated_entry", string originalEntryHashRustParamName = "original_action_hash", bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> SaveAsync(Dictionary<string, string> customDataKeyValuePairs = null, Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePairs = null, bool cachePropertyInfos = true, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> SaveAsync(dynamic paramsObject, bool autoGeneratUpdatedEntryObjectAndOriginalEntryHashRustParams = true, string updatedEntryRustParamName = "updated_entry", string originalEntryHashRustParamName = "original_action_hash", bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        Task<ReadyForZomeCallsEventArgs> WaitTillHoloNETInitializedAsync();
    }
}
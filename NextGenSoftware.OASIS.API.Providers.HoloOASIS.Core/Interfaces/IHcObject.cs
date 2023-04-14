
using NextGenSoftware.Holochain.HoloNET.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public interface IHcObject // : IHoloNETAuditEntryBaseClass
    {
        //TODO: Temp until release new version of HoloNET with interfaces defined.
        List<HoloNETAuditEntry> AuditEntries { get; set; }
        string CreatedBy { get; set; }
        DateTime CreatedDate { get; set; }
        string DeletedBy { get; set; }
        DateTime DeletedDate { get; set; }
        Guid Id { get; set; }
        bool IsActive { get; set; }
        bool IsAuditAgentCreateModifyDeleteFieldsEnabled { get; set; }
        bool IsAuditTrackingEnabled { get; set; }
        bool IsVersionTrackingEnabled { get; set; }
        string ModifiedBy { get; set; }
        DateTime ModifiedDate { get; set; }
        int Version { get; set; }

        EntryData EntryData { get; set; }
        string EntryHash { get; set; }
        HoloNETClient HoloNETClient { get; set; }
        bool IsInitialized { get; }
        bool IsInitializing { get; }
        string PreviousVersionEntryHash { get; set; }
        string ZomeCreateCollectionFunction { get; set; }
        string ZomeCreateEntryFunction { get; set; }
        string ZomeDeleteCollectionFunction { get; set; }
        string ZomeDeleteEntryFunction { get; set; }
        string ZomeLoadCollectionFunction { get; set; }
        string ZomeLoadEntryFunction { get; set; }
        string ZomeName { get; set; }
        string ZomeUpdateCollectionFunction { get; set; }
        string ZomeUpdateEntryFunction { get; set; }

        event HoloNETEntryBaseClass.Closed OnClosed;
        event HoloNETEntryBaseClass.Deleted OnDeleted;
        event HoloNETEntryBaseClass.Error OnError;
        event HoloNETEntryBaseClass.Initialized OnInitialized;
        event HoloNETEntryBaseClass.Loaded OnLoaded;
        event HoloNETEntryBaseClass.Saved OnSaved;

        HoloNETShutdownEventArgs Close(ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseConfigSettings);
        Task<HoloNETShutdownEventArgs> CloseAsync(DisconnectedCallBackMode disconnectedCallBackMode = DisconnectedCallBackMode.WaitForHolochainConductorToDisconnect, ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseConfigSettings);
        ZomeFunctionCallBackEventArgs Delete();
        ZomeFunctionCallBackEventArgs Delete(object customFieldToLoadByValue);
        ZomeFunctionCallBackEventArgs Delete(string entryHash);
        Task<ZomeFunctionCallBackEventArgs> DeleteAsync();
        Task<ZomeFunctionCallBackEventArgs> DeleteAsync(object customFieldToLoadByValue);
        Task<ZomeFunctionCallBackEventArgs> DeleteAsync(string entryHash);
        void Initialize(bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        Task InitializeAsync(ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateConfigWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        ZomeFunctionCallBackEventArgs Load(bool useReflectionToMapOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs Load(object customFieldToLoadByValue, bool useReflectionToMapOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs Load(string entryHash, bool useReflectionToMapOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> LoadAsync(bool useReflectionToMapOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> LoadAsync(object customFieldToLoadByValue, bool useReflectionToMapOntoEntryDataObject = true);
        Task<ZomeFunctionCallBackEventArgs> LoadAsync(string entryHash, bool useReflectionToMapOntoEntryDataObject = true);
        ZomeFunctionCallBackEventArgs LoadCollection(string collectionAnchor);
        Task<ZomeFunctionCallBackEventArgs> LoadCollectionAsync(string collectionAnchor);
        ZomeFunctionCallBackEventArgs Save(Dictionary<string, string> customDataKeyValuePair = null, Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePair = null, bool cachePropertyInfos = true);
        ZomeFunctionCallBackEventArgs Save(dynamic paramsObject);
        Task<ZomeFunctionCallBackEventArgs> SaveAsync(Dictionary<string, string> customDataKeyValuePair = null, Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePair = null, bool cachePropertyInfos = true);
        Task<ZomeFunctionCallBackEventArgs> SaveAsync(dynamic paramsObject);
        Task<ReadyForZomeCallsEventArgs> WaitTillHoloNETInitializedAsync();
    }
}
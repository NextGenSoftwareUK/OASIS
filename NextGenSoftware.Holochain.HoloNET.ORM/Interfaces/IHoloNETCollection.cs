using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.Holochain.HoloNET.ORM.Collections;
using NextGenSoftware.Holochain.HoloNET.ORM.Entries;

namespace NextGenSoftware.Holochain.HoloNET.ORM.Interfaces
{
    public interface IHoloNETCollection<T> where T : HoloNETEntryBase
    {
        List<T> EntriesAddedSinceLastSaved { get; }
        List<T> EntriesRemovedSinceLastSaved { get; }
        IHoloNETClientAppBase HoloNETClient { get; set; }
        bool IsChanges { get; }
        bool IsInitialized { get; }
        bool IsInitializing { get; }
        List<T> OriginalEntries { get; }
        string ZomeAddEntryToCollectionFunction { get; set; }
        string ZomeBatchUpdateCollectionFunction { get; set; }
        string ZomeLoadCollectionFunction { get; set; }
        string ZomeName { get; set; }
        string ZomeRemoveEntryFromCollectionFunction { get; set; }

        event HoloNETCollection<T>.Closed OnClosed;
        event HoloNETCollection<T>.CollectionLoaded OnCollectionLoaded;
        event HoloNETCollection<T>.CollectionSaved OnCollectionSaved;
        event HoloNETCollection<T>.Error OnError;
        event HoloNETCollection<T>.HoloNETEntryAddedToCollection OnHoloNETEntryAddedToCollection;
        event HoloNETCollection<T>.HoloNETEntryRemovedFromCollection OnHoloNETEntryRemovedFromCollection;
        event HoloNETCollection<T>.Initialized OnInitialized;

        ZomeFunctionCallBackEventArgs AddHoloNETEntryToCollectionAndSave(T holoNETEntry, bool saveHoloNETEntry = true, string collectionAnchor = "");
        Task<ZomeFunctionCallBackEventArgs> AddHoloNETEntryToCollectionAndSaveAsync(T holoNETEntry, bool saveHoloNETEntry = true, string collectionAnchor = "", bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        HoloNETShutdownEventArgs Close(ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings);
        Task<HoloNETShutdownEventArgs> CloseAsync(DisconnectedCallBackMode disconnectedCallBackMode = DisconnectedCallBackMode.WaitForHolochainConductorToDisconnect, ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings);
        void Initialize(bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        Task InitializeAsync(ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        //HoloNETCollectionLoadedResult<T> LoadCollection(string collectionAnchor = "",  Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        //Task<HoloNETCollectionLoadedResult<T>> LoadCollectionAsync(string collectionAnchor = "",  Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true);
        HoloNETCollectionLoadedResult<T> LoadCollection(string collectionAnchor = "", Dictionary<string, string> customDataKeyValuePairs = null);
        Task<HoloNETCollectionLoadedResult<T>> LoadCollectionAsync(string collectionAnchor = "", Dictionary<string, string> customDataKeyValuePairs = null);
        void ReCalculateEntriesAddedOrRemovedSinceLastSaved();
        ZomeFunctionCallBackEventArgs RemoveHoloNETEntryFromCollectionAndSave(T holoNETEntry, string collectionAnchor = "");
        Task<ZomeFunctionCallBackEventArgs> RemoveHoloNETEntryFromCollectionAndSaveAsync(T holoNETEntry, string collectionAnchor = "");
        HoloNETCollectionSavedResult SaveAllChanges(bool saveChangesMadeToEntries = true, bool saveAsOneBatchOperation = true, bool saveHoloNETEntryWhenAddingToCollection = true, bool continueOnError = true, string collectionAnchor = "", Dictionary<string, string> customDataKeyValuePairs = null, Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePairs = null, bool cachePropertyInfos = true);
        Task<HoloNETCollectionSavedResult> SaveAllChangesAsync(bool saveChangesMadeToEntries = true, bool saveAsOneBatchOperation = true, bool saveHoloNETEntryWhenAddingToCollection = true, bool continueOnError = true, string collectionAnchor = "", Dictionary<string, string> customDataKeyValuePairs = null, Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePairs = null, bool cachePropertyInfos = true);
        void UpdateEnriesState(Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePairs = null, bool cachePropertyInfos = true);
        Task<ReadyForZomeCallsEventArgs> WaitTillHoloNETInitializedAsync();
    }
}
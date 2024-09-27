using System.Dynamic;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.Holochain.HoloNET.ORM.Entries;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;
using NextGenSoftware.Logging;
using NextGenSoftware.Logging.Interfaces;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.WebSocket;

namespace NextGenSoftware.Holochain.HoloNET.ORM.Collections
{
    public class HoloNETCollection<T> : List<T>, IHoloNETCollection<T> where T : HoloNETEntryBase
    {
        private bool _disposeOfHoloNETClient = false;

        public delegate void Error(object sender, HoloNETErrorEventArgs e);

        /// <summary>
        /// Fired when there is an error either in HoloNETEntryBaseClass or the HoloNET client itself.  
        /// </summary>
        public event Error OnError;

        public delegate void Initialized(object sender, ReadyForZomeCallsEventArgs e);

        /// <summary>
        /// Fired after the Initialize method has finished initializing the HoloNET client. This will also call the Connect and RetrieveAgentPubKeyAndDnaHash methods on the HoloNET client. This event is then fired when the HoloNET client has successfully connected to the Holochain Conductor, retrieved the AgentPubKey & DnaHash & then fired the OnReadyForZomeCalls event. See also the IsInitializing and the IsInitialized properties.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         
        /// </summary>
        public event Initialized OnInitialized;


        public delegate void CollectionLoaded(object sender, HoloNETCollectionLoadedResult<T> e);

        /// <summary>
        /// Fired after the LoadCollection method has finished loading the Holochain Collection from the Holochain Conductor. This calls the CallZomeFunction on the HoloNET client passing in the zome function name specified in the constructor param `zomeLoadCollectionFunction` or property `ZomeLoadCollectionFunction` and then maps the data returned from the zome call onto your collection.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
        /// </summary>
        public event CollectionLoaded OnCollectionLoaded;


        public delegate void HoloNETEntryAddedToCollection(object sender, ZomeFunctionCallBackEventArgs e);

        /// <summary>
        /// Fired after an HoloNET Entry has been added to the collection. This can be from either calling the AddHoloNETEntryToCollection function or from the SaveCollection function which will automatically add any new entries to the collection. This calls the CallZomeFunction on the HoloNET client passing in the zome function name specified in the constructor param `zomeAddEntryToCollectionFunction` or property `ZomeAddEntryToCollectionFunction`.
        /// </summary>
        public event HoloNETEntryAddedToCollection OnHoloNETEntryAddedToCollection;


        public delegate void HoloNETEntryRemovedFromCollection(object sender, ZomeFunctionCallBackEventArgs e);

        /// <summary>
        /// Fired after an HoloNET Entry has been removed from the collection. This can be from either calling the RemoveHoloNETEntryFromCollection function or from the SaveCollection function which will automatically remove any old entries from the collection (ones that have been removed in c# client side code but not yet synced with the rust backend code). This calls the CallZomeFunction on the HoloNET client passing in the zome function name specified in the constructor param `zomeRemoveEntryToCollectionFunction` or property `ZomeRemoveEntryToCollectionFunction`.
        /// </summary>
        public event HoloNETEntryRemovedFromCollection OnHoloNETEntryRemovedFromCollection;


        public delegate void CollectionSaved(object sender, HoloNETCollectionSavedResult e);

        /// <summary>
        /// Fired after the Save/SaveAsync method has finished saving all HoloNET Entries that were either added, removed or updated. This may optionally update the collection in one batch operation. See Save/SaveAsync for more info.
        /// </summary>
        public event CollectionSaved OnCollectionSaved;


        public delegate void Closed(object sender, HoloNETShutdownEventArgs e);

        /// <summary>
        /// Fired after the Close method has finished closing the connection to the Holochain Conductor and has shutdown all running Holochain Conductors (if configured to do so). This method calls the ShutdownHoloNET internally.
        /// </summary>
        public event Closed OnClosed;

        /// <summary>
        /// This is a new class introduced in HoloNET 3 that makes it super easy to manage collections of HoloNETEntries so you do not need to interact with the client directly.
        /// It has two main types of constructors, one that allows you to pass in a HoloNETClient instance (which can be shared with other classes that extend the HoloNETEntryBase class or HoloNETEntryBase class as well as other HoloNETCollection's) or if you do not pass a HoloNETClient instance in using the other constructor it will create its own internal instance to use just for this class. 
        /// NOTE: This is a preview of some of the advanced functionality that will be present in the upcoming .NET HDK Low Code Generator, which generates dynamic rust and c# code from your metadata freeing you to focus on your amazing business idea and creativity rather than worrying about learning Holochain, Rust and then getting it to all work in Windows and with C#. HAppy Days! ;-)
        /// </summary>
        /// <param name="installedAppId">The AppId of the installed hApp this HoloNETCollection will be bound to (it will be bound to the internal HoloNETClientAppAgent). This will overrite the InstalledAppId stored in the HoloNETDNA (if there is one).</param>
        /// <param name="myAgentPubKey">The agentPubKey of the agent that this HoloNETCollection will be bound to (it will be bound to the internal HoloNETClientAppAgent). This will overrite the AgentPubKey defined in the HoloNETDNA (if there is one).</param>
        /// <param name="zomeName">This is the name of the rust zome in your hApp that this instance of the HoloNETEntryBaseClass maps onto. This will also update the ZomeName property.</param>
        /// <param name="zomeLoadCollectionFunction">This is the name of the rust zome function in your hApp that will be used to load a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the LoadCollection method. This also updates the ZomeLoadCollectionFunction property.</param>
        /// <param name="zomeAddEntryToCollectionFunction">This is the name of the rust zome function in your hApp that will be used to add Holochain Entries to a collection that this instance of the HoloNETCollection maps onto. This will be used by the AddHoloNETEntryToCollectionAndSave method. This also updates the ZomeAddEntryToCollectionFunction property.</param>
        /// <param name="zomeRemoveEntryFromCollectionFunction">This is the name of the rust zome function in your hApp that will be used to remove a Holochain Entry from a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the RemoveHoloNETEntryFromCollectionAndSave method. This also updates the ZomeRemoveEntryFromCollectionFunction property.</param>
        /// <param name="zomeBatchUpdateCollectionFunction">This is the name of the rust zome function in your hApp that will be used to bulk update Holochain Entries in a collection that this instance of the HoloNETCollection maps onto. This will be used by the SaveCollection method. This also updates the ZomeBatchUpdateCollectionFunction property. This param is optional.</param>        
        /// <param name="autoCallInitialize">Set this to true if you wish HoloNETEntryBaseClass to auto-call the Initialize method when a new instance is created. Set this to false if you do not wish it to do this, you may want to do this manually if you want to initialize (will call the Connect method on the HoloNET Client) at a later stage.</param>
        /// <param name="holoNETDNA">This is the HoloNETDNA object that controls how HoloNET operates. This will be passed into the internally created instance of the HoloNET Client.</param>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the HoloNETDNA once it has retrieved the DnaHash & AgentPubKey.</param>
        public HoloNETCollection(string installedAppId, string myAgentPubKey, string zomeName, string zomeLoadCollectionFunction, string zomeAddEntryToCollectionFunction, string zomeRemoveEntryFromCollectionFunction, string zomeBatchUpdateCollectionFunction = "", bool autoCallInitialize = true, IHoloNETDNA holoNETDNA = null, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            HoloNETClient = new HoloNETClientAppAgent(installedAppId, myAgentPubKey, holoNETDNA);
            _disposeOfHoloNETClient = true;

            ZomeName = zomeName;
            ZomeLoadCollectionFunction = zomeLoadCollectionFunction;
            ZomeAddEntryToCollectionFunction = zomeAddEntryToCollectionFunction;
            ZomeRemoveEntryFromCollectionFunction = zomeRemoveEntryFromCollectionFunction;
            ZomeBatchUpdateCollectionFunction = zomeBatchUpdateCollectionFunction;

            if (holoNETDNA != null)
                HoloNETClient.HoloNETDNA = holoNETDNA;

            if (autoCallInitialize)
                InitializeAsync(connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This is a new class introduced in HoloNET 3 that makes it super easy to manage collections of HoloNETEntries so you do not need to interact with the client directly.
        /// It has two main types of constructors, one that allows you to pass in a HoloNETClient instance (which can be shared with other classes that extend the HoloNETEntryBase class or HoloNETEntryBase class as well as other HoloNETCollection's) or if you do not pass a HoloNETClient instance in using the other constructor it will create its own internal instance to use just for this class. 
        /// NOTE: This is a preview of some of the advanced functionality that will be present in the upcoming .NET HDK Low Code Generator, which generates dynamic rust and c# code from your metadata freeing you to focus on your amazing business idea and creativity rather than worrying about learning Holochain, Rust and then getting it to all work in Windows and with C#. HAppy Days! ;-)
        /// </summary>
        /// <param name="zomeName">This is the name of the rust zome in your hApp that this instance of the HoloNETEntryBaseClass maps onto. This will also update the ZomeName property.</param>
        /// <param name="zomeLoadCollectionFunction">This is the name of the rust zome function in your hApp that will be used to load a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the LoadCollection method. This also updates the ZomeLoadCollectionFunction property.</param>
        /// <param name="zomeAddEntryToCollectionFunction">This is the name of the rust zome function in your hApp that will be used to add Holochain Entries to a collection that this instance of the HoloNETCollection maps onto. This will be used by the AddHoloNETEntryToCollectionAndSave method. This also updates the ZomeAddEntryToCollectionFunction property.</param>
        /// <param name="zomeRemoveEntryFromCollectionFunction">This is the name of the rust zome function in your hApp that will be used to remove a Holochain Entry from a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the RemoveHoloNETEntryFromCollectionAndSave method. This also updates the ZomeRemoveEntryFromCollectionFunction property.</param>
        /// <param name="zomeBatchUpdateCollectionFunction">This is the name of the rust zome function in your hApp that will be used to bulk update Holochain Entries in a collection that this instance of the HoloNETCollection maps onto. This will be used by the SaveCollection method. This also updates the ZomeBatchUpdateCollectionFunction property. This param is optional.</param>        
        /// <param name="autoCallInitialize">Set this to true if you wish HoloNETEntryBaseClass to auto-call the Initialize method when a new instance is created. Set this to false if you do not wish it to do this, you may want to do this manually if you want to initialize (will call the Connect method on the HoloNET Client) at a later stage.</param>
        /// <param name="holoNETDNA">This is the HoloNETDNA object that controls how HoloNET operates. This will be passed into the internally created instance of the HoloNET Client.</param>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the HoloNETDNA once it has retrieved the DnaHash & AgentPubKey.</param>
        public HoloNETCollection(string zomeName, string zomeLoadCollectionFunction, string zomeAddEntryToCollectionFunction, string zomeRemoveEntryFromCollectionFunction, string zomeBatchUpdateCollectionFunction = "", bool autoCallInitialize = true, IHoloNETDNA holoNETDNA = null, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            HoloNETClient = new HoloNETClientApp(holoNETDNA);
            _disposeOfHoloNETClient = true;

            ZomeName = zomeName;
            ZomeLoadCollectionFunction = zomeLoadCollectionFunction;
            ZomeAddEntryToCollectionFunction = zomeAddEntryToCollectionFunction;
            ZomeRemoveEntryFromCollectionFunction = zomeRemoveEntryFromCollectionFunction;
            ZomeBatchUpdateCollectionFunction = zomeBatchUpdateCollectionFunction;

            if (holoNETDNA != null)
                HoloNETClient.HoloNETDNA = holoNETDNA;

            if (autoCallInitialize)
                InitializeAsync(connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This is a new class introduced in HoloNET 3 that makes it super easy to manage collections of HoloNETEntries so you do not need to interact with the client directly.
        /// It has two main types of constructors, one that allows you to pass in a HoloNETClient instance (which can be shared with other classes that extend the HoloNETEntryBase class or HoloNETEntryBase class as well as other HoloNETCollection's) or if you do not pass a HoloNETClient instance in using the other constructor it will create its own internal instance to use just for this class. 
        /// NOTE: This is a preview of some of the advanced functionality that will be present in the upcoming .NET HDK Low Code Generator, which generates dynamic rust and c# code from your metadata freeing you to focus on your amazing business idea and creativity rather than worrying about learning Holochain, Rust and then getting it to all work in Windows and with C#. HAppy Days! ;-)
        /// </summary>
        /// <param name="installedAppId">The AppId of the installed hApp this HoloNETCollection will be bound to (it will be bound to the internal HoloNETClientAppAgent). This will overrite the InstalledAppId stored in the HoloNETDNA (if there is one).</param>
        /// <param name="myAgentPubKey">The agentPubKey of the agent that this HoloNETCollection will be bound to (it will be bound to the internal HoloNETClientAppAgent). This will overrite the AgentPubKey defined in the HoloNETDNA (if there is one).</param>
        /// <param name="zomeName">This is the name of the rust zome in your hApp that this instance of the HoloNETEntryBaseClass maps onto. This will also update the ZomeName property.</param>
        /// <param name="zomeLoadCollectionFunction">This is the name of the rust zome function in your hApp that will be used to load a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the LoadCollection method. This also updates the ZomeLoadCollectionFunction property.</param>
        /// <param name="zomeAddEntryToCollectionFunction">This is the name of the rust zome function in your hApp that will be used to add Holochain Entries to a collection that this instance of the HoloNETCollection maps onto. This will be used by the AddHoloNETEntryToCollectionAndSave method. This also updates the ZomeAddEntryToCollectionFunction property.</param>
        /// <param name="zomeRemoveEntryFromCollectionFunction">This is the name of the rust zome function in your hApp that will be used to remove a Holochain Entry from a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the RemoveHoloNETEntryFromCollectionAndSave method. This also updates the ZomeRemoveEntryFromCollectionFunction property.</param>
        /// <param name="logProvider">An implementation of the ILogProvider interface. [DefaultLogger](#DefaultLogger) is an example of this and is used by the constructor (top one) that does not have logProvider as a param. You can injet in (DI) your own implementations of the ILogProvider interface using this param.</param>
        /// <param name="alsoUseDefaultLogger">Set this to true if you wish HoloNET to also log to the DefaultLogger as well as any custom logger injected in. </param>
        /// <param name="zomeBatchUpdateCollectionFunction">This is the name of the rust zome function in your hApp that will be used to bulk update Holochain Entries in a collection that this instance of the HoloNETCollection maps onto. This will be used by the SaveCollection method. This also updates the ZomeBatchUpdateCollectionFunction property. This param is optional.</param>        
        /// <param name="autoCallInitialize">Set this to true if you wish [HoloNETEntryBaseClass](#HoloNETEntryBaseClass) to auto-call the [Initialize](#Initialize) method when a new instance is created. Set this to false if you do not wish it to do this, you may want to do this manually if you want to initialize (will call the [Connect](#connect) method on the HoloNET Client) at a later stage.</param>
        /// <param name="holoNETDNA">This is the HoloNETDNA object that controls how HoloNET operates. This will be passed into the internally created instance of the HoloNET Client.</param>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the HoloNETDNA once it has retrieved the DnaHash & AgentPubKey.</param>
        public HoloNETCollection(string installedAppId, string myAgentPubKey, string zomeName, string zomeLoadCollectionFunction, string zomeAddEntryToCollectionFunction, string zomeRemoveEntryFromCollectionFunction, ILogProvider logProvider, bool alsoUseDefaultLogger = false, string zomeBatchUpdateCollectionFunction = "", bool autoCallInitialize = true, IHoloNETDNA holoNETDNA = null, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true, bool logToConsole = true, bool logToFile = true, string releativePathToLogFolder = "Logs", string logFileName = "HoloNET.log", bool addAdditionalSpaceAfterEachLogEntry = false, bool showColouredLogs = true, ConsoleColor debugColour = ConsoleColor.White, ConsoleColor infoColour = ConsoleColor.Green, ConsoleColor warningColour = ConsoleColor.Yellow, ConsoleColor errorColour = ConsoleColor.Red)
        {
            HoloNETClient = new HoloNETClientAppAgent(installedAppId, myAgentPubKey, logProvider, alsoUseDefaultLogger, holoNETDNA);
            _disposeOfHoloNETClient = true;

            ZomeName = zomeName;
            ZomeLoadCollectionFunction = zomeLoadCollectionFunction;
            ZomeAddEntryToCollectionFunction = zomeAddEntryToCollectionFunction;
            ZomeRemoveEntryFromCollectionFunction = zomeRemoveEntryFromCollectionFunction;
            ZomeBatchUpdateCollectionFunction = zomeBatchUpdateCollectionFunction;

            if (holoNETDNA != null)
                HoloNETClient.HoloNETDNA = holoNETDNA;

            if (autoCallInitialize)
                InitializeAsync(connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This is a new class introduced in HoloNET 3 that makes it super easy to manage collections of HoloNETEntries so you do not need to interact with the client directly.
        /// It has two main types of constructors, one that allows you to pass in a HoloNETClient instance (which can be shared with other classes that extend the HoloNETEntryBase class or HoloNETEntryBase class as well as other HoloNETCollection's) or if you do not pass a HoloNETClient instance in using the other constructor it will create its own internal instance to use just for this class. 
        /// NOTE: This is a preview of some of the advanced functionality that will be present in the upcoming .NET HDK Low Code Generator, which generates dynamic rust and c# code from your metadata freeing you to focus on your amazing business idea and creativity rather than worrying about learning Holochain, Rust and then getting it to all work in Windows and with C#. HAppy Days! ;-)
        /// </summary>
        /// <param name="zomeName">This is the name of the rust zome in your hApp that this instance of the HoloNETEntryBaseClass maps onto. This will also update the ZomeName property.</param>
        /// <param name="zomeLoadCollectionFunction">This is the name of the rust zome function in your hApp that will be used to load a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the LoadCollection method. This also updates the ZomeLoadCollectionFunction property.</param>
        /// <param name="zomeAddEntryToCollectionFunction">This is the name of the rust zome function in your hApp that will be used to add Holochain Entries to a collection that this instance of the HoloNETCollection maps onto. This will be used by the AddHoloNETEntryToCollectionAndSave method. This also updates the ZomeAddEntryToCollectionFunction property.</param>
        /// <param name="zomeRemoveEntryFromCollectionFunction">This is the name of the rust zome function in your hApp that will be used to remove a Holochain Entry from a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the RemoveHoloNETEntryFromCollectionAndSave method. This also updates the ZomeRemoveEntryFromCollectionFunction property.</param>
        /// <param name="logProvider">An implementation of the ILogProvider interface. [DefaultLogger](#DefaultLogger) is an example of this and is used by the constructor (top one) that does not have logProvider as a param. You can injet in (DI) your own implementations of the ILogProvider interface using this param.</param>
        /// <param name="alsoUseDefaultLogger">Set this to true if you wish HoloNET to also log to the DefaultLogger as well as any custom logger injected in. </param>
        /// <param name="zomeBatchUpdateCollectionFunction">This is the name of the rust zome function in your hApp that will be used to bulk update Holochain Entries in a collection that this instance of the HoloNETCollection maps onto. This will be used by the SaveCollection method. This also updates the ZomeBatchUpdateCollectionFunction property. This param is optional.</param>        
        /// <param name="autoCallInitialize">Set this to true if you wish [HoloNETEntryBaseClass](#HoloNETEntryBaseClass) to auto-call the [Initialize](#Initialize) method when a new instance is created. Set this to false if you do not wish it to do this, you may want to do this manually if you want to initialize (will call the [Connect](#connect) method on the HoloNET Client) at a later stage.</param>
        /// <param name="holoNETDNA">This is the HoloNETDNA object that controls how HoloNET operates. This will be passed into the internally created instance of the HoloNET Client.</param>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the HoloNETDNA once it has retrieved the DnaHash & AgentPubKey.</param>
        public HoloNETCollection(string zomeName, string zomeLoadCollectionFunction, string zomeAddEntryToCollectionFunction, string zomeRemoveEntryFromCollectionFunction, ILogProvider logProvider, bool alsoUseDefaultLogger = false, string zomeBatchUpdateCollectionFunction = "", bool autoCallInitialize = true, IHoloNETDNA holoNETDNA = null, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true, bool logToConsole = true, bool logToFile = true, string releativePathToLogFolder = "Logs", string logFileName = "HoloNET.log", bool addAdditionalSpaceAfterEachLogEntry = false, bool showColouredLogs = true, ConsoleColor debugColour = ConsoleColor.White, ConsoleColor infoColour = ConsoleColor.Green, ConsoleColor warningColour = ConsoleColor.Yellow, ConsoleColor errorColour = ConsoleColor.Red)
        {
            HoloNETClient = new HoloNETClientApp(logProvider, alsoUseDefaultLogger, holoNETDNA);
            _disposeOfHoloNETClient = true;

            ZomeName = zomeName;
            ZomeLoadCollectionFunction = zomeLoadCollectionFunction;
            ZomeAddEntryToCollectionFunction = zomeAddEntryToCollectionFunction;
            ZomeRemoveEntryFromCollectionFunction = zomeRemoveEntryFromCollectionFunction;
            ZomeBatchUpdateCollectionFunction = zomeBatchUpdateCollectionFunction;

            if (holoNETDNA != null)
                HoloNETClient.HoloNETDNA = holoNETDNA;

            if (autoCallInitialize)
                InitializeAsync(connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This is a new class introduced in HoloNET 3 that makes it super easy to manage collections of HoloNETEntries so you do not need to interact with the client directly.
        /// It has two main types of constructors, one that allows you to pass in a HoloNETClient instance (which can be shared with other classes that extend the HoloNETEntryBase class or HoloNETEntryBase class as well as other HoloNETCollection's) or if you do not pass a HoloNETClient instance in using the other constructor it will create its own internal instance to use just for this class. 
        /// NOTE: This is a preview of some of the advanced functionality that will be present in the upcoming .NET HDK Low Code Generator, which generates dynamic rust and c# code from your metadata freeing you to focus on your amazing business idea and creativity rather than worrying about learning Holochain, Rust and then getting it to all work in Windows and with C#. HAppy Days! ;-)
        /// </summary>
        /// <param name="installedAppId">The AppId of the installed hApp this HoloNETCollection will be bound to (it will be bound to the internal HoloNETClientAppAgent). This will overrite the InstalledAppId stored in the HoloNETDNA (if there is one).</param>
        /// <param name="myAgentPubKey">The agentPubKey of the agent that this HoloNETCollection will be bound to (it will be bound to the internal HoloNETClientAppAgent). This will overrite the AgentPubKey defined in the HoloNETDNA (if there is one).</param>
        /// <param name="zomeName">This is the name of the rust zome in your hApp that this instance of the HoloNETEntryBaseClass maps onto. This will also update the ZomeName property.</param>
        /// <param name="zomeLoadCollectionFunction">This is the name of the rust zome function in your hApp that will be used to load a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the LoadCollection method. This also updates the ZomeLoadCollectionFunction property.</param>
        /// <param name="zomeAddEntryToCollectionFunction">This is the name of the rust zome function in your hApp that will be used to add Holochain Entries to a collection that this instance of the HoloNETCollection maps onto. This will be used by the AddHoloNETEntryToCollectionAndSave method. This also updates the ZomeAddEntryToCollectionFunction property.</param>
        /// <param name="zomeRemoveEntryFromCollectionFunction">This is the name of the rust zome function in your hApp that will be used to remove a Holochain Entry from a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the RemoveHoloNETEntryFromCollectionAndSave method. This also updates the ZomeRemoveEntryFromCollectionFunction property.</param>
        /// <param name="logProviders">Allows you to inject in (DI) more than one implementation of the ILogProvider interface. HoloNET will then log to each logProvider injected in. </param>
        /// <param name="zomeBatchUpdateCollectionFunction">This is the name of the rust zome function in your hApp that will be used to bulk update Holochain Entries in a collection that this instance of the HoloNETCollection maps onto. This will be used by the SaveCollection method. This also updates the ZomeBatchUpdateCollectionFunction property. This param is optional.</param>        
        /// <param name="alsoUseDefaultLogger">Set this to true if you wish HoloNET to also log to the DefaultLogger as well as any custom logger injected in. </param>
        /// <param name="autoCallInitialize">Set this to true if you wish [HoloNETEntryBaseClass](#HoloNETEntryBaseClass) to auto-call the [Initialize](#Initialize) method when a new instance is created. Set this to false if you do not wish it to do this, you may want to do this manually if you want to initialize (will call the [Connect](#connect) method on the HoloNET Client) at a later stage.</param>
        /// <param name="holoNETDNA">This is the HoloNETDNA object that controls how HoloNET operates. This will be passed into the internally created instance of the HoloNET Client.</param>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the HoloNETDNA once it has retrieved the DnaHash & AgentPubKey.</param>
        public HoloNETCollection(string installedAppId, string myAgentPubKey, string zomeName, string zomeLoadCollectionFunction, string zomeAddEntryToCollectionFunction, string zomeRemoveEntryFromCollectionFunction, IEnumerable<ILogProvider> logProviders, string zomeBatchUpdateCollectionFunction = "", bool alsoUseDefaultLogger = false, bool autoCallInitialize = true, IHoloNETDNA holoNETDNA = null, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            HoloNETClient = new HoloNETClientAppAgent(installedAppId, myAgentPubKey, logProviders, alsoUseDefaultLogger, holoNETDNA);
            _disposeOfHoloNETClient = true;

            ZomeName = zomeName;
            ZomeLoadCollectionFunction = zomeLoadCollectionFunction;
            ZomeAddEntryToCollectionFunction = zomeAddEntryToCollectionFunction;
            ZomeRemoveEntryFromCollectionFunction = zomeRemoveEntryFromCollectionFunction;
            ZomeBatchUpdateCollectionFunction = zomeBatchUpdateCollectionFunction;

            if (autoCallInitialize)
                InitializeAsync(connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This is a new class introduced in HoloNET 3 that makes it super easy to manage collections of HoloNETEntries so you do not need to interact with the client directly.
        /// It has two main types of constructors, one that allows you to pass in a HoloNETClient instance (which can be shared with other classes that extend the HoloNETEntryBase class or HoloNETEntryBase class as well as other HoloNETCollection's) or if you do not pass a HoloNETClient instance in using the other constructor it will create its own internal instance to use just for this class. 
        /// NOTE: This is a preview of some of the advanced functionality that will be present in the upcoming .NET HDK Low Code Generator, which generates dynamic rust and c# code from your metadata freeing you to focus on your amazing business idea and creativity rather than worrying about learning Holochain, Rust and then getting it to all work in Windows and with C#. HAppy Days! ;-)
        /// </summary>
        /// <param name="zomeName">This is the name of the rust zome in your hApp that this instance of the HoloNETEntryBaseClass maps onto. This will also update the ZomeName property.</param>
        /// <param name="zomeLoadCollectionFunction">This is the name of the rust zome function in your hApp that will be used to load a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the LoadCollection method. This also updates the ZomeLoadCollectionFunction property.</param>
        /// <param name="zomeAddEntryToCollectionFunction">This is the name of the rust zome function in your hApp that will be used to add Holochain Entries to a collection that this instance of the HoloNETCollection maps onto. This will be used by the AddHoloNETEntryToCollectionAndSave method. This also updates the ZomeAddEntryToCollectionFunction property.</param>
        /// <param name="zomeRemoveEntryFromCollectionFunction">This is the name of the rust zome function in your hApp that will be used to remove a Holochain Entry from a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the RemoveHoloNETEntryFromCollectionAndSave method. This also updates the ZomeRemoveEntryFromCollectionFunction property.</param>
        /// <param name="logProviders">Allows you to inject in (DI) more than one implementation of the ILogProvider interface. HoloNET will then log to each logProvider injected in. </param>
        /// <param name="zomeBatchUpdateCollectionFunction">This is the name of the rust zome function in your hApp that will be used to bulk update Holochain Entries in a collection that this instance of the HoloNETCollection maps onto. This will be used by the SaveCollection method. This also updates the ZomeBatchUpdateCollectionFunction property. This param is optional.</param>        
        /// <param name="alsoUseDefaultLogger">Set this to true if you wish HoloNET to also log to the DefaultLogger as well as any custom logger injected in. </param>
        /// <param name="autoCallInitialize">Set this to true if you wish [HoloNETEntryBaseClass](#HoloNETEntryBaseClass) to auto-call the [Initialize](#Initialize) method when a new instance is created. Set this to false if you do not wish it to do this, you may want to do this manually if you want to initialize (will call the [Connect](#connect) method on the HoloNET Client) at a later stage.</param>
        /// <param name="holoNETDNA">This is the HoloNETDNA object that controls how HoloNET operates. This will be passed into the internally created instance of the HoloNET Client.</param>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the HoloNETDNA once it has retrieved the DnaHash & AgentPubKey.</param>
        public HoloNETCollection(string zomeName, string zomeLoadCollectionFunction, string zomeAddEntryToCollectionFunction, string zomeRemoveEntryFromCollectionFunction, IEnumerable<ILogProvider> logProviders, string zomeBatchUpdateCollectionFunction = "", bool alsoUseDefaultLogger = false, bool autoCallInitialize = true, IHoloNETDNA holoNETDNA = null, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            HoloNETClient = new HoloNETClientApp(logProviders, alsoUseDefaultLogger, holoNETDNA);
            _disposeOfHoloNETClient = true;

            ZomeName = zomeName;
            ZomeLoadCollectionFunction = zomeLoadCollectionFunction;
            ZomeAddEntryToCollectionFunction = zomeAddEntryToCollectionFunction;
            ZomeRemoveEntryFromCollectionFunction = zomeRemoveEntryFromCollectionFunction;
            ZomeBatchUpdateCollectionFunction = zomeBatchUpdateCollectionFunction;

            if (autoCallInitialize)
                InitializeAsync(connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This is a new class introduced in HoloNET 3 that makes it super easy to manage collections of HoloNETEntries so you do not need to interact with the client directly.
        /// It has two main types of constructors, one that allows you to pass in a HoloNETClient instance (which can be shared with other classes that extend the HoloNETEntryBase class or HoloNETEntryBase class as well as other HoloNETCollection's) or if you do not pass a HoloNETClient instance in using the other constructor it will create its own internal instance to use just for this class. 
        /// NOTE: This is a preview of some of the advanced functionality that will be present in the upcoming .NET HDK Low Code Generator, which generates dynamic rust and c# code from your metadata freeing you to focus on your amazing business idea and creativity rather than worrying about learning Holochain, Rust and then getting it to all work in Windows and with C#. HAppy Days! ;-)
        /// </summary>
        /// <param name="installedAppId">The AppId of the installed hApp this HoloNETCollection will be bound to (it will be bound to the internal HoloNETClientAppAgent). This will overrite the InstalledAppId stored in the HoloNETDNA (if there is one).</param>
        /// <param name="myAgentPubKey">The agentPubKey of the agent that this HoloNETCollection will be bound to (it will be bound to the internal HoloNETClientAppAgent). This will overrite the AgentPubKey defined in the HoloNETDNA (if there is one).</param>
        /// <param name="zomeName">This is the name of the rust zome in your hApp that this instance of the HoloNETEntryBaseClass maps onto. This will also update the ZomeName property.</param>
        /// <param name="zomeLoadCollectionFunction">This is the name of the rust zome function in your hApp that will be used to load a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the LoadCollection method. This also updates the ZomeLoadCollectionFunction property.</param>
        /// <param name="zomeAddEntryToCollectionFunction">This is the name of the rust zome function in your hApp that will be used to add Holochain Entries to a collection that this instance of the HoloNETCollection maps onto. This will be used by the AddHoloNETEntryToCollectionAndSave method. This also updates the ZomeAddEntryToCollectionFunction property.</param>
        /// <param name="zomeRemoveEntryFromCollectionFunction">This is the name of the rust zome function in your hApp that will be used to remove a Holochain Entry from a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the RemoveHoloNETEntryFromCollectionAndSave method. This also updates the ZomeRemoveEntryFromCollectionFunction property.</param>
        /// <param name="logger">Allows you to inject in (DI) more than one implementation of the ILogger interface. HoloNET will then log to each logger injected in.</param>
        /// <param name="zomeBatchUpdateCollectionFunction">This is the name of the rust zome function in your hApp that will be used to bulk update Holochain Entries in a collection that this instance of the HoloNETCollection maps onto. This will be used by the SaveCollection method. This also updates the ZomeBatchUpdateCollectionFunction property. This param is optional.</param>        
        /// <param name="autoCallInitialize">Set this to true if you wish [HoloNETEntryBaseClass](#HoloNETEntryBaseClass) to auto-call the [Initialize](#Initialize) method when a new instance is created. Set this to false if you do not wish it to do this, you may want to do this manually if you want to initialize (will call the [Connect](#connect) method on the HoloNET Client) at a later stage.</param>
        /// <param name="holoNETDNA">This is the HoloNETDNA object that controls how HoloNET operates. This will be passed into the internally created instance of the HoloNET Client.</param>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the HoloNETDNA once it has retrieved the DnaHash & AgentPubKey.</param>
        public HoloNETCollection(string installedAppId, string myAgentPubKey, string zomeName, string zomeLoadCollectionFunction, string zomeAddEntryToCollectionFunction, string zomeRemoveEntryFromCollectionFunction, ILogger logger, string zomeBatchUpdateCollectionFunction = "", bool autoCallInitialize = true, IHoloNETDNA holoNETDNA = null, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            HoloNETClient = new HoloNETClientAppAgent(installedAppId, myAgentPubKey, logger, holoNETDNA);
            _disposeOfHoloNETClient = true;

            ZomeName = zomeName;
            ZomeLoadCollectionFunction = zomeLoadCollectionFunction;
            ZomeAddEntryToCollectionFunction = zomeAddEntryToCollectionFunction;
            ZomeRemoveEntryFromCollectionFunction = zomeRemoveEntryFromCollectionFunction;
            ZomeBatchUpdateCollectionFunction = zomeBatchUpdateCollectionFunction;

            if (autoCallInitialize)
                InitializeAsync(connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This is a new class introduced in HoloNET 3 that makes it super easy to manage collections of HoloNETEntries so you do not need to interact with the client directly.
        /// It has two main types of constructors, one that allows you to pass in a HoloNETClient instance (which can be shared with other classes that extend the HoloNETEntryBase class or HoloNETEntryBase class as well as other HoloNETCollection's) or if you do not pass a HoloNETClient instance in using the other constructor it will create its own internal instance to use just for this class. 
        /// NOTE: This is a preview of some of the advanced functionality that will be present in the upcoming .NET HDK Low Code Generator, which generates dynamic rust and c# code from your metadata freeing you to focus on your amazing business idea and creativity rather than worrying about learning Holochain, Rust and then getting it to all work in Windows and with C#. HAppy Days! ;-)
        /// </summary>
        /// <param name="zomeName">This is the name of the rust zome in your hApp that this instance of the HoloNETEntryBaseClass maps onto. This will also update the ZomeName property.</param>
        /// <param name="zomeLoadCollectionFunction">This is the name of the rust zome function in your hApp that will be used to load a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the LoadCollection method. This also updates the ZomeLoadCollectionFunction property.</param>
        /// <param name="zomeAddEntryToCollectionFunction">This is the name of the rust zome function in your hApp that will be used to add Holochain Entries to a collection that this instance of the HoloNETCollection maps onto. This will be used by the AddHoloNETEntryToCollectionAndSave method. This also updates the ZomeAddEntryToCollectionFunction property.</param>
        /// <param name="zomeRemoveEntryFromCollectionFunction">This is the name of the rust zome function in your hApp that will be used to remove a Holochain Entry from a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the RemoveHoloNETEntryFromCollectionAndSave method. This also updates the ZomeRemoveEntryFromCollectionFunction property.</param>
        /// <param name="logger">Allows you to inject in (DI) more than one implementation of the ILogger interface. HoloNET will then log to each logger injected in.</param>
        /// <param name="zomeBatchUpdateCollectionFunction">This is the name of the rust zome function in your hApp that will be used to bulk update Holochain Entries in a collection that this instance of the HoloNETCollection maps onto. This will be used by the SaveCollection method. This also updates the ZomeBatchUpdateCollectionFunction property. This param is optional.</param>        
        /// <param name="autoCallInitialize">Set this to true if you wish [HoloNETEntryBaseClass](#HoloNETEntryBaseClass) to auto-call the [Initialize](#Initialize) method when a new instance is created. Set this to false if you do not wish it to do this, you may want to do this manually if you want to initialize (will call the [Connect](#connect) method on the HoloNET Client) at a later stage.</param>
        /// <param name="holoNETDNA">This is the HoloNETDNA object that controls how HoloNET operates. This will be passed into the internally created instance of the HoloNET Client.</param>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the HoloNETDNA once it has retrieved the DnaHash & AgentPubKey.</param>
        public HoloNETCollection(string zomeName, string zomeLoadCollectionFunction, string zomeAddEntryToCollectionFunction, string zomeRemoveEntryFromCollectionFunction, ILogger logger, string zomeBatchUpdateCollectionFunction = "", bool autoCallInitialize = true, IHoloNETDNA holoNETDNA = null, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            HoloNETClient = new HoloNETClientApp(logger, holoNETDNA);
            _disposeOfHoloNETClient = true;

            ZomeName = zomeName;
            ZomeLoadCollectionFunction = zomeLoadCollectionFunction;
            ZomeAddEntryToCollectionFunction = zomeAddEntryToCollectionFunction;
            ZomeRemoveEntryFromCollectionFunction = zomeRemoveEntryFromCollectionFunction;
            ZomeBatchUpdateCollectionFunction = zomeBatchUpdateCollectionFunction;

            if (autoCallInitialize)
                InitializeAsync(connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This is a new class introduced in HoloNET 3 that makes it super easy to manage collections of HoloNETEntries so you do not need to interact with the client directly.
        /// It has two main types of constructors, one that allows you to pass in a HoloNETClient instance (which can be shared with other classes that extend the HoloNETEntryBase class or HoloNETEntryBase class as well as other HoloNETCollection's) or if you do not pass a HoloNETClient instance in using the other constructor it will create its own internal instance to use just for this class. 
        /// NOTE: This is a preview of some of the advanced functionality that will be present in the upcoming .NET HDK Low Code Generator, which generates dynamic rust and c# code from your metadata freeing you to focus on your amazing business idea and creativity rather than worrying about learning Holochain, Rust and then getting it to all work in Windows and with C#. HAppy Days! ;-)
        /// </summary>
        /// <param name="zomeName">This is the name of the rust zome in your hApp that this instance of the HoloNETEntryBaseClass maps onto. This will also update the ZomeName property.</param>
        /// <param name="zomeLoadCollectionFunction">This is the name of the rust zome function in your hApp that will be used to load a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the LoadCollection method. This also updates the ZomeLoadCollectionFunction property.</param>
        /// <param name="zomeAddEntryToCollectionFunction">This is the name of the rust zome function in your hApp that will be used to add Holochain Entries to a collection that this instance of the HoloNETCollection maps onto. This will be used by the AddHoloNETEntryToCollectionAndSave method. This also updates the ZomeAddEntryToCollectionFunction property.</param>
        /// <param name="zomeRemoveEntryFromCollectionFunction">This is the name of the rust zome function in your hApp that will be used to remove a Holochain Entry from a Holochain collection that this instance of the HoloNETCollection maps onto. This will be used by the RemoveHoloNETEntryFromCollectionAndSave method. This also updates the ZomeRemoveEntryFromCollectionFunction property.</param>
        /// <param name="holoNETClient">This is the HoloNETDNA object that controls how HoloNET operates. This will be passed into the internally created instance of the HoloNET Client.</param>
        /// <param name="zomeBatchUpdateCollectionFunction">This is the name of the rust zome function in your hApp that will be used to bulk update Holochain Entries in a collection that this instance of the HoloNETCollection maps onto. This will be used by the SaveCollection method. This also updates the ZomeBatchUpdateCollectionFunction property. This param is optional.</param>        
        /// <param name="autoCallInitialize">Set this to true if you wish [HoloNETEntryBaseClass](#HoloNETEntryBaseClass) to auto-call the [Initialize](#Initialize) method when a new instance is created. Set this to false if you do not wish it to do this, you may want to do this manually if you want to initialize (will call the [Connect](#connect) method on the HoloNET Client) at a later stage.</param>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the HoloNETDNA once it has retrieved the DnaHash & AgentPubKey.</param>
        public HoloNETCollection(string zomeName, string zomeLoadCollectionFunction, string zomeAddEntryToCollectionFunction, string zomeRemoveEntryFromCollectionFunction, HoloNETClientAppAgent holoNETClient, string zomeBatchUpdateCollectionFunction = "", bool autoCallInitialize = true, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            HoloNETClient = holoNETClient;
            //StoreEntryHashInEntry = storeEntryHashInEntry;
            ZomeName = zomeName;
            ZomeLoadCollectionFunction = zomeLoadCollectionFunction;
            ZomeAddEntryToCollectionFunction = zomeAddEntryToCollectionFunction;
            ZomeRemoveEntryFromCollectionFunction = zomeRemoveEntryFromCollectionFunction;
            ZomeBatchUpdateCollectionFunction = zomeBatchUpdateCollectionFunction;

            if (autoCallInitialize)
                InitializeAsync(connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This is a reference to the internal instance of the HoloNET Client (either the one passed in through a constructor or one created internally.)
        /// </summary>
        public IHoloNETClientAppBase HoloNETClient { get; set; }

        /// <summary>
        /// This will return true whilst HoloNETEntryBaseClass and it's internal HoloNET client is initializing. The Initialize method will begin the initialization process. This will also call the Connect and RetrieveAgentPubKeyAndDnaHash methods on the HoloNET client. Once the HoloNET client has successfully connected to the Holochain Conductor, retrieved the AgentPubKey & DnaHash & then raised the OnReadyForZomeCalls event it will raise the OnInitialized event. See also the IsInitialized property.
        /// </summary>
        public bool IsInitializing { get; private set; }

        /// <summary>
        /// This will return true once HoloNETEntryBaseClass and it's internal HoloNET client have finished initializing and the OnInitialized event has been raised. See also the IsInitializing property and the Initialize method.
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                return HoloNETClient != null ? HoloNETClient.IsReadyForZomesCalls : false;
            }
        }

        /// <summary>
        /// The name of the zome to call the respective ZomeLoadEntryFunction, ZomeCreateEntryFunction, ZomeUpdateEntryFunction & ZomeDeleteEntryFunction.
        /// </summary>
        public string ZomeName { get; set; }

        /// <summary>
        /// The name of the zome function to call to load a collection of entries.
        /// </summary>
        public string ZomeLoadCollectionFunction { get; set; }

        /// <summary>
        /// The name of the zome function to call to add a HoloNETEntry to a collection.
        /// </summary>
        public string ZomeAddEntryToCollectionFunction { get; set; }

        /// <summary>
        /// The name of the zome function to call to remove a HoloNETEntry from a collection.
        /// </summary>
        public string ZomeRemoveEntryFromCollectionFunction { get; set; }

        /// <summary>
        /// The name of the zome function to call to batch update all changes made to the collection (entries added, removed and updated (optional)),
        /// </summary>
        public string ZomeBatchUpdateCollectionFunction { get; set; }

        /// <summary>
        /// This will be true if any chnages have been made to this collection (entries added, removed or updated) since the last time the Save/SaveAsync functions were called. After they have been called it will be reset to false.
        /// </summary>
        public bool IsChanges { get; private set; }

        /// <summary>
        /// This contains the original entries since the last time this collection was loaded or saved.
        /// </summary>
        public List<T> OriginalEntries { get; private set; }

        /// <summary>
        /// The entries that have been added since the last time the Save/SaveAsync method has been called. When Save/SaveAsync is next called this list will be emptied once the save is successfull.
        /// </summary>
        public List<T> EntriesAddedSinceLastSaved { get; private set; } = new List<T>();

        /// <summary>
        /// The entries that have been removed since the last time the Save/SaveAsync method has been called. When Save/SaveAsync is next called this list will be emptied once the save is successfull.
        /// </summary>
        public List<T> EntriesRemovedSinceLastSaved { get; private set; } = new List<T>();

        /// <summary>
        /// This method will Initialize the HoloNETCollection along with the internal HoloNET Client and will raise the OnInitialized event once it has finished initializing. This will also call the Connect and RetrieveAgentPubKeyAndDnaHash methods on the HoloNET client. Once the HoloNET client has successfully connected to the Holochain Conductor, retrieved the AgentPubKey & DnaHash & then raised the OnReadyForZomeCalls event it will raise the OnInitialized event. See also the IsInitializing and the IsInitialized properties.
        /// </summary>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the HoloNETDNA once it has retrieved the DnaHash & AgentPubKey.</param>
        public virtual void Initialize(bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            InitializeAsync(ConnectedCallBackMode.UseCallBackEvents, RetrieveAgentPubKeyAndDnaHashMode.UseCallBackEvents, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This method will Initialize the HoloNETCollection along with the internal HoloNET Client and will raise the OnInitialized event once it has finished initializing. This will also call the Connect and RetrieveAgentPubKeyAndDnaHash methods on the HoloNET client. Once the HoloNET client has successfully connected to the Holochain Conductor, retrieved the AgentPubKey & DnaHash & then raised the OnReadyForZomeCalls event it will raise the OnInitialized event. See also the IsInitializing and the IsInitialized properties.
        /// </summary>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the HoloNETDNA once it has retrieved the DnaHash & AgentPubKey.</param>
        /// <returns></returns>
        public virtual async Task InitializeAsync(ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            try
            {
                if (IsInitialized)
                    return;
                //throw new InvalidOperationException("The HoloNET Client has already been initialized.");

                if (IsInitializing)
                    return;

                IsInitializing = true;

                HoloNETClient.OnError += HoloNETClient_OnError;
                HoloNETClient.OnReadyForZomeCalls += HoloNETClient_OnReadyForZomeCalls;
                // this.CollectionChanged += HoloNETObservableCollection_CollectionChanged;

                if (HoloNETClient.WebSocket.State != System.Net.WebSockets.WebSocketState.Connecting || HoloNETClient.WebSocket.State != System.Net.WebSockets.WebSocketState.Open)
                    await HoloNETClient.ConnectAsync(HoloNETClient.EndPoint, connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
            }
            catch (Exception ex)
            {
                HandleError("Unknown error occurred in InitializeAsync method in HoloNETCollection", ex);
            }
        }

        public void ReCalculateEntriesAddedOrRemovedSinceLastSaved()
        {
            bool found = false;
            EntriesAddedSinceLastSaved.Clear();
            EntriesRemovedSinceLastSaved.Clear();
            IsChanges = false;

            //Check for entries Added.
            foreach (T entry in this)
            {
                foreach (T originalEntry in OriginalEntries)
                {
                    if (originalEntry.EntryHash == entry.EntryHash)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    if (entry.State == Enums.HoloNETEntryState.Updated)
                        entry.State = Enums.HoloNETEntryState.UpdatedAndAddedToCollection;
                    else
                        entry.State = Enums.HoloNETEntryState.AddedToCollection;

                    EntriesAddedSinceLastSaved.Add(entry);
                    IsChanges = true;
                }
            }

            //Check for entries Removed.
            found = false;
            foreach (T originalEntry in OriginalEntries)
            {
                foreach (T entry in this)
                {
                    if (originalEntry.EntryHash == entry.EntryHash)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    if (originalEntry.State == Enums.HoloNETEntryState.Updated)
                        originalEntry.State = Enums.HoloNETEntryState.UpdatedAndRemovedFromCollections;
                    else
                        originalEntry.State = Enums.HoloNETEntryState.RemovedFromCollection;

                    EntriesRemovedSinceLastSaved.Add(originalEntry);
                    IsChanges = true;
                }
            }
        }

        /// <summary>
        /// This mehod will call the WaitTillReadyForZomeCallsAsync method on the HoloNET Client. 
        /// </summary>
        /// <returns></returns>
        public virtual async Task<ReadyForZomeCallsEventArgs> WaitTillHoloNETInitializedAsync()
        {
            if (!IsInitialized && !IsInitializing)
                await InitializeAsync();

            return await HoloNETClient.WaitTillReadyForZomeCallsAsync();
        }


        /// <summary>
        /// This method will load the entries for this collection from the Holochain Conductor. This calls the CallZomeFunction on the HoloNET client passing in the zome function name specified in the constructor param `zomeLoadCollectionFunction` or property `ZomeLoadCollectionFunction` and then maps the data returned from the zome call onto your collection of data objects. It will then raise the OnCollectionLoaded event.
        /// </summary>
        /// <param name="collectionAnchor">The anchor of the Holochain Collection you wish to load from. This is optional.</param>
        /// <param name="customDataKeyValuePairs">This is a optional dictionary containing keyvalue pairs of custom data you wish to inject into the params that are sent to the ZomeLoadCollectionFunction.</param>
        /// <returns></returns>
        //public virtual async Task<HoloNETCollectionLoadedResult<T>> LoadCollectionAsync(string collectionAnchor = "", Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true)
        public virtual async Task<HoloNETCollectionLoadedResult<T>> LoadCollectionAsync(string collectionAnchor = "", Dictionary<string, string> customDataKeyValuePairs = null)
        {
            HoloNETCollectionLoadedResult<T> result = new HoloNETCollectionLoadedResult<T>();
            ZomeFunctionCallBackEventArgs zomeResult = null;

            try
            {
                if (!IsInitialized && !IsInitializing)
                    await InitializeAsync();

                if (customDataKeyValuePairs != null)
                {
                    dynamic paramsObject = new ExpandoObject();

                    foreach (string key in customDataKeyValuePairs.Keys)
                        ExpandoObjectHelpers.AddProperty(paramsObject, key, customDataKeyValuePairs[key]);

                    if (!string.IsNullOrEmpty(collectionAnchor))
                        ExpandoObjectHelpers.AddProperty(paramsObject, "collectionAnchor", collectionAnchor);

                    zomeResult = await HoloNETClient.CallZomeFunctionAsync(ZomeName, ZomeLoadCollectionFunction, paramsObject);
                }
                else
                    zomeResult = await HoloNETClient.CallZomeFunctionAsync(ZomeName, ZomeLoadCollectionFunction, collectionAnchor);

 
                if (zomeResult != null && !zomeResult.IsError)
                {
                    foreach (Record record in zomeResult.Records)
                    {
                        HoloNETEntryBase entry = record.EntryDataObject as HoloNETEntryBase;

                        if (entry != null)
                        {
                            entry.HoloNETClient = HoloNETClient; //The entries will share the same clinet/connection as the collection.
                            entry.OrginalDataKeyValuePairs = record.EntryKeyValuePairs;
                            entry.OrginalEntry = record.EntryDataObject;
                            //entry.OrginalKeyValuePairs = entryData.

                            //TODO: REMOVE AFTER, TEMP TILL GET ZOMECALLS WORKING AGAIN!
                            //entry.MockData();

                            Add(record.EntryDataObject);
                            //Add((T)entry);
                        }

                        //Add(entryData.EntryDataObject);
                    }
                }
                else
                {
                    result.IsError = true;

                    if (zomeResult != null)
                        result.Message = zomeResult.Message;
                }

                result.ZomeFunctionCallBackEventArgs = zomeResult;
                result.EntriesLoaded = this.ToList();
                ResetChangeTracking();

                OnCollectionLoaded?.Invoke(this, result);
                return result;
            }
            catch (Exception ex)
            {
                return HandleError<HoloNETCollectionLoadedResult<T>>("Unknown error occurred in LoadCollectionAsync method in HoloNETCollection", ex);
            }
        }

        /// <summary>
        /// This method will load the entries for this collection from the Holochain Conductor. This calls the CallZomeFunction on the HoloNET client passing in the zome function name specified in the constructor param `zomeLoadCollectionFunction` or property `ZomeLoadCollectionFunction` and then maps the data returned from the zome call onto your collection of data objects. It will then raise the OnCollectionLoaded event.
        /// </summary>
        /// <param name="collectionAnchor">The anchor of the Holochain Collection you wish to load from. This is optional.</param>
        /// <param name="customDataKeyValuePairs">This is a optional dictionary containing keyvalue pairs of custom data you wish to inject into the params that are sent to the ZomeLoadCollectionFunction.</param>
        /// <returns></returns>
        //public virtual HoloNETCollectionLoadedResult<T> LoadCollection(string collectionAnchor = "", Dictionary<string, string> customDataKeyValuePairs = null, bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true)
        public virtual HoloNETCollectionLoadedResult<T> LoadCollection(string collectionAnchor = "", Dictionary<string, string> customDataKeyValuePairs = null)
        {
            //return LoadCollectionAsync(collectionAnchor, customDataKeyValuePairs, useReflectionToMapKeyValuePairResponseOntoEntryDataObject).Result;
            return LoadCollectionAsync(collectionAnchor, customDataKeyValuePairs).Result;
        }

        /// <summary>
        /// This method will add the HoloNETEntry to the collection. If 'saveHoloNETEntry' is set to true (default) it will call the SaveAsync method on the HoloNETEntry before adding it to the collection. This calls the CallZomeFunction on the HoloNET client passing in the zome function name specified in the constructor param 'zomeAddEntryToCollectionFunction' or property 'ZomeAddEntryToCollectionFunction'.  It will then raise the OnHoloNETEntryAddedToCollection event.
        /// </summary>
        /// <param name="holoNETEntry">The HoloNETEntry to add to the collection.</param>
        /// <param name="saveHoloNETEntry">If this is set to true (default) it will call the SaveAsync method on the HoloNETEntry before adding it to the collection.</param>
        /// <param name="collectionAnchor">The anchor of the Holochain Collection you wish to add the entry to. This is optional.</param>
        /// <returns></returns>
        public virtual async Task<ZomeFunctionCallBackEventArgs> AddHoloNETEntryToCollectionAndSaveAsync(T holoNETEntry, bool saveHoloNETEntry = true, string collectionAnchor = "", bool useReflectionToMapKeyValuePairResponseOntoEntryDataObject = true)
        {
            ZomeFunctionCallBackEventArgs result = new ZomeFunctionCallBackEventArgs();

            try
            {
                if (!IsInitialized && !IsInitializing)
                    await InitializeAsync();

                if (saveHoloNETEntry)
                {
                    if (holoNETEntry.HoloNETClient == null)
                        holoNETEntry.HoloNETClient = HoloNETClient;

                    ZomeFunctionCallBackEventArgs saveResult = await holoNETEntry.SaveAsync();

                    if (saveResult.IsError)
                    {
                        result.IsError = true;
                        result.Message = $"Error occured in AddHoloNETEntryToCollectionAndSaveAsync in HoloNETCollection saving the HoloNETEntry. Reason: {saveResult.Message}";
                    }
                }

                if (!result.IsError)
                {
                    result = await HoloNETClient.CallZomeFunctionAsync(ZomeName, ZomeAddEntryToCollectionFunction, collectionAnchor);

                    if (result != null && !result.IsError)
                    {
                        Add(holoNETEntry);
                        OriginalEntries.Add(holoNETEntry);
                    }
                }

                OnHoloNETEntryAddedToCollection?.Invoke(this, result);
                return result;
            }
            catch (Exception ex)
            {
                return HandleError<ZomeFunctionCallBackEventArgs>("Unknown error occurred in AddHoloNETEntryToCollectionAndSaveAsync method in HoloNETCollection", ex);
            }
        }

        /// <summary>
        /// This method will add the HoloNETEntry to the collection. If 'saveHoloNETEntry' is set to true (default) it will call the SaveAsync method on the HoloNETEntry before adding it to the collection. This calls the CallZomeFunction on the HoloNET client passing in the zome function name specified in the constructor param 'zomeAddEntryToCollectionFunction' or property 'ZomeAddEntryToCollectionFunction'.  It will then raise the OnHoloNETEntryAddedToCollection event.
        /// </summary>
        /// <param name="holoNETEntry">The HoloNETEntry to add to the collection.</param>
        /// <param name="saveHoloNETEntry">If this is set to true (default) it will call the SaveAsync method on the HoloNETEntry before adding it to the collection.</param>
        /// <param name="collectionAnchor">The anchor of the Holochain Collection you wish to add the entry to. This is optional.</param>
        /// <returns></returns>
        public virtual ZomeFunctionCallBackEventArgs AddHoloNETEntryToCollectionAndSave(T holoNETEntry, bool saveHoloNETEntry = true, string collectionAnchor = "")
        {
            return AddHoloNETEntryToCollectionAndSaveAsync(holoNETEntry, saveHoloNETEntry, collectionAnchor).Result;
        }

        /// <summary>
        /// This method will remnove the HoloNETEntry from the collection. This calls the CallZomeFunction on the HoloNET client passing in the zome function name specified in the constructor param 'zomeRemoveEntryFromCollectionFunction' or property 'ZomeRemoveEntryFromCollectionFunction'. It will then raise the OnHoloNETEntryRemovedFromCollection event.
        /// </summary>
        /// <param name="holoNETEntry">The HoloNETEntry to remove from the collection.</param>
        /// <param name="collectionAnchor">The anchor of the Holochain Collection you wish to remove the entry from. This is optional.</param>
        /// <returns></returns>
        public virtual async Task<ZomeFunctionCallBackEventArgs> RemoveHoloNETEntryFromCollectionAndSaveAsync(T holoNETEntry, string collectionAnchor = "")
        {
            try
            {
                if (!IsInitialized && !IsInitializing)
                    await InitializeAsync();

                ZomeFunctionCallBackEventArgs result = await HoloNETClient.CallZomeFunctionAsync(ZomeName, ZomeRemoveEntryFromCollectionFunction, collectionAnchor);

                if (result != null && !result.IsError)
                {
                    Remove(holoNETEntry);
                    OriginalEntries.Remove(holoNETEntry);
                }

                OnHoloNETEntryRemovedFromCollection?.Invoke(this, result);
                return result;
            }
            catch (Exception ex)
            {
                return HandleError<ZomeFunctionCallBackEventArgs>("Unknown error occurred in RemoveHoloNETEntryFromCollectionAndSaveAsync method in HoloNETCollection", ex);
            }
        }

        /// <summary>
        /// This method will remnove the HoloNETEntry from the collection. This calls the CallZomeFunction on the HoloNET client passing in the zome function name specified in the constructor param 'zomeRemoveEntryFromCollectionFunction' or property 'ZomeRemoveEntryFromCollectionFunction'. It will then raise the OnHoloNETEntryRemovedFromCollection event.
        /// </summary>
        /// <param name="holoNETEntry">The HoloNETEntry to remove from the collection.</param>
        /// <param name="collectionAnchor">The anchor of the Holochain Collection you wish to remove the entry from. This is optional.</param>
        /// <returns></returns>
        public virtual ZomeFunctionCallBackEventArgs RemoveHoloNETEntryFromCollectionAndSave(T holoNETEntry, string collectionAnchor = "")
        {
            return RemoveHoloNETEntryFromCollectionAndSaveAsync(holoNETEntry, collectionAnchor).Result;
        }

        /// <summary>
        /// This method will save all changes made to the collection since the last time this method was called and persist the changes to the Holochain Conductor. This includes any entries that were added or removed to the collection via the Add and Remove methods (in-memory only). It also persists any entries that were updated if the 'saveChangesMadeToEntries' param is set to true (defaults to true). If entries were added or removed via the AddHoloNETEntryToCollectionAndSave and RemoveHoloNETEntryFromCollectionAndSave methods then these entries will NOT be added/removed again. If the 'saveAsOneBatchOperation' param is set to true (default), any new entries added, old entries removed or entries changed will be updated as one batch operation. This will be faster than making many seperate calls for adding, remove or updating entries. For this to work you need to make sure the 'ZomeBatchUpdateCollectionFunction' is set or was passed in via one of the constructors, it will then batch update any entries that have changed (added, removed or changed) since the last time this method was called.
        /// If 'saveAsOneBatchOperation' is set to false then it will call the SaveAsync method for updating each entry (if 'saveChangesMadeToEntries' is set to true), 'AddHoloNETEntryToCollectionAndSaveAsync' for adding new entries and 'RemoveHoloNETEntryFromCollectionAndSaveAsync' for removing old entries. This of course will be slower than updating them as a batch depending on the size of the collection. This will invoke the 'OnCollectionSaved' event once it has finished saving any changes to the collection.
        /// </summary>
        /// <param name="saveChangesMadeToEntries">Set this to true if you wish to update any changes made to the entries themselves. This defaults to true.</param>
        /// <param name="saveAsOneBatchOperation">Set this to true to save any new entries added, old entries removed or entries changed as one batch operation. This will be faster than making many seperate calls for adding, remove or updating entries. For this to work you need to make sure the 'ZomeBatchUpdateCollectionFunction' is set, this is where the entries added, removed and updated will be sent as a param. Entries updated is optional, set 'saveChangesMadeToEntries' to false if you do not wish to update them (defaults to true). </param>
        /// <param name="saveHoloNETEntryWhenAddingToCollection">If this is set to true (default) it will call the SaveAsync method on the HoloNETEntry before adding it to the collection.</param>
        /// <param name="continueOnError">Set this to true (default) if you wish HoloNET to continue attemtping to save changes when an error occurs adding, removing or updating entries. NOTE: This is only relevant if 'saveAsOneBatchOperation' is false. </param>
        /// <param name="collectionAnchor">The anchor of the Holochain Collection you wish to update. This is optional.</param>
        /// <param name="customDataKeyValuePairs">This is a optional dictionary containing keyvalue pairs of custom data you wish to inject into the params that are sent to the Save zome function when saving a HoloNETEntry.</param>
        /// <param name="holochainFieldsIsEnabledKeyValuePairs">This is a optional dictionary containing keyvalue pairs to allow properties that contain the HolochainFieldName to be omitted from the properties that this function checks for changes. The key (case senstive) needs to match a property that has the HolochainFieldName attribute.</param>
        /// <param name="cachePropertyInfos">Set this to true (default) if you want HoloNET to cache the property info's for the Entry (this can reduce the slight overhead used by reflection).</param>
        /// <returns>This returns 'HoloNETCollectionSavedResult' containing lists of all entries saved or entries that errored along with any errors that occured.</returns>
        public virtual async Task<HoloNETCollectionSavedResult> SaveAllChangesAsync(bool saveChangesMadeToEntries = true, bool saveAsOneBatchOperation = true, bool saveHoloNETEntryWhenAddingToCollection = true, bool continueOnError = true, string collectionAnchor = "", Dictionary<string, string> customDataKeyValuePairs = null, Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePairs = null, bool cachePropertyInfos = true)
        {
            HoloNETCollectionSavedResult result = new HoloNETCollectionSavedResult();

            try
            {
                if (!IsInitialized && !IsInitializing)
                    await InitializeAsync();

                ReCalculateEntriesAddedOrRemovedSinceLastSaved();

                if (!saveAsOneBatchOperation)
                {
                    foreach (T entry in EntriesAddedSinceLastSaved)
                    {
                        ZomeFunctionCallBackEventArgs saveResult = await AddHoloNETEntryToCollectionAndSaveAsync(entry, saveHoloNETEntryWhenAddingToCollection);

                        if (saveResult != null && !saveResult.IsError)
                            result.EntiesAdded.Add(entry);
                        else
                        {
                            result.EntiesSaveErrors.Add(entry);
                            result.ErrorMessages.Add($"Error occured in SaveAllChangesAsync method in HoloNETCollection adding a new HoloNETEntry to the HoloNET Collection. Reason: {saveResult.Message}");
                            result.IsError = true;

                            if (!continueOnError)
                                break;
                        }
                    }

                    if (!result.IsError)
                    {
                        foreach (T entry in EntriesRemovedSinceLastSaved)
                        {
                            ZomeFunctionCallBackEventArgs saveResult = await RemoveHoloNETEntryFromCollectionAndSaveAsync(entry);

                            if (saveResult != null && !saveResult.IsError)
                                result.EntiesRemoved.Add(entry);
                            else
                            {
                                result.EntiesSaveErrors.Add(entry);
                                result.ErrorMessages.Add($"Error occured in SaveAllChangesAsync method in HoloNETCollection removing a HoloNETEntry from the HoloNET Collection. Reason: {saveResult.Message}");
                                result.IsError = true;

                                if (!continueOnError)
                                    break;
                            }
                        }
                    }

                    if (!result.IsError && saveChangesMadeToEntries)
                    {
                        for (int i = 0; i < Count; i++)
                        {
                            //If the user has not manually set the IsChanged property then we can calculate it now using reflection.
                            if (!this[i].IsChanged)
                                this[i].HasEntryChanged(holochainFieldsIsEnabledKeyValuePairs, cachePropertyInfos);

                            if (this[i].IsChanged)
                            {
                                this[i].State = Enums.HoloNETEntryState.Updated;
                                ZomeFunctionCallBackEventArgs saveResult = await this[i].SaveAsync(customDataKeyValuePairs, holochainFieldsIsEnabledKeyValuePairs, cachePropertyInfos);

                                if (saveResult != null && !saveResult.IsError)
                                    result.EntiesSaved.Add(this[i]);
                                else
                                {
                                    result.EntiesSaveErrors.Add(this[i]);
                                    result.ErrorMessages.Add($"Error occured in SaveAllChangesAsync method in HoloNETCollection saving HoloNETEntry with entryHash {this[i].EntryHash}. Reason: {saveResult.Message}");
                                    result.IsError = true;

                                    if (!continueOnError)
                                        break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    dynamic paramsObject = new ExpandoObject();
                    List<HoloNETEntryBase> savingsEntries = new List<HoloNETEntryBase>();
                    int iEntry = 1;

                    foreach (T entry in EntriesAddedSinceLastSaved)
                    {
                        //Build params object containing each object (could be a long list of params!) What is the rust limit?
                        ExpandoObjectHelpers.AddProperty(paramsObject, iEntry.ToString(), entry.BuildDynamicParamsObject());
                        savingsEntries.Add(entry);
                        iEntry++;
                    }

                    foreach (T entry in EntriesRemovedSinceLastSaved)
                    {
                        //Build params object containing each object (could be a long list of params!) What is the rust limit?
                        ExpandoObjectHelpers.AddProperty(paramsObject, iEntry.ToString(), entry.BuildDynamicParamsObject());
                        savingsEntries.Add(entry);
                        iEntry++;
                    }

                    if (saveChangesMadeToEntries)
                    {
                        for (int i = 0; i < Count; i++)
                        {
                            //If the user has not manually set the IsChanged property then we can calculate it now using reflection.
                            if (!this[i].IsChanged)
                                this[i].HasEntryChanged(holochainFieldsIsEnabledKeyValuePairs, cachePropertyInfos);

                            if (this[i].IsChanged)
                            {
                                this[i].State = Enums.HoloNETEntryState.Updated;

                                //Build params object containing each object (could be a long list of params!) What is the rust limit?
                                ExpandoObjectHelpers.AddProperty(paramsObject, iEntry.ToString(), this[i].BuildDynamicParamsObject());
                                savingsEntries.Add(this[i]);
                            }

                            iEntry++;
                        }
                    }

                    ZomeFunctionCallBackEventArgs batchResult = await HoloNETClient.CallZomeFunctionAsync(ZomeName, ZomeBatchUpdateCollectionFunction, paramsObject);

                    if (batchResult != null && !batchResult.IsError)
                        result.EntiesSaved.AddRange(savingsEntries);
                    else
                    {
                        result.EntiesSaveErrors.AddRange(savingsEntries);
                        result.Message = $"Error occured in SaveAllChangesAsync method in HoloNETCollection calling the batch update zome function {ZomeBatchUpdateCollectionFunction}. Reason: {batchResult.Message}";
                        result.IsError = true;
                    }
                }

                ResetChangeTracking();
                OnCollectionSaved?.Invoke(this, result);
                return result;
            }
            catch (Exception ex)
            {
                return HandleError<HoloNETCollectionSavedResult>("Unknown error occurred in SaveAllChangesAsync method in HoloNETCollection", ex);
            }
        }

        /// <summary>
        /// This method will save all changes made to the collection since the last time this method was called and persist the changes to the Holochain Conductor. This includes any entries that were added or removed to the collection via the Add and Remove methods (in-memory only). It also persists any entries that were updated if the 'saveChangesMadeToEntries' param is set to true (defaults to true). If entries were added or removed via the AddHoloNETEntryToCollectionAndSave and RemoveHoloNETEntryFromCollectionAndSave methods then these entries will NOT be added/removed again. If the 'saveAsOneBatchOperation' param is set to true (default), any new entries added, old entries removed or entries changed will be updated as one batch operation. This will be faster than making many seperate calls for adding, remove or updating entries. For this to work you need to make sure the 'ZomeBatchUpdateCollectionFunction' is set or was passed in via one of the constructors, it will then batch update any entries that have changed (added, removed or changed) since the last time this method was called.
        /// If 'saveAsOneBatchOperation' is set to false then it will call the SaveAsync method for updating each entry (if 'saveChangesMadeToEntries' is set to true), 'AddHoloNETEntryToCollectionAndSaveAsync' for adding new entries and 'RemoveHoloNETEntryFromCollectionAndSaveAsync' for removing old entries. This of course will be slower than updating them as a batch depending on the size of the collection. This will invoke the 'OnCollectionSaved' event once it has finished saving any changes to the collection.
        /// </summary>
        /// <param name="saveChangesMadeToEntries">Set this to true if you wish to update any changes made to the entries themselves. This defaults to true.</param>
        /// <param name="saveAsOneBatchOperation">Set this to true to save any new entries added, old entries removed or entries changed as one batch operation. This will be faster than making many seperate calls for adding, remove or updating entries. For this to work you need to make sure the 'ZomeBatchUpdateCollectionFunction' is set, this is where the entries added, removed and updated will be sent as a param. Entries updated is optional, set 'saveChangesMadeToEntries' to false if you do not wish to update them (defaults to true). </param>
        /// <param name="saveHoloNETEntryWhenAddingToCollection">If this is set to true (default) it will call the SaveAsync method on the HoloNETEntry before adding it to the collection.</param>
        /// <param name="continueOnError">Set this to true (default) if you wish HoloNET to continue attemtping to save changes when an error occurs adding, removing or updating entries. NOTE: This is only relevant if 'saveAsOneBatchOperation' is false. </param>
        /// <param name="collectionAnchor">The anchor of the Holochain Collection you wish to update. This is optional.</param>
        /// <param name="customDataKeyValuePairs">This is a optional dictionary containing keyvalue pairs of custom data you wish to inject into the params that are sent to the Save zome function when saving a HoloNETEntry.</param>
        /// <param name="holochainFieldsIsEnabledKeyValuePairs">This is a optional dictionary containing keyvalue pairs to allow properties that contain the HolochainFieldName to be omitted from the properties that this function checks for changes. The key (case senstive) needs to match a property that has the HolochainFieldName attribute.</param>
        /// <param name="cachePropertyInfos">Set this to true (default) if you want HoloNET to cache the property info's for the Entry (this can reduce the slight overhead used by reflection).</param>
        /// <returns>This returns 'HoloNETCollectionSavedResult' containing lists of all entries saved or entries that errored along with any errors that occured.</returns>
        public virtual HoloNETCollectionSavedResult SaveAllChanges(bool saveChangesMadeToEntries = true, bool saveAsOneBatchOperation = true, bool saveHoloNETEntryWhenAddingToCollection = true, bool continueOnError = true, string collectionAnchor = "", Dictionary<string, string> customDataKeyValuePairs = null, Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePairs = null, bool cachePropertyInfos = true)
        {
            return SaveAllChangesAsync(saveChangesMadeToEntries, saveAsOneBatchOperation, saveHoloNETEntryWhenAddingToCollection, continueOnError, collectionAnchor, customDataKeyValuePairs, holochainFieldsIsEnabledKeyValuePairs, cachePropertyInfos).Result;
        }

        /// <summary>
        /// Updates the collection's entries state (added, removed or updated).
        /// </summary>
        /// <param name="holochainFieldsIsEnabledKeyValuePairs">This is a optional dictionary containing keyvalue pairs to allow properties that contain the HolochainFieldName to be omitted from the properties that this function checks for changes. The key (case senstive) needs to match a property that has the HolochainFieldName attribute.</param>
        /// <param name="cachePropertyInfos">Set this to true (default) if you want HoloNET to cache the property info's for the Entry (this can reduce the slight overhead used by reflection).</param>
        public void UpdateEnriesState(Dictionary<string, bool> holochainFieldsIsEnabledKeyValuePairs = null, bool cachePropertyInfos = true)
        {
            ReCalculateEntriesAddedOrRemovedSinceLastSaved();

            //Check for entries Updated.
            for (int i = 0; i < Count; i++)
            {
                //If the user has not manually set the IsChanged property then we can calculate it now using reflection.
                if (!this[i].IsChanged)
                    this[i].HasEntryChanged(holochainFieldsIsEnabledKeyValuePairs, cachePropertyInfos);
            }
        }

        /// <summary>
        /// Will close this HoloNETCollection and then shutdown its internal HoloNET instance (if one was not passed in) and its current connection to the Holochain Conductor and then shutdown all running Holochain Conductors (if configured to do so) as well as any other tasks to shut HoloNET down cleanly. This method calls the ShutdownHoloNET method internally. Once it has finished shutting down HoloNET it will raise the OnClosed event.
        /// You can specify if HoloNET should wait until it has finished disconnecting and shutting down the conductors before returning to the caller or whether it should return immediately and then use the OnDisconnected, OnHolochainConductorsShutdownComplete & OnHoloNETShutdownComplete events to notify the caller.
        /// </summary>
        /// <param name="disconnectedCallBackMode">If this is set to `WaitForHolochainConductorToDisconnect` (default) then it will await until it has disconnected before returning to the caller, otherwise (it is set to `UseCallBackEvents`) it will return immediately and then raise the [OnDisconnected](#ondisconnected) once it is disconnected.</param>
        /// <param name="shutdownHolochainConductorsMode">Once it has successfully disconnected it will automatically call the ShutDownAllHolochainConductors method if the `shutdownHolochainConductorsMode` flag (defaults to `UseHoloNETDNASettings`) is not set to `DoNotShutdownAnyConductors`. Other values it can be are 'ShutdownCurrentConductorOnly' or 'ShutdownAllConductors'. Please see the ShutDownConductors method below for more detail.</param>
        /// <returns></returns>
        public virtual async Task<HoloNETShutdownEventArgs> CloseAsync(DisconnectedCallBackMode disconnectedCallBackMode = DisconnectedCallBackMode.WaitForHolochainConductorToDisconnect, ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings)
        {
            HoloNETShutdownEventArgs returnValue = null;

            try
            {
                if (HoloNETClient != null && _disposeOfHoloNETClient)
                {
                    returnValue = await HoloNETClient.ShutdownHoloNETAsync(disconnectedCallBackMode, shutdownHolochainConductorsMode);
                    UnsubscribeEvents();
                    HoloNETClient = null;
                }

                OnClosed?.Invoke(this, returnValue);
            }
            catch (Exception ex)
            {
                returnValue = HandleError<HoloNETShutdownEventArgs>("Unknown error occurred in CloseAsync method in HoloNETCollection", ex);
            }

            return returnValue;
        }

        /// <summary>
        /// Will close this HoloNET Entry and then shutdown its internal HoloNET instance (if one was not passed in) and its current connetion to the Holochain Conductor and then shutdown all running Holochain Conductors (if configured to do so) as well as any other tasks to shut HoloNET down cleanly. This method calls the ShutdownHoloNET method internally. Once it has finished shutting down HoloNET it will raise the OnClosed event.
        /// Unlike the async version, this non async version will not wait until HoloNET disconnects & shutsdown any Holochain Conductors before it returns to the caller. It will later raise the Disconnected, HolochainConductorsShutdownComplete & HoloNETShutdownComplete events. If you wish to wait for HoloNET to disconnect and shutdown the conductors(s) before returning then please use CloseAsync instead. It will also not contain any Holochain conductor shutdown stats and the HolochainConductorsShutdownEventArgs property will be null (Only the CloseAsync version contains this info).
        /// </summary>
        /// <param name="shutdownHolochainConductorsMode">Once it has successfully disconnected it will automatically call the ShutDownAllHolochainConductors method if the `shutdownHolochainConductorsMode` flag (defaults to `UseHoloNETDNASettings`) is not set to `DoNotShutdownAnyConductors`. Other values it can be are 'ShutdownCurrentConductorOnly' or 'ShutdownAllConductors'. Please see the ShutDownConductors method below for more detail.</param>
        public virtual HoloNETShutdownEventArgs Close(ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings)
        {
            HoloNETShutdownEventArgs returnValue = null;

            try
            {
                if (HoloNETClient != null && _disposeOfHoloNETClient)
                {
                    returnValue = HoloNETClient.ShutdownHoloNET(shutdownHolochainConductorsMode);
                    UnsubscribeEvents();
                    HoloNETClient = null;
                }

                OnClosed?.Invoke(this, returnValue);
            }
            catch (Exception ex)
            {
                returnValue = HandleError<HoloNETShutdownEventArgs>("Unknown error occurred in Close method in HoloNETCollection", ex);
            }

            return returnValue;
        }

        private void UnsubscribeEvents()
        {
            HoloNETClient.OnError -= HoloNETClient_OnError;
            HoloNETClient.OnReadyForZomeCalls -= HoloNETClient_OnReadyForZomeCalls;
        }

        private void HoloNETClient_OnError(object sender, HoloNETErrorEventArgs e)
        {
            OnError?.Invoke(this, e);
        }

        private void HoloNETClient_OnReadyForZomeCalls(object sender, ReadyForZomeCallsEventArgs e)
        {
            IsInitializing = false;
            OnInitialized?.Invoke(this, e);
        }

        protected void HandleError(string message, Exception exception)
        {
            message = string.Concat(message, exception != null ? $". Error Details: {exception}" : "");
            HoloNETClient.Logger.Log(message, LogType.Error);

            OnError?.Invoke(this, new HoloNETErrorEventArgs { EndPoint = HoloNETClient.WebSocket.EndPoint, Reason = message, ErrorDetails = exception });

            switch (HoloNETClient.HoloNETDNA.ErrorHandlingBehaviour)
            {
                case Client.ErrorHandlingBehaviour.AlwaysThrowExceptionOnError:
                    throw new HoloNETException(message, exception, HoloNETClient.WebSocket.EndPoint);

                case Client.ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent:
                    {
                        if (OnError == null)
                            throw new HoloNETException(message, exception, HoloNETClient.WebSocket.EndPoint);
                    }
                    break;
            }
        }

        private T HandleError<T>(string message, Exception exception) where T : CallBackBaseEventArgs, new()
        {
            HandleError(message, exception);
            return new T() { IsError = true, Message = string.Concat(message, exception != null ? $". Error Details: {exception}" : "") };
        }

        private void ResetChangeTracking()
        {
            EntriesAddedSinceLastSaved.Clear();
            EntriesRemovedSinceLastSaved.Clear();
            IsChanges = false;
            OriginalEntries = this.ToList();
        }
    }
}
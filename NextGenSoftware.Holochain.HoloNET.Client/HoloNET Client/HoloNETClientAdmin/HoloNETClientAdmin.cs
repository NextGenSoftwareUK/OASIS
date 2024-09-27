using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.Logging.Interfaces;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public partial class HoloNETClientAdmin : HoloNETClientBase, IHoloNETClientAdmin
    {
        private Dictionary<string, string> _disablingAppLookup = new Dictionary<string, string>();
        private Dictionary<string, string> _uninstallingAppLookup = new Dictionary<string, string>();
        private Dictionary<string, TaskCompletionSource<AgentPubKeyGeneratedCallBackEventArgs>> _taskCompletionAgentPubKeyGeneratedCallBack = new Dictionary<string, TaskCompletionSource<AgentPubKeyGeneratedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AppInstalledCallBackEventArgs>> _taskCompletionAppInstalledCallBack = new Dictionary<string, TaskCompletionSource<AppInstalledCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AppUninstalledCallBackEventArgs>> _taskCompletionAppUninstalledCallBack = new Dictionary<string, TaskCompletionSource<AppUninstalledCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AppEnabledCallBackEventArgs>> _taskCompletionAppEnabledCallBack = new Dictionary<string, TaskCompletionSource<AppEnabledCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AppDisabledCallBackEventArgs>> _taskCompletionAppDisabledCallBack = new Dictionary<string, TaskCompletionSource<AppDisabledCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<ZomeCallCapabilityGrantedCallBackEventArgs>> _taskCompletionZomeCapabilityGrantedCallBack = new Dictionary<string, TaskCompletionSource<ZomeCallCapabilityGrantedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AppInterfaceAttachedCallBackEventArgs>> _taskCompletionAppInterfaceAttachedCallBack = new Dictionary<string, TaskCompletionSource<AppInterfaceAttachedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<DnaRegisteredCallBackEventArgs>> _taskCompletionDnaRegisteredCallBack = new Dictionary<string, TaskCompletionSource<DnaRegisteredCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AppsListedCallBackEventArgs>> _taskCompletionAppsListedCallBack = new Dictionary<string, TaskCompletionSource<AppsListedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<DnasListedCallBackEventArgs>> _taskCompletionDnasListedCallBack = new Dictionary<string, TaskCompletionSource<DnasListedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<CellIdsListedCallBackEventArgs>> _taskCompletionCellIdsListedCallBack = new Dictionary<string, TaskCompletionSource<CellIdsListedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AppInterfacesListedCallBackEventArgs>> _taskCompletionAppInterfacesListedCallBack = new Dictionary<string, TaskCompletionSource<AppInterfacesListedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<FullStateDumpedCallBackEventArgs>> _taskCompletionFullStateDumpedCallBack = new Dictionary<string, TaskCompletionSource<FullStateDumpedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<StateDumpedCallBackEventArgs>> _taskCompletionStateDumpedCallBack = new Dictionary<string, TaskCompletionSource<StateDumpedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<DnaDefinitionReturnedCallBackEventArgs>> _taskCompletionDnaDefinitionReturnedCallBack = new Dictionary<string, TaskCompletionSource<DnaDefinitionReturnedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<CoordinatorsUpdatedCallBackEventArgs>> _taskCompletionCoordinatorsUpdatedCallBack = new Dictionary<string, TaskCompletionSource<CoordinatorsUpdatedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AgentInfoReturnedCallBackEventArgs>> _taskCompletionAgentInfoReturnedCallBack = new Dictionary<string, TaskCompletionSource<AgentInfoReturnedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AgentInfoAddedCallBackEventArgs>> _taskCompletionAgentInfoAddedCallBack = new Dictionary<string, TaskCompletionSource<AgentInfoAddedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<CloneCellDeletedCallBackEventArgs>> _taskCompletionCloneCellDeletedCallBack = new Dictionary<string, TaskCompletionSource<CloneCellDeletedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<StorageInfoReturnedCallBackEventArgs>> _taskCompletionStorageInfoReturnedCallBack = new Dictionary<string, TaskCompletionSource<StorageInfoReturnedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<NetworkStatsDumpedCallBackEventArgs>> _taskCompletionNetworkStatsDumpedCallBack = new Dictionary<string, TaskCompletionSource<NetworkStatsDumpedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<NetworkMetricsDumpedCallBackEventArgs>> _taskCompletionNetworkMetricsDumpedCallBack = new Dictionary<string, TaskCompletionSource<NetworkMetricsDumpedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<RecordsGraftedCallBackEventArgs>> _taskCompletionRecordsGraftedCallBack = new Dictionary<string, TaskCompletionSource<RecordsGraftedCallBackEventArgs>>();
        private Dictionary<string, TaskCompletionSource<AdminInterfacesAddedCallBackEventArgs>> _taskCompletionAdminInterfacesAddedCallBack = new Dictionary<string, TaskCompletionSource<AdminInterfacesAddedCallBackEventArgs>>();

        //Events

        public delegate void InstallEnableSignAttachAndConnectToHappCallBack(object sender, InstallEnableSignAttachAndConnectToHappEventArgs e);

        /// <summary>
        /// Fired when the hApp has been installed, enabled, signed and attached.
        /// </summary>
        public event InstallEnableSignAttachAndConnectToHappCallBack OnInstallEnableSignAttachAndConnectToHappCallBack;


        public delegate void InstallEnableSignAndAttachHappCallBack(object sender, InstallEnableSignAndAttachHappEventArgs e);

        /// <summary>
        /// Fired when the hApp has been installed, enabled, signed and attached.
        /// </summary>
        public event InstallEnableSignAndAttachHappCallBack OnInstallEnableSignAndAttachHappCallBack;




        public delegate void AgentPubKeyGeneratedCallBack(object sender, AgentPubKeyGeneratedCallBackEventArgs e);

        /// <summary>
        /// Fired when the client receives the generated AgentPubKey from the conductor.
        /// </summary>
        public event AgentPubKeyGeneratedCallBack OnAgentPubKeyGeneratedCallBack;



        public delegate void AppInstalledCallBack(object sender, AppInstalledCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after a hApp has been installed via the InstallAppAsyc/InstallApp method.
        /// </summary>
        public event AppInstalledCallBack OnAppInstalledCallBack;


        public delegate void AppUninstalledCallBack(object sender, AppUninstalledCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after a hApp has been uninstalled via the UninstallAppAsyc/UninstallApp method.
        /// </summary>
        public event AppUninstalledCallBack OnAppUninstalledCallBack;


        public delegate void AppEnabledCallBack(object sender, AppEnabledCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after a hApp has been enabled via the EnableAppAsyc/EnableApp method.
        /// </summary>
        public event AppEnabledCallBack OnAppEnabledCallBack;


        public delegate void AppDisabledCallBack(object sender, AppDisabledCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after a hApp has been disabled via the DisableAppAsyc/DisableApp method.
        /// </summary>
        public event AppDisabledCallBack OnAppDisabledCallBack;


        public delegate void ZomeCallCapabilityGrantedCallBack(object sender, ZomeCallCapabilityGrantedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after a cell has been authorized via the AuthorizeSigningCredentialsForZomeCallsAsync/AuthorizeSigningCredentialsForZomeCalls method.
        /// </summary>
        public event ZomeCallCapabilityGrantedCallBack OnZomeCallCapabilityGrantedCallBack;


        public delegate void AppInterfaceAttachedCallBack(object sender, AppInterfaceAttachedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the app interface has been attached via the AttachAppInterfaceAsyc/AttachAppInterface method.
        /// </summary>
        public event AppInterfaceAttachedCallBack OnAppInterfaceAttachedCallBack;


        public delegate void DnaRegisteredCallBack(object sender, DnaRegisteredCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the dna has been registered via the RegisterDnaAsync/RegisterDna method.
        /// </summary>
        public event DnaRegisteredCallBack OnDnaRegisteredCallBack;


        public delegate void AppsListedCallBack(object sender, AppsListedCallBackEventArgs e);


        public delegate void AppInterfacesListedCallBack(object sender, AppInterfacesListedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the ListAppInterfacesAsync/ListAppInterfaces method is called.
        /// </summary>
        public event AppInterfacesListedCallBack OnAppInterfacesListedCallBack;

        /// <summary>
        /// Fired when a response is received from the conductor after the ListAppsAsync/ListApps method is called.
        /// </summary>
        public event AppsListedCallBack OnAppsListedCallBack;


        public delegate void DnasListedCallBack(object sender, DnasListedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the ListDnasAsync/ListDnas method is called.
        /// </summary>
        public event DnasListedCallBack OnDnasListedCallBack;


        public delegate void CellIdsListedCallBack(object sender, CellIdsListedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the ListCellIdsAsync/ListCellIds method is called.
        /// </summary>
        public event CellIdsListedCallBack OnCellIdsListedCallBack;


        public delegate void FullStateDumpedCallBack(object sender, FullStateDumpedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the DumpFullStateAsync/DumpFullState method is called.
        /// </summary>
        public event FullStateDumpedCallBack OnFullStateDumpedCallBack;


        public delegate void StateDumpedCallBack(object sender, StateDumpedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the DumpStateAsync/DumpState method is called.
        /// </summary>
        public event StateDumpedCallBack OnStateDumpedCallBack;


        public delegate void DnaDefinitionReturnedCallBack(object sender, DnaDefinitionReturnedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the DNA Definition for a DNA Hash after the GetDnaDefinitionAsync/GetDnaDefinition method is called.
        /// </summary>
        public event DnaDefinitionReturnedCallBack OnDnaDefinitionReturnedCallBack;


        public delegate void CoordinatorsUpdatedCallBack(object sender, CoordinatorsUpdatedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor after the UpdateCoordinatorsAsync/UpdateCoordinators method is called.
        /// </summary>
        public event CoordinatorsUpdatedCallBack OnCoordinatorsUpdatedCallBack;


        public delegate void AgentInfoReturnedCallBack(object sender, AgentInfoReturnedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the GetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event AgentInfoReturnedCallBack OnAgentInfoReturnedCallBack;


        public delegate void AgentInfoAddedCallBack(object sender, AgentInfoAddedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the GetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event AgentInfoAddedCallBack OnAgentInfoAddedCallBack;


        public delegate void CloneCellDeletedCallBack(object sender, CloneCellDeletedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the GetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event CloneCellDeletedCallBack OnCloneCellDeletedCallBack;


        public delegate void StorageInfoReturnedCallBack(object sender, StorageInfoReturnedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the GetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event StorageInfoReturnedCallBack OnStorageInfoReturnedCallBack;


        public delegate void NetworkStatsDumpedCallBack(object sender, NetworkStatsDumpedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the GetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event NetworkStatsDumpedCallBack OnNetworkStatsDumpedCallBack;


        public delegate void NetworkMetricsDumpedCallBack(object sender, NetworkMetricsDumpedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the GetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event NetworkMetricsDumpedCallBack OnNetworkMetricsDumpedCallBack;


        public delegate void RecordsGraftedCallBack(object sender, RecordsGraftedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the GetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event RecordsGraftedCallBack OnRecordsGraftedCallBack;


        public delegate void AdminInterfacesAddedCallBack(object sender, AdminInterfacesAddedCallBackEventArgs e);

        /// <summary>
        /// Fired when a response is received from the conductor containing the requested Agent Info after the GetAgentInfoAsync/GetAgentInfo method is called.
        /// </summary>
        public event AdminInterfacesAddedCallBack OnAdminInterfacesAddedCallBack;


        /// <summary>
        /// This constructor uses the built-in DefaultLogger and the settings contained in the HoloNETDNA.
        /// </summary>
        /// <param name="holoNETDNA">The HoloNETDNA you wish to use for this connection (optional). If this is not passed in then it will use the default HoloNETDNA defined in the HoloNETDNA property.</param>
        public HoloNETClientAdmin(HoloNETDNA holoNETDNA = null) : base(holoNETDNA)
        {

        }

        /// <summary>
        /// This constructor allows you to inject in (DI) your own implementation (logProvider) of the ILogProvider interface, which will be added to the Logger.LogProviders collection. This Logger instance is also passed to the WebSocket library. HoloNET will then log to each of these logProviders contained within the Logger. It will also use the settings contained in the HoloNETDNA.
        /// </summary>
        /// <param name="logProvider">The implementation of the ILogProvider interface (custom logProvider).</param>
        /// <param name="alsoUseDefaultLogger">Set this to true if you wish HoloNET to also log to the DefaultLogger as well as any custom logger injected in.</param>
        /// <param name="holoNETDNA">The HoloNETDNA you wish to use for this connection (optional). If this is not passed in then it will use the default HoloNETDNA defined in the HoloNETDNA property.</param>
        public HoloNETClientAdmin(ILogProvider logProvider, bool alsoUseDefaultLogger = false, IHoloNETDNA holoNETDNA = null) : base(logProvider, alsoUseDefaultLogger, holoNETDNA)
        {

        }

        /// <summary>
        /// This constructor allows you to inject in (DI) multiple implementations (logProviders) of the ILogProvider interface, which will be added to the Logger.LogProviders collection. This Logger instance is also passed to the WebSocket library. HoloNET will then log to each of these logProviders contained within the Logger. It will also use the settings contained in the HoloNETDNA.
        /// </summary>
        /// <param name="logProviders">The implementations of the ILogProvider interface (custom logProviders).</param>
        /// <param name="alsoUseDefaultLogger">Set this to true if you wish HoloNET to also log to the DefaultLogger as well as any custom loggers injected in.</param>
        /// <param name="holoNETDNA">The HoloNETDNA you wish to use for this connection (optional). If this is not passed in then it will use the default HoloNETDNA defined in the HoloNETDNA property.</param>
        public HoloNETClientAdmin(IEnumerable<ILogProvider> logProviders, bool alsoUseDefaultLogger = false, IHoloNETDNA holoNETDNA = null) : base(logProviders, alsoUseDefaultLogger, holoNETDNA)
        {

        }

        /// <summary>
        /// This constructor allows you to inject in (DI) a Logger instance (which could contain multiple logProviders). This will then override the default Logger found on the Logger property. This Logger instance is also passed to the WebSocket library. HoloNET will then log to each of these logProviders contained within the Logger. It will also use the settings contained in the HoloNETDNA.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="holoNETDNA">The HoloNETDNA you wish to use for this connection (optional). If this is not passed in then it will use the default HoloNETDNA defined in the HoloNETDNA property.</param>
        public HoloNETClientAdmin(ILogger logger, IHoloNETDNA holoNETDNA = null) : base(logger, holoNETDNA)
        {

        }

        /// <summary>
        /// This method simply connects to the Holochain conductor. It raises the OnConnected event once it is has successfully established a connection. It then calls the RetrieveAgentPubKeyAndDnaHash method to retrieve the AgentPubKey & DnaHash. If the `connectedCallBackMode` flag is set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.
        /// </summary>
        /// <param name="holochainConductorURI">The URI that the Holochain Conductor is running.</param>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the [HoloNETHoloNETDNA](#holonetconfig) once it has retrieved the DnaHash & AgentPubKey.</param>
        /// <returns></returns>
        public override async Task<HoloNETConnectedEventArgs> ConnectAsync(Uri holochainConductorURI, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            return await ConnectAsync(holochainConductorURI.AbsoluteUri, connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This method simply connects to the Holochain conductor. It raises the OnConnected event once it is has successfully established a connection. It then calls the RetrieveAgentPubKeyAndDnaHash method to retrieve the AgentPubKey & DnaHash. If the `connectedCallBackMode` flag is set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.
        /// </summary>
        /// <param name="holochainConductorURI">The URI that the Holochain Conductor is running.</param>
        ///// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the [HoloNETHoloNETDNA](#holonetconfig) once it has retrieved the DnaHash & AgentPubKey.</param>
        /// <returns></returns>
        public override HoloNETConnectedEventArgs Connect(Uri holochainConductorURI, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            return ConnectAsync(holochainConductorURI, ConnectedCallBackMode.UseCallBackEvents, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved).Result;
        }

        /// <summary>
        /// This method simply connects to the Holochain conductor. It raises the OnConnected event once it is has successfully established a connection. It then calls the RetrieveAgentPubKeyAndDnaHash method to retrieve the AgentPubKey & DnaHash. If the `connectedCallBackMode` flag is set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.
        /// </summary>
        /// <param name="holochainConductorURI">The URI that the Holochain Conductor is running.</param>
        /// <param name="connectedCallBackMode">If set to `WaitForHolochainConductorToConnect` (default) it will await until it is connected before returning, otherwise it will return immediately and then call the OnConnected event once it has finished connecting.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashMode">If set to `Wait` (default) it will await until it has finished retrieving the AgentPubKey & DnaHash before returning, otherwise it will return immediately and then call the OnReadyForZomeCalls event once it has finished retrieving the DnaHash & AgentPubKey.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the [HoloNETHoloNETDNA](#holonetconfig) once it has retrieved the DnaHash & AgentPubKey.</param>
        /// <returns></returns>
        public override async Task<HoloNETConnectedEventArgs> ConnectAsync(string holochainConductorURI = "", ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            if (string.IsNullOrEmpty(holochainConductorURI))
                holochainConductorURI = HoloNETDNA.HolochainConductorAdminURI;

            Log($"ADMIN: Connecting To Admin WebSocket...", LogType.Info);
            return await base.ConnectAsync(holochainConductorURI, connectedCallBackMode, retrieveAgentPubKeyAndDnaHashMode, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved);
        }

        /// <summary>
        /// This method simply connects to the Holochain conductor. It raises the OnConnected event once it is has successfully established a connection. It then calls the RetrieveAgentPubKeyAndDnaHash method to retrieve the AgentPubKey & DnaHash.
        /// </summary>
        /// <param name="holochainConductorURI">The URI that the Holochain Conductor is running.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromConductor">Set this to true for HoloNET to automatically retrieve the AgentPubKey & DnaHash from the Holochain Conductor after it has connected. This defaults to true.</param>
        /// <param name="retrieveAgentPubKeyAndDnaHashFromSandbox">Set this to true if you wish HoloNET to automatically retrieve the AgentPubKey & DnaHash from the hc sandbox after it has connected. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromConductorIfSandBoxFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the Holochain Conductor if it fails to get them from the HC Sandbox command. This defaults to true.</param>
        /// <param name="automaticallyAttemptToRetrieveFromSandBoxIfConductorFails">If this is set to true it will automatically attempt to get the AgentPubKey & DnaHash from the HC Sandbox command if it fails to get them from the Holochain Conductor. This defaults to true.</param>
        /// <param name="updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved">Set this to true (default) to automatically update the [HoloNETHoloNETDNA](#holonetconfig) once it has retrieved the DnaHash & AgentPubKey.</param>
        public override HoloNETConnectedEventArgs Connect(string holochainConductorURI = "", bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = false, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true)
        {
            return ConnectAsync(holochainConductorURI, ConnectedCallBackMode.UseCallBackEvents, RetrieveAgentPubKeyAndDnaHashMode.UseCallBackEvents, retrieveAgentPubKeyAndDnaHashFromConductor, retrieveAgentPubKeyAndDnaHashFromSandbox, automaticallyAttemptToRetrieveFromConductorIfSandBoxFails, automaticallyAttemptToRetrieveFromSandBoxIfConductorFails, updateHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved).Result;
        }
    }
}
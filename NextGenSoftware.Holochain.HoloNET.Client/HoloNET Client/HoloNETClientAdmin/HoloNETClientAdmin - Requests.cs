using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Nito.AsyncEx.Synchronous;
using NextGenSoftware.Logging;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using Sodium;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public partial class HoloNETClientAdmin : HoloNETClientBase, IHoloNETClientAdmin
    {
        /// <summary>
        /// Will init the hApp, which includes installing and enabling the app, signing credentials & attaching the app interface.
        /// </summary>
        //public async Task<InstallEnableSignAndAttachHappEventArgs> InstallEnableSignAndAttachHappAsync(string hAppId, string hAppInstallPath, string roleName, CapGrantAccessType capGrantAccessType = CapGrantAccessType.Unrestricted, GrantedFunctionsType grantedFunctionsType = GrantedFunctionsType.All, List<(string, string)> grantedFunctions = null, bool uninstallhAppIfAlreadyInstalled = true, bool log = true, Action<string, LogType> loggingFunction = null)
        public async Task<InstallEnableSignAndAttachHappEventArgs> InstallEnableSignAndAttachHappAsync(string hAppId, string hAppInstallPath, string roleName, CapGrantAccessType capGrantAccessType = CapGrantAccessType.Assigned, GrantedFunctionsType grantedFunctionsType = GrantedFunctionsType.All, List<(string, string)> grantedFunctions = null, bool uninstallhAppIfAlreadyInstalled = true, bool log = true, Action<string, LogType> loggingFunction = null)
        {
            InstallEnableSignAndAttachHappEventArgs result = new InstallEnableSignAndAttachHappEventArgs();

            try
            {
                if (log)
                    Log($"ADMIN: Checking If App {hAppId} Is Already Installed...", LogType.Info, loggingFunction);

                GetAppInfoCallBackEventArgs appInfoResult = await GetAppInfoAsync(hAppId);

                if (appInfoResult != null && appInfoResult.AppInfo != null && uninstallhAppIfAlreadyInstalled)
                {
                    if (log)
                        Log($"ADMIN: App {hAppId} Is Already Installed So Uninstalling Now...", LogType.Info);

                    AppUninstalledCallBackEventArgs uninstallResult = await UninstallAppAsync(hAppId);

                    if (uninstallResult != null && uninstallResult.IsError)
                    {
                        if (log)
                            Log($"ADMIN: Error Uninstalling App {hAppId}. Reason: {uninstallResult.Message}", LogType.Info, loggingFunction);
                    }
                    else
                    {
                        if (log)
                            Log($"ADMIN: Uninstalled App {hAppId}.", LogType.Info, loggingFunction);
                    }
                }

                if (log)
                    Log($"ADMIN: Generating New AgentPubKey...", LogType.Info, loggingFunction);

                result.AgentPubKeyGeneratedResult = await GenerateAgentPubKeyAsync();

                if (result.AgentPubKeyGeneratedResult != null && !result.AgentPubKeyGeneratedResult.IsError)
                {
                    if (log)
                    {
                        Log($"ADMIN: AgentPubKey Generated Successfully. AgentPubKey: {result.AgentPubKeyGeneratedResult.AgentPubKey}", LogType.Info, loggingFunction);
                        Log($"ADMIN: Installing App {hAppId}...", LogType.Info, loggingFunction);
                    }

                    result.IsAgentPubKeyGenerated = true;
                    result.AppInstalledResult = await InstallAppAsync(hAppId, hAppInstallPath, null);

                    if (result.AppInstalledResult != null && !result.AppInstalledResult.IsError)
                    {
                        result.IsAppInstalled = true;
                        result.AgentPubKey = result.AppInstalledResult.AgentPubKey;
                        result.DnaHash = result.AppInstalledResult.DnaHash;
                        result.CellId = result.AppInstalledResult.CellId;
                        result.AppStatus = result.AppInstalledResult.AppInfoResponse.data.AppStatus;
                        result.AppStatusReason = result.AppInstalledResult.AppInfoResponse.data.AppStatusReason;
                        result.AppManifest = result.AppInstalledResult.AppInfoResponse.data.manifest;

                        if (!string.IsNullOrEmpty(roleName) && result.AppInstalledResult.AppInfoResponse != null && 
                            result.AppInstalledResult.AppInfoResponse.data != null && 
                            result.AppInstalledResult.AppInfoResponse.data.cell_info != null && 
                            result.AppInstalledResult.AppInfoResponse.data.cell_info.ContainsKey(roleName) && 
                            result.AppInstalledResult.AppInfoResponse.data.cell_info[roleName] != null && 
                            result.AppInstalledResult.AppInfoResponse.data.cell_info[roleName].Count > 0 && 
                            result.AppInstalledResult.AppInfoResponse.data.cell_info[roleName][0] != null)
                                result.CellType = result.AppInstalledResult.AppInfoResponse.data.cell_info[roleName][0].CellInfoType;

                        if (result.CellType == CellInfoType.Provisioned)
                        {
                            if (log)
                            {
                                Log($"ADMIN: {hAppId} App Installed. AgentPubKey: {result.AgentPubKey}, DnaHash: {result.DnaHash}, Manifest: {result.AppManifest.name}, CellType: {Enum.GetName(typeof(CellInfoType), result.CellType)}", LogType.Info, loggingFunction);
                                Log($"ADMIN: Enabling App {hAppId}...", LogType.Info, loggingFunction);
                            }

                            result.AppEnabledResult = await EnableAppAsync(hAppId);

                            if (result.AppEnabledResult != null && !result.AppEnabledResult.IsError)
                            {
                                if (log)
                                {
                                    Log($"ADMIN: {hAppId} App Enabled.", LogType.Info, loggingFunction);
                                    Log($"ADMIN: Signing Credentials (Zome Call Capabilities) For App {hAppId}...", LogType.Info, loggingFunction);
                                }

                                result.IsAppEnabled = true;
                                result.ZomeCallCapabilityGrantedResult = await AuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync(result.AppInstalledResult.CellId, capGrantAccessType, grantedFunctionsType, grantedFunctions);

                                if (result.ZomeCallCapabilityGrantedResult != null && !result.ZomeCallCapabilityGrantedResult.IsError)
                                {
                                    if (log)
                                    {
                                        Log($"ADMIN: {hAppId} App Signing Credentials Authorized.", LogType.Info, loggingFunction);
                                        Log($"ADMIN: Attaching App Interface For App {hAppId}...", LogType.Info, loggingFunction);
                                    }

                                    result.IsAppSigned = true;
                                    result.AppInterfaceAttachedResult = await AttachAppInterfaceAsync();

                                    if (result.AppInterfaceAttachedResult != null && !result.AppInterfaceAttachedResult.IsError)
                                    {
                                        result.IsAppAttached = true;
                                        result.IsSuccess = true;
                                        result.AttachedOnPort = result.AppInterfaceAttachedResult.Port;

                                        if (log)
                                            Log($"ADMIN: {hAppId} App Interface Attached On Port {result.AppInterfaceAttachedResult.Port}.", LogType.Info, loggingFunction);
                                    }
                                    else
                                        HandleError(result, $"ADMIN: Error Attaching App Interface For App {hAppId}. Reason: {result.AppInterfaceAttachedResult.Message}", log, loggingFunction);
                                }
                                else
                                    HandleError(result, $"ADMIN: Error Signing Credentials For App {hAppId}. Reason: {result.ZomeCallCapabilityGrantedResult.Message}", log, loggingFunction);
                            }
                            else
                                HandleError(result, $"ADMIN: Error Enabling App {hAppId}. Reason: {result.AppEnabledResult.Message}", log, loggingFunction);
                        }
                        else
                            HandleError(result, $"ADMIN: Error Installing App {hAppId}. Reason: CellType ({Enum.GetName(typeof(CellInfoType), result.CellType)}) Is Not Provisioned So Aborting.", log, loggingFunction);
                    }
                    else
                        HandleError(result, $"ADMIN: Error Installing App {hAppId}. Reason: {result.AppInstalledResult.Message}", log, loggingFunction);
                }
                else
                    HandleError(result, $"ADMIN: Error Generating AgentPubKey. Reason: {result.AgentPubKeyGeneratedResult.Message}", log, loggingFunction);
            }
            catch (Exception ex)
            {
                HandleError(result, $"ADMIN: Unknown Error occured in HoloNETClientAdmin.InstallEnableSignAndAttachHappAsync. Reason: {ex}", log, loggingFunction);
            }

            OnInstallEnableSignAndAttachHappCallBack?.Invoke(this, result);
            return result;
        }

        /// <summary>
        /// Will init the hApp, which includes installing and enabling the app, signing credentials & attaching the app interface. It will then connect to the hApp and return the HoloNETClientAppAgent connection.
        /// </summary>
        //public async Task<InstallEnableSignAttachAndConnectToHappEventArgs> InstallEnableSignAttachAndConnectToHappAsync(string hAppId, string hAppInstallPath, string roleName, CapGrantAccessType capGrantAccessType = CapGrantAccessType.Unrestricted, GrantedFunctionsType grantedFunctionsType = GrantedFunctionsType.All, List<(string, string)> grantedFunctions = null, bool uninstallhAppIfAlreadyInstalled = true, bool log = true, Action<string, LogType> loggingFunction = null)
        public async Task<InstallEnableSignAttachAndConnectToHappEventArgs> InstallEnableSignAttachAndConnectToHappAsync(string hAppId, string hAppInstallPath, string roleName, CapGrantAccessType capGrantAccessType = CapGrantAccessType.Assigned, GrantedFunctionsType grantedFunctionsType = GrantedFunctionsType.All, List<(string, string)> grantedFunctions = null, bool uninstallhAppIfAlreadyInstalled = true, bool log = true, Action<string, LogType> loggingFunction = null)
        {
            InstallEnableSignAttachAndConnectToHappEventArgs result = new InstallEnableSignAttachAndConnectToHappEventArgs();

            try
            {
                InstallEnableSignAndAttachHappEventArgs installedResult = await InstallEnableSignAndAttachHappAsync(hAppId, hAppInstallPath, roleName, capGrantAccessType, grantedFunctionsType, grantedFunctions, uninstallhAppIfAlreadyInstalled, log, loggingFunction);

                if (installedResult != null && !installedResult.IsError && installedResult.IsSuccess)
                {
                    if (log)
                        Log($"APP: Connecting to HoloNETClientAppAgent 'ws://127.0.0.1:{installedResult.AttachedOnPort}' {hAppId}...", LogType.Info, loggingFunction);

                    result.IsSuccess = true;
                    result.IsAppInstalled = installedResult.IsAppInstalled;
                    result.IsAppEnabled = installedResult.IsAppEnabled;
                    result.IsAppSigned = installedResult.IsAppSigned;
                    result.IsAppAttached = installedResult.IsAppAttached;

                    result.AgentPubKey = installedResult.AgentPubKey;
                    result.DnaHash = installedResult.DnaHash;
                    result.CellId = installedResult.CellId;
                    result.AppStatus = installedResult.AppStatus;
                    result.AppStatusReason = installedResult.AppStatusReason;
                    result.AppManifest = installedResult.AppManifest;
                    result.AttachedOnPort = installedResult.AttachedOnPort;

                    result.AgentPubKeyGeneratedResult = installedResult.AgentPubKeyGeneratedResult;
                    result.AppInstalledResult = installedResult.AppInstalledResult;
                    result.AppEnabledResult = installedResult.AppEnabledResult;
                    result.ZomeCallCapabilityGrantedResult = installedResult.ZomeCallCapabilityGrantedResult;
                    result.AppInterfaceAttachedResult = installedResult.AppInterfaceAttachedResult;

                    result.HoloNETClientAppAgent = new HoloNETClientAppAgent(hAppId, result.AgentPubKey);
                    result.HoloNETConnectedResult = await result.HoloNETClientAppAgent.ConnectAsync(hAppId, $"ws://127.0.0.1:{installedResult.AttachedOnPort}");

                    if (result.HoloNETConnectedResult != null && !result.HoloNETConnectedResult.IsError && result.HoloNETConnectedResult.IsConnected)
                    {
                        if (log)
                            Log($"APP: Connected to HoloNETClientAppAgent 'ws://127.0.0.1:{installedResult.AttachedOnPort}' {hAppId}.", LogType.Info, loggingFunction);

                        result.IsAppConnected = true;
                    }
                    else
                        HandleError(result, $"APP: Error occured in HoloNETClientAdmin.InstallEnableSignAttachAndConnectToHappAsync calling HoloNETClientAppAgent.ConnectAsync. Reason: {result.HoloNETConnectedResult.Message}", log, loggingFunction);
                }
                else
                    HandleError(result, $"ADMIN: Error occured in HoloNETClientAdmin.InstallEnableSignAttachAndConnectToHappAsync calling InstallEnableSignAndAttachHappAsync. Reason: {installedResult.Message}", log, loggingFunction);
            }
            catch (Exception ex)
            {
                HandleError(result, $"ADMIN: Unknown Error occured in HoloNETClientAdmin.InstallEnableSignAttachAndConnectToHappAsync. Reason: {ex}", log, loggingFunction);
            }

            OnInstallEnableSignAttachAndConnectToHappCallBack?.Invoke(this, result);
            return result;
        }

        /// <summary>
        /// Will init the hApp, which includes installing and enabling the app, signing credentials & attaching the app interface. It will then connect to the hApp and return the HoloNETClientAppAgent connection.
        /// </summary>
        public InstallEnableSignAttachAndConnectToHappEventArgs InstallEnableSignAttachAndConnectToHapp(string hAppId, string hAppInstallPath, string roleName, CapGrantAccessType capGrantAccessType = CapGrantAccessType.Unrestricted, GrantedFunctionsType grantedFunctionsType = GrantedFunctionsType.All, List<(string, string)> grantedFunctions = null, bool uninstallhAppIfAlreadyInstalled = true, bool log = true, Action<string, LogType> loggingFunction = null)
        {
            return InstallEnableSignAttachAndConnectToHappAsync(hAppId, hAppInstallPath, roleName, capGrantAccessType, grantedFunctionsType, grantedFunctions, uninstallhAppIfAlreadyInstalled, log, loggingFunction).Result;
        }

        public async Task<AgentPubKeyGeneratedCallBackEventArgs> GenerateAgentPubKeyAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, bool updateAgentPubKeyInHoloNETDNA = true, string id = "")
        {
            _updateDnaHashAndAgentPubKey = updateAgentPubKeyInHoloNETDNA;
            return await CallFunctionAsync(HoloNETRequestType.AdminGrantZomeCallCapability, "generate_agent_pub_key", null, _taskCompletionAgentPubKeyGeneratedCallBack, "OnAgentPubKeyGeneratedCallBack", conductorResponseCallBackMode, id);
        }

        //public void GenerateAgentPubKey(bool updateAgentPubKeyInHoloNETDNA = true, string id = "")
        //{
        //    _updateDnaHashAndAgentPubKey = updateAgentPubKeyInHoloNETDNA;
        //    CallFunction(HoloNETRequestType.GenerateAgentPubKey, "generate_agent_pub_key", null, id);
        //}

        public void GenerateAgentPubKey(bool updateAgentPubKeyInHoloNETDNA = true, string id = "")
        {
            //var result = AsyncContext.Run(() => GenerateAgentPubKeyAsync(ConductorResponseCallBackMode.UseCallBackEvents, updateAgentPubKeyInHoloNETDNA, id));
            //GenerateAgentPubKeyAsync(ConductorResponseCallBackMode.UseCallBackEvents, updateAgentPubKeyInHoloNETDNA, id).Wait();
            //GenerateAgentPubKeyAsync(ConductorResponseCallBackMode.UseCallBackEvents, updateAgentPubKeyInHoloNETDNA, id).WaitAsync(new TimeSpan(0, 0, 3));
            //var task = Task.Run(async () => await GenerateAgentPubKeyAsync(ConductorResponseCallBackMode.UseCallBackEvents, updateAgentPubKeyInHoloNETDNA, id));

            //GenerateAgentPubKeyAsync(ConductorResponseCallBackMode.UseCallBackEvents, updateAgentPubKeyInHoloNETDNA, id).RunSynchronously();
            GenerateAgentPubKeyAsync(ConductorResponseCallBackMode.UseCallBackEvents, updateAgentPubKeyInHoloNETDNA, id).WaitAndUnwrapException();
        }

        public async Task<AppInstalledCallBackEventArgs> InstallAppAsync(string installedAppId, string hAppPath, string agentKey = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await InstallAppInternalAsync(installedAppId, hAppPath, null, agentKey, membraneProofs, network_seed, conductorResponseCallBackMode, id);
        }

        public void InstallApp(string agentKey, string installedAppId, string hAppPath, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, string id = null)
        {
            InstallAppInternal(agentKey, installedAppId, hAppPath, null, membraneProofs, network_seed, id);
        }

        public async Task<AppInstalledCallBackEventArgs> InstallAppAsync(string installedAppId, AppBundle appBundle, string agentKey = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await InstallAppInternalAsync(installedAppId, null, appBundle, agentKey, membraneProofs, network_seed, conductorResponseCallBackMode, id);
        }

        public AppInstalledCallBackEventArgs InstallApp(string installedAppId, AppBundle appBundle, string agentKey = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, string id = null)
        {
            return InstallAppInternalAsync(installedAppId, null, appBundle, agentKey, membraneProofs, network_seed, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AppUninstalledCallBackEventArgs> UninstallAppAsync(string installedAppId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            if (string.IsNullOrEmpty(id))
                id = GetRequestId();

            _uninstallingAppLookup[id] = installedAppId;

            return await CallFunctionAsync(HoloNETRequestType.AdminUninstallApp, "uninstall_app", new UninstallAppRequest()
            {
                installed_app_id = installedAppId
            }, _taskCompletionAppUninstalledCallBack, "OnAppUninstalledCallBack", conductorResponseCallBackMode, id);
        }

        public AppUninstalledCallBackEventArgs UninstallApp(string installedAppId, string id = null)
        {
            return UninstallAppAsync(installedAppId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AppEnabledCallBackEventArgs> EnableAppAsync(string installedAppId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminEnableApp, "enable_app", new EnableAppRequest()
            {
                installed_app_id = installedAppId
            }, _taskCompletionAppEnabledCallBack, "OnAppEnabledCallBack", conductorResponseCallBackMode, id);
        }

        public AppEnabledCallBackEventArgs EnablelApp(string installedAppId, string id = null)
        {
            return EnableAppAsync(installedAppId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AppDisabledCallBackEventArgs> DisableAppAsync(string installedAppId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            if (string.IsNullOrEmpty(id))
                id = GetRequestId();

            _disablingAppLookup[id] = installedAppId;

            return await CallFunctionAsync(HoloNETRequestType.AdminDisableApp, "disable_app", new EnableAppRequest()
            {
                installed_app_id = installedAppId
            }, _taskCompletionAppDisabledCallBack, "OnAppDisabledCallBack", conductorResponseCallBackMode, id);
        }

        public AppDisabledCallBackEventArgs DisableApp(string installedAppId, string id = null)
        {
            return DisableAppAsync(installedAppId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<ZomeCallCapabilityGrantedCallBackEventArgs> AuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync(string AgentPubKey, string DnaHash, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = "")
        {
            return await AuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync(GetCellId(DnaHash, AgentPubKey), capGrantAccessType, grantedFunctionsType, functions, conductorResponseCallBackMode, id);
        }

        public ZomeCallCapabilityGrantedCallBackEventArgs AuthorizeSigningCredentialsAndGrantZomeCallCapability(string AgentPubKey, string DnaHash, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, string id = "")
        {
            return AuthorizeSigningCredentialsAndGrantZomeCallCapability(GetCellId(DnaHash, AgentPubKey), capGrantAccessType, grantedFunctionsType, functions, id);
        }

        public async Task<ZomeCallCapabilityGrantedCallBackEventArgs> AuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync(byte[] AgentPubKey, byte[] DnaHash, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = "")
        {
            return await AuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync(GetCellId(DnaHash, AgentPubKey), capGrantAccessType, grantedFunctionsType, functions, conductorResponseCallBackMode, id);
        }

        public ZomeCallCapabilityGrantedCallBackEventArgs AuthorizeSigningCredentialsAndGrantZomeCallCapability(byte[] AgentPubKey, byte[] DnaHash, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, string id = "")
        {
            return AuthorizeSigningCredentialsAndGrantZomeCallCapability(GetCellId(DnaHash, AgentPubKey), capGrantAccessType, grantedFunctionsType, functions, id);
        }

        public async Task<ZomeCallCapabilityGrantedCallBackEventArgs> AuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync(byte[][] cellId, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = "")
        {
            (ZomeCallCapabilityGrantedCallBackEventArgs args, Dictionary<GrantedFunctionsType, List<(string, string)>> grantedFunctions, byte[] signingKey) = AuthorizeSigningCredentials(cellId, grantedFunctionsType, functions, id);

            if (!args.IsError)
            {
                return await CallFunctionAsync(HoloNETRequestType.AdminGrantZomeCallCapability, "grant_zome_call_capability", CreateGrantZomeCallCapabilityRequest(cellId, capGrantAccessType, grantedFunctions, signingKey),
                _taskCompletionZomeCapabilityGrantedCallBack, "OnZomeCallCapabilityGranted", conductorResponseCallBackMode, id);
            }
            else
                return args;
        }

        public ZomeCallCapabilityGrantedCallBackEventArgs AuthorizeSigningCredentialsAndGrantZomeCallCapability(byte[][] cellId, CapGrantAccessType capGrantAccessType, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, string id = "")
        {
            (ZomeCallCapabilityGrantedCallBackEventArgs args, Dictionary<GrantedFunctionsType, List<(string, string)>> grantedFunctions, byte[] signingKey) = AuthorizeSigningCredentials(cellId, grantedFunctionsType, functions, id);

            if (!args.IsError)
            {
                CallFunction(HoloNETRequestType.AdminGrantZomeCallCapability, "grant_zome_call_capability", CreateGrantZomeCallCapabilityRequest(cellId, capGrantAccessType, grantedFunctions, signingKey), id);
                return new ZomeCallCapabilityGrantedCallBackEventArgs() { EndPoint = EndPoint, Id = id, Message = "The call has been sent to the conductor.  Please wait for the event 'OnZomeCallCapabilityGranted' to view the response." };
            }
            else
                return args;
        }

        public byte[] GetCapGrantSecret(byte[][] cellId)
        {
            return _signingCredentialsForCell[$"{ConvertHoloHashToString(cellId[1])}:{ConvertHoloHashToString(cellId[0])}"].CapSecret;
        }

        public byte[] GetCapGrantSecret(string agentPubKey, string dnaHash)
        {
            return _signingCredentialsForCell[$"{agentPubKey}:{dnaHash}"].CapSecret;
        }

        public async Task<AppInterfaceAttachedCallBackEventArgs> AttachAppInterfaceAsync(UInt16? port = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminAttachAppInterface, "attach_app_interface", new AttachAppInterfaceRequest()
            {
                port = port
            }, _taskCompletionAppInterfaceAttachedCallBack, "OnAppInterfaceAttachedCallBack", conductorResponseCallBackMode, id);
        }

        public AppInterfaceAttachedCallBackEventArgs AttachAppInterface(UInt16? port = null, string id = null)
        {
            return AttachAppInterfaceAsync(port, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<DnaRegisteredCallBackEventArgs> RegisterDnaAsync(string path, string network_seed = null, object properties = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await RegisterDnaAsync(path, null, null, network_seed, properties, conductorResponseCallBackMode, id);
        }

        public DnaRegisteredCallBackEventArgs RegisterDna(string path, string network_seed = null, object properties = null, string id = null)
        {
            return RegisterDnaAsync(path, network_seed, properties, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<DnaRegisteredCallBackEventArgs> RegisterDnaAsync(byte[] hash, string network_seed = null, object properties = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await RegisterDnaAsync(null, null, hash, network_seed, properties, conductorResponseCallBackMode, id);
        }

        public DnaRegisteredCallBackEventArgs RegisterDna(byte[] hash, string network_seed = null, object properties = null, string id = null)
        {
            return RegisterDnaAsync(hash, network_seed, properties, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<DnaRegisteredCallBackEventArgs> RegisterDnaAsync(DnaBundle bundle, string network_seed = null, object properties = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await RegisterDnaAsync(null, bundle, null, network_seed, properties, conductorResponseCallBackMode, id);
        }

        public DnaRegisteredCallBackEventArgs RegisterDna(DnaBundle bundle, string network_seed = null, object properties = null, string id = null)
        {
            return RegisterDnaAsync(bundle, network_seed, properties, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<GetAppInfoCallBackEventArgs> GetAppInfoAsync(string installedAppId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            GetAppInfoCallBackEventArgs result = new GetAppInfoCallBackEventArgs();
            AppsListedCallBackEventArgs listAppsResult = await ListAppsAsync(AppStatusFilter.All);

            if (listAppsResult != null && !listAppsResult.IsError)
            {
                foreach (AppInfo app in listAppsResult.Apps)
                {
                    if (app.installed_app_id == installedAppId)
                    {
                        result.AppInfo = app;
                        break;
                    }
                }
            }

            if (result.AppInfo == null)
            {
                result.IsError = true;
                result.Message = "App Not Found";
            }

            return result;
        }

        public GetAppInfoCallBackEventArgs GetAppInfo(string installedAppId, string id = null)
        {
            return GetAppInfoAsync(installedAppId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AppsListedCallBackEventArgs> ListAppsAsync(AppStatusFilter appStatusFilter, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            _updateDnaHashAndAgentPubKey = false;

            return await CallFunctionAsync(HoloNETRequestType.AdminListApps, "list_apps", new ListAppsRequest()
            {
                status_filter = appStatusFilter != AppStatusFilter.All ? appStatusFilter : null
            }, _taskCompletionAppsListedCallBack, "OnAppsListedCallBack", conductorResponseCallBackMode, id);
        }

        public AppsListedCallBackEventArgs ListApps(AppStatusFilter appStatusFilter, string id = null)
        {
            return ListAppsAsync(appStatusFilter, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<DnasListedCallBackEventArgs> ListDnasAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminListDnas, "list_dnas", null, _taskCompletionDnasListedCallBack, "OnDnasListedCallBack", conductorResponseCallBackMode, id);
        }

        public DnasListedCallBackEventArgs ListDnas(string id = null)
        {
            return ListDnasAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<CellIdsListedCallBackEventArgs> ListCellIdsAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminListCellIds, "list_cell_ids", null, _taskCompletionCellIdsListedCallBack, "OnCellIdsListed", conductorResponseCallBackMode, id);
        }

        public CellIdsListedCallBackEventArgs ListCellIds(string id = null)
        {
            return ListCellIdsAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public async Task<AppInterfacesListedCallBackEventArgs> ListInterfacesAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminListAppInterfaces, "list_app_interfaces", null, _taskCompletionAppInterfacesListedCallBack, "OnAppInterfacesListedCallBack", conductorResponseCallBackMode, id);
        }

        public AppInterfacesListedCallBackEventArgs ListInterfaces(string id = null)
        {
            return ListInterfacesAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON.
        /// </summary>
        /// <param name="cellId">The cell id to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<FullStateDumpedCallBackEventArgs> DumpFullStateAsync(byte[][] cellId, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminDumpFullState, "dump_full_state", new DumpFullStateRequest()
            {
                cell_id = cellId,
                dht_ops_cursor = dHTOpsCursor
            }, _taskCompletionFullStateDumpedCallBack, "OnFullStateDumpedCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON.
        /// </summary>
        /// <param name="cellId">The cell id to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public FullStateDumpedCallBackEventArgs DumpFullState(byte[][] cellId, int? dHTOpsCursor = null, string id = null)
        {
            return DumpFullStateAsync(cellId, dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<FullStateDumpedCallBackEventArgs> DumpFullStateAsync(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await DumpFullStateAsync(GetCellId(dnaHash, agentPubKey), dHTOpsCursor, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public FullStateDumpedCallBackEventArgs DumpFullState(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, string id = null)
        {
            return DumpFullStateAsync(agentPubKey, dnaHash, dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON. This will dump the state for the current AgentPubKey/DnaHash stored in HoloNETDNA.AgentPubKey & HoloNETDNA.DnaHash. If there it is not stored in the HoloNETDNA it will automatically generate one for you and retrieve from the conductor.
        /// </summary>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<FullStateDumpedCallBackEventArgs> DumpFullStateAsync(int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await DumpFullStateAsync(await GetCellIdAsync(), dHTOpsCursor, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the full state of the specified cell, including its chain and DHT shard, as JSON. This will dump the state for the current AgentPubKey/DnaHash stored in HoloNETDNA.AgentPubKey & HoloNETDNA.DnaHash.
        /// </summary>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public FullStateDumpedCallBackEventArgs DumpFullState(int? dHTOpsCursor = null, string id = null)
        {
            return DumpFullStateAsync(dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON.
        /// </summary>
        /// <param name="cellId">The cell id to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<StateDumpedCallBackEventArgs> DumpStateAsync(byte[][] cellId, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminDumpState, "dump_state", new DumpStateRequest()
            {
                cell_id = cellId
            }, _taskCompletionStateDumpedCallBack, "OnStateDumpedCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON.
        /// </summary>
        /// <param name="cellId">The cell id to dump the full state for.</param>
        /// <param name="dHTOpsCursor">The last seen DhtOp RowId, returned in the full dump state. Only DhtOps with RowId greater than the cursor will be returned.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public StateDumpedCallBackEventArgs DumpState(byte[][] cellId, int? dHTOpsCursor = null, string id = null)
        {
            return DumpStateAsync(cellId, dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="dHTOpsCursor"></param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<StateDumpedCallBackEventArgs> DumpStateAsync(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await DumpStateAsync(GetCellId(dnaHash, agentPubKey), dHTOpsCursor, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="dHTOpsCursor"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public StateDumpedCallBackEventArgs DumpState(string agentPubKey, string dnaHash, int? dHTOpsCursor = null, string id = null)
        {
            return DumpStateAsync(agentPubKey, dnaHash, dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON. This will dump the state for the current AgentPubKey/DnaHash stored in HoloNETDNA.AgentPubKey & HoloNETDNA.DnaHash.
        /// </summary>
        /// <param name="dHTOpsCursor"></param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<StateDumpedCallBackEventArgs> DumpStateAsync(int? dHTOpsCursor = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await DumpStateAsync(await GetCellIdAsync(), dHTOpsCursor, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Dump the state of the specified cell, including its source chain, as JSON. This will dump the state for the current AgentPubKey/DnaHash stored in HoloNETDNA.AgentPubKey & HoloNETDNA.DnaHash.
        /// </summary>
        /// <param name="dHTOpsCursor"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public StateDumpedCallBackEventArgs DumpState(int? dHTOpsCursor = null, string id = null)
        {
            return DumpStateAsync(dHTOpsCursor, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }


        /// <summary>
        /// Get the DNA definition for the specified DNA hash.
        /// </summary>
        /// <param name="dnaHash">The hash of the dna.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<DnaDefinitionReturnedCallBackEventArgs> GetDnaDefinitionAsync(byte[] dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminGetDnaDefinition, "get_dna_definition", dnaHash, _taskCompletionDnaDefinitionReturnedCallBack, "OnDnaDefinitionReturnedCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Get the DNA definition for the specified DNA hash.
        /// </summary>
        /// <param name="dnaHash">The hash of the dna.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public DnaDefinitionReturnedCallBackEventArgs GetDnaDefinition(byte[] dnaHash, string id = null)
        {
            return GetDnaDefinitionAsync(dnaHash, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Get the DNA definition for the specified DNA hash.
        /// </summary>
        /// <param name="dnaHash">The hash of the dna.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<DnaDefinitionReturnedCallBackEventArgs> GetDnaDefinitionAsync(string dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await GetDnaDefinitionAsync(ConvertHoloHashToBytes(dnaHash), conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Get the DNA definition for the specified DNA hash.
        /// </summary>
        /// <param name="dnaHash">The hash of the dna.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public DnaDefinitionReturnedCallBackEventArgs GetDnaDefinition(string dnaHash, string id = null)
        {
            return GetDnaDefinitionAsync(dnaHash, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<CoordinatorsUpdatedCallBackEventArgs> UpdateCoordinatorsAsync(byte[] dnaHash, string path, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await UpdateCoordinatorsAsync(dnaHash, path, null, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="dnaHash"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public CoordinatorsUpdatedCallBackEventArgs UpdateCoordinators(byte[] dnaHash, string path, string id = null)
        {
            return UpdateCoordinatorsAsync(dnaHash, path, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<CoordinatorsUpdatedCallBackEventArgs> UpdateCoordinatorsAsync(string dnaHash, string path, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await UpdateCoordinatorsAsync(ConvertHoloHashToBytes(dnaHash), path, null, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="dnaHash"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public CoordinatorsUpdatedCallBackEventArgs UpdateCoordinators(string dnaHash, string path, string id = null)
        {
            return UpdateCoordinatorsAsync(dnaHash, path, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<CoordinatorsUpdatedCallBackEventArgs> UpdateCoordinatorsAsync(byte[] dnaHash, CoordinatorBundle bundle, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await UpdateCoordinatorsAsync(dnaHash, null, bundle, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="dnaHash"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public CoordinatorsUpdatedCallBackEventArgs UpdateCoordinators(byte[] dnaHash, CoordinatorBundle bundle, string id = null)
        {
            return UpdateCoordinatorsAsync(dnaHash, bundle, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<CoordinatorsUpdatedCallBackEventArgs> UpdateCoordinatorsAsync(string dnaHash, CoordinatorBundle bundle, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await UpdateCoordinatorsAsync(ConvertHoloHashToBytes(dnaHash), null, bundle, conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="dnaHash"></param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public CoordinatorsUpdatedCallBackEventArgs UpdateCoordinators(string dnaHash, CoordinatorBundle bundle, string id = null)
        {
            return UpdateCoordinatorsAsync(dnaHash, bundle, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }


        /// <summary>
        /// Request all available info about an agent.
        /// </summary>
        /// <param name="cellId">The cell id to retrive the angent info for.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AgentInfoReturnedCallBackEventArgs> GetAgentInfoAsync(byte[][] cellId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminAgentInfo, "agent_info", new GetAgentInfoRequest()
            {
                cell_id = cellId
            }, _taskCompletionAgentInfoReturnedCallBack, "OnAgentInfoReturnedCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Request all available info about an agent.
        /// </summary>
        /// <param name="cellId">The cell id to retrive the angent info for.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AgentInfoReturnedCallBackEventArgs GetAgentInfo(byte[][] cellId, string id = null)
        {
            return GetAgentInfoAsync(cellId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Request all available info about an agent.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AgentInfoReturnedCallBackEventArgs> GetAgentInfoAsync(string agentPubKey, string dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await GetAgentInfoAsync(GetCellId(dnaHash, agentPubKey), conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Request all available info about an agent.
        /// </summary>
        /// <param name="agentPubKey">The AgentPubKey for the cell to dump the full state for.</param>
        /// <param name="dnaHash">The DnaHash for the cell to dump the full state for.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AgentInfoReturnedCallBackEventArgs GetAgentInfo(string agentPubKey, string dnaHash, string id = null)
        {
            return GetAgentInfoAsync(agentPubKey, dnaHash, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Request all available info about an agent. This will retreive info for the current AgentPubKey/DnaHash stored in HoloNETDNA.AgentPubKey & HoloNETDNA.DnaHash.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AgentInfoReturnedCallBackEventArgs> GetAgentInfoAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await GetAgentInfoAsync(await GetCellIdAsync(), conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Request all available info about an agent. This will retreive info for the current AgentPubKey/DnaHash stored in HoloNETDNA.AgentPubKey & HoloNETDNA.DnaHash.
        /// </summary>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AgentInfoReturnedCallBackEventArgs GetAgentInfo(string id = null)
        {
            return GetAgentInfoAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        ///  Add existing agent(s) to Holochain.
        /// </summary>
        /// <param name="agentInfos">The agentInfo's to add.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<AgentInfoAddedCallBackEventArgs> AddAgentInfoAsync(AgentInfo[] agentInfos, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminAddAgentInfo, "add_agent_info", new AddAgentInfoRequest()
            {
                agent_infos = agentInfos
            }, _taskCompletionAgentInfoAddedCallBack, "OnAgentInfoAddedCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Add existing agent(s) to Holochain.
        /// </summary>
        /// <param name="agentInfos">The agentInfo's to add.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public AgentInfoAddedCallBackEventArgs AddAgentInfo(AgentInfo[] agentInfos, string id = null)
        {
            return AddAgentInfoAsync(agentInfos, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="roleName">The clone id (string/rolename).</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<CloneCellDeletedCallBackEventArgs> DeleteCloneCellAsync(string appId, string roleName, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminDeleteClonedCell, "delete_clone_cell", new DeleteCloneCellRequest()
            {
                app_id = appId,
                clone_cell_id = roleName
            }, _taskCompletionCloneCellDeletedCallBack, "OnCloneCellDeletedCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="cellId"> The cell id of the cloned cell.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public CloneCellDeletedCallBackEventArgs DeleteCloneCell(string appId, string roleName, string id = null)
        {
            return DeleteCloneCellAsync(appId, roleName, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="cellId"> The cell id of the cloned cell.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<CloneCellDeletedCallBackEventArgs> DeleteCloneCellAsync(string appId, byte[][] cellId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminDeleteClonedCell, "delete_clone_cell", new DeleteCloneCellRequest()
            {
                app_id = appId,
                clone_cell_id = cellId
            }, _taskCompletionCloneCellDeletedCallBack, "OnCloneCellDeletedCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="cellId"> The cell id of the cloned cell.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public CloneCellDeletedCallBackEventArgs DeleteCloneCell(string appId, byte[][] cellId, string id = null)
        {
            return DeleteCloneCellAsync(appId, cellId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="agentPubKey">The AgentPubKey for the cell.</param>
        /// <param name="dnaHash">The DnaHash for the cell.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<CloneCellDeletedCallBackEventArgs> DeleteCloneCellAsync(string appId, string agentPubKey, string dnaHash, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await DeleteCloneCellAsync(appId, GetCellId(dnaHash, agentPubKey), conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Delete a clone cell that was previously disabled.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="agentPubKey">The AgentPubKey for the cell.</param>
        /// <param name="dnaHash">The DnaHash for the cell.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public CloneCellDeletedCallBackEventArgs DeleteCloneCell(string appId, string agentPubKey, string dnaHash, string id = null)
        {
            return DeleteCloneCellAsync(appId, agentPubKey, dnaHash, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Delete a clone cell that was previously disabled. This will use the current AgentPubKey/DnaHash stored in HoloNETDNA.AgentPubKey & HoloNETDNA.DnaHash.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<CloneCellDeletedCallBackEventArgs> DeleteCloneCellAsync(string appId, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await DeleteCloneCellAsync(appId, await GetCellIdAsync(), conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Delete a clone cell that was previously disabled. This will use the current AgentPubKey/DnaHash stored in HoloNETDNA.AgentPubKey & HoloNETDNA.DnaHash.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public CloneCellDeletedCallBackEventArgs DeleteCloneCell(string appId, string id = null)
        {
            return DeleteCloneCellAsync(appId, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Get'the storgage info used by hApps.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<StorageInfoReturnedCallBackEventArgs> GetStorageInfoAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminStorageInfo, "storage_info", null, _taskCompletionStorageInfoReturnedCallBack, "OnStorageInfoReturnedCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        /// Get'the storgage info used by hApps.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="cloneCellId"> The clone id or cell id of the clone cell. Can be RoleName (string) or CellId (byte[][]).</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public StorageInfoReturnedCallBackEventArgs GetStorageInfo(string id = null)
        {
            return GetStorageInfoAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump raw json network statistics from the backend networking lib.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<NetworkStatsDumpedCallBackEventArgs> DumpNetworkStatsAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminDumpNetworkStats, "dump_network_stats", null, _taskCompletionNetworkStatsDumpedCallBack, "OnNetworkStatsDumpedCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Dump raw json network statistics from the backend networking lib.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="cloneCellId"> The clone id or cell id of the clone cell. Can be RoleName (string) or CellId (byte[][]).</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public NetworkStatsDumpedCallBackEventArgs DumpNetworkStats(string id = null)
        {
            return DumpNetworkStatsAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// Dump the network metrics tracked by kitsune.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<NetworkMetricsDumpedCallBackEventArgs> DumpNetworkMetricsAsync(ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminDumpNetworkMetrics, "dump_network_metrics", null, _taskCompletionNetworkMetricsDumpedCallBack, "OnNetworkMetricsDumpedCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Dump the network metrics tracked by kitsune.
        /// </summary>
        /// <param name="appId">The app id that the clone cell belongs to.</param>
        /// <param name="cloneCellId"> The clone id or cell id of the clone cell. Can be RoleName (string) or CellId (byte[][]).</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public NetworkMetricsDumpedCallBackEventArgs DumpNetworkMetrics(string id = null)
        {
            return DumpNetworkMetricsAsync(ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        /// <summary>
        /// “Graft” record's onto the source chain of the specified CellId.
        /// </summary>
        /// <param name="cellId">The cell that the records are being inserted into.</param>
        /// <param name="validate">If this is true, then the records will be validated before insertion. This is much slower but is useful for verifying the chain is valid. If this is false, then records will be inserted as is. This could lead to an invalid chain.</param>
        /// <param name="records">The records to be inserted into the source chain.</param>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public async Task<RecordsGraftedCallBackEventArgs> GraftRecordsAsync(byte[][] cellId, bool validate, object[] records, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminGraftRecords, "graft_records", new GraftRecordsRequest()
            {
                 cell_id = cellId,
                 validate = validate,
                 records = records
            }, _taskCompletionRecordsGraftedCallBack, "OnRecordsGraftedCallBack", conductorResponseCallBackMode, id);
        }

        /// <summary>
        ///  Dump the network metrics tracked by kitsune.
        /// </summary>
        /// <param name="cellId">The cell that the records are being inserted into.</param>
        /// <param name="validate">If this is true, then the records will be validated before insertion. This is much slower but is useful for verifying the chain is valid. If this is false, then records will be inserted as is. This could lead to an invalid chain.</param>
        /// <param name="records">The records to be inserted into the source chain.</param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        public RecordsGraftedCallBackEventArgs GraftRecords(byte[][] cellId, bool validate, object[] records, string id = null)
        {
            return GraftRecordsAsync(cellId, validate, records, ConductorResponseCallBackMode.UseCallBackEvents, id).Result;
        }

        public override async Task<byte[][]> GetCellIdAsync()
        {
            if (string.IsNullOrEmpty(HoloNETDNA.AgentPubKey))
                await GenerateAgentPubKeyAsync();

            return await base.GetCellIdAsync();
        }

        private async Task<AppInstalledCallBackEventArgs> InstallAppInternalAsync(string installedAppId, string hAppPath = null, AppBundle appBundle = null, string agentKey = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            if (string.IsNullOrEmpty(agentKey))
            {
                if (string.IsNullOrEmpty(HoloNETDNA.AgentPubKey))
                    await GenerateAgentPubKeyAsync();

                agentKey = HoloNETDNA.AgentPubKey;
            }

            if (membraneProofs == null)
                membraneProofs = new Dictionary<string, byte[]>();

            return await CallFunctionAsync(HoloNETRequestType.AdminInstallApp, "install_app", new InstallAppRequest()
            {
                path = hAppPath,
                bundle = appBundle,
                agent_key = ConvertHoloHashToBytes(agentKey),
                installed_app_id = installedAppId,
                membrane_proofs = membraneProofs,
                network_seed = network_seed
            }, _taskCompletionAppInstalledCallBack, "OnAppInstalledCallBack", conductorResponseCallBackMode, id);
        }

        Dictionary<string, string> _installingAppId = new Dictionary<string, string>();

        private void InstallAppInternal(string agentKey, string installedAppId, string hAppPath = null, AppBundle appBundle = null, Dictionary<string, byte[]> membraneProofs = null, string network_seed = null, string id = null)
        {
            if (string.IsNullOrEmpty(agentKey))
            {
                if (string.IsNullOrEmpty(HoloNETDNA.AgentPubKey))
                {
                    //TODO: Later we may want to add the same functionality in the async version to automatically retreive the agentPubKey but for non async version would require a little more work to store the values passed in in a dictionary keyed by id (id would need to be generated first).

                    if (string.IsNullOrEmpty(id))
                        id = GetRequestId();

                    _installingAppId[id] = installedAppId;

                    //    //_installingApp = true;
                    //    //_installedAppId = installedAppId;
                    //    //_hAppPath = hAppPath;
                    //    //_appBundle = appBundle;
                    //    //_membraneProofs
                    //    GenerateAgentPubKey();
                }

                agentKey = HoloNETDNA.AgentPubKey;
            }

            if (!string.IsNullOrEmpty(agentKey))
            {
                if (membraneProofs == null)
                    membraneProofs = new Dictionary<string, byte[]>();

                CallFunction(HoloNETRequestType.AdminInstallApp, "install_app", new InstallAppRequest()
                {
                    path = hAppPath,
                    bundle = appBundle,
                    agent_key = ConvertHoloHashToBytes(agentKey),
                    installed_app_id = installedAppId,
                    membrane_proofs = membraneProofs,
                    network_seed = network_seed
                }, id);
            }
        }

        private async Task<DnaRegisteredCallBackEventArgs> RegisterDnaAsync(string path, DnaBundle bundle, byte[] hash, string network_seed = null, object properties = null, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminRegisterDna, "register_dna", new RegisterDnaRequest()
            {
                path = path,
                bundle = bundle,
                hash = hash,
                modifiers = new DnaModifiers()
                {
                    network_seed = network_seed,
                    properties = properties
                }
            }, _taskCompletionDnaRegisteredCallBack, "OnDnaRegisteredCallBack", conductorResponseCallBackMode, id);
        }

        private GrantZomeCallCapabilityRequest CreateGrantZomeCallCapabilityRequest(byte[][] cellId, CapGrantAccessType capGrantAccessType, Dictionary<GrantedFunctionsType, List<(string, string)>> grantedFunctions, byte[] signingKey)
        {
            byte[] secret = _signingCredentialsForCell[$"{ConvertHoloHashToString(cellId[1])}:{ConvertHoloHashToString(cellId[0])}"].CapSecret;

            GrantZomeCallCapabilityRequest request = new GrantZomeCallCapabilityRequest()
            {
                cell_id = cellId,
                cap_grant = new ZomeCallCapGrant()
                {
                    tag = "zome-call-signing-key",
                    functions = grantedFunctions,
                }
            };

            switch (capGrantAccessType)
            {
                case CapGrantAccessType.Assigned:
                    {
                        request.cap_grant.access = new CapGrantAccessAssigned()
                        {
                            Assigned = new CapGrantAccessAssignedDetails()
                            {
                                secret = secret,
                                assignees = new byte[1][] { signingKey }
                            }
                        };
                    }
                    break;

                case CapGrantAccessType.Unrestricted:
                    {
                        request.cap_grant.access = new CapGrantAccessUnrestricted()
                        {
                            Unrestricted = null
                        };
                    }
                    break;

                case CapGrantAccessType.Transferable:
                    {
                        request.cap_grant.access = new CapGrantAccessTransferable()
                        {
                            Transferable = new CapGrantAccessTransferableDetails()
                            {
                                secret = secret
                            }
                        };
                    }
                    break;
            }

            return request;
        }

        private (ZomeCallCapabilityGrantedCallBackEventArgs, Dictionary<GrantedFunctionsType, List<(string, string)>>, byte[]) AuthorizeSigningCredentials(byte[][] cellId, GrantedFunctionsType grantedFunctionsType, List<(string, string)> functions = null, string id = "")
        {
            if (cellId == null)
            {
                string msg = "Error occured in AuthorizeSigningCredentialsAsync function. cellId is null.";
                HandleError(msg, null);
                return (new ZomeCallCapabilityGrantedCallBackEventArgs() { IsError = true, EndPoint = EndPoint, Id = id, Message = msg }, null, null);
            }

            if (string.IsNullOrEmpty(HoloNETDNA.AgentPubKey))
            {
                string msg = "Error occured in AuthorizeSigningCredentialsAsync function. HoloNETDNA.AgentPubKey is null. Please set or call GenerateAgentPubKey method.";
                HandleError(msg, null);
                return (new ZomeCallCapabilityGrantedCallBackEventArgs() { IsError = true, EndPoint = EndPoint, Id = id, Message = msg }, null, null);
            }

            if (grantedFunctionsType == GrantedFunctionsType.Listed && functions == null)
            {
                string msg = "Error occured in AuthorizeSigningCredentialsAsync function. GrantedFunctionsType was set to Listed but no functions were passed in.";
                HandleError(msg, null);
                return (new ZomeCallCapabilityGrantedCallBackEventArgs() { IsError = true, EndPoint = EndPoint, Id = id, Message = msg }, null, null);
            }

            Sodium.KeyPair pair = Sodium.PublicKeyAuth.GenerateKeyPair(RandomNumberGenerator.GetBytes(32));
            //byte[] DHTLocation = ConvertHoloHashToBytes(HoloNETDNA.AgentPubKey).TakeLast(4).ToArray();
            //byte[] signingKey = new byte[] { 132, 32, 36 }.Concat(pair.PublicKey).Concat(DHTLocation).ToArray();

            var signingKey = new byte[39];
            Buffer.BlockCopy(new byte[3] { 132, 32, 36 }, 0, signingKey, 0, 3);
            Buffer.BlockCopy(pair.PublicKey, 0, signingKey, 3, 32);
            Buffer.BlockCopy(new byte[4] { 0, 0, 0, 0 }, 0, signingKey, 35, 4);

            Dictionary<GrantedFunctionsType, List<(string, string)>> grantedFunctions = new Dictionary<GrantedFunctionsType, List<(string, string)>>();

            if (grantedFunctionsType == GrantedFunctionsType.All)
                grantedFunctions[GrantedFunctionsType.All] = null;
            else
                grantedFunctions[GrantedFunctionsType.Listed] = functions;

            //_signingCredentialsForCell[cellId] = new SigningCredentials()
            //_signingCredentialsForCell[$"{HoloNETDNA.AgentPubKey}:{HoloNETDNA.DnaHash}"] = new SigningCredentials()
            _signingCredentialsForCell[$"{ConvertHoloHashToString(cellId[1])}:{ConvertHoloHashToString(cellId[0])}"] = new SigningCredentials()
            {
                CapSecret = SodiumCore.GetRandomBytes(64), //RandomNumberGenerator.GetBytes(64),
                KeyPair = new Data.Admin.Requests.Objects.KeyPair() { PrivateKey = pair.PrivateKey, PublicKey = pair.PublicKey },
                SigningKey = signingKey
            };

            return (new ZomeCallCapabilityGrantedCallBackEventArgs(), grantedFunctions, signingKey);
        }

        /// <summary>
        /// Update the coordinataor zomes.
        /// </summary>
        /// <param name="conductorResponseCallBackMode">The Concuctor Response CallBack Mode, set this to 'WaitForHolochainConductorResponse' if you want the function to wait for the Holochain Conductor response before returning that response or set it to 'UseCallBackEvents' to return from the function immediately and then raise the 'OnDumpFullStateCallBack' event when the conductor responds.   </param>
        /// <param name="id">The request id, leave null if you want HoloNET to manage this for you.</param>
        /// <returns></returns>
        private async Task<CoordinatorsUpdatedCallBackEventArgs> UpdateCoordinatorsAsync(byte[] dnaHash, string path, CoordinatorBundle bundle, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null)
        {
            return await CallFunctionAsync(HoloNETRequestType.AdminUpdateCoordinators, "update_coordinators", new UpdateCoordinatorsRequest()
            {
                dnaHash = dnaHash,
                path = path,
                bundle = bundle
            }, _taskCompletionCoordinatorsUpdatedCallBack, "OnCoordinatorsUpdatedCallBack", conductorResponseCallBackMode, id);
        }

        private async Task<T> CallFunctionAsync<T>(HoloNETRequestType requestType, string holochainConductorFunctionName, dynamic holoNETDataDetailed, Dictionary<string, TaskCompletionSource<T>> _taskCompletionCallBack, string eventCallBackName, ConductorResponseCallBackMode conductorResponseCallBackMode = ConductorResponseCallBackMode.WaitForHolochainConductorResponse, string id = null) where T : HoloNETDataReceivedBaseEventArgs, new()
        {
            HoloNETData holoNETData = new HoloNETData()
            {
                type = holochainConductorFunctionName,
                data = holoNETDataDetailed
            };

            if (string.IsNullOrEmpty(id))
                id = GetRequestId();

            _taskCompletionCallBack[id] = new TaskCompletionSource<T> { };
            await SendHoloNETRequestAsync(holoNETData, requestType, id);

            if (conductorResponseCallBackMode == ConductorResponseCallBackMode.WaitForHolochainConductorResponse)
            {
                Task<T> returnValue = _taskCompletionCallBack[id].Task;
                return await returnValue;
            }
            else
                return new T() { EndPoint = EndPoint, Id = id, Message = $"conductorResponseCallBackMode is set to UseCallBackEvents so please wait for {eventCallBackName} event for the result." };
        }

        private void CallFunction(HoloNETRequestType requestType, string holochainConductorFunctionName, dynamic holoNETDataDetailed, string id = null)
        {
            HoloNETData holoNETData = new HoloNETData()
            {
                type = holochainConductorFunctionName,
                data = holoNETDataDetailed
            };

            if (string.IsNullOrEmpty(id))
                id = GetRequestId();

            //SendHoloNETRequest(holoNETData, HoloNETRequestType.ZomeCall, id);
            SendHoloNETRequest(holoNETData, requestType, id);
            //return new T() { EndPoint = EndPoint, Id = id, Message = $"conductorResponseCallBackMode is set to UseCallBackEvents so please wait for {eventCallBackName} event for the result." };
        }
    }
}
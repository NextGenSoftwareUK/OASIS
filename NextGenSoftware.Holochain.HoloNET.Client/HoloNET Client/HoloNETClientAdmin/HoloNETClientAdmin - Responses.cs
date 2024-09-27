using System;
using MessagePack;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Logging;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public partial class HoloNETClientAdmin : HoloNETClientBase, IHoloNETClientAdmin
    {
        protected override IHoloNETResponse ProcessDataReceived(WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            IHoloNETResponse response = null;

            try
            {
                response = base.ProcessDataReceived(dataReceivedEventArgs);

                if (response != null)
                {
                    if (!response.IsError)
                    {
                        switch (response.HoloNETResponseType)
                        {
                            case HoloNETResponseType.AdminAgentPubKeyGenerated:
                                DecodeAgentPubKeyGeneratedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminAppInstalled:
                                DecodeAppInstalledReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminAppUninstalled:
                                DecodeAppUninstalledReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminAppEnabled:
                                DecodeAppEnabledReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminAppDisabled:
                                DecodeAppDisabledReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminZomeCallCapabilityGranted:
                                DecodeZomeCallCapabilityGrantedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminAppInterfaceAttached:
                                DecodeAppInterfaceAttachedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminDnaRegistered:
                                DecodeDnaRegisteredReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminDnaDefinitionReturned:
                                DecodeDnaDefinitionReturned(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminAppInterfacesListed:
                                DecodeAppInterfacesListedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminAppsListed:
                                DecodeAppsListedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminDnasListed:
                                DecodeDnasListedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminCellIdsListed:
                                DecodeCellIdsListedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminAgentInfoReturned:
                                DecodeAgentInfoReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminAgentInfoAdded:
                                DecodeAgentInfoAddedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminCoordinatorsUpdated:
                                DecodeCoordinatorsUpdatedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminCloneCellDeleted:
                                DecodeCloneCellDeletedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminStateDumped:
                                DecodeStateDumpedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminFullStateDumped:
                                DecodeStateDumpedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminNetworkMetricsDumped:
                                DecodeNetworkMetricsDumpedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminNetworkStatsDumped:
                                DecodeNetworkStatsDumpedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminStorageInfoReturned:
                                DecodeStorageInfoReturned(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminRecordsGrafted:
                                DecodeRecordsGraftedReceived(response, dataReceivedEventArgs);
                                break;

                            case HoloNETResponseType.AdminAdminInterfacesAdded:
                                DecodeAdminInterfacesAddedReceived(response, dataReceivedEventArgs);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = "Error in HoloNETClient.ProcessDataReceived method.";
                HandleError(msg, ex);
            }

            return response;
        }

        protected override string ProcessErrorReceivedFromConductor(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string msg = base.ProcessErrorReceivedFromConductor(response, dataReceivedEventArgs);

            if (response != null && response.id > 0 && _requestTypeLookup != null && _requestTypeLookup.ContainsKey(response.id.ToString()))
            {
                switch (_requestTypeLookup[response.id.ToString()])
                {
                    case HoloNETRequestType.AdminGenerateAgentPubKey:
                        RaiseAgentPubKeyGeneratedEvent(ProcessResponeError<AgentPubKeyGeneratedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminGenerateAgentPubKey", msg));
                        break;

                    case HoloNETRequestType.AdminInstallApp:
                        RaiseAppInstalledEvent(ProcessResponeError<AppInstalledCallBackEventArgs>(response, dataReceivedEventArgs, "AdminInstallApp", msg));
                        break;

                    case HoloNETRequestType.AdminUninstallApp:
                        RaiseAppUninstalledEvent(ProcessResponeError<AppUninstalledCallBackEventArgs>(response, dataReceivedEventArgs, "AdminUninstallApp", msg));
                        break;

                    case HoloNETRequestType.AdminEnableApp:
                        RaiseAppEnabledEvent(ProcessResponeError<AppEnabledCallBackEventArgs>(response, dataReceivedEventArgs, "AdminEnableApp", msg));
                        break;

                    case HoloNETRequestType.AdminDisableApp:
                        RaiseAppDisabledEvent(ProcessResponeError<AppDisabledCallBackEventArgs>(response, dataReceivedEventArgs, "AdminDisableApp", msg));
                        break;

                    case HoloNETRequestType.AdminGrantZomeCallCapability:
                        RaiseZomeCallCapabilityGrantedEvent(ProcessResponeError<ZomeCallCapabilityGrantedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminGrantZomeCallCapability", msg));
                        break;

                    case HoloNETRequestType.AdminAttachAppInterface:
                        RaiseAppInterfaceAttachedEvent(ProcessResponeError<AppInterfaceAttachedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminGrantZomeCallCapability", msg));
                        break;

                    case HoloNETRequestType.AdminRegisterDna:
                        RaiseDnaRegisteredEvent(ProcessResponeError<DnaRegisteredCallBackEventArgs>(response, dataReceivedEventArgs, "AdminRegisterDna", msg));
                        break;

                    case HoloNETRequestType.AdminListAppInterfaces:
                        RaiseAppInterfacesListedEvent(ProcessResponeError<AppInterfacesListedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminListAppInterfaces", msg));
                        break;

                    case HoloNETRequestType.AdminListApps:
                        RaiseAppsListedEvent(ProcessResponeError<AppsListedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminListApps", msg));
                        break;

                    case HoloNETRequestType.AdminListDnas:
                        RaiseDnasListedEvent(ProcessResponeError<DnasListedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminListDnas", msg));
                        break;

                    case HoloNETRequestType.AdminListCellIds:
                        RaiseCellIdsListedEvent(ProcessResponeError<CellIdsListedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminListCellIds", msg));
                        break;

                    case HoloNETRequestType.AdminAgentInfo:
                        RaiseAgentInfoReturnedEvent(ProcessResponeError<AgentInfoReturnedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminAgentInfo", msg));
                        break;

                    case HoloNETRequestType.AdminAddAgentInfo:
                        RaiseAgentInfoAddedEvent(ProcessResponeError<AgentInfoAddedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminAddAgentInfo", msg));
                        break;

                    case HoloNETRequestType.AdminUpdateCoordinators:
                        RaiseCoordinatorsUpdatedEvent(ProcessResponeError<CoordinatorsUpdatedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminUpdateCoordinators", msg));
                        break;

                    case HoloNETRequestType.AdminDeleteClonedCell:
                        RaiseCloneCellDeletedEvent(ProcessResponeError<CloneCellDeletedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminDeleteClonedCell", msg));
                        break;

                    case HoloNETRequestType.AdminDumpState:
                        RaiseStateDumpedEvent(ProcessResponeError<StateDumpedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminDumpState", msg));
                        break;

                    case HoloNETRequestType.AdminDumpFullState:
                        RaiseFullStateDumpedEvent(ProcessResponeError<FullStateDumpedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminDumpFullState", msg));
                        break;

                    case HoloNETRequestType.AdminDumpNetworkMetrics:
                        RaiseNetworkMetricsDumpedEvent(ProcessResponeError<NetworkMetricsDumpedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminDumpNetworkMetrics", msg));
                        break;

                    case HoloNETRequestType.AdminDumpNetworkStats:
                        RaiseNetworkStatsDumpedEvent(ProcessResponeError<NetworkStatsDumpedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminDumpNetworkStats", msg));
                        break;

                    case HoloNETRequestType.AdminStorageInfo:
                        RaiseStorageInfoReturnedEvent(ProcessResponeError<StorageInfoReturnedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminStorageInfo", msg));
                        break;

                    case HoloNETRequestType.AdminGraftRecords:
                        RaiseRecordsGraftedEvent(ProcessResponeError<RecordsGraftedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminGraftRecords", msg));
                        break;

                    case HoloNETRequestType.AdminAddAdminInterfaces:
                        RaiseAdminInterfacesAddedEvent(ProcessResponeError<AdminInterfacesAddedCallBackEventArgs>(response, dataReceivedEventArgs, "AdminAddAdminInterfaces", msg));
                        break;     
                }
            }

            return msg;
        }

        private void DecodeAgentPubKeyGeneratedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            AgentPubKeyGeneratedCallBackEventArgs args = CreateHoloNETArgs<AgentPubKeyGeneratedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminAgentPubKeyGenerated;

            try
            {
                Logger.Log("ADMIN: AGENT PUB KEY GENERATED DATA DETECTED\n", LogType.Info);
                AppResponse appResponse = MessagePackSerializer.Deserialize<AppResponse>(response.data, messagePackSerializerOptions);
                args.AgentPubKey = ConvertHoloHashToString(appResponse.data);
                args.AppResponse = appResponse;

                Logger.Log($"AGENT PUB KEY GENERATED: {args.AgentPubKey}\n", LogType.Info);

                if (_updateDnaHashAndAgentPubKey)
                    HoloNETDNA.AgentPubKey = args.AgentPubKey;
            }
            catch (Exception ex)
            {
                HandleError(args, $"An unknown error occurred in HoloNETClient.DecodeAgentPubKeyGeneratedReceived. Reason: {ex}");
            }

            RaiseAgentPubKeyGeneratedEvent(args);
        }

        private void DecodeAppInstalledReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            AppInstalledCallBackEventArgs args = new AppInstalledCallBackEventArgs();
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeAppInstalledReceived. Reason: ";
            args.HoloNETResponseType = HoloNETResponseType.AdminAppInstalled;

            try
            {
                Logger.Log("ADMIN: APP INSTALLED DATA DETECTED\n", LogType.Info);

                AppInfoResponse appInfoResponse = MessagePackSerializer.Deserialize<AppInfoResponse>(response.data, messagePackSerializerOptions);
                args = CreateHoloNETArgs<AppInstalledCallBackEventArgs>(response, dataReceivedEventArgs);

                if (appInfoResponse != null)
                {
                    appInfoResponse.data = ProcessAppInfo(appInfoResponse.data, args);
                    args.AppInfoResponse = appInfoResponse;
                }
                else
                    HandleError(args, $"{errorMessage} appInfoResponse is null.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseAppInstalledEvent(args);
        }

        private void DecodeAppUninstalledReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            AppUninstalledCallBackEventArgs args = new AppUninstalledCallBackEventArgs();
            args.HoloNETResponseType = HoloNETResponseType.AdminAppUninstalled;

            try
            {
                Logger.Log("ADMIN: APP UNINSTALLED DATA DETECTED\n", LogType.Info);
                args = CreateHoloNETArgs<AppUninstalledCallBackEventArgs>(response, dataReceivedEventArgs);

                if (_uninstallingAppLookup != null && _uninstallingAppLookup.ContainsKey(response.id.ToString()))
                {
                    args.InstalledAppId = _uninstallingAppLookup[response.id.ToString()];
                    _uninstallingAppLookup.Remove(response.id.ToString());
                }
            }
            catch (Exception ex)
            {
                HandleError(args, $"An unknown error occurred in HoloNETClient.DecodeAppUninstalledReceived. Reason: {ex}");
            }

            RaiseAppUninstalledEvent(args);
        }

        private void DecodeAppEnabledReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeAppEnabledReceived. Reason: ";
            AppEnabledCallBackEventArgs args = CreateHoloNETArgs<AppEnabledCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminAppEnabled;

            try
            {
                Logger.Log("ADMIN: APP ENABLED DATA DETECTED\n", LogType.Info);
                EnableAppResponse enableAppResponse = MessagePackSerializer.Deserialize<EnableAppResponse>(response.data, messagePackSerializerOptions);

                if (enableAppResponse != null)
                {
                    enableAppResponse.data.app = ProcessAppInfo(enableAppResponse.data.app, args);
                    args.AppInfoResponse = new AppInfoResponse() { data = enableAppResponse.data.app };
                    args.Errors = enableAppResponse.data.errors; //TODO: Need to find out what this contains and the correct data structure.
                }
                else
                {
                    HandleError(args, $"{errorMessage} An error occurred deserialzing EnableAppResponse from the Holochain Conductor.");
                }
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseAppEnabledEvent(args);
        }

        private void DecodeAppDisabledReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeAppEnabledReceived. Reason: ";
            AppDisabledCallBackEventArgs args = CreateHoloNETArgs<AppDisabledCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminAppDisabled;

            try
            {
                Logger.Log("ADMIN: APP DISABLED DATA DETECTED\n", LogType.Info);

                if (_disablingAppLookup != null && _disablingAppLookup.ContainsKey(response.id.ToString()))
                {
                    args.InstalledAppId = _disablingAppLookup[response.id.ToString()];
                    _disablingAppLookup.Remove(response.id.ToString());
                }
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseAppDisabledEvent(args);
        }

        private void DecodeZomeCallCapabilityGrantedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeZomeCallCapabilityGrantedReceived. Reason: ";
            ZomeCallCapabilityGrantedCallBackEventArgs args = CreateHoloNETArgs<ZomeCallCapabilityGrantedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminAppInterfaceAttached;

            try
            {
                Logger.Log("ADMIN: ZOME CALL CAPABILITY GRANTED\n", LogType.Info);
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseZomeCallCapabilityGrantedEvent(args);
        }

        private void DecodeAppInterfaceAttachedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeAppInterfaceAttachedReceived. Reason: ";
            AppInterfaceAttachedCallBackEventArgs args = CreateHoloNETArgs<AppInterfaceAttachedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminAppInterfaceAttached;

            try
            {
                Logger.Log("ADMIN: APP INTERFACE ATTACHED\n", LogType.Info);
                //object attachAppInterfaceResponse = MessagePackSerializer.Deserialize<object>(response.data, messagePackSerializerOptions);
                AppResponse appResponse = MessagePackSerializer.Deserialize<AppResponse>(response.data, messagePackSerializerOptions);

                if (appResponse != null)
                {
                    args.Port = Convert.ToUInt16(appResponse.data["port"]);

                    //AttachAppInterfaceResponse attachAppInterfaceResponse = MessagePackSerializer.Deserialize<AttachAppInterfaceResponse>(appResponse.data, messagePackSerializerOptions);
                    //attachAppInterfaceResponse.Port = attachAppInterfaceResponse.Port;
                }
                else
                    HandleError(args, $"{errorMessage} Error occured in HoloNETClient.DecodeAppInterfaceAttachedReceived. attachAppInterfaceResponse is null.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseAppInterfaceAttachedEvent(args);
        }

        private void DecodeDnaRegisteredReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeDnaRegisteredReceived. Reason: ";
            DnaRegisteredCallBackEventArgs args = CreateHoloNETArgs<DnaRegisteredCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminDnaRegistered;

            try
            {
                Logger.Log("ADMIN: DNA REGISTERED\n", LogType.Info);
                byte[] responseData = MessagePackSerializer.Deserialize<byte[]>(response.data, messagePackSerializerOptions);

                if (responseData != null)
                    args.HoloHash = responseData;
                else
                    HandleError(args, $"{errorMessage} ResponseData failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseDnaRegisteredEvent(args);
        }

        private void DecodeDnaDefinitionReturned(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeDnaDefinitionReturned. Reason: ";
            DnaDefinitionReturnedCallBackEventArgs args = CreateHoloNETArgs<DnaDefinitionReturnedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminDnaDefinitionReturned;

            try
            {
                Logger.Log("ADMIN: DNA DEFINTION RETURNED\n", LogType.Info);
                DnaDefinitionResponse dnaDefinitionResponse = MessagePackSerializer.Deserialize<DnaDefinitionResponse>(response.data, messagePackSerializerOptions);
                DnaDefinition dnaDefinition = new DnaDefinition();

                if (dnaDefinitionResponse != null)
                {
                    dnaDefinition.Name = dnaDefinitionResponse.data.name;
                    dnaDefinition.Modifiers = dnaDefinitionResponse.data.modifiers;
                    dnaDefinition.CoordinatorZomes = DecodeZomesDef(dnaDefinitionResponse.data.coordinator_zomes_raw as object[]);
                    dnaDefinition.IntegrityZomes = DecodeZomesDef(dnaDefinitionResponse.data.integrity_zomes_raw as object[]);
                    args.DnaDefinition = dnaDefinition;
                }
                else
                    HandleError(args, $"{errorMessage} dnaDefinition failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseDnaDefinitionReturnedEvent(args);
        }

        private void DecodeAgentInfoReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeAgentInfoReceived. Reason: ";
            AgentInfoReturnedCallBackEventArgs args = CreateHoloNETArgs<AgentInfoReturnedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminAgentInfoReturned;

            try
            {
                Logger.Log("ADMIN: AGENT INFO RETURNED\n", LogType.Info);
                //AgentInfo agentInfo = MessagePackSerializer.Deserialize<AgentInfo>(response.data, messagePackSerializerOptions);
                HoloNETData agentInfo = MessagePackSerializer.Deserialize<HoloNETData>(response.data, messagePackSerializerOptions);
                //object agentInfo = MessagePackSerializer.Deserialize<object>(response.data, messagePackSerializerOptions);

                object[] agentInfoObj = agentInfo.data as object[];
                Dictionary<object, object> agentInfoDict = agentInfoObj[0] as Dictionary<object, object>;
                args.AgentInfo = new AgentInfo()
                {
                    agent = agentInfoDict["agent"] as byte[],
                    signature = agentInfoDict["signature"] as byte[],
                    agent_info = agentInfoDict["agent_info"] as byte[]
                };

                //if (agentInfo != null)
                //    args.AgentInfo = agentInfo;
                //else
                //    HandleError(args, $"{errorMessage} dataResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseAgentInfoReturnedEvent(args);
        }

        private void DecodeAgentInfoAddedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeAgentInfoAddedReceived. Reason: ";
            AgentInfoAddedCallBackEventArgs args = CreateHoloNETArgs<AgentInfoAddedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminAgentInfoAdded;

            try
            {
                Logger.Log("ADMIN: AGENT INFO ADDED\n", LogType.Info);
                object agentInfo = MessagePackSerializer.Deserialize<object>(response.data, messagePackSerializerOptions);

                //if (agentInfo != null)
                //    args.AgentInfo = agentInfo;
                //else
                //    HandleError(args, $"{errorMessage} dataResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseAgentInfoAddedEvent(args);
        }

        private void DecodeAppsListedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeAppsListedReceived. Reason: ";
            AppsListedCallBackEventArgs args = CreateHoloNETArgs<AppsListedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminAppsListed;

            try
            {
                Logger.Log("ADMIN: APPS LISTED\n", LogType.Info);
                ListAppsResponse appResponse = MessagePackSerializer.Deserialize<ListAppsResponse>(response.data, messagePackSerializerOptions);

                if (appResponse != null)
                {
                    string hApps = "";
                    foreach (AppInfo appInfo in appResponse.Apps)
                    {
                        AppInfoCallBackEventArgs appInfoArgs = new AppInfoCallBackEventArgs();
                        AppInfo processedAppInfo = ProcessAppInfo(appInfo, appInfoArgs, false);

                        if (!appInfoArgs.IsError)
                            args.Apps.Add(processedAppInfo);
                        else
                        {
                            args.IsError = true;
                            args.Message = $"{args.Message} # {appInfoArgs.Message}";
                        }

                        hApps = $"{hApps}hAPP: {appInfo.installed_app_id}, AgentPubKey: {appInfo.AgentPubKey}, DnaHash: {appInfo.DnaHash}\"\n";
                    }

                    Logger.Log($"hApps:\n{hApps}", LogType.Info);

                    if (args.IsError)
                        HandleError(args.Message);
                }
                else
                    HandleError(args, $"{errorMessage} appResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseAppsListedEvent(args);
        }

        private void DecodeDnasListedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeDnasListedReceived. Reason: ";
            DnasListedCallBackEventArgs args = CreateHoloNETArgs<DnasListedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminDnasListed;

            try
            {
                Logger.Log("ADMIN: DNA's LISTED\n", LogType.Info);
                AppResponse appResponse = MessagePackSerializer.Deserialize<AppResponse>(response.data, messagePackSerializerOptions);

                if (appResponse != null)
                {
                    string dnas = "";
                    foreach (byte[] dna in appResponse.data)
                    {
                        dnas = $"{dnas}\n{ConvertHoloHashToString(dna)}";
                        args.Dnas.Add(dna);
                    }

                    Logger.Log($"DnaHashes's: {dnas}", LogType.Info);
                }
                else
                    HandleError(args, $"{errorMessage} appResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseDnasListedEvent(args);
        }

        private void DecodeCellIdsListedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeCellIdsListedReceived. Reason: ";
            CellIdsListedCallBackEventArgs args = CreateHoloNETArgs<CellIdsListedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminCellIdsListed;

            try
            {
                Logger.Log("ADMIN: CELLID's LISTED\n", LogType.Info);
                AppResponse appResponse = MessagePackSerializer.Deserialize<AppResponse>(response.data, messagePackSerializerOptions);

                if (appResponse != null)
                {
                    string cellIds = "";
                    foreach (object[] cell in appResponse.data)
                    {
                        byte[][] cellId = new byte[2][];
                        cellId[0] = (byte[])cell[0];
                        cellId[1] = (byte[])cell[1];
                        args.CellIds.Add(cellId);
                        cellIds = $"{cellIds}\nCell: (DnaHash: {ConvertHoloHashToString(cellId[0])} AgentPubKey: {ConvertHoloHashToString(cellId[1])})";
                    }

                    Logger.Log($"Cell Id's: {cellIds}", LogType.Info);
                }
                else
                    HandleError(args, $"{errorMessage} appResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseCellIdsListedEvent(args);
        }

        private void DecodeAppInterfacesListedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeAppInterfacesListedReceived. Reason: ";
            AppInterfacesListedCallBackEventArgs args = CreateHoloNETArgs<AppInterfacesListedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminAppInterfacesListed;

            try
            {
                Logger.Log("ADMIN: APP INTERFACES LISTED\n", LogType.Info);
                AppResponse appResponse = MessagePackSerializer.Deserialize<AppResponse>(response.data, messagePackSerializerOptions);

                if (appResponse != null)
                {
                    string ports = "";
                    foreach (ushort port in appResponse.data)
                    {
                        ports = $"{ports}{port},";
                        args.WebSocketPorts.Add(port);
                    }

                    ports = ports.Substring(0, ports.Length - 1);
                    Logger.Log($"App Interfaces Listed: {ports}", LogType.Info);
                }
                else
                    HandleError(args, $"{errorMessage} appResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseAppInterfacesListedEvent(args);
        }

        private void DecodeCoordinatorsUpdatedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeCoordinatorsUpdatedReceived. Reason: ";
            CoordinatorsUpdatedCallBackEventArgs args = CreateHoloNETArgs<CoordinatorsUpdatedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminCoordinatorsUpdated;

            try
            {
                Logger.Log("ADMIN: CoordinatorsUpdated\n", LogType.Info);
                object dataResponse = MessagePackSerializer.Deserialize<object>(response.data, messagePackSerializerOptions);

                if (dataResponse == null)
                    HandleError(args, $"{errorMessage} dataResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseCoordinatorsUpdatedEvent(args);
        }

        private void DecodeCloneCellDeletedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeCloneCellDeletedReceived. Reason: ";
            CloneCellDeletedCallBackEventArgs args = CreateHoloNETArgs<CloneCellDeletedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminCloneCellDeleted;

            try
            {
                Logger.Log("ADMIN: CLONE CELL DELETED\n", LogType.Info);
                object dataResponse = MessagePackSerializer.Deserialize<object>(response.data, messagePackSerializerOptions);

                if (dataResponse == null)
                    HandleError(args, $"{errorMessage} dataResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseCloneCellDeletedEvent(args);
        }

        private void DecodeStateDumpedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeStateDumpedReceived. Reason: ";
            StateDumpedCallBackEventArgs args = CreateHoloNETArgs<StateDumpedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminStateDumped;

            try
            {
                Logger.Log("ADMIN: STATE DUMPED\n", LogType.Info);
                string dataResponse = MessagePackSerializer.Deserialize<string>(response.data, messagePackSerializerOptions);

                if (dataResponse != null)
                    args.DumpedStateJSON = dataResponse;
                else
                    HandleError(args, $"{errorMessage} dataResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseStateDumpedEvent(args);
        }

        private void DecodeFullStateDumpedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeFullStateDumpedReceived. Reason: ";
            FullStateDumpedCallBackEventArgs args = CreateHoloNETArgs<FullStateDumpedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminFullStateDumped;

            try
            {
                Logger.Log("ADMIN: FULL STATE DUMPED\n", LogType.Info);
                FullStateDumpedResponse fullStateDumpedResponse = MessagePackSerializer.Deserialize<FullStateDumpedResponse>(response.data, messagePackSerializerOptions);

                if (fullStateDumpedResponse != null)
                    args.DumpedState = fullStateDumpedResponse;
                else
                    HandleError(args, $"{errorMessage} fullStateDumpedResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseFullStateDumpedEvent(args);
        }

        private void DecodeNetworkMetricsDumpedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeNetworkMetricsDumpedReceived. Reason: ";
            NetworkMetricsDumpedCallBackEventArgs args = CreateHoloNETArgs<NetworkMetricsDumpedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminNetworkMetricsDumped;

            try
            {
                Logger.Log("ADMIN: NETWORK METRICS DUMPED\n", LogType.Info);
                string dataResponse = MessagePackSerializer.Deserialize<string>(response.data, messagePackSerializerOptions);

                if (dataResponse != null)
                    args.NetworkMetricsDumpJSON = dataResponse;
                else
                    HandleError(args, $"{errorMessage} dataResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseNetworkMetricsDumpedEvent(args);
        }

        private void DecodeNetworkStatsDumpedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeNetworkStatsDumpedReceived. Reason: ";
            NetworkStatsDumpedCallBackEventArgs args = CreateHoloNETArgs<NetworkStatsDumpedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminNetworkStatsDumped;

            try
            {
                Logger.Log("ADMIN: NETWORK STATS DUMPED\n", LogType.Info);
                string dataResponse = MessagePackSerializer.Deserialize<string>(response.data, messagePackSerializerOptions);

                if (dataResponse != null)
                    args.NetworkStatsDumpJSON = dataResponse;
                else
                    HandleError(args, $"{errorMessage} dataResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseNetworkStatsDumpedEvent(args);
        }

        private void DecodeStorageInfoReturned(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeStorageInfoReturned. Reason: ";
            StorageInfoReturnedCallBackEventArgs args = CreateHoloNETArgs<StorageInfoReturnedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminStorageInfoReturned;

            try
            {
                Logger.Log("ADMIN: STORAGE INFO RETURNED\n", LogType.Info);
                StorageInfoResponse storageInfoResponse = MessagePackSerializer.Deserialize<StorageInfoResponse>(response.data, messagePackSerializerOptions);

                if (storageInfoResponse != null)
                    args.StorageInfoResponse = storageInfoResponse;
                else
                    HandleError(args, $"{errorMessage} storageInfoResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseStorageInfoReturnedEvent(args);
        }

        private void DecodeRecordsGraftedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeRecordsGraftedReceived. Reason: ";
            RecordsGraftedCallBackEventArgs args = CreateHoloNETArgs<RecordsGraftedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminRecordsGrafted;

            try
            {
                Logger.Log("ADMIN: RECORDS GRAFTED\n", LogType.Info);
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseRecordsGraftedEvent(args);
        }

        private void DecodeAdminInterfacesAddedReceived(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeAdminInterfacesAddedReceived. Reason: ";
            AdminInterfacesAddedCallBackEventArgs args = CreateHoloNETArgs<AdminInterfacesAddedCallBackEventArgs>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminAdminInterfacesAdded;

            try
            {
                Logger.Log("ADMIN: INTERFACES ADDED\n", LogType.Info);
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseAdminInterfacesAddedEvent(args);
        }

        //Generic version
        private void DecodeResponseReceived<T>(IHoloNETResponse response, WebSocket.DataReceivedEventArgs dataReceivedEventArgs, string eventName, Dictionary<string, TaskCompletionSource<HoloNETDataReceivedBaseBaseEventArgs<T>>> taskCompletionCallBack)
        {
            string errorMessage = "An unknown error occurred in HoloNETClient.DecodeResponseReceived. Reason: ";
            HoloNETDataReceivedBaseBaseEventArgs<T> args = CreateHoloNETArgs<HoloNETDataReceivedBaseBaseEventArgs<T>>(response, dataReceivedEventArgs);
            args.HoloNETResponseType = HoloNETResponseType.AdminStateDumped;

            try
            {
                Logger.Log("ADMIN: RESPONSE\n", LogType.Info);
                T dataResponse = MessagePackSerializer.Deserialize<T>(response.data, messagePackSerializerOptions);

                if (dataResponse != null)
                    args.Response = dataResponse;
                else
                    HandleError(args, $"{errorMessage} dataResponse failed to deserialize.");
            }
            catch (Exception ex)
            {
                HandleError(args, $"{errorMessage} {ex}");
            }

            RaiseResponseReceivedEvent(args, eventName, taskCompletionCallBack);
        }

        private void RaiseAgentPubKeyGeneratedEvent(AgentPubKeyGeneratedCallBackEventArgs adminAgentPubKeyGeneratedCallBackEventArgs)
        {
            LogEvent("AdminAgentPubKeyGenerated", adminAgentPubKeyGeneratedCallBackEventArgs);
            OnAgentPubKeyGeneratedCallBack?.Invoke(this, adminAgentPubKeyGeneratedCallBackEventArgs);

            if (_taskCompletionAgentPubKeyGeneratedCallBack != null && !string.IsNullOrEmpty(adminAgentPubKeyGeneratedCallBackEventArgs.Id) && _taskCompletionAgentPubKeyGeneratedCallBack.ContainsKey(adminAgentPubKeyGeneratedCallBackEventArgs.Id))
            {
                _taskCompletionAgentPubKeyGeneratedCallBack[adminAgentPubKeyGeneratedCallBackEventArgs.Id].SetResult(adminAgentPubKeyGeneratedCallBackEventArgs);
                _taskCompletionAgentPubKeyGeneratedCallBack.Remove(adminAgentPubKeyGeneratedCallBackEventArgs.Id);
            }
        }

        private void RaiseAppInstalledEvent(AppInstalledCallBackEventArgs adminAppInstalledCallBackEventArgs)
        {
            LogEvent("AdminAppInstalled", adminAppInstalledCallBackEventArgs);
            OnAppInstalledCallBack?.Invoke(this, adminAppInstalledCallBackEventArgs);

            if (_taskCompletionAppInstalledCallBack != null && !string.IsNullOrEmpty(adminAppInstalledCallBackEventArgs.Id) && _taskCompletionAppInstalledCallBack.ContainsKey(adminAppInstalledCallBackEventArgs.Id))
            {
                _taskCompletionAppInstalledCallBack[adminAppInstalledCallBackEventArgs.Id].SetResult(adminAppInstalledCallBackEventArgs);
                _taskCompletionAppInstalledCallBack.Remove(adminAppInstalledCallBackEventArgs.Id);
            }
        }

        private void RaiseAppUninstalledEvent(AppUninstalledCallBackEventArgs adminAppUninstalledCallBackEventArgs)
        {
            LogEvent("AdminAppUninstalled", adminAppUninstalledCallBackEventArgs);
            OnAppUninstalledCallBack?.Invoke(this, adminAppUninstalledCallBackEventArgs);

            if (_taskCompletionAppUninstalledCallBack != null && !string.IsNullOrEmpty(adminAppUninstalledCallBackEventArgs.Id) && _taskCompletionAppUninstalledCallBack.ContainsKey(adminAppUninstalledCallBackEventArgs.Id))
            {
                _taskCompletionAppUninstalledCallBack[adminAppUninstalledCallBackEventArgs.Id].SetResult(adminAppUninstalledCallBackEventArgs);
                _taskCompletionAppUninstalledCallBack.Remove(adminAppUninstalledCallBackEventArgs.Id);
            }
        }

        private void RaiseAppEnabledEvent(AppEnabledCallBackEventArgs adminAppEnabledCallBackEventArgs)
        {
            LogEvent("AdminAppEnabled", adminAppEnabledCallBackEventArgs);
            OnAppEnabledCallBack?.Invoke(this, adminAppEnabledCallBackEventArgs);

            if (_taskCompletionAppEnabledCallBack != null && !string.IsNullOrEmpty(adminAppEnabledCallBackEventArgs.Id) && _taskCompletionAppEnabledCallBack.ContainsKey(adminAppEnabledCallBackEventArgs.Id))
            {
                _taskCompletionAppEnabledCallBack[adminAppEnabledCallBackEventArgs.Id].SetResult(adminAppEnabledCallBackEventArgs);
                _taskCompletionAppEnabledCallBack.Remove(adminAppEnabledCallBackEventArgs.Id);
            }
        }

        private void RaiseAppDisabledEvent(AppDisabledCallBackEventArgs adminAppDisabledCallBackEventArgs)
        {
            LogEvent("AdminAppDisabled", adminAppDisabledCallBackEventArgs);
            OnAppDisabledCallBack?.Invoke(this, adminAppDisabledCallBackEventArgs);

            if (_taskCompletionAppDisabledCallBack != null && !string.IsNullOrEmpty(adminAppDisabledCallBackEventArgs.Id) && _taskCompletionAppDisabledCallBack.ContainsKey(adminAppDisabledCallBackEventArgs.Id))
            {
                _taskCompletionAppDisabledCallBack[adminAppDisabledCallBackEventArgs.Id].SetResult(adminAppDisabledCallBackEventArgs);
                _taskCompletionAppDisabledCallBack.Remove(adminAppDisabledCallBackEventArgs.Id);
            }
        }

        private void RaiseZomeCallCapabilityGrantedEvent(ZomeCallCapabilityGrantedCallBackEventArgs adminZomeCallCapabilityGrantedCallBackEventArgs)
        {
            LogEvent("AdminZomeCallCapabilityGranted", adminZomeCallCapabilityGrantedCallBackEventArgs);
            OnZomeCallCapabilityGrantedCallBack?.Invoke(this, adminZomeCallCapabilityGrantedCallBackEventArgs);

            if (_taskCompletionZomeCapabilityGrantedCallBack != null && !string.IsNullOrEmpty(adminZomeCallCapabilityGrantedCallBackEventArgs.Id) && _taskCompletionZomeCapabilityGrantedCallBack.ContainsKey(adminZomeCallCapabilityGrantedCallBackEventArgs.Id))
            {
                _taskCompletionZomeCapabilityGrantedCallBack[adminZomeCallCapabilityGrantedCallBackEventArgs.Id].SetResult(adminZomeCallCapabilityGrantedCallBackEventArgs);
                _taskCompletionZomeCapabilityGrantedCallBack.Remove(adminZomeCallCapabilityGrantedCallBackEventArgs.Id);
            }
        }

        private void RaiseAppInterfaceAttachedEvent(AppInterfaceAttachedCallBackEventArgs adminAppInterfaceAttachedCallBackEventArgs)
        {
            LogEvent("AdminAppInterfaceAttached", adminAppInterfaceAttachedCallBackEventArgs);
            OnAppInterfaceAttachedCallBack?.Invoke(this, adminAppInterfaceAttachedCallBackEventArgs);

            if (_taskCompletionAppInterfaceAttachedCallBack != null && !string.IsNullOrEmpty(adminAppInterfaceAttachedCallBackEventArgs.Id) && _taskCompletionAppInterfaceAttachedCallBack.ContainsKey(adminAppInterfaceAttachedCallBackEventArgs.Id))
            {
                _taskCompletionAppInterfaceAttachedCallBack[adminAppInterfaceAttachedCallBackEventArgs.Id].SetResult(adminAppInterfaceAttachedCallBackEventArgs);
                _taskCompletionAppInterfaceAttachedCallBack.Remove(adminAppInterfaceAttachedCallBackEventArgs.Id);
            }
        }

        private void RaiseDnaRegisteredEvent(DnaRegisteredCallBackEventArgs dnaRegisteredCallBackEventArgs)
        {
            LogEvent("AdminDnaRegistered", dnaRegisteredCallBackEventArgs);
            OnDnaRegisteredCallBack?.Invoke(this, dnaRegisteredCallBackEventArgs);

            if (_taskCompletionDnaRegisteredCallBack != null && !string.IsNullOrEmpty(dnaRegisteredCallBackEventArgs.Id) && _taskCompletionDnaRegisteredCallBack.ContainsKey(dnaRegisteredCallBackEventArgs.Id))
            {
                _taskCompletionDnaRegisteredCallBack[dnaRegisteredCallBackEventArgs.Id].SetResult(dnaRegisteredCallBackEventArgs);
                _taskCompletionDnaRegisteredCallBack.Remove(dnaRegisteredCallBackEventArgs.Id);
            }
        }

        private void RaiseDnaDefinitionReturnedEvent(DnaDefinitionReturnedCallBackEventArgs dnaDefinitionReturnedCallBackEventArgs)
        {
            LogEvent("AdminDnaDefintionReturned", dnaDefinitionReturnedCallBackEventArgs);
            OnDnaDefinitionReturnedCallBack?.Invoke(this, dnaDefinitionReturnedCallBackEventArgs);

            if (_taskCompletionDnaDefinitionReturnedCallBack != null && !string.IsNullOrEmpty(dnaDefinitionReturnedCallBackEventArgs.Id) && _taskCompletionDnaDefinitionReturnedCallBack.ContainsKey(dnaDefinitionReturnedCallBackEventArgs.Id))
            {
                _taskCompletionDnaDefinitionReturnedCallBack[dnaDefinitionReturnedCallBackEventArgs.Id].SetResult(dnaDefinitionReturnedCallBackEventArgs);
                _taskCompletionDnaDefinitionReturnedCallBack.Remove(dnaDefinitionReturnedCallBackEventArgs.Id);
            }
        }

        private void RaiseAppInterfacesListedEvent(AppInterfacesListedCallBackEventArgs adminAppsListedCallBackEventArgs)
        {
            LogEvent("AdminAppInterfacesListed", adminAppsListedCallBackEventArgs);
            OnAppInterfacesListedCallBack?.Invoke(this, adminAppsListedCallBackEventArgs);

            if (_taskCompletionAppInterfacesListedCallBack != null && !string.IsNullOrEmpty(adminAppsListedCallBackEventArgs.Id) && _taskCompletionAppInterfacesListedCallBack.ContainsKey(adminAppsListedCallBackEventArgs.Id))
            {
                _taskCompletionAppInterfacesListedCallBack[adminAppsListedCallBackEventArgs.Id].SetResult(adminAppsListedCallBackEventArgs);
                _taskCompletionAppInterfacesListedCallBack.Remove(adminAppsListedCallBackEventArgs.Id);
            }
        }

        private void RaiseAppsListedEvent(AppsListedCallBackEventArgs adminAppsListedCallBackEventArgs)
        {
            LogEvent("AdminAppsListed", adminAppsListedCallBackEventArgs);
            OnAppsListedCallBack?.Invoke(this, adminAppsListedCallBackEventArgs);

            if (_taskCompletionAppsListedCallBack != null && !string.IsNullOrEmpty(adminAppsListedCallBackEventArgs.Id) && _taskCompletionAppsListedCallBack.ContainsKey(adminAppsListedCallBackEventArgs.Id))
            {
                _taskCompletionAppsListedCallBack[adminAppsListedCallBackEventArgs.Id].SetResult(adminAppsListedCallBackEventArgs);
                _taskCompletionDnasListedCallBack.Remove(adminAppsListedCallBackEventArgs.Id);
            }
        }

        private void RaiseDnasListedEvent(DnasListedCallBackEventArgs dnasListedCallBackEventArgs)
        {
            LogEvent("AdminDnasListed", dnasListedCallBackEventArgs);
            OnDnasListedCallBack?.Invoke(this, dnasListedCallBackEventArgs);

            if (_taskCompletionDnasListedCallBack != null && !string.IsNullOrEmpty(dnasListedCallBackEventArgs.Id) && _taskCompletionDnasListedCallBack.ContainsKey(dnasListedCallBackEventArgs.Id))
            {
                _taskCompletionDnasListedCallBack[dnasListedCallBackEventArgs.Id].SetResult(dnasListedCallBackEventArgs);
                _taskCompletionDnasListedCallBack.Remove(dnasListedCallBackEventArgs.Id);
            }
        }

        private void RaiseCellIdsListedEvent(CellIdsListedCallBackEventArgs cellIdsListedCallBackEventArgs)
        {
            LogEvent("AdminCellIdsListed", cellIdsListedCallBackEventArgs);
            OnCellIdsListedCallBack?.Invoke(this, cellIdsListedCallBackEventArgs);

            if (_taskCompletionCellIdsListedCallBack != null && !string.IsNullOrEmpty(cellIdsListedCallBackEventArgs.Id) && _taskCompletionCellIdsListedCallBack.ContainsKey(cellIdsListedCallBackEventArgs.Id))
            {
                _taskCompletionCellIdsListedCallBack[cellIdsListedCallBackEventArgs.Id].SetResult(cellIdsListedCallBackEventArgs);
                _taskCompletionCellIdsListedCallBack.Remove(cellIdsListedCallBackEventArgs.Id);
            }
        }

        private void RaiseAgentInfoReturnedEvent(AgentInfoReturnedCallBackEventArgs agentInfoReturnedCallBackEventArgs)
        {
            LogEvent("AdminAgentInfoReturned", agentInfoReturnedCallBackEventArgs);
            OnAgentInfoReturnedCallBack?.Invoke(this, agentInfoReturnedCallBackEventArgs);

            if (_taskCompletionAgentInfoReturnedCallBack != null && !string.IsNullOrEmpty(agentInfoReturnedCallBackEventArgs.Id) && _taskCompletionAgentInfoReturnedCallBack.ContainsKey(agentInfoReturnedCallBackEventArgs.Id))
            {
                _taskCompletionAgentInfoReturnedCallBack[agentInfoReturnedCallBackEventArgs.Id].SetResult(agentInfoReturnedCallBackEventArgs);
                _taskCompletionAgentInfoReturnedCallBack.Remove(agentInfoReturnedCallBackEventArgs.Id);
            }
        }

        private void RaiseAgentInfoAddedEvent(AgentInfoAddedCallBackEventArgs agentInfoAddedCallBackEventArgs)
        {
            LogEvent("AdminAgentInfoAdded", agentInfoAddedCallBackEventArgs);
            OnAgentInfoAddedCallBack?.Invoke(this, agentInfoAddedCallBackEventArgs);

            if (_taskCompletionAgentInfoAddedCallBack != null && !string.IsNullOrEmpty(agentInfoAddedCallBackEventArgs.Id) && _taskCompletionAgentInfoReturnedCallBack.ContainsKey(agentInfoAddedCallBackEventArgs.Id))
            {
                _taskCompletionAgentInfoAddedCallBack[agentInfoAddedCallBackEventArgs.Id].SetResult(agentInfoAddedCallBackEventArgs);
                _taskCompletionAgentInfoAddedCallBack.Remove(agentInfoAddedCallBackEventArgs.Id);
            }
        }

        private void RaiseCoordinatorsUpdatedEvent(CoordinatorsUpdatedCallBackEventArgs coordinatorsUpdatedCallBackEventArgs)
        {
            LogEvent("AdmiCoordinatorsUpdated", coordinatorsUpdatedCallBackEventArgs);
            OnCoordinatorsUpdatedCallBack?.Invoke(this, coordinatorsUpdatedCallBackEventArgs);

            if (_taskCompletionCoordinatorsUpdatedCallBack != null && !string.IsNullOrEmpty(coordinatorsUpdatedCallBackEventArgs.Id) && _taskCompletionCoordinatorsUpdatedCallBack.ContainsKey(coordinatorsUpdatedCallBackEventArgs.Id))
            {
                _taskCompletionCoordinatorsUpdatedCallBack[coordinatorsUpdatedCallBackEventArgs.Id].SetResult(coordinatorsUpdatedCallBackEventArgs);
                _taskCompletionCloneCellDeletedCallBack.Remove(coordinatorsUpdatedCallBackEventArgs.Id);
            }
        }

        private void RaiseCloneCellDeletedEvent(CloneCellDeletedCallBackEventArgs cloneCellDeletedCallBackEventArgs)
        {
            LogEvent("AdminCloneCellDeleted", cloneCellDeletedCallBackEventArgs);
            OnCloneCellDeletedCallBack?.Invoke(this, cloneCellDeletedCallBackEventArgs);

            if (_taskCompletionCloneCellDeletedCallBack != null && !string.IsNullOrEmpty(cloneCellDeletedCallBackEventArgs.Id) && _taskCompletionCloneCellDeletedCallBack.ContainsKey(cloneCellDeletedCallBackEventArgs.Id))
            {
                _taskCompletionCloneCellDeletedCallBack[cloneCellDeletedCallBackEventArgs.Id].SetResult(cloneCellDeletedCallBackEventArgs);
                _taskCompletionCloneCellDeletedCallBack.Remove(cloneCellDeletedCallBackEventArgs.Id);
            }
        }

        private void RaiseStateDumpedEvent(StateDumpedCallBackEventArgs stateDumpedCallBackEventArgs)
        {
            LogEvent("AdminStateDumped", stateDumpedCallBackEventArgs);
            OnStateDumpedCallBack?.Invoke(this, stateDumpedCallBackEventArgs);

            if (_taskCompletionStateDumpedCallBack != null && !string.IsNullOrEmpty(stateDumpedCallBackEventArgs.Id) && _taskCompletionStateDumpedCallBack.ContainsKey(stateDumpedCallBackEventArgs.Id))
            {
                _taskCompletionStateDumpedCallBack[stateDumpedCallBackEventArgs.Id].SetResult(stateDumpedCallBackEventArgs);
                _taskCompletionStateDumpedCallBack.Remove(stateDumpedCallBackEventArgs.Id);
            }
        }

        private void RaiseFullStateDumpedEvent(FullStateDumpedCallBackEventArgs fullStateDumpedCallBackEventArgs)
        {
            LogEvent("AdminStateDumped", fullStateDumpedCallBackEventArgs);
            OnFullStateDumpedCallBack?.Invoke(this, fullStateDumpedCallBackEventArgs);

            if (_taskCompletionFullStateDumpedCallBack != null && !string.IsNullOrEmpty(fullStateDumpedCallBackEventArgs.Id) && _taskCompletionFullStateDumpedCallBack.ContainsKey(fullStateDumpedCallBackEventArgs.Id))
            {
                _taskCompletionFullStateDumpedCallBack[fullStateDumpedCallBackEventArgs.Id].SetResult(fullStateDumpedCallBackEventArgs);
                _taskCompletionFullStateDumpedCallBack.Remove(fullStateDumpedCallBackEventArgs.Id);
            }
        }

        private void RaiseNetworkMetricsDumpedEvent(NetworkMetricsDumpedCallBackEventArgs networkMetricsDumpedCallBackEventArgs)
        {
            LogEvent("AdminNetworkMetricsDumped", networkMetricsDumpedCallBackEventArgs);
            OnNetworkMetricsDumpedCallBack?.Invoke(this, networkMetricsDumpedCallBackEventArgs);

            if (_taskCompletionNetworkMetricsDumpedCallBack != null && !string.IsNullOrEmpty(networkMetricsDumpedCallBackEventArgs.Id) && _taskCompletionNetworkMetricsDumpedCallBack.ContainsKey(networkMetricsDumpedCallBackEventArgs.Id))
            {
                _taskCompletionNetworkMetricsDumpedCallBack[networkMetricsDumpedCallBackEventArgs.Id].SetResult(networkMetricsDumpedCallBackEventArgs);
                _taskCompletionNetworkMetricsDumpedCallBack.Remove(networkMetricsDumpedCallBackEventArgs.Id);
            }
        }

        private void RaiseNetworkStatsDumpedEvent(NetworkStatsDumpedCallBackEventArgs networkStatsDumpedCallBackEventArgs)
        {
            LogEvent("AdminNetworkStatsDumped", networkStatsDumpedCallBackEventArgs);
            OnNetworkStatsDumpedCallBack?.Invoke(this, networkStatsDumpedCallBackEventArgs);

            if (_taskCompletionNetworkStatsDumpedCallBack != null && !string.IsNullOrEmpty(networkStatsDumpedCallBackEventArgs.Id) && _taskCompletionNetworkStatsDumpedCallBack.ContainsKey(networkStatsDumpedCallBackEventArgs.Id))
            {
                _taskCompletionNetworkStatsDumpedCallBack[networkStatsDumpedCallBackEventArgs.Id].SetResult(networkStatsDumpedCallBackEventArgs);
                _taskCompletionNetworkStatsDumpedCallBack.Remove(networkStatsDumpedCallBackEventArgs.Id);
            }
        }

        private void RaiseStorageInfoReturnedEvent(StorageInfoReturnedCallBackEventArgs storageInfoReturnedCallBackEventArgs)
        {
            LogEvent("AdminNetworkStatsDumped", storageInfoReturnedCallBackEventArgs);
            OnStorageInfoReturnedCallBack?.Invoke(this, storageInfoReturnedCallBackEventArgs);

            if (_taskCompletionStorageInfoReturnedCallBack != null && !string.IsNullOrEmpty(storageInfoReturnedCallBackEventArgs.Id) && _taskCompletionStorageInfoReturnedCallBack.ContainsKey(storageInfoReturnedCallBackEventArgs.Id))
            {
                _taskCompletionStorageInfoReturnedCallBack[storageInfoReturnedCallBackEventArgs.Id].SetResult(storageInfoReturnedCallBackEventArgs);
                _taskCompletionStorageInfoReturnedCallBack.Remove(storageInfoReturnedCallBackEventArgs.Id);
            }
        }

        private void RaiseRecordsGraftedEvent(RecordsGraftedCallBackEventArgs recordsGraftedCallBackEventArgs)
        {
            LogEvent("AdminRecordsGrafted", recordsGraftedCallBackEventArgs);
            OnRecordsGraftedCallBack?.Invoke(this, recordsGraftedCallBackEventArgs);

            if (_taskCompletionRecordsGraftedCallBack != null && !string.IsNullOrEmpty(recordsGraftedCallBackEventArgs.Id) && _taskCompletionRecordsGraftedCallBack.ContainsKey(recordsGraftedCallBackEventArgs.Id))
            {
                _taskCompletionRecordsGraftedCallBack[recordsGraftedCallBackEventArgs.Id].SetResult(recordsGraftedCallBackEventArgs);
                _taskCompletionRecordsGraftedCallBack.Remove(recordsGraftedCallBackEventArgs.Id);
            }
        }

        private void RaiseAdminInterfacesAddedEvent(AdminInterfacesAddedCallBackEventArgs adminInterfacesAddedCallBackEvent)
        {
            LogEvent("AdminInterfacesAdded", adminInterfacesAddedCallBackEvent);
            OnAdminInterfacesAddedCallBack?.Invoke(this, adminInterfacesAddedCallBackEvent);

            if (_taskCompletionAdminInterfacesAddedCallBack != null && !string.IsNullOrEmpty(adminInterfacesAddedCallBackEvent.Id) && _taskCompletionAdminInterfacesAddedCallBack.ContainsKey(adminInterfacesAddedCallBackEvent.Id))
            {
                _taskCompletionAdminInterfacesAddedCallBack[adminInterfacesAddedCallBackEvent.Id].SetResult(adminInterfacesAddedCallBackEvent);
                _taskCompletionAdminInterfacesAddedCallBack.Remove(adminInterfacesAddedCallBackEvent.Id);
            }
        }

        //TODO: Finish trying to make this generic to massively reduce the repeated code! ;-)
        //private void RaiseResponseReceivedEvent<T>(HoloNETDataReceivedBaseBaseEventArgs<T> holoNETDataReceivedBaseBaseEventArgs, string eventName, delegate callBack, Dictionary<string, TaskCompletionSource<HoloNETDataReceivedBaseBaseEventArgs<T>>> taskCompletionCallBack)
        //{
        //    LogEvent(eventName, holoNETDataReceivedBaseBaseEventArgs);
        //    OnStateDumpedCallBack?.Invoke(this, holoNETDataReceivedBaseBaseEventArgs);

        //    if (taskCompletionCallBack != null && !string.IsNullOrEmpty(holoNETDataReceivedBaseBaseEventArgs.Id) && taskCompletionCallBack.ContainsKey(holoNETDataReceivedBaseBaseEventArgs.Id))
        //        taskCompletionCallBack[holoNETDataReceivedBaseBaseEventArgs.Id].SetResult(holoNETDataReceivedBaseBaseEventArgs);
        //}

        private void RaiseResponseReceivedEvent<T>(HoloNETDataReceivedBaseBaseEventArgs<T> holoNETDataReceivedBaseBaseEventArgs, string eventName, Dictionary<string, TaskCompletionSource<HoloNETDataReceivedBaseBaseEventArgs<T>>> taskCompletionCallBack)
        {
            LogEvent(eventName, holoNETDataReceivedBaseBaseEventArgs);
            //OnStateDumpedCallBack?.Invoke(this, holoNETDataReceivedBaseBaseEventArgs);  //TODO: Need to work out how to pass an event as a param!

            if (taskCompletionCallBack != null && !string.IsNullOrEmpty(holoNETDataReceivedBaseBaseEventArgs.Id) && taskCompletionCallBack.ContainsKey(holoNETDataReceivedBaseBaseEventArgs.Id))
            {
                taskCompletionCallBack[holoNETDataReceivedBaseBaseEventArgs.Id].SetResult(holoNETDataReceivedBaseBaseEventArgs);
                taskCompletionCallBack.Remove(holoNETDataReceivedBaseBaseEventArgs.Id);
            }
        }

        private List<ZomeDefinition> DecodeZomesDef(object[] zomesDefRaw)
        {
            List<ZomeDefinition> zomeDefinitions = new List<ZomeDefinition>();
            ZomeDefinition zomeDefinition = null;

            if (zomesDefRaw != null)
            {
                foreach (object zome in zomesDefRaw)
                {
                    object[] keyValuePairs = zome as object[];

                    if (keyValuePairs != null)
                    {
                        zomeDefinition = new ZomeDefinition();
                        zomeDefinition.ZomeName = keyValuePairs[0].ToString();

                        Dictionary<object, object> zomeProps = keyValuePairs[1] as Dictionary<object, object>;

                        if (zomeProps != null)
                        {
                            if (zomeProps.ContainsKey("dependencies"))
                            {
                                object[] deps = zomeProps["dependencies"] as object[];

                                foreach (object dep in deps)
                                    zomeDefinition.Dependencies.Add(dep.ToString());
                            }

                            if (zomeProps.ContainsKey("wasm_hash"))
                                zomeDefinition.wasm_hash = zomeProps["wasm_hash"] as byte[];
                        }

                        zomeDefinitions.Add(zomeDefinition);
                    }
                }
            }

            return zomeDefinitions;
        }
    }
}
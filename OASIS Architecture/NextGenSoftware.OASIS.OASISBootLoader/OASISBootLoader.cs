using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Logging;
using NextGenSoftware.Logging.NLogger;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
//using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
// TODO: Fix HoloOASIS - missing holochain-client-csharp dependency (excluded from Docker build)
//using NextGenSoftware.OASIS.API.Providers.HoloOASIS;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
//using NextGenSoftware.OASIS.API.Providers.RadixOASIS; // TODO: Fix RadixOASIS provider - incomplete implementation
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;
using NextGenSoftware.OASIS.API.Providers.PolygonOASIS;
using NextGenSoftware.OASIS.API.Providers.RootstockOASIS;
using NextGenSoftware.OASIS.API.Providers.MonadOASIS;
using NextGenSoftware.OASIS.API.Providers.TONOASIS;
//using NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;
//using NextGenSoftware.OASIS.API.Providers.CardanoOASIS;
//using NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;
//using NextGenSoftware.OASIS.API.Providers.BNBChainOASIS;
//using NextGenSoftware.OASIS.API.Providers.FantomOASIS;
//using NextGenSoftware.OASIS.API.Providers.OptimismOASIS;
//using NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS;
//using NextGenSoftware.OASIS.API.Providers.ElrondOASIS;
//using NextGenSoftware.OASIS.API.Providers.AptosOASIS;
//using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
//using NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;
//using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
//using NextGenSoftware.OASIS.API.Providers.NEAROASIS;
//using NextGenSoftware.OASIS.API.Providers.BaseOASIS;
//using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
//using NextGenSoftware.OASIS.API.Providers.MoralisOASIS;
//using NextGenSoftware.OASIS.API.Providers.AcitvityPubOASIS;
using NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;
// TODO: Fix StarknetOASIS - has compilation errors due to API changes (HolonType.BridgeTransaction, ProviderManager methods)
//using NextGenSoftware.OASIS.API.Providers.StarknetOASIS;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.Utilities;
using System.Runtime.InteropServices;
//using System.Reflection;

namespace NextGenSoftware.OASIS.OASISBootLoader
{
    public static class OASISBootLoader
    {
        //private static string _OASISVersion = null;
        private const string SYSTEM_EMAIL = "anorak@oasisweb4.com";
        public static bool IsOASISBooted { get; private set; } = false;
        public static bool IsOASISBooting { get; private set; } = false;

        public delegate void OASISBootLoaderError(object sender, OASISErrorEventArgs e);
        public static event OASISBootLoaderError OnOASISBootLoaderError;

        public static string OASISRuntimeVersion { get; set; } = "4.0.0";
        public static string OASISAPIVersion { get; set; } = "4.0.0";
        public static string COSMICVersion { get; set; } = "2.1.1";
        public static string STARODKVersion { get; set; } = "3.0.0";
        public static string STARRuntimeVersion { get; set; } = "3.0.0";
        public static string STARNETVersion { get; set; } = "1.1.1";
        public static string STARAPIVersion { get; set; } = "1.0.1";

        public static string DotNetVersion
        {
            get
            {
                //return string.Concat(Environment.Version.ToString(), "(", RuntimeInformation.FrameworkDescription, ")");
                return Environment.Version.ToString();
            }
        }

        //public static string DotNetVersionDetailed
        //{
        //    get
        //    {
        //        return RuntimeInformation.FrameworkDescription;
        //    }
        //}

        //public static string OASISVersion
        //{
        //    get
        //    {
        //        if (_OASISVersion == null)
        //        {
        //            Assembly assembly = typeof(OASISBootLoader).Assembly;
        //            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
        //            _OASISVersion = fvi.FileVersion;
        //        }

        //        return _OASISVersion;
        //    }
        //}

        public static string OASISDNAPath
        {
            get
            {
                return OASISDNAManager.OASISDNAPath;
            }
            set
            {
                OASISDNAManager.OASISDNAPath = value;
            }
        }

        public static OASISDNA OASISDNA
        {
            get
            {
                return OASISDNAManager.OASISDNA;
            }
            set
            {
                OASISDNAManager.OASISDNA = value;
            }
        }

        public static OASISResult<bool> BootOASIS(string OASISDNAFileName, bool activateDefaultStorageProvider = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<OASISDNA> loadResult = LoadOASISDNA(OASISDNAFileName);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = BootOASIS(OASISDNA, activateDefaultStorageProvider);
            else
                OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISBootLoader.BootOASIS Loading The OASISDNA.json File. Reason: {loadResult.Message}");

            return result;
        }

        public static async Task<OASISResult<bool>> BootOASISAsync(string OASISDNAFileName, bool activateDefaultStorageProvider = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<OASISDNA> loadResult = await LoadOASISDNAAsync(OASISDNAFileName);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                result = await BootOASISAsync(OASISDNA, activateDefaultStorageProvider);
            else
                OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISBootLoader.BootOASISAsync Loading The OASISDNA.json File. Reason: {loadResult.Message}");

            return result;
        }

        public static OASISResult<bool> BootOASIS(OASISDNA OASISDNA, bool activateDefaultStorageProvider = true)
        {
            return BootOASISAsync(OASISDNA, activateDefaultStorageProvider).Result;
        }

        public static async Task<OASISResult<bool>> BootOASISAsync(OASISDNA OASISDNA, bool activateDefaultStorageProvider = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            object OASISProviderBootTypeObject = null;
            string errorMessage = "Error Occured In OASISBootLoader.BootOASISAsync. Reason: ";

            try
            {
                if (!IsOASISBooting)
                {
                    IsOASISBooting = true;

                    if (OASISDNA == null)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNA is null! Please make sure you pass in a valid OASISDNA and make sure the OASISDNA.json file exists in the path specefied.");
                        return result;
                    }

                    OASISDNAManager.OASISDNA = OASISDNA;
                    LoggingManager.CurrentLoggingFramework = (LoggingFramework)Enum.Parse(typeof(LoggingFramework), OASISDNA.OASIS.Logging.LoggingFramework);

                    switch (LoggingManager.CurrentLoggingFramework)
                    {
                        case LoggingFramework.Default:
                            LoggingManager.Init(OASISDNA.OASIS.Logging.LogToConsole, OASISDNA.OASIS.Logging.LogToFile, OASISDNA.OASIS.Logging.LogPath, OASISDNA.OASIS.Logging.LogFileName, OASISDNA.OASIS.Logging.MaxLogFileSize, OASISDNA.OASIS.Logging.FileLoggingMode, OASISDNA.OASIS.Logging.ConsoleLoggingMode, null, OASISDNA.OASIS.Logging.InsertExtraNewLineAfterLogMessage, OASISDNA.OASIS.Logging.IndentLogMessagesBy, OASISDNA.OASIS.Logging.ShowColouredLogs, OASISDNA.OASIS.Logging.DebugColour, OASISDNA.OASIS.Logging.InfoColour, OASISDNA.OASIS.Logging.WarningColour, OASISDNA.OASIS.Logging.ErrorColour);
                            break;

                        case LoggingFramework.NLog:
                            LoggingManager.Init(new NLogProvider(), OASISDNA.OASIS.Logging.AlsoUseDefaultLogProvider);
                            break;
                    }

                    //LoggingManager.Log($"DONE", LogType.Info, false, false, false, 0);
                    //LoggingManager.Log($"INIT LOGGING... DONE", LogType.Info);

                    LoggingManager.Log($"BOOTING OASIS...", LogType.Info, true);

                    OASISErrorHandling.LogAllErrors = OASISDNA.OASIS.ErrorHandling.LogAllErrors;
                    OASISErrorHandling.LogAllWarnings = OASISDNA.OASIS.ErrorHandling.LogAllWarnings;
                    OASISErrorHandling.ShowStackTrace = OASISDNA.OASIS.ErrorHandling.ShowStackTrace;
                    OASISErrorHandling.ThrowExceptionsOnErrors = OASISDNA.OASIS.ErrorHandling.ThrowExceptionsOnErrors;
                    OASISErrorHandling.ThrowExceptionsOnWarnings = OASISDNA.OASIS.ErrorHandling.ThrowExceptionsOnWarnings;
                    OASISErrorHandling.WarningHandlingBehaviour = OASISDNA.OASIS.ErrorHandling.WarningHandlingBehaviour;
                    OASISErrorHandling.ErrorHandlingBehaviour = OASISDNA.OASIS.ErrorHandling.ErrorHandlingBehaviour;

                    ProviderManager.Instance.IsAutoFailOverEnabled = OASISDNA.OASIS.StorageProviders.AutoFailOverEnabled;
                    //ProviderManager.Instance.IsAutoFailOverEnabledForAvatarLogin = OASISDNA.OASIS.StorageProviders.AutoFailOverEnabledForAvatarLogin;
                    //ProviderManager.Instance.IsAutoFailOverEnabledForCheckIfEmailAlreadyInUse = OASISDNA.OASIS.StorageProviders.AutoFailOverEnabledForCheckIfEmailAlreadyInUse;
                    //ProviderManager.Instance.IsAutoFailOverEnabledForCheckIfUsernameAlreadyInUse = OASISDNA.OASIS.StorageProviders.AutoFailOverEnabledForCheckIfUsernameAlreadyInUse;
                    ProviderManager.Instance.IsAutoLoadBalanceEnabled = OASISDNA.OASIS.StorageProviders.AutoLoadBalanceEnabled;
                    ProviderManager.Instance.IsAutoReplicationEnabled = OASISDNA.OASIS.StorageProviders.AutoReplicationEnabled;

                    LoggingManager.Log($"FIRING UP THE OASIS HYPERDRIVE...", LogType.Info, true);
                    //LoggingManager.Log($"LOADING PROVIDER LISTS...", LogType.Info, true, false, false, 1, true);
                    LoggingManager.BeginLogAction($"LOADING PROVIDER LISTS...", LogType.Info);
                    OASISResult<bool> loadProviderListsResult = LoadProviderLists();

                    if (loadProviderListsResult != null && !loadProviderListsResult.IsError && !loadProviderListsResult.IsWarning)
                        //LoggingManager.Log($"DONE", LogType.Info, false, false, false, 0);
                        LoggingManager.EndLogAction($"DONE", LogType.Info);
                    else
                    {
                        if (loadProviderListsResult.IsWarning)
                        {
                            //LoggingManager.Log($"DONE BUT WARNING(S) OCCURED: {loadProviderListsResult.Message}", LogType.Info, false, false, false, 0);
                            OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Warning Occured In OASISBootLoader.LoadProviderLists. Reason: {loadProviderListsResult.Message}");
                        }
                        if (loadProviderListsResult.IsError)
                        {
                            //LoggingManager.Log($"DONE BUT ERROR(S) OCCURED: {loadProviderListsResult.Message}", LogType.Info, false, false, false, 0);
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error Occured In OASISBootLoader.LoadProviderLists. Reason: {loadProviderListsResult.Message}");
                        }
                    }

                    if (Enum.TryParse(typeof(OASISProviderBootType), OASISDNA.OASIS.StorageProviders.OASISProviderBootType,
                        out OASISProviderBootTypeObject))
                    {
                        ProviderManager.Instance.OASISProviderBootType = (OASISProviderBootType)OASISProviderBootTypeObject;

                        if (ProviderManager.Instance.OASISProviderBootType == OASISProviderBootType.Warm ||
                            ProviderManager.Instance.OASISProviderBootType == OASISProviderBootType.Hot)
                        {
                            //LoggingManager.Log($"REGISTERING PROVIDERS...", LogType.Info, true, false, false, 1, true);
                            LoggingManager.BeginLogAction($"REGISTERING PROVIDERS...", LogType.Info);
                            result = RegisterProvidersInAllLists();
                            LoggingManager.EndLogAction($"DONE", LogType.Info);
                            //LoggingManager.Log($"DONE", LogType.Info, false, false, false, 0);

                            if (activateDefaultStorageProvider)
                            {
                                LoggingManager.Log($"ACTIVATING DEFAULT PROVIDER...", LogType.Info, true);
                                OASISResult<IOASISStorageProvider> activateResult = await GetAndActivateDefaultStorageProviderAsync();

                                if (activateResult != null && activateResult.IsError)
                                    OASISErrorHandling.HandleWarning(ref result, $"Error Occured In OASISBootLoader.BootOASISAsync. Reason: GetAndActivateDefaultStorageProviderAsync returned the following error: {activateResult.Message}");
                                //else
                                //LoggingManager.Log($"DONE", LogType.Info, false, false, false, 0);
                                //LoggingManager.Log($"ACTIVATING DEFAULT PROVIDER... DONE", LogType.Info);
                            }
                        }
                        else
                        {
                            IsOASISBooted = true;
                            result.Result = true;
                            result.Message = "OASIS Booted but OASISProviderBootType is set to Cold so no providers have been registered or activated.";
                        }
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = string.Concat("OASISProviderBootType '", OASISDNA.OASIS.StorageProviders.OASISProviderBootType, "' defined in OASISDNA is invalid. Valid values are: ", EnumHelper.GetEnumValues(typeof(OASISProviderBootType), EnumHelperListType.ItemsSeperatedByComma));
                    }

                    if (result.Result && !result.IsError)
                    {
                        List<EnumValue<ProviderType>> currentProviderFailOverList = ProviderManager.Instance.GetProviderAutoFailOverList();
                        ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(ProviderManager.Instance.GetProviderAutoFailOverListForCheckIfOASISSystemAccountExists());

                        LoggingManager.BeginLogAction($"Looking For OASIS System Account For Email {SYSTEM_EMAIL}...", LogType.Info);
                        OASISResult<IAvatar> oasisSystemAccountResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(SYSTEM_EMAIL);
                        ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(currentProviderFailOverList);

                        //if (!string.IsNullOrEmpty(OASISDNA.OASIS.OASISSystemAccountId))
                        if (oasisSystemAccountResult != null && oasisSystemAccountResult.Result != null && !oasisSystemAccountResult.IsError)
                        {
                            LoggingManager.EndLogAction($"DONE", LogType.Info);
                            LoggingManager.Log($"OASIS System Account Found: Id: {oasisSystemAccountResult.Result.Id}, Name: {oasisSystemAccountResult.Result.Name}, Email: {oasisSystemAccountResult.Result.Email}, Username: {oasisSystemAccountResult.Result.Username}, Type: {oasisSystemAccountResult.Result.AvatarType.Name}.", LogType.Info);

                            if (oasisSystemAccountResult.Result.AvatarType.Value != AvatarType.System || oasisSystemAccountResult.Result.Name != "OASIS SYSTEM" || oasisSystemAccountResult.Result.Username != "root")
                                LoggingManager.Log($"OASIS System Account Invalid! Please change the SmtpUser to a different email in the Email section in the OASISDNA and then boot the OASIS again so a new OASIS System Account can be generated using the new email.", LogType.Error);
                            else
                            {
                                //Make sure the OASISDNA is updated with the right id.
                                OASISDNA.OASIS.OASISSystemAccountId = oasisSystemAccountResult.Result.Id.ToString();
                                await OASISDNAManager.SaveDNAAsync(OASISDNAPath, OASISDNA);
                            }
                        }
                        else
                        {
                            LoggingManager.EndLogAction($"DONE", LogType.Info);
                            LoggingManager.BeginLogAction($"OASIS System Account Not Found So Generating For Email {SYSTEM_EMAIL} Now...", LogType.Info);

                            //TODO: Need to make this more secure in future to prevent others creating similar accounts (but none will ever have AvatarType of System, this is the only place that can be created but we need to make sure a normal user accout is not hacked or changed to a system one. But currently it cannot do any harm because this system account is currently not used for anything, it simply creates the default OASIS Omniverse when STAR CLI first boots up on a running ONODE (before a avatar is created or logged in).
                            CLIEngine.SupressConsoleLogging = true;
                            //OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.RegisterAsync("", "OASIS", "SYSTEM", OASISDNA.OASIS.Email.SmtpUser, "", "root", AvatarType.System, OASISType.OASISBootLoader);
                            OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.RegisterAsync("", "OASIS", "SYSTEM", SYSTEM_EMAIL, "", "root", AvatarType.System, OASISType.OASISBootLoader);
                            CLIEngine.SupressConsoleLogging = false;

                            if (avatarResult != null && !avatarResult.IsError)
                            {
                                OASISDNA.OASIS.OASISSystemAccountId = avatarResult.Result.Id.ToString();
                                await OASISDNAManager.SaveDNAAsync(OASISDNAPath, OASISDNA);

                                LoggingManager.EndLogAction($"DONE", LogType.Info);
                                LoggingManager.Log($"OASIS System Account Generated: Id: {avatarResult.Result.Id}, Name: {avatarResult.Result.Name}, Email: {avatarResult.Result.Email}, Username: {avatarResult.Result.Username}, Type: {avatarResult.Result.AvatarType.Name}.", LogType.Info);
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISBootLoader.BootOASISAsync Calling AvatarManager.Instance.RegisterAsync Attempting To Create The OASISSystem Account. Reason: {avatarResult.Message}");
                        }

                        IsOASISBooted = true;
                        LoggingManager.Log($"OASIS HYPERDRIVE ONLINE.", LogType.Info);
                        LoggingManager.Log($"OASIS BOOTED.", LogType.Info);

                        //if (!string.IsNullOrEmpty(result.Message))
                        //    LoggingManager.Log($"{result.Message}", LogType.Info);

                        LoggingManager.Log($"OASIS RUNTIME VERSION: v{OASISRuntimeVersion}.", LogType.Info);
                        LoggingManager.Log($"OASIS API VERSION:     v{OASISAPIVersion}.", LogType.Info);
                        LoggingManager.Log($"COSMIC ORM VERSION:    v{COSMICVersion}.", LogType.Info);
                        LoggingManager.Log($"STAR RUNTIME VERSION:  v{STARRuntimeVersion}.", LogType.Info);
                        LoggingManager.Log($"STAR ODK VERSION:      v{STARODKVersion}.", LogType.Info);
                        LoggingManager.Log($"STARNET VERSION:       v{STARNETVersion}.", LogType.Info);
                        LoggingManager.Log($"STAR API VERSION:      v{STARAPIVersion}.", LogType.Info);
                        LoggingManager.Log($".NET VERSION:          v{DotNetVersion}.", LogType.Info);
                        //LoggingManager.Log($"OASIS RUNTIME VERSION (LIVE): {OASISDNA.OASIS.CurrentLiveVersion}.", LogType.Info);
                        //LoggingManager.Log($"OASIS RUNTIME VERSION (STAGING): {OASISDNA.OASIS.CurrentStagingVersion}.", LogType.Info);
                    }

                    IsOASISBooting = false;
                }
                else
                {
                    result.Result = false;
                    result.Message = "Already Initializing...";
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In OASISBootLoader In Method BootOASISAsync. Reason: {e.ToString()}. Stack Trace2: {e.StackTrace}");
            }

            return result;
        }

        public static OASISResult<bool> BootOASIS(bool activateDefaultStorageProvider = true)
        {
            return BootOASIS(OASISDNAPath, activateDefaultStorageProvider);
        }

        public static async Task<OASISResult<bool>> BootOASISASync(bool activateDefaultStorageProvider = true)
        {
            return await BootOASISAsync(OASISDNAPath, activateDefaultStorageProvider);
        }

        public static OASISResult<bool> ShutdownOASIS()
        {
            OASISResult<bool> result = new OASISResult<bool>(true);
            LoggingManager.Log($"SHUTTING DOWN THE OASIS... PLEASE STAND BY.", LogType.Info, true);

            foreach (IOASISStorageProvider provider in ProviderManager.Instance.GetStorageProviders())
            {
                try
                {
                    OASISResult<bool> providerResult = provider.DeActivateProvider();

                    if (providerResult != null && providerResult.IsError)
                        result.InnerMessages.Add($"Error Occured In Provider {provider.ProviderName} Calling DeActivateProvider. Reason: {providerResult.Message}");
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleWarning(ref result, $"Error Occured In OASISBootLoader In Method ShutdownOASIS Calling DeActivateProvider On Provider {provider.ProviderName}. Reason: {e}", true);
                }
            }

            string message = "";

            if (result.WarningCount > 0)
                message = $"{result.WarningCount} Providers Failed To ShutDown. Details: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}.\n\nCheck The Logs For More Detailed Info...";

            LoggingManager.Log($"OASIS SHUTDOWN. {message}", LogType.Info);
            return result;
        }

        public static async Task<OASISResult<bool>> ShutdownOASISAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>(true);
            LoggingManager.Log($"SHUTTING DOWN THE OASIS... PLEASE STAND BY.", LogType.Info, true);

            foreach (IOASISStorageProvider provider in ProviderManager.Instance.GetStorageProviders())
            {
                try
                {
                    OASISResult<bool> providerResult = await provider.DeActivateProviderAsync();

                    if (providerResult != null && providerResult.IsError)
                        result.InnerMessages.Add($"Error Occured In Provider {provider.ProviderName} Calling DeActivateProviderAsync. Reason: {providerResult.Message}");
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleWarning(ref result, $"Error Occured In OASISBootLoader In Method ShutdownOASIS Calling DeActivateProviderAsync On Provider {provider.ProviderName}. Reason: {e}", true);
                }
            }

            string message = "";

            if (result.WarningCount > 0)
                message = $"{result.WarningCount} Providers Failed To ShutDown. Details: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}.\n\nCheck The Logs For More Detailed Info...";

            LoggingManager.Log($"OASIS SHUTDOWN. {message}", LogType.Info);
            return result;
        }

        public static OASISResult<IOASISStorageProvider> GetAndActivateDefaultStorageProvider()
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();

            try
            {
                if (ProviderManager.Instance.CurrentStorageProvider == null)
                {
                    if (!IsOASISBooted)
                    {
                        OASISResult<bool> initResult = BootOASIS(OASISDNAPath);

                        if (initResult.IsError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error Occured in OASISBootLoader.GetAndActivateDefaultStorageProvider calling BootOASISAsync. Reason: {initResult.Message}");
                            return result;
                        }
                    }

                    foreach (EnumValue<ProviderType> providerType in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        OASISResult<IOASISStorageProvider> providerManagerResult = GetAndActivateStorageProvider(providerType.Value);

                        if ((providerManagerResult.IsError || providerManagerResult.Result == null))
                        {
                            OASISErrorHandling.HandleError(ref result, providerManagerResult.Message);
                            result.InnerMessages.Add(providerManagerResult.Message);
                            result.IsWarning = true;
                            result.IsError = false;

                            if (!ProviderManager.Instance.IsAutoFailOverEnabled)
                                break;
                        }
                        else
                            break;
                    }

                    result = ProcessResults(result);
                }
                else
                    result.Result = ProviderManager.Instance.CurrentStorageProvider;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In OASISBootLoader In Method GetAndActivateDefaultStorageProvider. Reason: {e}");
            }

            return result;
        }

        public static async Task<OASISResult<IOASISStorageProvider>> GetAndActivateDefaultStorageProviderAsync()
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();

            try
            {
                if (ProviderManager.Instance.CurrentStorageProvider == null)
                {
                    if (!IsOASISBooted && !IsOASISBooting)
                    {
                        OASISResult<bool> initResult = await BootOASISAsync(OASISDNAPath);

                        if (initResult.IsError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error Occured in OASISBootLoader.GetAndActivateDefaultStorageProviderAsync calling BootOASISAsync. Reason: {initResult.Message}");
                            return result;
                        }
                    }

                    foreach (EnumValue<ProviderType> providerType in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        OASISResult<IOASISStorageProvider> providerManagerResult = await GetAndActivateStorageProviderAsync(providerType.Value);

                        if ((providerManagerResult.IsError || providerManagerResult.Result == null))
                        {
                            //OASISErrorHandling.HandleWarning(ref result, providerManagerResult.Message);
                            result.IsWarning = true;
                            result.InnerMessages.Add(providerManagerResult.Message);

                            if (!ProviderManager.Instance.IsAutoFailOverEnabled)
                                break;
                        }
                        else
                        {
                            result.Result = providerManagerResult.Result;
                            break;
                        }
                    }

                    result = ProcessResults(result);
                }
                else
                    result.Result = ProviderManager.Instance.CurrentStorageProvider;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In OASISBootLoader In Method GetAndActivateDefaultStorageProviderAsync. Reason: {e}");
            }

            return result;
        }

        public static OASISResult<IOASISStorageProvider> GetAndActivateStorageProvider(ProviderType providerType, string customConnectionString = null, bool forceRegister = false, bool setGlobally = false)
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();

            try
            {
                if (!IsOASISBooted && !IsOASISBooting)
                {
                    OASISResult<bool> bootResult = BootOASIS(OASISDNAPath);

                    if (bootResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error booting OASIS. Reason: ", bootResult.Message));
                        return result;
                    }
                }

                //TODO: Think we can have this in ProviderManger and have default connection strings/settings for each provider.
                if (providerType != ProviderManager.Instance.CurrentStorageProviderType.Value)
                {
                    RegisterProvider(providerType, customConnectionString, forceRegister);
                    result = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType, setGlobally);
                }

                if (result.IsError != true)
                {
                    if (setGlobally && ProviderManager.Instance.CurrentStorageProvider !=
                        ProviderManager.Instance.DefaultGlobalStorageProvider)
                        ProviderManager.Instance.DefaultGlobalStorageProvider = ProviderManager.Instance.CurrentStorageProvider;

                    ProviderManager.Instance.OverrideProviderType = true;
                    result.Result = ProviderManager.Instance.CurrentStorageProvider;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In OASISBootLoader In Method GetAndActivateStorageProvider. Reason: {e}");
            }

            return result;
        }

        public static async Task<OASISResult<IOASISStorageProvider>> GetAndActivateStorageProviderAsync(ProviderType providerType, string customConnectionString = null, bool forceRegister = false, bool setGlobally = false)
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();

            try
            {
                if (!IsOASISBooted && !IsOASISBooting)
                {
                    OASISResult<bool> bootResult = BootOASIS(OASISDNAPath);

                    if (bootResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error booting OASIS. Reason: ", bootResult.Message));
                        return result;
                    }
                }

                //TODO: Think we can have this in ProviderManger and have default connection strings/settings for each provider.
                if (providerType != ProviderManager.Instance.CurrentStorageProviderType.Value)
                {
                    await RegisterProviderAsync(providerType, customConnectionString, forceRegister);
                    result = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType, setGlobally);
                }

                if (result.IsError != true)
                {
                    if (setGlobally && ProviderManager.Instance.CurrentStorageProvider !=
                        ProviderManager.Instance.DefaultGlobalStorageProvider)
                        ProviderManager.Instance.DefaultGlobalStorageProvider = ProviderManager.Instance.CurrentStorageProvider;

                    ProviderManager.Instance.OverrideProviderType = true;
                    result.Result = ProviderManager.Instance.CurrentStorageProvider;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In OASISBootLoader In Method GetAndActivateStorageProviderAsync. Reason: {e}");
            }

            return result;
        }

        public static OASISResult<IOASISStorageProvider> RegisterProvider(ProviderType providerType, string overrideConnectionString = null, bool forceRegister = false, bool activateProviderIfOASISProviderBootTypeIsHot = true)
        {
            OASISResult<IOASISStorageProvider> result = null;

            try
            {
                if (!IsOASISBooted && !IsOASISBooting)
                    BootOASIS(OASISDNAPath);

                result = RegisterProviderInternal(providerType, overrideConnectionString, forceRegister);

                if (ProviderManager.Instance.OASISProviderBootType == OASISProviderBootType.Hot && activateProviderIfOASISProviderBootTypeIsHot)
                    ProviderManager.Instance.ActivateProvider(result.Result);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In OASISBootLoader In Method RegisterProvider. Reason: {e}");
            }

            return result;
        }

        public static async Task<OASISResult<IOASISStorageProvider>> RegisterProviderAsync(ProviderType providerType, string overrideConnectionString = null, bool forceRegister = false, bool activateProviderIfOASISProviderBootTypeIsHot = true)
        {
            OASISResult<IOASISStorageProvider> result = null;

            try
            {
                if (!IsOASISBooted && !IsOASISBooting)
                    BootOASIS(OASISDNAPath);

                result = RegisterProviderInternal(providerType, overrideConnectionString, forceRegister);

                if (ProviderManager.Instance.OASISProviderBootType == OASISProviderBootType.Hot && activateProviderIfOASISProviderBootTypeIsHot)
                    await ProviderManager.Instance.ActivateProviderAsync(result.Result);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In OASISBootLoader In Method RegisterProviderAsync. Reason: {e}");
            }

            return result;
        }

        public static OASISResult<bool> RegisterProvidersInAutoFailOverList(bool abortIfOneProviderFailsToRegister = false)
        {
            return RegisterProviders(ProviderManager.Instance.GetProviderAutoFailOverList(), abortIfOneProviderFailsToRegister);
        }

        public static OASISResult<bool> RegisterProvidersInAutoFailOverListForAvatarLogin(bool abortIfOneProviderFailsToRegister = false)
        {
            return RegisterProviders(ProviderManager.Instance.GetProviderAutoFailOverListForAvatarLogin(), abortIfOneProviderFailsToRegister);
        }

        public static OASISResult<bool> RegisterProvidersInAutoFailOverListForCheckIfEmailAlreadyInUse(bool abortIfOneProviderFailsToRegister = false)
        {
            return RegisterProviders(ProviderManager.Instance.GetProviderAutoFailOverListForCheckIfEmailAlreadyInUse(), abortIfOneProviderFailsToRegister);
        }

        public static OASISResult<bool> RegisterProvidersInAutoFailOverListForCheckIfUsernameAlreadyInUse(bool abortIfOneProviderFailsToRegister = false)
        {
            return RegisterProviders(ProviderManager.Instance.GetProviderAutoFailOverListForCheckIfUsernameAlreadyInUse(), abortIfOneProviderFailsToRegister);
        }

        public static OASISResult<bool> RegisterProvidersInAutoLoadBalanceList(bool abortIfOneProviderFailsToRegister = false)
        {
            return RegisterProviders(ProviderManager.Instance.GetProviderAutoLoadBalanceList(),
                abortIfOneProviderFailsToRegister);
        }

        public static OASISResult<bool> RegisterProvidersInAutoReplicatingList(bool abortIfOneProviderFailsToRegister = false)
        {
            return RegisterProviders(ProviderManager.Instance.GetProvidersThatAreAutoReplicating(),
                abortIfOneProviderFailsToRegister);
        }

        public static OASISResult<bool> RegisterProvidersInAllLists(bool abortIfOneProviderFailsToRegister = false)
        {
            OASISResult<bool> result = new OASISResult<bool>(true);

            result = ProcessResult("AutoFailOverList",
                RegisterProvidersInAutoFailOverList(abortIfOneProviderFailsToRegister), result);

            if (result.IsError && abortIfOneProviderFailsToRegister)
                return result;

            result = ProcessResult("AutoFailOverListForAvatarLogin",
                RegisterProvidersInAutoFailOverListForAvatarLogin(abortIfOneProviderFailsToRegister), result);

            if (result.IsError && abortIfOneProviderFailsToRegister)
                return result;

            result = ProcessResult("AutoFailOverListForCheckIfEmailAlreadyInUse",
                RegisterProvidersInAutoFailOverListForCheckIfEmailAlreadyInUse(abortIfOneProviderFailsToRegister), result);

            if (result.IsError && abortIfOneProviderFailsToRegister)
                return result;

            result = ProcessResult("AutoFailOverListForCheckIfUsernameAlreadyInUse",
                RegisterProvidersInAutoFailOverListForCheckIfUsernameAlreadyInUse(abortIfOneProviderFailsToRegister), result);

            if (result.IsError && abortIfOneProviderFailsToRegister)
                return result;

            result = ProcessResult("AutoLoadBalanceList",
                RegisterProvidersInAutoLoadBalanceList(abortIfOneProviderFailsToRegister), result);

            if (result.IsError && abortIfOneProviderFailsToRegister)
                return result;

            result = ProcessResult("AutoReplicatingList",
                RegisterProvidersInAutoReplicatingList(abortIfOneProviderFailsToRegister), result);

            return result;
        }

        public static OASISResult<bool> RegisterProviders(List<EnumValue<ProviderType>> providerTypes, bool abortIfOneProviderFailsToRegister = false)
        {
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (EnumValue<ProviderType> providerType in providerTypes)
                providerTypesList.Add(providerType.Value);

            return RegisterProviders(providerTypesList, abortIfOneProviderFailsToRegister);
        }

        public static OASISResult<bool> RegisterProviders(List<ProviderType> providerTypes, bool abortIfOneProviderFailsToRegister = false)
        {
            OASISResult<bool> result = new OASISResult<bool>(true);

            foreach (ProviderType providerType in providerTypes)
            {
                // If a provider fails to register then log it and add to returned error message but continue onto the next provider in the list...
                if (RegisterProvider(providerType) == null)
                {
                    result.Result = false;
                    result.IsError = true;

                    string errorMessage = string.Concat("OASIS Provider ", Enum.GetName(typeof(ProviderType), providerType), " failed to register.\n");
                    result.Message = string.Concat(result.Message, errorMessage);
                    LoggingManager.Log(errorMessage, LogType.Error);

                    if (abortIfOneProviderFailsToRegister)
                        break;
                }
            }

            return result;
        }

        private static OASISResult<IOASISStorageProvider> RegisterProviderInternal(ProviderType providerType, string overrideConnectionString = null, bool forceRegister = false)
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();

            try
            {
                // If they wish to forceRegister then if it is already registered then unregister it first (when connectionstring changes for example).
                if (forceRegister && ProviderManager.Instance.IsProviderRegistered(providerType))
                    ProviderManager.Instance.UnRegisterProvider(providerType);

                if (!ProviderManager.Instance.IsProviderRegistered(providerType))
                {
                    switch (providerType)
                    {
                        // TODO: Fix HoloOASIS - missing holochain-client-csharp dependency (excluded from Docker build)
                        //case ProviderType.HoloOASIS:
                        //    {
                        //        HoloOASIS holoOASIS = new HoloOASIS(
                        //                    overrideConnectionString == null
                        //                        ? OASISDNA.OASIS.StorageProviders.HoloOASIS.LocalNodeURI
                        //                        : overrideConnectionString, 
                        //                    OASISDNA.OASIS.StorageProviders.HoloOASIS.HoloNetworkURI, 
                        //                    OASISDNA.OASIS.StorageProviders.HoloOASIS.UseLocalNode, 
                        //                    OASISDNA.OASIS.StorageProviders.HoloOASIS.UseHoloNetwork, 
                        //                    OASISDNA.OASIS.StorageProviders.HoloOASIS.HoloNETORMUseReflection);
                        //
                        //        holoOASIS.OnStorageProviderError += HoloOASIS_StorageProviderError;
                        //        result.Result = holoOASIS;
                        //    }
                        //    break;

                        case ProviderType.SQLLiteDBOASIS:
                            {
                                //TODO: need to fix or re-write SQLLiteDBOASIS Provider ASAP!

                                SQLLiteDBOASIS SQLLiteDBOASIS = new SQLLiteDBOASIS(overrideConnectionString == null
                                    ? OASISDNA.OASIS.StorageProviders.SQLLiteDBOASIS.ConnectionString
                                    : overrideConnectionString);
                                SQLLiteDBOASIS.OnStorageProviderError += SQLLiteDBOASIS_StorageProviderError;
                                result.Result = SQLLiteDBOASIS;
                            }
                            break;

                        case ProviderType.MongoDBOASIS:
                            {
                                MongoDBOASIS mongoOASIS =
                                    new MongoDBOASIS(
                                        overrideConnectionString == null
                                            ? OASISDNA.OASIS.StorageProviders.MongoDBOASIS.ConnectionString
                                            : overrideConnectionString, OASISDNA.OASIS.StorageProviders.MongoDBOASIS.DBName, OASISDNA);
                                mongoOASIS.OnStorageProviderError += MongoOASIS_StorageProviderError;
                                result.Result = mongoOASIS;
                            }
                            break;

                        case ProviderType.SolanaOASIS:
                            {
                                SolanaOASIS solanaOasis = new(
                                    OASISDNA.OASIS.StorageProviders.SolanaOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.SolanaOASIS.PrivateKey,
                                    OASISDNA.OASIS.StorageProviders.SolanaOASIS.PublicKey);
                                solanaOasis.OnStorageProviderError += SolanaOASIS_StorageProviderError;
                                result.Result = solanaOasis;
                            }
                            break;

                        // TODO: Fix RadixOASIS provider - incomplete implementation
                        //case ProviderType.RadixOASIS:
                        //    {
                        //        RadixOASIS radixOASIS = new(
                        //            OASISDNA.OASIS.StorageProviders.RadixOASIS.HostUri,
                        //            OASISDNA.OASIS.StorageProviders.RadixOASIS.NetworkId,
                        //            OASISDNA.OASIS.StorageProviders.RadixOASIS.AccountAddress,
                        //            OASISDNA.OASIS.StorageProviders.RadixOASIS.PrivateKey);
                        //        radixOASIS.OnStorageProviderError += RadixOASIS_StorageProviderError;
                        //        result.Result = radixOASIS;
                        //    }
                        //    break;

                        case ProviderType.EOSIOOASIS:
                            {
                                EOSIOOASIS EOSIOOASIS = new EOSIOOASIS(
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountName,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ChainId,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountPrivateKey);
                                EOSIOOASIS.OnStorageProviderError += EOSIOOASIS_StorageProviderError;
                                result.Result = EOSIOOASIS;
                            }
                            break;

                        //case ProviderType.TelosOASIS:
                        //    {
                        //        TelosOASIS TelosOASIS = new TelosOASIS(
                        //            OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString,
                        //            OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountName,
                        //            OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ChainId,
                        //            OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountPrivateKey);
                        //        TelosOASIS.OnStorageProviderError += TelosOASIS_StorageProviderError;
                        //        result.Result = TelosOASIS;
                        //    }
                        //    break;

                        //case ProviderType.SEEDSOASIS:
                        //    {
                        //        SEEDSOASIS SEEDSOASIS = new SEEDSOASIS(new EOSIOOASIS(OASISDNA.OASIS.StorageProviders.SEEDSOASIS.ConnectionString));
                        //        ProviderManager.Instance.RegisterProvider(SEEDSOASIS);
                        //        registeredProvider = SEEDSOASIS;
                        //    }
                        //    break;

                        case ProviderType.Neo4jOASIS:
                            {
                                Neo4jOASIS Neo4jOASIS = new Neo4jOASIS(
                                    overrideConnectionString == null
                                        ? OASISDNA.OASIS.StorageProviders.Neo4jOASIS.ConnectionString
                                        : overrideConnectionString, OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Username,
                                    OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Password);
                                Neo4jOASIS.OnStorageProviderError += Neo4jOASIS_StorageProviderError;
                                result.Result = Neo4jOASIS;
                            }
                            break;

                        case ProviderType.IPFSOASIS:
                            {
                                IPFSOASIS IPFSOASIS = null;

                                //Example of how to pass in OASISDNA if the Provider needs to update the DNA.
                                if (overrideConnectionString != null)
                                {
                                    OASISDNA overrideDNA = OASISDNA;
                                    overrideDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString = overrideConnectionString;
                                    IPFSOASIS = new IPFSOASIS(overrideDNA, OASISDNAPath);
                                }
                                else
                                    IPFSOASIS = new IPFSOASIS(OASISDNA, OASISDNAPath);

                                IPFSOASIS.OnStorageProviderError += IPFSOASIS_StorageProviderError;
                                result.Result = IPFSOASIS;
                            }
                            break;

                        case ProviderType.PinataOASIS:
                            {
                                PinataOASIS PinataOASIS = null;

                                //Example of how to pass in OASISDNA if the Provider needs to update the DNA.
                                if (overrideConnectionString != null)
                                {
                                    OASISDNA overrideDNA = OASISDNA;
                                    overrideDNA.OASIS.StorageProviders.PinataOASIS.ConnectionString = overrideConnectionString;
                                    PinataOASIS = new PinataOASIS(overrideDNA, OASISDNAPath);
                                }
                                else
                                    PinataOASIS = new PinataOASIS(OASISDNA, OASISDNAPath);

                                PinataOASIS.OnStorageProviderError += PinataOASIS_StorageProviderError;
                                result.Result = PinataOASIS;
                            }
                            break;

                        case ProviderType.EthereumOASIS:
                            {
                                EthereumOASIS EthereumOASIS = new(
                                    OASISDNA.OASIS.StorageProviders.EthereumOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.EthereumOASIS.ChainPrivateKey,
                                    OASISDNA.OASIS.StorageProviders.EthereumOASIS.ChainId,
                                    OASISDNA.OASIS.StorageProviders.EthereumOASIS.ContractAddress);
                                EthereumOASIS.OnStorageProviderError += EthereumOASIS_StorageProviderError;
                                result.Result = EthereumOASIS;
                            }
                            break;

                        case ProviderType.ArbitrumOASIS:
                            {
                                ArbitrumOASIS ArbitrumOASIS = new(
                                    OASISDNA.OASIS.StorageProviders.ArbitrumOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.ArbitrumOASIS.ChainPrivateKey,
                                    OASISDNA.OASIS.StorageProviders.ArbitrumOASIS.ChainId,
                                    OASISDNA.OASIS.StorageProviders.ArbitrumOASIS.ContractAddress);
                                ArbitrumOASIS.OnStorageProviderError += ArbitrumOASIS_StorageProviderError;
                                result.Result = ArbitrumOASIS;
                            }
                            break;
                        case ProviderType.RootstockOASIS:
                            {
                                RootstockOASIS RootstockOASIS = new(
                                    OASISDNA.OASIS.StorageProviders.RootstockOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.RootstockOASIS.ChainPrivateKey,
                                    OASISDNA.OASIS.StorageProviders.RootstockOASIS.ContractAddress);
                                RootstockOASIS.OnStorageProviderError += RootstockOASIS_StorageProviderError;
                                result.Result = RootstockOASIS;
                            }
                            break;
                        case ProviderType.PolygonOASIS:
                            {
                                PolygonOASIS PolygonOASIS = new(
                                    OASISDNA.OASIS.StorageProviders.PolygonOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.PolygonOASIS.ChainPrivateKey,
                                    OASISDNA.OASIS.StorageProviders.PolygonOASIS.ContractAddress);
                                PolygonOASIS.OnStorageProviderError += PolygonOASIS_StorageProviderError;
                                result.Result = PolygonOASIS;
                            }
                            break;
                        case ProviderType.MonadOASIS:
                            {
                                MonadOASIS MonadoOASIS = new(
                                    OASISDNA.OASIS.StorageProviders.MonadOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.MonadOASIS.ChainPrivateKey,
                                    OASISDNA.OASIS.StorageProviders.MonadOASIS.ContractAddress);
                                MonadoOASIS.OnStorageProviderError += MonadOASIS_StorageProviderError;
                                result.Result = MonadoOASIS;
                            }
                            break;
                        case ProviderType.TONOASIS:
                            {
                                TONOASIS tonoOASIS = new(
                                    OASISDNA.OASIS.StorageProviders.TONOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.TONOASIS.ChainPrivateKey,
                                    OASISDNA.OASIS.StorageProviders.TONOASIS.ContractAddress);
                                tonoOASIS.OnStorageProviderError += TONOASIS_StorageProviderError;
                                result.Result = tonoOASIS;
                            }
                            break;

                        // TODO: Fix StarknetOASIS compilation errors before enabling
                        //case ProviderType.StarknetOASIS:
                        //    {
                        //        StarknetOASIS starknetOASIS = new StarknetOASIS(
                        //            OASISDNA.OASIS.StorageProviders.StarknetOASIS?.Network ?? "alpha-goerli",
                        //            OASISDNA.OASIS.StorageProviders.StarknetOASIS?.ApiBaseUrl ?? "https://alpha4.starknet.io");
                        //        result.Result = starknetOASIS;
                        //    }
                        //    break;

                        case ProviderType.AztecOASIS:
                            {
                                // AztecOASIS - using default constructor (will use environment variables or defaults)
                                // TODO: Add AztecOASIS configuration to OASIS_DNA.json structure
                                AztecOASIS aztecOASIS = new AztecOASIS();
                                result.Result = aztecOASIS;
                            }
                            break;

                        case ProviderType.ZcashOASIS:
                            {
                                // ZcashOASIS - using default constructor (will use environment variables or defaults)
                                // TODO: Add ZcashOASIS configuration to OASIS_DNA.json structure
                                ZcashOASIS zcashOASIS = new ZcashOASIS();
                                result.Result = zcashOASIS;
                            }
                            break;

                        case ProviderType.ThreeFoldOASIS:
                            {
                                ThreeFoldOASIS ThreeFoldOASIS = new ThreeFoldOASIS(overrideConnectionString == null
                                    ? OASISDNA.OASIS.StorageProviders.ThreeFoldOASIS.ConnectionString
                                    : overrideConnectionString);
                                ThreeFoldOASIS.OnStorageProviderError += ThreeFoldOASIS_StorageProviderError;
                                result.Result = ThreeFoldOASIS;
                            }
                            break;

                        case ProviderType.LocalFileOASIS:
                            {
                                LocalFileOASIS localFileOASIS = new LocalFileOASIS(OASISDNA.OASIS.StorageProviders.LocalFileOASIS.FilePath);
                                localFileOASIS.OnStorageProviderError += LocalFileOASIS_StorageProviderError;
                                result.Result = localFileOASIS;
                            }
                            break;

                        case ProviderType.AzureCosmosDBOASIS:
                            {
                                AzureCosmosDBOASIS azureCosmosDBOASIS = new AzureCosmosDBOASIS(
                                    new Uri(OASISDNA.OASIS.StorageProviders.AzureCosmosDBOASIS.ServiceEndpoint),
                                    OASISDNA.OASIS.StorageProviders.AzureCosmosDBOASIS.AuthKey,
                                    OASISDNA.OASIS.StorageProviders.AzureCosmosDBOASIS.DBName,
                                    ListHelper.ConvertToList(OASISDNA.OASIS.StorageProviders.AzureCosmosDBOASIS.CollectionNames));

                                azureCosmosDBOASIS.OnStorageProviderError += AzureCosmosDBOASIS_StorageProviderError;
                                result.Result = azureCosmosDBOASIS;
                            }
                            break;

                        //case ProviderType.BitcoinOASIS:
                        //    {
                        //        BitcoinOASIS bitcoinOASIS = new BitcoinOASIS(
                        //            OASISDNA.OASIS.StorageProviders.BitcoinOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.BitcoinOASIS.Network);
                        //        result.Result = bitcoinOASIS;
                        //    }
                        //    break;

                        //case ProviderType.CardanoOASIS:
                        //    {
                        //        CardanoOASIS cardanoOASIS = new CardanoOASIS(
                        //            OASISDNA.OASIS.StorageProviders.CardanoOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.CardanoOASIS.NetworkId);
                        //        result.Result = cardanoOASIS;
                        //    }
                        //    break;

                        //case ProviderType.PolkadotOASIS:
                        //    {
                        //        PolkadotOASIS polkadotOASIS = new PolkadotOASIS(
                        //            OASISDNA.OASIS.StorageProviders.PolkadotOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.PolkadotOASIS.Network);
                        //        result.Result = polkadotOASIS;
                        //    }
                        //    break;

                        //case ProviderType.BNBChainOASIS:
                        //    {
                        //        BNBChainOASIS bnbChainOASIS = new BNBChainOASIS(
                        //            OASISDNA.OASIS.StorageProviders.BNBChainOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.BNBChainOASIS.NetworkId,
                        //            OASISDNA.OASIS.StorageProviders.BNBChainOASIS.ChainId);
                        //        result.Result = bnbChainOASIS;
                        //    }
                        //    break;

                        //case ProviderType.FantomOASIS:
                        //    {
                        //        FantomOASIS fantomOASIS = new FantomOASIS(
                        //            OASISDNA.OASIS.StorageProviders.FantomOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.FantomOASIS.NetworkId,
                        //            OASISDNA.OASIS.StorageProviders.FantomOASIS.ChainId);
                        //        result.Result = fantomOASIS;
                        //    }
                        //    break;

                        //case ProviderType.OptimismOASIS:
                        //    {
                        //        OptimismOASIS optimismOASIS = new OptimismOASIS(
                        //            OASISDNA.OASIS.StorageProviders.OptimismOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.OptimismOASIS.NetworkId,
                        //            OASISDNA.OASIS.StorageProviders.OptimismOASIS.ChainId);
                        //        result.Result = optimismOASIS;
                        //    }
                        //    break;

                        //case ProviderType.ChainLinkOASIS:
                        //    {
                        //        ChainLinkOASIS chainLinkOASIS = new ChainLinkOASIS(
                        //            OASISDNA.OASIS.StorageProviders.ChainLinkOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.ChainLinkOASIS.NetworkId,
                        //            OASISDNA.OASIS.StorageProviders.ChainLinkOASIS.ChainId);
                        //        result.Result = chainLinkOASIS;
                        //    }
                        //    break;

                        //case ProviderType.ElrondOASIS:
                        //    {
                        //        ElrondOASIS elrondOASIS = new ElrondOASIS(
                        //            OASISDNA.OASIS.StorageProviders.ElrondOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.ElrondOASIS.Network,
                        //            OASISDNA.OASIS.StorageProviders.ElrondOASIS.ChainId);
                        //        result.Result = elrondOASIS;
                        //    }
                        //    break;

                        //case ProviderType.AptosOASIS:
                        //    {
                        //        AptosOASIS aptosOASIS = new AptosOASIS(
                        //            OASISDNA.OASIS.StorageProviders.AptosOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.AptosOASIS.Network,
                        //            OASISDNA.OASIS.StorageProviders.AptosOASIS.ChainId);
                        //        result.Result = aptosOASIS;
                        //    }
                        //    break;

                        //case ProviderType.TRONOASIS:
                        //    {
                        //        TRONOASIS tronOASIS = new TRONOASIS(
                        //            OASISDNA.OASIS.StorageProviders.TRONOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.TRONOASIS.Network,
                        //            OASISDNA.OASIS.StorageProviders.TRONOASIS.ChainId);
                        //        result.Result = tronOASIS;
                        //    }
                        //    break;

                        case ProviderType.HashgraphOASIS:
                            {
                                HashgraphOASIS hashgraphOASIS = new HashgraphOASIS(
                                    OASISDNA.OASIS.StorageProviders.HashgraphOASIS.RpcEndpoint,
                                    OASISDNA.OASIS.StorageProviders.HashgraphOASIS.Network,
                                    OASISDNA.OASIS.StorageProviders.HashgraphOASIS.ChainId);
                                result.Result = hashgraphOASIS;
                            }
                            break;

                        //case ProviderType.AvalancheOASIS:
                        //    {
                        //        AvalancheOASIS avalancheOASIS = new AvalancheOASIS(
                        //            OASISDNA.OASIS.StorageProviders.AvalancheOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.AvalancheOASIS.ChainPrivateKey,
                        //            OASISDNA.OASIS.StorageProviders.AvalancheOASIS.ChainId,
                        //            OASISDNA.OASIS.StorageProviders.AvalancheOASIS.ContractAddress);
                        //        result.Result = avalancheOASIS;
                        //    }
                        //    break;

                        //case ProviderType.CosmosBlockChainOASIS:
                        //    {
                        //        CosmosBlockChainOASIS cosmosOASIS = new CosmosBlockChainOASIS(
                        //            OASISDNA.OASIS.StorageProviders.CosmosBlockChainOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.CosmosBlockChainOASIS.Network,
                        //            OASISDNA.OASIS.StorageProviders.CosmosBlockChainOASIS.ChainId);
                        //        result.Result = cosmosOASIS;
                        //    }
                        //    break;

                        //case ProviderType.NEAROASIS:
                        //    {
                        //        NEAROASIS nearOASIS = new NEAROASIS(
                        //            OASISDNA.OASIS.StorageProviders.NEAROASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.NEAROASIS.Network,
                        //            OASISDNA.OASIS.StorageProviders.NEAROASIS.ChainId);
                        //        result.Result = nearOASIS;
                        //    }
                        //    break;

                        //case ProviderType.BaseOASIS:
                        //    {
                        //        BaseOASIS baseOASIS = new BaseOASIS(
                        //            OASISDNA.OASIS.StorageProviders.BaseOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.BaseOASIS.NetworkId,
                        //            OASISDNA.OASIS.StorageProviders.BaseOASIS.ChainId);
                        //        result.Result = baseOASIS;
                        //    }
                        //    break;

                        //case ProviderType.SuiOASIS:
                        //    {
                        //        SuiOASIS suiOASIS = new SuiOASIS(
                        //            OASISDNA.OASIS.StorageProviders.SuiOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.SuiOASIS.Network,
                        //            OASISDNA.OASIS.StorageProviders.SuiOASIS.ChainId);
                        //        result.Result = suiOASIS;
                        //    }
                        //    break;

                        //case ProviderType.MoralisOASIS:
                        //    {
                        //        MoralisOASIS moralisOASIS = new MoralisOASIS(
                        //            OASISDNA.OASIS.StorageProviders.MoralisOASIS.ApiKey,
                        //            OASISDNA.OASIS.StorageProviders.MoralisOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.MoralisOASIS.Network);
                        //        result.Result = moralisOASIS;
                        //    }
                        //    break;

                        //case ProviderType.TelosOASIS:
                        //    {
                        //        TelosOASIS telosOASIS = new TelosOASIS(
                        //            OASISDNA.OASIS.StorageProviders.TelosOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.TelosOASIS.Network,
                        //            OASISDNA.OASIS.StorageProviders.TelosOASIS.ChainId,
                        //            "");
                        //        result.Result = telosOASIS;
                        //    }
                        //    break;

                        //case ProviderType.ActivityPubOASIS:
                        //    {
                        //        AcitvityPubOASIS activityPubOASIS = new AcitvityPubOASIS(
                        //            OASISDNA.OASIS.StorageProviders.ActivityPubOASIS.BaseUrl,
                        //            OASISDNA.OASIS.StorageProviders.ActivityPubOASIS.UserAgent,
                        //            OASISDNA.OASIS.StorageProviders.ActivityPubOASIS.AcceptHeader,
                        //            OASISDNA.OASIS.StorageProviders.ActivityPubOASIS.TimeoutSeconds);
                        //        result.Result = activityPubOASIS;
                        //    }
                        //    break;

                        case ProviderType.GoogleCloudOASIS:
                            {
                                GoogleCloudOASIS googleCloudOASIS = new GoogleCloudOASIS(
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.ProjectId,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.BucketName,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.CredentialsPath,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.FirestoreDatabaseId,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.BigQueryDatasetId,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.EnableStorage,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.EnableFirestore,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.EnableBigQuery);
                                result.Result = googleCloudOASIS;
                            }
                            break;
                    }

                    if (result.Result != null)
                        ProviderManager.Instance.RegisterProvider(result.Result);
                }
                else
                    result.Result = (IOASISStorageProvider)ProviderManager.Instance.GetProvider(providerType);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In OASISBootLoader In Method RegisterProviderInternal. Reason: {e}");
            }

            return result;
        }

        private static OASISResult<IOASISStorageProvider> ProcessResults(OASISResult<IOASISStorageProvider> result)
        {
            if (ProviderManager.Instance.CurrentStorageProvider == null)
            {
                result.IsError = true;

                if (ProviderManager.Instance.IsAutoFailOverEnabled)
                    result.Message = $"CRITCAL ERROR: None of the OASIS Providers listed in the AutoFailOver List managed to start. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}Check logs or InnerMessages for more details. Providers in AutoFailOverList are {ProviderManager.Instance.GetProviderAutoFailOverListAsString()}.";
                else
                    result.Message = $"CRITCAL ERROR: AutoFailOver is DISABLED and the first provider in the list failed to start. Reason: {result.InnerMessages[0]}";
            }
            else if (result.InnerMessages.Count > 0)
            {
                result.IsWarning = true;
                result.Message = $"WARNING: The {ProviderManager.Instance.CurrentStorageProviderType.Name} Provider started but others failed to start. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}Please check the logs or InnerMessages for more details. Providers in AutoFailOverList are {ProviderManager.Instance.GetProviderAutoFailOverListAsString()}.";
            }

            return result;
        }

        private static OASISResult<bool> ProcessResult(string listName, OASISResult<bool> listResult, OASISResult<bool> allListResult)
        {
            if (listResult.IsError)
            {
                string errorMessage = string.Concat("Error registering providers in ", listName, ". Error Details: \n", listResult.Message);
                allListResult.IsError = true;
                allListResult.Message = string.Concat(allListResult.Message, errorMessage);
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            return allListResult;
        }

        private static OASISResult<List<ProviderType>> GetProviderTypesFromDNA(string providerListName, string providerList)
        {
            OASISResult<List<ProviderType>> result = new OASISResult<List<ProviderType>>();
            List<ProviderType> providerTypes = new List<ProviderType>();
            object providerTypeObject = null;
            string errorMessage = "Error Occured In OASISBootLoader In Method GetProviderTypesFromDNA. Reason: ";

            try
            {
                if (providerList != null)
                {
                    string[] providers = providerList.Split(",");

                    foreach (string provider in providers)
                    {
                        if (Enum.TryParse(typeof(ProviderType), provider.Trim(), out providerTypeObject))
                            providerTypes.Add((ProviderType)providerTypeObject);
                        else
                            throw new ArgumentOutOfRangeException(providerListName,
                                string.Concat("ERROR: The OASIS DNA file ", OASISDNAPath,
                                    " contains an invalid entry in the ", providerListName,
                                    " comma delimited list. Entry found was ", provider.Trim(), ". Valid entries are:\n\n",
                                    EnumHelper.GetEnumValues(typeof(ProviderType))));
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerListName} list is null! Please check the OASISDNA.json and try again.");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{e}");
            }

            result.Result = providerTypes;
            return result;
        }

        private static OASISResult<OASISDNA> LoadOASISDNA(string OASISDNAPath)
        {
            return OASISDNAManager.LoadDNA(OASISDNAPath);
        }

        private static async Task<OASISResult<OASISDNA>> LoadOASISDNAAsync(string OASISDNAPath)
        {
            return await OASISDNAManager.LoadDNAAsync(OASISDNAPath);
        }

        private static OASISResult<bool> LoadProviderLists()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error Occured In OASISBootLoader.LoadProviderLists. Reason: ";

            OASISResult<List<ProviderType>> providerTypesResult = GetProviderTypesFromDNA("AutoFailOverProviders", OASISDNA.OASIS.StorageProviders.AutoFailOverProviders);

            if (providerTypesResult != null && !providerTypesResult.IsError)
                ProviderManager.Instance.SetAutoFailOverForProviders(true, providerTypesResult.Result);
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Error Occured Calling GetProviderTypesFromDNA. Reason: {providerTypesResult.Message}");


            providerTypesResult = GetProviderTypesFromDNA("AutoFailOverProvidersForAvatarLogin", OASISDNA.OASIS.StorageProviders.AutoFailOverProvidersForAvatarLogin);

            if (providerTypesResult != null && !providerTypesResult.IsError)
                ProviderManager.Instance.SetAutoFailOverForProvidersForAvatarLogin(true, providerTypesResult.Result);
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Error Occured Calling GetProviderTypesFromDNA. Reason: {providerTypesResult.Message}");


            providerTypesResult = GetProviderTypesFromDNA("AutoFailOverProvidersForCheckIfEmailAlreadyInUse", OASISDNA.OASIS.StorageProviders.AutoFailOverProvidersForCheckIfEmailAlreadyInUse);

            if (providerTypesResult != null && !providerTypesResult.IsError)
                ProviderManager.Instance.SetAutoFailOverForProvidersForCheckIfEmailAlreadyInUse(true, providerTypesResult.Result);
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Error Occured Calling GetProviderTypesFromDNA. Reason: {providerTypesResult.Message}");


            providerTypesResult = GetProviderTypesFromDNA("AutoFailOverProvidersForCheckIfUsernameAlreadyInUse", OASISDNA.OASIS.StorageProviders.AutoFailOverProvidersForCheckIfUsernameAlreadyInUse);

            if (providerTypesResult != null && !providerTypesResult.IsError)
                ProviderManager.Instance.SetAutoFailOverForProvidersForCheckIfUsernameAlreadyInUse(true, providerTypesResult.Result);
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Error Occured Calling GetProviderTypesFromDNA. Reason: {providerTypesResult.Message}");


            providerTypesResult = GetProviderTypesFromDNA("AutoFailOverProvidersForCheckIfOASISSystemAccountExists", OASISDNA.OASIS.StorageProviders.AutoFailOverProvidersForCheckIfOASISSystemAccountExists);

            if (providerTypesResult != null && !providerTypesResult.IsError)
                ProviderManager.Instance.SetAutoFailOverForProvidersForCheckIfOASISSystemAccountExists(true, providerTypesResult.Result);
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Error Occured Calling GetProviderTypesFromDNA. Reason: {providerTypesResult.Message}");


            providerTypesResult = GetProviderTypesFromDNA("AutoLoadBalanceProviders", OASISDNA.OASIS.StorageProviders.AutoLoadBalanceProviders);

            if (providerTypesResult != null && !providerTypesResult.IsError)
                ProviderManager.Instance.SetAutoLoadBalanceForProviders(true, providerTypesResult.Result);
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Error Occured Calling GetProviderTypesFromDNA. Reason: {providerTypesResult.Message}");


            providerTypesResult = GetProviderTypesFromDNA("AutoReplicationProviders", OASISDNA.OASIS.StorageProviders.AutoReplicationProviders);

            if (providerTypesResult != null && !providerTypesResult.IsError)
                ProviderManager.Instance.SetAutoReplicationForProviders(true, providerTypesResult.Result);
            else
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Error Occured Calling GetProviderTypesFromDNA. Reason: {providerTypesResult.Message}");

            if (result.WarningCount > 0)
                result.Message = $"{result.WarningCount} Errors Occured Loading Provider Lists. Details: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

            return result;
        }

        private static void IPFSOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("IPFSOASIS", e);
        }

        private static void PinataOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("PinataOASIS", e);
        }

        private static void Neo4jOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("Neo4jOASIS", e);
        }

        private static void SQLLiteDBOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("SQLLiteDBOASIS", e);
        }

        private static void EOSIOOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("EOSIOOASIS", e);
        }

        private static void MongoOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("MongoOASIS", e);
        }

        private static void SolanaOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("SolanaOASIS", e);
        }

        // TODO: Fix RadixOASIS provider - incomplete implementation
        //private static void RadixOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        //{
        //    HandleProviderError("RadixOASIS", e);
        //}

        // TODO: Fix HoloOASIS - missing holochain-client-csharp dependency (excluded from Docker build)
        //private static void HoloOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        //{
        //    HandleProviderError("HoloOASIS", e);
        //}

        private static void AzureCosmosDBOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("AzureCosmosDBOASIS", e);
        }

        private static void LocalFileOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("LocalFileOASIS", e);
        }

        private static void ThreeFoldOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("ThreeFoldOASI", e);
        }

        private static void EthereumOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("EthereumOASIS", e);
        }

        private static void ArbitrumOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("ArbitrumOASIS", e);
        }

        private static void PolygonOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("PolygonOASIS", e);
        }

        private static void MonadOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("MonadOASIS", e);
        }

        private static void TONOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("TONOASIS", e);
        }

        private static void RootstockOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("RootstockOASIS", e);
        }

        private static void TelosOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("TelosOASIS", e);
        }

        private static void ArbitrumOASIS_OnStorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("ArbitrumOASIS", e);
        }

        private static void HandleProviderError(string providerName, OASISErrorEventArgs error)
        {
            string msg = $"Error Occured In OASISBootLoader: Reason: {providerName}_StorageProviderError: {error.Reason}, Error Details: {error.Exception}";
            OnOASISBootLoaderError?.Invoke(null, new OASISErrorEventArgs() { EndPoint = error.EndPoint, Exception = error.Exception, Reason = msg });
            OASISErrorHandling.HandleError(msg, error.Exception);
        }
    }
}
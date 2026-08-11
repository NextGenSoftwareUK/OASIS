//using NextGenSoftware.OASIS.API.Providers.TONOASIS; // Not referenced in Core Only solution
//using NextGenSoftware.OASIS.API.Providers.ZkSyncOASIS;
//using NextGenSoftware.OASIS.API.Providers.LineaOASIS;
//using NextGenSoftware.OASIS.API.Providers.ScrollOASIS;
//using NextGenSoftware.OASIS.API.Providers.XRPLOASIS;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.Logging;
using NextGenSoftware.Logging.NLogger;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;
using NextGenSoftware.OASIS.API.Providers.AptosOASIS;
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;
using NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;
using NextGenSoftware.OASIS.API.Providers.BaseOASIS;
using NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;
using NextGenSoftware.OASIS.API.Providers.BNBChainOASIS;
using NextGenSoftware.OASIS.API.Providers.CardanoOASIS;
using NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using NextGenSoftware.OASIS.API.Providers.FantomOASIS;
using NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura;
using NextGenSoftware.OASIS.API.Providers.OptimismOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.API.Providers.PolygonOASIS;
using NextGenSoftware.OASIS.API.Providers.RootstockOASIS;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using NextGenSoftware.OASIS.API.Providers.NEAROASIS;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS; // TODO: Fix TRONOASIS build errors
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Providers.ElrondOASIS;
//using NextGenSoftware.OASIS.API.Providers.PolkaDotOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader
{
    public static partial class OASISBootLoader
    {

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

                    OASISResult<bool> secretKeyResult = await OASISDNAManager.EnsureSecuritySecretKeyPersistedAsync();
                    if (secretKeyResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}{secretKeyResult.Message}");
                        IsOASISBooting = false;
                        return result;
                    }

                    if (secretKeyResult.IsWarning && !string.IsNullOrEmpty(secretKeyResult.Message))
                        OASISErrorHandling.HandleWarning(ref result, secretKeyResult.Message);

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
                    ProviderManager.Instance.IsAutoFailOverLocalProvidersEnabled = OASISDNA.OASIS.StorageProviders.AutoFailOverLocalProvidersEnabled;
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
                        LoggingManager.Log($"WEB6 API VERSION:      v{WEB6APIVersion}.", LogType.Info);
                        LoggingManager.Log($"WEB7 API VERSION:      v{WEB7APIVersion}.", LogType.Info);
                        LoggingManager.Log($"WEB8 API VERSION:      v{WEB8APIVersion}.", LogType.Info);
                        LoggingManager.Log($"WEB9 API VERSION:      v{WEB9APIVersion}.", LogType.Info);
                        LoggingManager.Log($"WEB10 API VERSION:     v{WEB10APIVersion}.", LogType.Info);
                        LoggingManager.Log($".NET VERSION:          v{DotNetVersion}.", LogType.Info);
                        //LoggingManager.Log($"OASIS RUNTIME VERSION (LIVE): {OASISDNA.OASIS.CurrentLiveVersion}.", LogType.Info);
                        //LoggingManager.Log($"OASIS RUNTIME VERSION (STAGING): {OASISDNA.OASIS.CurrentStagingVersion}.", LogType.Info);

                        OASISResult<bool> jwtReady = await OASISDNAManager.EnsureJwtSecretKeyReadyForAvatarAuthAsync();
                        if (jwtReady.IsWarning && !string.IsNullOrEmpty(jwtReady.Message))
                            LoggingManager.Log(jwtReady.Message, LogType.Warning);
                        if (jwtReady.IsError)
                            OASISErrorHandling.HandleWarning(ref result, jwtReady.Message ?? "EnsureJwtSecretKeyReadyForAvatarAuth failed.");
                        try
                        {
                            ProviderManager.Instance.OASISDNA = OASISDNA;
                        }
                        catch
                        {
                            // non-fatal
                        }
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

    }
}

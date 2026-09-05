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

    }
}

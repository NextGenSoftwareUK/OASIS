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
            string dnaPath = Environment.GetEnvironmentVariable("OASIS_DNA_PATH") ?? Path.Combine(AppContext.BaseDirectory, OASISDNAPath);

            Console.WriteLine($"2CurrentDirectory: {Environment.CurrentDirectory}");
            Console.WriteLine($"BaseDirectory: {AppContext.BaseDirectory}");
            Console.WriteLine($"DNA Path being used: {OASISDNAManager.OASISDNAPath}");
            Console.WriteLine($"AppRootDirectory: {AppPathHelper.ResolveAppRootDirectory()}");
            Console.WriteLine($"AppRootDirectory: {AppPathHelper.ResolveAppRootDirectory()}");
            Console.WriteLine($"OASISDNAPath: {OASISDNAPath}");
            Console.WriteLine($"dnaPath: {dnaPath}");

            var dnaResult = OASISDNAManager.LoadDNA(dnaPath);

            Console.WriteLine($"DNA Load Success: {!dnaResult.IsError}");
            Console.WriteLine($"DNA Load Message: {dnaResult.Message}");
            Console.WriteLine($"OASISDNA null: {OASISDNAManager.OASISDNA == null}");

            return dnaResult;
            //return OASISDNAManager.LoadDNA(OASISDNAPath);
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

            providerTypesResult = GetProviderTypesFromDNA("AutoFailOverLocalProviders", OASISDNA.OASIS.StorageProviders.AutoFailOverLocalProviders);

            if (providerTypesResult != null && !providerTypesResult.IsError)
                ProviderManager.Instance.SetAutoFailOverLocalForProviders(true, providerTypesResult.Result);
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

        private static void ArweaveOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("ArweaveOASIS", e);
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

        private static void HoloOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("HoloOASIS", e);
        }

        private static void AzureCosmosDBOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("AzureCosmosDBOASIS", e);
        }

        private static void LocalFileOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("LocalFileOASIS", e);
        }

        private static void ActivityPubOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("ActivityPubOASIS", e);
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

        private static void RootstockOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("RootstockOASIS", e);
        }

        private static void BaseOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("BaseOASIS", e);
        }

        private static void AvalancheOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("AvalancheOASIS", e);
        }

        private static void AptosOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("AptosOASIS", e);
        }

        private static void SuiOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("SuiOASIS", e);
        }

        private static void TRONOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("TRONOASIS", e);
        }

        private static void HashgraphOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("HashgraphOASIS", e);
        }

        private static void CosmosBlockChainOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("CosmosBlockChainOASIS", e);
        }

        private static void BitcoinOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("BitcoinOASIS", e);
        }

        private static void NEAROASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("NEAROASIS", e);
        }

        private static void MoralisOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("MoralisOASIS", e);
        }

        private static void TelosOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("TelosOASIS", e);
        }

        private static void SEEDSOASIS_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            HandleProviderError("SEEDSOASIS", e);
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

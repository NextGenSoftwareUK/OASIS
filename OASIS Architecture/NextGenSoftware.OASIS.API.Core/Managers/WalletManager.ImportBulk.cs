using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Rijndael256;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class WalletManager : OASISManager
    {
        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> ImportAllWalletsUsingJSONFileByIdAsync(Guid avatarId, string pathToJSONFile)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in ImportAllWalletsUsingJSONFileByIdAsync. Reason: ";
            Dictionary<ProviderType, List<ProviderWallet>> importedWallets = null;

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    importedWallets = JsonConvert.DeserializeObject<Dictionary<ProviderType, List<ProviderWallet>>>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export all function and not the export function.");
                    return result;
                }

                if (importedWallets != null)
                {
                    result.Result = new Dictionary<ProviderType, List<IProviderWallet>>();

                    foreach (ProviderType providerType in importedWallets.Keys)
                    {
                        result.Result[providerType] = new List<IProviderWallet>();
                        foreach (ProviderWallet wallet in importedWallets[providerType])
                            result.Result[providerType].Add(wallet);
                    }
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByIdAsync(avatarId);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        foreach (ProviderType providerType in result.Result.Keys)
                        {
                            if (!walletsResult.Result.ContainsKey(providerType))
                                walletsResult.Result[providerType] = new List<IProviderWallet>();

                            foreach (IProviderWallet wallet in result.Result[providerType])
                            {
                                if (!walletsResult.Result[providerType].Any(x => x.Id == wallet.Id))
                                {
                                    result.SavedCount++;
                                    walletsResult.Result[providerType].Add(wallet);
                                }
                                else
                                {
                                    CLIEngine.SupressConsoleLogging = true;
                                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} The wallet with id {wallet.Id} and name '{wallet.Name}' for provider type {Enum.GetName(typeof(ProviderType), providerType)} already exists so it cannot be imported again!");
                                    CLIEngine.SupressConsoleLogging = false;
                                }
                            }
                        }

                        OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByIdAsync(avatarId, walletsResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                            result.IsSaved = true;
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByIdAsync. Reason: {saveResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByIdAsync. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            if (!result.IsError)
            {
                if (result.WarningCount > 0)
                {
                    result.IsWarning = true;
                    result.Message = $"{result.SavedCount} Wallets Imported with {result.WarningCount} Warnings! \n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                }
                else
                    result.Message = $"{result.SavedCount} Wallets Imported Successfully";
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> ImportAllWalletsUsingJSONFileById(Guid avatarId, string pathToJSONFile)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in ImportAllWalletsUsingJSONFileById. Reason: ";
            Dictionary<ProviderType, List<ProviderWallet>> importedWallets = null;

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    importedWallets = JsonConvert.DeserializeObject<Dictionary<ProviderType, List<ProviderWallet>>>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export all function and not the export function.");
                    return result;
                }

                if (importedWallets != null)
                {
                    result.Result = new Dictionary<ProviderType, List<IProviderWallet>>();

                    foreach (ProviderType providerType in importedWallets.Keys)
                    {
                        result.Result[providerType] = new List<IProviderWallet>();
                        foreach (ProviderWallet wallet in importedWallets[providerType])
                            result.Result[providerType].Add(wallet);
                    }
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarById(avatarId);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        foreach (ProviderType providerType in result.Result.Keys)
                        {
                            if (!walletsResult.Result.ContainsKey(providerType))
                                walletsResult.Result[providerType] = new List<IProviderWallet>();

                            foreach (IProviderWallet wallet in result.Result[providerType])
                            {
                                if (!walletsResult.Result[providerType].Any(x => x.Id == wallet.Id))
                                {
                                    result.SavedCount++;
                                    walletsResult.Result[providerType].Add(wallet);
                                }
                                else
                                {
                                    CLIEngine.SupressConsoleLogging = true;
                                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} The wallet with id {wallet.Id} and name '{wallet.Name}' for provider type {Enum.GetName(typeof(ProviderType), providerType)} already exists so it cannot be imported again!");
                                    CLIEngine.SupressConsoleLogging = false;
                                }
                            }
                        }

                        OASISResult<bool> saveResult = SaveProviderWalletsForAvatarById(avatarId, walletsResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                            result.IsSaved = true;
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarById. Reason: {saveResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarById. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            if (!result.IsError)
            {
                if (result.WarningCount > 0)
                {
                    result.IsWarning = true;
                    result.Message = $"{result.SavedCount} Wallets Imported with {result.WarningCount} Warnings! \n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                }
                else
                    result.Message = $"{result.SavedCount} Wallets Imported Successfully";
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> ImportAllWalletsUsingJSONFileByUsernameAsync(string username, string pathToJSONFile)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in ImportAllWalletsUsingJSONFileByUsernameAsync. Reason: ";
            Dictionary<ProviderType, List<ProviderWallet>> importedWallets = null;

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    importedWallets = JsonConvert.DeserializeObject<Dictionary<ProviderType, List<ProviderWallet>>>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export all function and not the export function.");
                    return result;
                }

                if (importedWallets != null)
                {
                    result.Result = new Dictionary<ProviderType, List<IProviderWallet>>();

                    foreach (ProviderType providerType in importedWallets.Keys)
                    {
                        result.Result[providerType] = new List<IProviderWallet>();
                        foreach (ProviderWallet wallet in importedWallets[providerType])
                            result.Result[providerType].Add(wallet);
                    }
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(username);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        foreach (ProviderType providerType in result.Result.Keys)
                        {
                            if (!walletsResult.Result.ContainsKey(providerType))
                                walletsResult.Result[providerType] = new List<IProviderWallet>();

                            foreach (IProviderWallet wallet in result.Result[providerType])
                            {
                                if (!walletsResult.Result[providerType].Any(x => x.Id == wallet.Id))
                                {
                                    result.SavedCount++;
                                    walletsResult.Result[providerType].Add(wallet);
                                }
                                else
                                {
                                    CLIEngine.SupressConsoleLogging = true;
                                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} The wallet with id {wallet.Id} and name '{wallet.Name}' for provider type {Enum.GetName(typeof(ProviderType), providerType)} already exists so it cannot be imported again!");
                                    CLIEngine.SupressConsoleLogging = false;
                                }
                            }
                        }

                        OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByUsernameAsync(username, walletsResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                            result.IsSaved = true;
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByUsernameAsync. Reason: {saveResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByUsernameAsync. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            if (!result.IsError)
            {
                if (result.WarningCount > 0)
                {
                    result.IsWarning = true;
                    result.Message = $"{result.SavedCount} Wallets Imported with {result.WarningCount} Warnings! \n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                }
                else
                    result.Message = $"{result.SavedCount} Wallets Imported Successfully";
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> ImportAllWalletsUsingJSONFileByUsername(string username, string pathToJSONFile)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in ImportAllWalletsUsingJSONFileByUsername. Reason: ";
            Dictionary<ProviderType, List<ProviderWallet>> importedWallets = null;

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    importedWallets = JsonConvert.DeserializeObject<Dictionary<ProviderType, List<ProviderWallet>>>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export all function and not the export function.");
                    return result;
                }
                
                if (importedWallets != null)
                {
                    result.Result = new Dictionary<ProviderType, List<IProviderWallet>>();

                    foreach (ProviderType providerType in importedWallets.Keys)
                    {
                        result.Result[providerType] = new List<IProviderWallet>();
                        foreach (ProviderWallet wallet in importedWallets[providerType])
                            result.Result[providerType].Add(wallet);
                    }
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByUsername(username);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        foreach (ProviderType providerType in result.Result.Keys)
                        {
                            if (!walletsResult.Result.ContainsKey(providerType))
                                walletsResult.Result[providerType] = new List<IProviderWallet>();

                            foreach (IProviderWallet wallet in result.Result[providerType])
                            {
                                if (!walletsResult.Result[providerType].Any(x => x.Id == wallet.Id))
                                {
                                    result.SavedCount++;
                                    walletsResult.Result[providerType].Add(wallet);
                                }
                                else
                                {
                                    CLIEngine.SupressConsoleLogging = true;
                                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} The wallet with id {wallet.Id} and name '{wallet.Name}' for provider type {Enum.GetName(typeof(ProviderType), providerType)} already exists so it cannot be imported again!");
                                    CLIEngine.SupressConsoleLogging = false;
                                }
                            }
                        }

                        OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByUsername(username, walletsResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                            result.IsSaved = true;
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByUsername. Reason: {saveResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByUsername. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            if (!result.IsError)
            {
                if (result.WarningCount > 0)
                {
                    result.IsWarning = true;
                    result.Message = $"{result.SavedCount} Wallets Imported with {result.WarningCount} Warnings! \n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                }
                else
                    result.Message = $"{result.SavedCount} Wallets Imported Successfully";
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> ImportAllWalletsUsingJSONFileByEmailAsync(string email, string pathToJSONFile)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in ImportAllWalletsUsingJSONFileByEmailAsync. Reason: ";
            Dictionary<ProviderType, List<ProviderWallet>> importedWallets = null;

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    importedWallets = JsonConvert.DeserializeObject<Dictionary<ProviderType, List<ProviderWallet>>>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export all function and not the export function.");
                    return result;
                }

                if (importedWallets != null)
                {
                    result.Result = new Dictionary<ProviderType, List<IProviderWallet>>();

                    foreach (ProviderType providerType in importedWallets.Keys)
                    {
                        result.Result[providerType] = new List<IProviderWallet>();
                        foreach (ProviderWallet wallet in importedWallets[providerType])
                            result.Result[providerType].Add(wallet);
                    }
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(email);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        foreach (ProviderType providerType in result.Result.Keys)
                        {
                            if (!walletsResult.Result.ContainsKey(providerType))
                                walletsResult.Result[providerType] = new List<IProviderWallet>();

                            foreach (IProviderWallet wallet in result.Result[providerType])
                            {
                                if (!walletsResult.Result[providerType].Any(x => x.Id == wallet.Id))
                                {
                                    walletsResult.Result[providerType].Add(wallet);
                                    result.SavedCount++;
                                }
                                else
                                {
                                    CLIEngine.SupressConsoleLogging = true;
                                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} The wallet with id {wallet.Id} and name '{wallet.Name}' for provider type {Enum.GetName(typeof(ProviderType), providerType)} already exists so it cannot be imported again!");
                                    CLIEngine.SupressConsoleLogging = false;
                                }
                            }
                        }

                        OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByEmailAsync(email, walletsResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                            result.IsSaved = true;
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByEmailAsync. Reason: {saveResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByEmailAsync. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            if (!result.IsError)
            {
                if (result.WarningCount > 0)
                {
                    result.IsWarning = true;
                    result.Message = $"{result.SavedCount} Wallets Imported with {result.WarningCount} Warnings! \n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                }
                else
                    result.Message = $"{result.SavedCount} Wallets Imported Successfully";
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> ImportAllWalletsUsingJSONFileByEmail(string email, string pathToJSONFile)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in ImportAllWalletsUsingJSONFileByEmail. Reason: ";
            Dictionary<ProviderType, List<ProviderWallet>> importedWallets = null;

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    importedWallets = JsonConvert.DeserializeObject<Dictionary<ProviderType, List<ProviderWallet>>>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export all function and not the export function.");
                    return result;
                }

                if (importedWallets != null)
                {
                    result.Result = new Dictionary<ProviderType, List<IProviderWallet>>();

                    foreach (ProviderType providerType in importedWallets.Keys)
                    {
                        result.Result[providerType] = new List<IProviderWallet>();
                        foreach (ProviderWallet wallet in importedWallets[providerType])
                            result.Result[providerType].Add(wallet);
                    }
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByEmail(email);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        foreach (ProviderType providerType in result.Result.Keys)
                        {
                            if (!walletsResult.Result.ContainsKey(providerType))
                                walletsResult.Result[providerType] = new List<IProviderWallet>();

                            foreach (IProviderWallet wallet in result.Result[providerType])
                            {
                                if (!walletsResult.Result[providerType].Any(x => x.Id == wallet.Id))
                                {
                                    result.SavedCount++;
                                    walletsResult.Result[providerType].Add(wallet);
                                }
                                else
                                {
                                    CLIEngine.SupressConsoleLogging = true;
                                    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} The wallet with id {wallet.Id} and name '{wallet.Name}' for provider type {Enum.GetName(typeof(ProviderType), providerType)} already exists so it cannot be imported again!");
                                    CLIEngine.SupressConsoleLogging = false;
                                }
                            }
                        }

                        OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByEmail(email, walletsResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                            result.IsSaved = true;
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByEmail. Reason: {saveResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByEmail. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            if (!result.IsError)
            {
                if (result.WarningCount > 0)
                {
                    result.IsWarning = true;
                    result.Message = $"{result.SavedCount} Wallets Imported with {result.WarningCount} Warnings! \n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                }
                else
                    result.Message = $"{result.SavedCount} Wallets Imported Successfully";
            }

            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingPrivateKeyById(Guid avatarId, string key, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPrivateKeyToAvatarById(Guid.Empty, avatarId, providerToImportTo, key);
        }

        public OASISResult<IProviderWallet> ImportWalletUsingPrivateKeyByUsername(string username, string key, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPrivateKeyToAvatarByUsername(Guid.Empty, username, providerToImportTo, key);
        }

        public OASISResult<IProviderWallet> ImportWalletUsingPrivateKeyByEmail(string email, string key, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPrivateKeyToAvatarByUsername(Guid.Empty, email, providerToImportTo, key);
        }

        public OASISResult<IProviderWallet> ImportWalletUsingPublicKeyById(Guid avatarId, string key, string walletAddress, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPublicKeyToAvatarById(Guid.Empty, avatarId, providerToImportTo, key, walletAddress);
        }

        public OASISResult<IProviderWallet> ImportWalletUsingPublicKeyByUsername(string username, string key, string walletAddress, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPublicKeyToAvatarByUsername(Guid.Empty, username, providerToImportTo, key, walletAddress);
        }

        public OASISResult<IProviderWallet> ImportWalletUsingPublicKeyByEmail(string email, string key, string walletAddress, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPublicKeyToAvatarByEmail(Guid.Empty, email, providerToImportTo, key, walletAddress);
        }

    }
}
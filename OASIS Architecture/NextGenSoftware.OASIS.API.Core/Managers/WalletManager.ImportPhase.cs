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
        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingSecretPhaseByIdAsync(Guid avatarId, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseByIdAsync. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByIdAsync(avatarId, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingSecretPhaseById(Guid avatarId, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseById. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarById(avatarId, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingSecretPhaseByUsernameAsync(string username, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseByUsernameAsync. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(username, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingSecretPhaseByUsername(string username, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseByUsername. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByUsername(username, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingSecretPhaseByEmailAsync(string email, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseByEmailAsync. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(email, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingSecretPhaseByEmail(string email, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseByEmail. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByEmail(email, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingJSONFileByIdAsync(Guid avatarId, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFileByIdAsync. Reason: ";

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export function and not the export all function.");
                    return result;
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByIdAsync(avatarId);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByIdAsync(avatarId, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByIdAsync. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByIdAsync. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingJSONFileById(Guid avatarId, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFileById. Reason: ";

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export function and not the export all function.");
                    return result;
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarById(avatarId);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarById(avatarId, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarById. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarById. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingJSONFileByUsernameAsync(string username, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFileByUsernameAsync. Reason: ";

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export function and not the export all function.");
                    return result;
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(username);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByUsernameAsync(username, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByUsernameAsync. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByUsernameAsync. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingJSONFileByUsername(string username, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFileByUsername. Reason: ";

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export function and not the export all function.");
                    return result;
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByUsername(username);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByUsername(username, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByUsername. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByUsername. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingJSONFileByEmailAsync(string email, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFileByEmailAsync. Reason: ";

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export function and not the export all function.");
                    return result;
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(email);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByEmailAsync(email, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByEmailAsync. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByEmailAsync. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingJSONFileByEmail(string email, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFileByEmail. Reason: ";

            try
            {
                if (!File.Exists(pathToJSONFile))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} was not found!");
                    return result;
                }

                try
                {
                    result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The json file {pathToJSONFile} is invalid! Make sure you only import files exported using the export function and not the export all function.");
                    return result;
                }

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByEmail(email);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByEmail(email, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByEmail. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByEmail. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

    }
}
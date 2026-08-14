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
    public partial class WalletManager
    {
        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdUsingHyperDriveAsync(Guid id, bool showOnlyDefault = false, bool showPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result =
                new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByIdAsync(id, showOnlyDefault, showPrivateKeys, providerTypeToLoadFrom: type.Value);
                result.Result = walletsResult.Result;

                if (!walletsResult.IsError && walletsResult.Result != null)
                    break;
                else
                    OASISErrorHandling.HandleWarning(ref result, $"Error occured in LoadProviderWalletsForAvatarByIdUsingHyperDriveAsync in WalletManager loading wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (result.Result == null || result.IsError)
                OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load wallets for avatar with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
            {
                result.IsLoaded = true;

                if (result.WarningCount > 0)
                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with id ", id, " loaded it's wallets successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsingHyperDriveId(Guid id, bool showOnlyDefault = false, bool showPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result =
                new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarById(id, showOnlyDefault, showPrivateKeys, providerTypeToLoadFrom: type.Value);
                result.Result = walletsResult.Result;

                if (!walletsResult.IsError && walletsResult.Result != null)
                    break;
                else
                    OASISErrorHandling.HandleWarning(ref result, $"Error occured in LoadProviderWalletsForAvatarByUsingHyperDriveId in WalletManager loading wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (result.Result == null || result.IsError)
                OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load wallets for avatar with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
            {
                result.IsLoaded = true;

                if (result.WarningCount > 0)
                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with id ", id, " loaded it's wallets successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsernameUsingHyperDrive(string username, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByUsernameUsingHyperDrive method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(username, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameUsingHyperDriveAsync(string username, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false,  ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByUsernameUsingHyperDriveAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmailUsingHyperDrive(string email, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByEmailUsingHyperDrive method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByEmail(email, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailUsingHyperDriveAsync(string email, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByEmailUsingHyperDriveAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(email, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

  
        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            if (wallets == null)
            {
                result.IsError = true;
                result.Message = "The wallets dictionary is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarById method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                //providerType = ProviderType.LocalFileOASIS; //TODO: TEMP!
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);
                errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    //Make sure private keys are ONLY stored locally.
                    if (ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocal || ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                    {
                        //TODO: Was going to load the private keys from the local storage and then restore any missing private keys before saving (in case they had been removed before saving to a non-local storage provider) but then there will be no way of knowing if the keys have been removed by the user (if they were then this would then incorrectly restore them again!).
                        //Commented out code was an alternative to saving the private keys seperatley as the next block below does...
                        //(result, IAvatar originalAvatar) = OASISResultHelper<IAvatar, IAvatar>.UnWrapOASISResult(ref result, LoadAvatar(avatar.Id, true, providerType), String.Concat(errorMessage, "Error loading avatar. Reason: {0}"));

                        //if (!result.IsError)
                        //{

                        //}


                        //We need to save the wallets (with private keys) seperatley to the local storage provider otherwise the next time a non local provider replicates to local it will overwrite the wallets and private keys (will be blank).
                        //TODO: The PrivateKeys are already encrypted but I want to add an extra layer of protection to encrypt the full wallet! ;-)
                        //TODO: Soon will also add a 3rd level of protection by quantum encrypting the keys/wallets... :)

                        var walletsTask = Task.Run(() => ((IOASISLocalStorageProvider)providerResult.Result).SaveProviderWalletsForAvatarById(id, wallets));

                        if (walletsTask.Wait(TimeSpan.FromSeconds(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)))
                        {
                            if (walletsTask.Result.IsError || !walletsTask.Result.Result)
                            {
                                if (string.IsNullOrEmpty(walletsTask.Result.Message))
                                    walletsTask.Result.Message = "Unknown error occured saving provider wallets.";

                                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, walletsTask.Result.Message), walletsTask.Result.DetailedMessage);
                            }
                            else
                            {
                                result.Result = true;
                                result.IsSaved = true;
                            }
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "timeout occured saving provider wallets."));
                    }
                    //else
                    //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The providerType ProviderCategory must be either StorageLocal or StorageLocalAndNetwork.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"An error occured setting the provider {providerType}. Reason: ", providerResult.Message), providerResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            if (wallets == null)
            {
                result.IsError = true;
                result.Message = "The wallets dictionary is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarByIdAsync method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                //providerType = ProviderType.LocalFileOASIS; //TODO:Temp!

                CLIEngine.SupressConsoleLogging = true;
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                CLIEngine.SupressConsoleLogging = false;

                errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    //Make sure private keys are ONLY stored locally.
                    //if (ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocal || ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                    //{
                        //TODO: Was going to load the private keys from the local storage and then restore any missing private keys before saving (in case they had been removed before saving to a non-local storage provider) but then there will be no way of knowing if the keys have been removed by the user (if they were then this would then incorrectly restore them again!).
                        //Commented out code was an alternative to saving the private keys seperatley as the next block below does...
                        //(result, IAvatar originalAvatar) = OASISResultHelper<IAvatar, IAvatar>.UnWrapOASISResult(ref result, LoadAvatar(avatar.Id, true, providerType), String.Concat(errorMessage, "Error loading avatar. Reason: {0}"));

                        //if (!result.IsError)
                        //{

                        //}


                        //We need to save the wallets (with private keys) seperatley to the local storage provider otherwise the next time a non local provider replicates to local it will overwrite the wallets and private keys (will be blank).
                        //TODO: The PrivateKeys are already encrypted but I want to add an extra layer of protection to encrypt the full wallet! ;-)
                        //TODO: Soon will also add a 3rd level of protection by quantum encrypting the keys/wallets... :)

                        var walletsTask = ((IOASISLocalStorageProvider)providerResult.Result).SaveProviderWalletsForAvatarByIdAsync(id, wallets);

                        if (await Task.WhenAny(walletsTask, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == walletsTask)
                        {
                            if (walletsTask.Result.IsError || !walletsTask.Result.Result)
                            {
                                if (string.IsNullOrEmpty(walletsTask.Result.Message))
                                    walletsTask.Result.Message = "Unknown error occured saving provider wallets.";

                                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, walletsTask.Result.Message), walletsTask.Result.DetailedMessage);
                            }
                            else
                            {
                                result.Result = true;
                                result.IsSaved = true;
                            }
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "timeout occured saving provider wallets."));
                    //}
                   // else
                    //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The providerType ProviderCategory must be either StorageLocal or StorageLocalAndNetwork.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"An error occured setting the provider {providerType}. Reason: ", providerResult.Message), providerResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarByUsername(string username, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            if (wallets == null)
            {
                result.IsError = true;
                result.Message = "The wallets dictionary is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarByUsername method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(username, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = SaveProviderWalletsForAvatarById(avatarResult.Result.Id, wallets, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load for provider {providerType}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByUsernameAsync(string username, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            if (wallets == null)
            {
                result.IsError = true;
                result.Message = "The wallets dictionary is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarByUsernameAsync method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = SaveProviderWalletsForAvatarById(avatarResult.Result.Id, wallets, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load for provider {providerType}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarByEmail(string email, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            if (wallets == null)
            {
                result.IsError = true;
                result.Message = "The wallets dictionary is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarByEmail method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByEmail(email, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = SaveProviderWalletsForAvatarById(avatarResult.Result.Id, wallets, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load for provider {providerType}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByEmailAsync(string email, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            if (wallets == null)
            {
                result.IsError = true;
                result.Message = "The wallets dictionary is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarByEmailAsync method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(email, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = SaveProviderWalletsForAvatarById(avatarResult.Result.Id, wallets, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load for provider {providerType}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }


        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> wallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            if (wallets == null)
            {
                result.IsError = true;
                result.Message = "The wallets dictionary is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            //TODO: TEMP!
            OASISResult<bool> walletsResult = SaveProviderWalletsForAvatarById(id, wallets, ProviderType.LocalFileOASIS);
            
            if (walletsResult != null && walletsResult.Result != null)
                result.Result = walletsResult.Result;


            //TODO: May add local storage providers to their own list? To save looping through lots of non-local ones or is this not really needed? :)
            //foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
            //{
            //    OASISResult<bool> walletsResult = SaveProviderWalletsForAvatarById(id, wallets, type.Value);
            //    result.Result = walletsResult.Result;

            //    if (!walletsResult.IsError && walletsResult.Result)
            //    {
            //        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            //        break;
            //    }
            //    else
            //        OASISErrorHandling.HandleWarning(ref result, $"Error occured in SaveProviderWalletsForAvatarById in WalletManager saving wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            //}

            if (!result.Result || result.IsError)
                OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save wallets for avatar with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
            {
                result.IsSaved = true;

                if (result.WarningCount > 0)
                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar wallets ", id, " successfully saved for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                    result.Message = "Avatar Wallets Successfully Saved.";

                //TODO: Need to move into background thread ASAP!
                //TODO: Even if all providers failed above, we should still attempt again in a background thread for a fixed number of attempts (default 3) every X seconds (default 5) configured in OASISDNA.json.
                //TODO: Auto-Failover should also re-try in a background thread after reporting the intial error above and then report after the retries either failed or succeeded later...
                //if (ProviderManager.Instance.IsAutoReplicationEnabled)
                //{
                //    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                //    {
                //        if (type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                //        {
                //            OASISResult<bool> walletsResult = SaveProviderWalletsForAvatarById(id, wallets, type.Value);
                //            result.Result = walletsResult.Result;

                //            if (walletsResult.IsError || !walletsResult.Result)
                //                OASISErrorHandling.HandleWarning(ref result, $"Error occured in LoadProviderWalletsForAvatarById in WalletManager saving wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
                //        }
                //    }

                //    if (result.WarningCount > 0)
                //        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar wallets ", id, " successfully saved for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                //    else
                //        LoggingManager.Log("Avatar Wallets Successfully Saved/Replicated", LogType.Info, ref result, true, false);
                //}
            }

            ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> wallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            if (wallets == null)
            {
                result.IsError = true;
                result.Message = "The wallets dictionary is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            //TODO: May add local storage providers to their own list? To save looping through lots of non-local ones or is this not really needed? :)
            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
            {
                OASISResult<bool> walletsResult = await SaveProviderWalletsForAvatarByIdAsync(id, wallets, type.Value);
                result.Result = walletsResult.Result;

                if (!walletsResult.IsError && walletsResult.Result)
                {
                    previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                    break;
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"Error occured in SaveProviderWalletsForAvatarById in WalletManager saving wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (!result.Result || result.IsError)
                OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save wallets for avatar with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
            {
                result.IsSaved = true;

                if (result.WarningCount > 0)
                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar wallets ", id, " successfully saved for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                    result.Message = "Avatar Wallets Successfully Saved.";

                //TODO: Need to move into background thread ASAP!
                //TODO: Even if all providers failed above, we should still attempt again in a background thread for a fixed number of attempts (default 3) every X seconds (default 5) configured in OASISDNA.json.
                //TODO: Auto-Failover should also re-try in a background thread after reporting the intial error above and then report after the retries either failed or succeeded later...
                if (ProviderManager.Instance.IsAutoReplicationEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                    {
                        if (type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            OASISResult<bool> walletsResult = await SaveProviderWalletsForAvatarByIdAsync(id, wallets, type.Value);
                            result.Result = walletsResult.Result;

                            if (walletsResult.IsError || !walletsResult.Result)
                                OASISErrorHandling.HandleWarning(ref result, $"Error occured in SaveProviderWalletsForAvatarByIdAsync in WalletManager saving wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
                        }
                    }

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar wallets ", id, " successfully saved for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                    else
                        LoggingManager.Log("Avatar Wallets Successfully Saved/Replicated", LogType.Info, ref result, true, false);
                }
            }

            CLIEngine.SupressConsoleLogging = true;
            await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            CLIEngine.SupressConsoleLogging = false;

            return result;
        }

    }
}

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
        public async Task<OASISResult<IProviderWallet>> LoadProviderWalletForAvatarByIdAsync(Guid avatarId, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByIdAsync(avatarId, false, showPrivateKeys, showSecretWords, providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            result.IsLoaded = true;
                            result.IsError = false;

                            //OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, avatarId, providerType);

                            //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            //{
                            //    result.Result = walletResult.Result;
                            //    result.IsLoaded = true;
                            //    result.IsError = false;
                            //}
                            //else
                            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> LoadProviderWalletForAvatarById(Guid avatarId, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarById(avatarId, providerTypeToLoadFrom: providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            result.IsLoaded = true;
                            result.IsError = false;

                            //OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, avatarId, providerType);

                            //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            //{
                            //    result.Result = walletResult.Result;
                            //    result.IsLoaded = true;
                            //    result.IsError = false;
                            //}
                            //else
                            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");s
                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> LoadProviderWalletForAvatarByUsernameAsync(string username, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByUsernameAsync(username, false, showPrivateKeys, showSecretWords, providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            //TODO: Check that avatarId isnt needed here (hopefully privatekey should already be loaded from the local provider!)
                            result.IsLoaded = true;
                            result.IsError = false;

                            //OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, avatarId, providerType);

                            //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            //{
                            //    result.Result = walletResult.Result;
                            //    result.IsLoaded = true;
                            //    result.IsError = false;
                            //}
                            //else
                            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");
                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> LoadProviderWalletForAvatarByUsername(string username, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByUsername(username, providerTypeToLoadFrom: providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            result.IsLoaded = true;
                            result.IsError = false;

                            //OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, avatarId, providerType);

                            //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            //{
                            //    result.Result = walletResult.Result;
                            //    result.IsLoaded = true;
                            //    result.IsError = false;
                            //}
                            //else
                            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");
                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> LoadProviderWalletForAvatarByEmailAsync(string email, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByEmailAsync(email, false, showPrivateKeys, showSecretWords, providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            result.IsLoaded = true;
                            result.IsError = false;

                            //OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, avatarId, providerType);

                            //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            //{
                            //    result.Result = walletResult.Result;
                            //    result.IsLoaded = true;
                            //    result.IsError = false;
                            //}
                            //else
                            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");
                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> LoadProviderWalletForAvatarByEmail(string email, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByEmail method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByEmail(email, providerTypeToLoadFrom: providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            result.IsLoaded = true;
                            result.IsError = false;

                            //OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, avatarId, providerType);

                            //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            //{
                            //    result.Result = walletResult.Result;
                            //    result.IsLoaded = true;
                            //    result.IsError = false;
                            //}
                            //else
                            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");
                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                providerTypeToLoadFrom = ProviderType.LocalFileOASIS; //TODO: Temp!

                CLIEngine.SupressConsoleLogging = true;
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerTypeToLoadFrom);
                CLIEngine.SupressConsoleLogging = false;

                errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    if (providerResult.Result is IOASISLocalStorageProvider localProvider1)
                    {
                        result = localProvider1.LoadProviderWalletsForAvatarById(id);

                        if (result != null && result.Result != null && !result.IsError)
                            result.Result = FilterWallets(result.Result, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                        else
                            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Error occured loading wallets calling LoadProviderWalletsForAvatarById. Reason: "), result.Message);
                    }
                    else
                    {
                        // Non-local providers don't store private keys; return empty wallets.
                        result.Result = new Dictionary<ProviderType, List<IProviderWallet>>();
                        result.Message = "Provider does not support local wallet storage.";
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Error occured setting the provider. Reason: ", providerResult.Message), providerResult.Message);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarById method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                providerTypeToLoadFrom = ProviderType.LocalFileOASIS; //TODO: Temp!
                CLIEngine.SupressConsoleLogging = true;
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerTypeToLoadFrom);
                errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    if (providerResult.Result is IOASISLocalStorageProvider localProvider2)
                    {
                        result = localProvider2.LoadProviderWalletsForAvatarById(id);

                        if (result != null && result.Result != null && !result.IsError)
                            result.Result = FilterWallets(result.Result, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                        else
                            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Error occured loading wallets calling LoadProviderWalletsForAvatarById. Reason: "), result.Message);
                    }
                    else
                    {
                        // Non-local providers don't store private keys; return empty wallets.
                        result.Result = new Dictionary<ProviderType, List<IProviderWallet>>();
                        result.Message = "Provider does not support local wallet storage.";
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Error occured setting the provider. Reason: ", providerResult.Message), providerResult.Message);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = await LoadProviderWalletsForAvatarByIdAsync(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsername(string username, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByUsername method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(username, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(email, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = await LoadProviderWalletsForAvatarByIdAsync(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmail(string email, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByEmail method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByEmail(email, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }


        public async Task<OASISResult<List<IProviderWallet>>> LoadProviderWalletsForProviderByAvatarIdAsync(Guid avatarId, ProviderType walletProviderType, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = await LoadProviderWalletsForAvatarByIdAsync(avatarId, showOnlyDefault, showPrivateKeys);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarIdAsync. Reason: {result.Result}");

            return result;
        }

        public OASISResult<List<IProviderWallet>> LoadProviderWalletsForProviderByAvatarId(Guid avatarId, ProviderType walletProviderType, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = LoadProviderWalletsForAvatarById(avatarId);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarId. Reason: {result.Result}");

            return result;
        }

        public async Task<OASISResult<List<IProviderWallet>>> LoadProviderWalletsForProviderByAvatarUsernameAsync(string username, ProviderType walletProviderType, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = await LoadProviderWalletsForAvatarByUsernameAsync(username);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarUsernameAsync. Reason: {result.Result}");

            return result;
        }

        public OASISResult<List<IProviderWallet>> LoadProviderWalletsForProviderByAvatarUsername(string username, ProviderType walletProviderType, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = LoadProviderWalletsForAvatarByUsername(username);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarUsername. Reason: {result.Result}");

            return result;
        }

        public async Task<OASISResult<List<IProviderWallet>>> LoadProviderWalletsForProviderByAvatarEmailAsync(string email, ProviderType walletProviderType, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = await LoadProviderWalletsForAvatarByEmailAsync(email);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForAvatarByEmailAsync. Reason: {result.Result}");

            return result;
        }

        public OASISResult<List<IProviderWallet>> LoadProviderWalletsForProviderByAvatarEmail(string email, ProviderType walletProviderType, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = LoadProviderWalletsForAvatarByEmail(email);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForAvatarByEmail. Reason: {result.Result}");

            return result;
        }

    }
}

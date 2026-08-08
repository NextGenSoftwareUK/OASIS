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
        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByIdAsync(Guid avatarId, ProviderType providerType, bool showOnlyDefaultWallet = false, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in GetAvatarDefaultWalletById method in WalletManager. Reason: ";

            try
            {
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByIdAsync(avatarId, showOnlyDefaultWallet, showPrivateKeys, showSecretWords, providerType);
                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    var defaultAvatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.IsDefaultWallet);
                    if (defaultAvatarWallet == null)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Avatar doesn't have a default wallet!");
                    }
                    else
                    {
                        result.Result = defaultAvatarWallet;
                        result.IsLoaded = true;
                        result.IsError = false;

                        //OASISResult<IProviderWallet> walletResult = ProcessDecryption(defaultAvatarWallet, showPrivateKeys, showSecretWords, avatarId, providerType);

                        //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                        //{
                        //    result.Result = walletResult.Result;
                        //    result.IsLoaded = true;
                        //    result.IsError = false;
                        //}
                        //else
                        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByUsernameAsync(string avatarUsername, bool showOnlyDefaultWallet = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in GetAvatarDefaultWalletByUsername method in WalletManager. Reason: ";

            try
            {
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByUsernameAsync(avatarUsername, showOnlyDefaultWallet, showPrivateKeys, showSecretWords, providerType);
                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    var defaultAvatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.IsDefaultWallet);
                    if (defaultAvatarWallet == null)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Avatar doesn't have a default wallet!");
                    }
                    else
                    {
                        result.Result = defaultAvatarWallet;
                        result.IsLoaded = true;
                        result.IsError = false;

                        ////TODO: Check that avatarId isnt needed here (hopefully privatekey should already be loaded from the local provider!)
                        //OASISResult<IProviderWallet> walletResult = ProcessDecryption(defaultAvatarWallet, showPrivateKeys, showSecretWords, Guid.Empty, providerType);

                        //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                        //{
                        //    result.Result = walletResult.Result;
                        //    result.IsLoaded = true;
                        //    result.IsError = false;
                        //}
                        //else
                        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");
                    }   
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByEmailAsync(string email, ProviderType providerType, bool showOnlyDefaultWallet = false, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in GetAvatarDefaultWalletByEmail method in WalletManager. Reason: ";

            try
            {
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByEmailAsync(email, showOnlyDefaultWallet, showPrivateKeys, showSecretWords, providerType);
                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    var defaultAvatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.IsDefaultWallet);
                    if (defaultAvatarWallet == null)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Avatar doesn't have a default wallet!");
                    }
                    else
                    {
                        result.Result = defaultAvatarWallet;
                        result.IsLoaded = true;
                        result.IsError = false;

                        //TODO: Check that avatarId isnt needed here (hopefully privatekey should already be loaded from the local provider!)
                        //OASISResult<IProviderWallet> walletResult = ProcessDecryption(defaultAvatarWallet, showPrivateKeys, showSecretWords, Guid.Empty, providerType);

                        //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                        //{
                        //    result.Result = walletResult.Result;
                        //    result.IsLoaded = true;
                        //    result.IsError = false;
                        //}
                        //else
                        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");
                    }   
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> SetAvatarDefaultWalletByIdAsync(Guid avatarId, Guid walletId, ProviderType providerType)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in SetAvatarDefaultWalletById method in WalletManager. Reason: ";

            try
            {
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByIdAsync(avatarId);

                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    var avatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.WalletId == walletId);

                    if (avatarWallet == null)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} Not found!");
                    }
                    else
                    {
                        if (avatarWallet.IsDefaultWallet)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} is already the Default Wallet!");
                        else
                        {
                            foreach (IProviderWallet wallet in allAvatarWalletsByProvider.Result[providerType])
                                wallet.IsDefaultWallet = false;

                            avatarWallet.IsDefaultWallet = true;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByIdAsync(avatarId, allAvatarWalletsByProvider.Result, providerType);

                            if (saveResult != null && saveResult.Result)
                                result.Result = avatarWallet;

                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> SetAvatarDefaultWalletByUsernameAsync(string avatarUsername, Guid walletId, ProviderType providerType)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in SetAvatarDefaultWalletByUsername method in WalletManager. Reason: ";

            try
            {
                //var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByUsernameAsync(avatarUsername, false, false, providerType);
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByUsernameAsync(avatarUsername, false, false);
                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    var avatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.WalletId == walletId);

                    if (avatarWallet == null)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} Not found!");
                    }
                    else
                    {
                        if (avatarWallet.IsDefaultWallet)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} is already the Default Wallet!");
                        else
                        {
                            foreach (IProviderWallet wallet in allAvatarWalletsByProvider.Result[providerType])
                                wallet.IsDefaultWallet = false;

                            avatarWallet.IsDefaultWallet = true;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByUsernameAsync(avatarUsername, allAvatarWalletsByProvider.Result, providerType);

                            if (saveResult != null && saveResult.Result)
                                result.Result = avatarWallet;

                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> SetAvatarDefaultWalletByEmailAsync(string email, Guid walletId, ProviderType providerType)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in SetAvatarDefaultWalletByEmail method in WalletManager. Reason: ";

            try
            {
                //var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByEmailAsync(email, false, false, providerType);
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByEmailAsync(email, false, false);

                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    if (allAvatarWalletsByProvider.Result[providerType].Any(x => x.IsDefaultWallet))
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar already have default wallet!");
                    }
                    else
                    {
                        var avatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.WalletId == walletId);

                        if (avatarWallet == null)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} Not found!");
                        }
                        else
                        {
                            if (avatarWallet.IsDefaultWallet)
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} is already the Default Wallet!");
                            else
                            {
                                foreach (IProviderWallet wallet in allAvatarWalletsByProvider.Result[providerType])
                                    wallet.IsDefaultWallet = false;

                                avatarWallet.IsDefaultWallet = true;

                                OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByEmailAsync(email, allAvatarWalletsByProvider.Result, providerType);

                                if (saveResult != null && saveResult.Result)
                                    result.Result = avatarWallet;

                                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

    }
}
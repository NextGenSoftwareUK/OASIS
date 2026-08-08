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
        public async Task<OASISResult<IProviderWallet>> UpdateWalletForAvatarByIdAsync(Guid avatarId, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByIdAsync. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByIdAsync(avatarId, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByIdAsync(avatarId, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByIdAsync. Reason: {saveResult.Message}");

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

        public OASISResult<IProviderWallet> UpdateWalletForAvatarById(Guid avatarId, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarById. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarById(avatarId, false, false, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarById(avatarId, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarById. Reason: {saveResult.Message}");

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

        public async Task<OASISResult<IProviderWallet>> UpdateWalletForAvatarByUsernameAsync(string username, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByUsernameAsync. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByUsernameAsync(username, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByUsernameAsync(username, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByIdAsync. Reason: {saveResult.Message}");

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

        public OASISResult<IProviderWallet> UpdateWalletForAvatarByUsername(string username, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByUsername. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByUsername(username, false, false, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByUsername(username, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarById. Reason: {saveResult.Message}");

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

        public async Task<OASISResult<IProviderWallet>> UpdateWalletForAvatarByEmailAsync(string email, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByEmailAsync. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByEmailAsync(email, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByEmailAsync(email, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByEmailAsync. Reason: {saveResult.Message}");

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

        public OASISResult<IProviderWallet> UpdateWalletForAvatarByEmail(string email, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByEmail. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByEmail(email, false, false, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByEmail(email, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByEmail. Reason: {saveResult.Message}");

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

    }
}
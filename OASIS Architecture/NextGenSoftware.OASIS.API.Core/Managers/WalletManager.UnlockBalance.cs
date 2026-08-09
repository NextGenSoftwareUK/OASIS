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
        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The unlock token request is required. Please provide a valid request with TokenAddress, Web3TokenId, and ProviderType.";
                return result;
            }
            string errorMessage = "Error occured in UnlockTokenAsync. Reason: ";

            UnlockWeb3TokenRequest unlockWeb3TokenRequest = new UnlockWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = await oasisBlockchainProvider.UnlockTokenAsync(unlockWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Unlocked Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenUnlocked)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to unlock the token & WaitTillTokenUnlocked is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToUnlockEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToUnlockEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to unlock the token. Reason: Timeout expired, AttemptToUnlockEveryXSeconds ({request.AttemptToUnlockEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} callingProviderManager.Instance.GetProvider.");

            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The unlock token request is required. Please provide a valid request with TokenAddress, Web3TokenId, and ProviderType.";
                return result;
            }
            string errorMessage = "Error occured in UnlockToken. Reason: ";

            UnlockWeb3TokenRequest unlockWeb3TokenRequest = new UnlockWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = oasisBlockchainProvider.UnlockToken(unlockWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Unlocked Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenUnlocked)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to unlock the token & WaitTillTokenUnlocked is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToUnlockEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToUnlockEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to unlock the token. Reason: Timeout expired, AttemptToUnlockEveryXSeconds ({request.AttemptToUnlockEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        public async Task<OASISResult<double>> GetTotalBalanceForAllProviderWalletsForAvatarByIdAsync(Guid avatarId)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarByIdAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByIdAsync(avatarId, false, false);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
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

        public OASISResult<double> GetTotalBalanceForAllProviderWalletsForAvatarById(Guid avatarId)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarById method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarById(avatarId);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
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

        public async Task<OASISResult<double>> GetTotalBalanceForAllProviderWalletsForAvatarByUsernameAsync(string username)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarByUsernameAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByUsernameAsync(username);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
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

        public OASISResult<double> GetTotalBalanceForAllProviderWalletsForAvatarByUsername(string username)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarByUsername method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByUsername(username);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
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

        public async Task<OASISResult<double>> GetTotalBalanceForAllProviderWalletsForAvatarByEmailAsync(string email)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarByEmailAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByEmailAsync(email);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
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

        public OASISResult<double> GetTotalBalanceForAllProviderWalletsForAvatarByEmail(string email)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarByEmail method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByEmail(email);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
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

        public async Task<OASISResult<double>> GetTotalBalanceForProviderWalletsForAvatarByIdAsync(Guid avatarId, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = await LoadProviderWalletsForProviderByAvatarIdAsync(avatarId, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
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

        public OASISResult<double> GetTotalBalanceForProviderWalletsForAvatarById(Guid avatarId, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarById method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = LoadProviderWalletsForProviderByAvatarId(avatarId, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
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

        public async Task<OASISResult<double>> GetTotalBalanceForProviderWalletsForAvatarByUsernameAsync(string username, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = await LoadProviderWalletsForProviderByAvatarUsernameAsync(username, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
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

        public OASISResult<double> GetTotalBalanceForProviderWalletsForAvatarByUsername(string username, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarByUsername method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = LoadProviderWalletsForProviderByAvatarUsername(username, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
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

        public async Task<OASISResult<double>> GetTotalBalanceForProviderWalletsForAvatarByEmailAsync(string email, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = await LoadProviderWalletsForProviderByAvatarEmailAsync(email, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
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

        public OASISResult<double> GetTotalBalanceForProviderWalletsForAvatarByEmail(string email, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarByEmail method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = LoadProviderWalletsForProviderByAvatarEmail(email, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
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

        public async Task<OASISResult<double>> GetBalanceForWalletForAvatarByIdAsync(Guid avatarId, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = await LoadProviderWalletForAvatarByIdAsync(avatarId, walletId, false, false, providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetBalanceForWalletForAvatarById(Guid avatarId, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarById method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = LoadProviderWalletForAvatarById(avatarId, walletId, false, false, providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetBalanceForWalletForAvatarByUsernameAsync(string username, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = await LoadProviderWalletForAvatarByUsernameAsync(username, walletId, providerType: providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<double> GetBalanceForWalletForAvatarByUsername(string username, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = LoadProviderWalletForAvatarByUsername(username, walletId, false, false, providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetBalanceForWalletForAvatarByEmailAsync(string email, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = await LoadProviderWalletForAvatarByEmailAsync(email, walletId, providerType: providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetBalanceForWalletForAvatarByEmail(string email, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = LoadProviderWalletForAvatarByEmail(email, walletId, false, false, providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

    }
}

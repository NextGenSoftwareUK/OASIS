using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS
    {

        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromWalletAddress,
                    ToAddress = toWalletAddress,
                    Amount = amount,
                    Memo = memoText
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionAsync: {ex.Message}", ex);
            }
            return result;
        }


        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.HashgraphOASIS);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                    return result;
                }

                var fromAddress = fromWalletResult.Result?.WalletAddress;
                var toAddress = toWalletResult.Result?.WalletAddress;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for avatars");
                    return result;
                }

                // Create Hashgraph transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarId} to {toAvatarId}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByIdAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.HashgraphOASIS);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                    return result;
                }

                var fromAddress = fromWalletResult.Result?.WalletAddress;
                var toAddress = toWalletResult.Result?.WalletAddress;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for avatars");
                    return result;
                }

                // Create Hashgraph token transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    TokenId = token,
                    Memo = $"OASIS token transaction from {fromAvatarId} to {toAvatarId}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph token transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph token transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph token transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByIdAsync with token: {ex.Message}", ex);
            }
            return result;
        }


        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarUsername);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, toAvatarUsername);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for usernames");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for usernames");
                    return result;
                }

                // Create Hashgraph transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarUsername} to {toAvatarUsername}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByUsernameAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarUsername);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, toAvatarUsername);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for usernames");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for usernames");
                    return result;
                }

                // Create Hashgraph token transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    TokenId = token,
                    Memo = $"OASIS token transaction from {fromAvatarUsername} to {toAvatarUsername}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph token transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph token transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph token transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByUsernameAsync with token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarEmail);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, toAvatarEmail);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for emails");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for emails");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarEmail} to {toAvatarEmail}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByEmailAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarEmail);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.HashgraphOASIS, toAvatarEmail);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for emails");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for emails");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarEmail} to {toAvatarEmail}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByEmailAsync(token): {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.HashgraphOASIS);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get default wallet addresses for avatars");
                    return result;
                }

                var fromAddress = fromWalletResult.Result?.WalletAddress;
                var toAddress = toWalletResult.Result?.WalletAddress;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find default wallet addresses for avatars");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS default wallet transaction from {fromAvatarId} to {toAvatarId}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByDefaultWalletAsync: {ex.Message}", ex);
            }
            return result;
        }



    }
}

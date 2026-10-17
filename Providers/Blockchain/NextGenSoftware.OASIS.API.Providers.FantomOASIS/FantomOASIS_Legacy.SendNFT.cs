using System;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.FantomOASIS
{
    public partial class FantomOASIS_Legacy
    {
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarByUsernameAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarByUsernameAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarByEmail is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarByEmail: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarByEmailAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarByEmailAsync: {ex.Message}");
            }
            return response;
        }

        // Import methods
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                if (holons == null || !holons.Any())
                {
                    OASISErrorHandling.HandleError(ref response, "Holons collection cannot be null or empty");
                    return response;
                }

                // Real Fantom implementation: Import holons by saving them
                var saveResult = await SaveHolonsAsync(holons, true, true, 0, 0, true, false);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error importing holons: {saveResult.Message}");
                    return response;
                }

                response.Result = true;
                response.IsError = false;
                response.Message = $"Imported {holons.Count()} holons to Fantom successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ImportAsync: {ex.Message}", ex);
            }
            return response;
        }



        public void Dispose()
        {
            _httpClient?.Dispose();
        }



        /// <summary>
        /// Get Fantom smart contract ABI for OASIS operations
        /// </summary>
        private string GetFantomContractABI()
        {
            return @"[
                {
                    ""inputs"": [
                        {""internalType"": ""string"", ""name"": ""avatarId"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""username"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""email"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""firstName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""lastName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""avatarType"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""metadata"", ""type"": ""string""}
                    ],
                    ""name"": ""createAvatar"",
                    ""outputs"": [
                        {""internalType"": ""bool"", ""name"": """", ""type"": ""bool""}
                    ],
                    ""stateMutability"": ""nonpayable"",
                    ""type"": ""function""
                },
                {
                    ""inputs"": [
                        {""internalType"": ""string"", ""name"": ""avatarId"", ""type"": ""string""}
                    ],
                    ""name"": ""getAvatar"",
                    ""outputs"": [
                        {""internalType"": ""string"", ""name"": ""username"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""email"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""firstName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""lastName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""avatarType"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""metadata"", ""type"": ""string""}
                    ],
                    ""stateMutability"": ""view"",
                    ""type"": ""function""
                },
                {
                    ""inputs"": [
                        {""internalType"": ""string"", ""name"": ""avatarId"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""username"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""email"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""firstName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""lastName"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""avatarType"", ""type"": ""string""},
                        {""internalType"": ""string"", ""name"": ""metadata"", ""type"": ""string""}
                    ],
                    ""name"": ""updateAvatar"",
                    ""outputs"": [
                        {""internalType"": ""bool"", ""name"": """", ""type"": ""bool""}
                    ],
                    ""stateMutability"": ""nonpayable"",
                    ""type"": ""function""
                },
                {
                    ""inputs"": [
                        {""internalType"": ""string"", ""name"": ""avatarId"", ""type"": ""string""}
                    ],
                    ""name"": ""deleteAvatar"",
                    ""outputs"": [
                        {""internalType"": ""bool"", ""name"": """", ""type"": ""bool""}
                    ],
                    ""stateMutability"": ""nonpayable"",
                    ""type"": ""function""
                }
            ]";
        }


        // NFT-specific lock/unlock methods
        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        {
            return LockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = string.Empty,
                    ToWalletAddress = bridgePoolAddress,
                    TokenAddress = request.NFTTokenAddress,
                    TokenId = request.Web3NFTId.ToString(),
                    Amount = 1,
                    // FromWalletPrivateKey removed - not in SendWeb3NFTRequest interface
                };

                var sendResult = await SendNFTAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.IsError = false;
                result.Result.TransactionResult = sendResult.Result.TransactionResult;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = bridgePoolAddress,
                    ToWalletAddress = string.Empty,
                    TokenAddress = request.NFTTokenAddress,
                    TokenId = request.Web3NFTId.ToString(),
                    Amount = 1
                };

                var sendResult = await SendNFTAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.IsError = false;
                result.Result.TransactionResult = sendResult.Result.TransactionResult;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
            }
            return result;
        }


        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(accountAddress);
                result.Result = Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Fantom account balance: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();

                result.Result = (publicKey, privateKey, string.Empty);
                result.IsError = false;
                result.Message = "Fantom account created successfully. Seed phrase not applicable for direct key generation.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating Fantom account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                var wallet = new Nethereum.HdWallet.Wallet(seedPhrase, null);
                var account = wallet.GetAccount(0);

                result.Result = (account.Address, account.PrivateKey);
                result.IsError = false;
                result.Message = "Fantom account restored successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring Fantom account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Sender account address and private key are required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                var account = new Account(senderPrivateKey, BigInteger.Parse(_chainId));
                var web3 = new Web3(account, _rpcEndpoint);

                var bridgePoolAddress = _account?.Address ?? _contractAddress;
                var transactionReceipt = await web3.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(bridgePoolAddress, amount, 2);

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transactionReceipt.TransactionHash,
                    IsSuccessful = transactionReceipt.Status.Value == 1,
                    Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!_isActivated || _web3Client == null || _account == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Receiver account address is required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                var transactionReceipt = await _web3Client.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount, 2);

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transactionReceipt.TransactionHash,
                    IsSuccessful = transactionReceipt.Status.Value == 1,
                    Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!_isActivated || _web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Fantom provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                var transactionReceipt = await _web3Client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

                if (transactionReceipt == null)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = true;
                    result.Message = "Transaction not found.";
                }
                else if (transactionReceipt.Status.Value == 1)
                {
                    result.Result = BridgeTransactionStatus.Completed;
                    result.IsError = false;
                }
                else
                {
                    result.Result = BridgeTransactionStatus.Canceled;
                    result.IsError = true;
                    result.Message = "Transaction failed on chain.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Fantom transaction status: {ex.Message}", ex);
                result.Result = BridgeTransactionStatus.NotFound;
            }
            return result;
        }


    }
}

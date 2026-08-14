using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Extensions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

public partial class RadixOASIS
{


    public override async Task<OASISResult<bool>> ActivateProviderAsync()
    {
        var result = new OASISResult<bool>();

        try
        {
            _radixService = new RadixService(_config, _httpClient);
            
            // Test connection by getting a dummy balance
            var testResult = await _radixService.GetAccountBalanceAsync(_config.AccountAddress);
            
            if (testResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Failed to activate RadixOASIS provider: {testResult.Message}");
                return result;
            }

            // Initialize first-party oracle node (Airnode-style, no middleware)
            _chainObserver = new RadixChainObserver(_radixService, _config);
            _oracleNode = new RadixOracleNode(_radixService, _config);

            result.Result = true;
            IsProviderActivated = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error activating RadixOASIS provider: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<bool> ActivateProvider()
    {
        return ActivateProviderAsync().Result;
    }

    public override async Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        // Stop oracle node if running
        if (_oracleNode?.IsRunning == true)
        {
            await _oracleNode.StopAsync();
        }

        _radixService = null;
        _oracleNode = null;
        _chainObserver = null;
        IsProviderActivated = false;
        return await Task.FromResult(new OASISResult<bool>(true));
    }

    public override OASISResult<bool> DeActivateProvider()
    {
        // Stop oracle node if running
        if (_oracleNode?.IsRunning == true)
        {
            _oracleNode.StopAsync().Wait();
        }

        _radixService = null;
        _oracleNode = null;
        _chainObserver = null;
        IsProviderActivated = false;
        return new OASISResult<bool>(true);
    }



    /// <summary>
    /// Sends a transaction on the Radix network
    /// </summary>
    public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
    }

    /// <summary>
    /// Sends a transaction on the Radix network asynchronously
    /// </summary>
    public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        var result = new OASISResult<ITransactionResponse>();
        
        try
        {
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            // Support sending from any address
            // If fromWalletAddress is provided and different from config account, 
            // we use it; otherwise use the configured account as sender
            // Note: Full multi-address support requires wallet management infrastructure
            // For now, we use the configured account as sender
            var depositResult = await _radixService.DepositAsync(amount, toWalletAddress);
            
            if (depositResult.IsError || depositResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, 
                    depositResult.Message ?? "Failed to send transaction");
                return result;
            }

            // Create transaction response
            result.Result = new TransactionResponse
            {
                TransactionResult = depositResult.Result?.TransactionId ?? "Unknown"
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<ITransactionResponse>(ref result,
                $"Error sending transaction: {ex.Message}", ex);
            return result;
        }
    }

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
    {
        return SendTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "To wallet address is required");
                return result;
            }

            // Use DepositAsync to send tokens
            var depositResult = await _radixService.DepositAsync(request.Amount, request.ToWalletAddress);
            
            if (depositResult.IsError || depositResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, 
                    depositResult.Message ?? "Failed to send token");
                return result;
            }

            // Create transaction response
            result.Result = new TransactionResponse
            {
                TransactionResult = depositResult.Result.TransactionId ?? depositResult.Result.DuplicateTransactionId ?? "Unknown"
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<ITransactionResponse>(ref result,
                $"Error sending token: {ex.Message}", ex);
            return result;
        }
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
    {
        return MintTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            // Like other providers: token address from MetaData or config, mint-to from MetaData or wallet
            var tokenAddress = request?.MetaData?.GetValueOrDefault("TokenAddress") ?? _config.OasisBlueprintAddress ?? "";
            if (string.IsNullOrWhiteSpace(tokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required (set in request.MetaData[\"TokenAddress\"] or config)");
                return result;
            }

            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(
                request.MintedByAvatarId != Guid.Empty ? request.MintedByAvatarId : Guid.NewGuid(), 
                ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for minting");
                return result;
            }

            var mintAmount = request.Amount;
            var mintToAddress = request?.MetaData?.GetValueOrDefault("ToWalletAddress") ?? walletResult.Result.WalletAddress;

            // Build transaction manifest for token minting
            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = tokenAddress,
                        methodName = "mint",
                        args = new[]
                        {
                            new { kind = "Decimal", value = mintAmount.ToString() },
                            new { kind = "Address", value = mintToAddress }
                        }
                    }
                },
                blobs = new object[0]
            };

            // Get construction metadata and build transaction
            var metadataResult = await _httpClient.GetConstructionMetadataAsync(_config);
            if (metadataResult == null)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get Radix construction metadata");
                return result;
            }

            var transactionHeader = new
            {
                networkId = _config.NetworkId,
                startEpochInclusive = metadataResult.CurrentEpoch,
                endEpochExclusive = metadataResult.CurrentEpoch + 50,
                nonce = new Random().Next(),
                notaryPublicKey = walletResult.Result.PublicKey,
                notaryIsSignatory = true,
                tipPercentage = 0
            };

            var transactionData = new
            {
                network = network,
                manifest = System.Text.Json.JsonSerializer.Serialize(manifest),
                header = transactionHeader,
                message = new { kind = "None" }
            };

            // Submit transaction via Radix Gateway API
            var submitResult = await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
                _httpClient,
                $"{_config.HostUri}/core/lts/transaction/submit",
                transactionData);

            if (submitResult.IsError || submitResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to submit Radix mint transaction: {submitResult.Message}");
                return result;
            }

            result.Result = new TransactionResponse
            {
                TransactionResult = submitResult.Result.TransactionHash ?? "Token mint initiated"
            };
            result.IsError = false;
            result.Message = "Token minted successfully via Radix";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error minting token: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
    {
        return BurnTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }

            // Get wallet for signing
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(
                request.BurntByAvatarId != Guid.Empty ? request.BurntByAvatarId : Guid.NewGuid(), 
                ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for burning");
                return result;
            }

            // Burn ONE NFT at TokenAddress (NFTTokenAddress) – no amount, single NFT
            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = request.TokenAddress,
                        methodName = "burn",
                        args = new[]
                        {
                            new { kind = "NonFungibleLocalId", value = "1" },
                            new { kind = "Address", value = walletResult.Result.WalletAddress }
                        }
                    }
                },
                blobs = new object[0]
            };

            // Get construction metadata and build transaction
            var metadataResult = await _httpClient.GetConstructionMetadataAsync(_config);
            if (metadataResult == null)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get Radix construction metadata");
                return result;
            }

            var transactionHeader = new
            {
                networkId = _config.NetworkId,
                startEpochInclusive = metadataResult.CurrentEpoch,
                endEpochExclusive = metadataResult.CurrentEpoch + 50,
                nonce = new Random().Next(),
                notaryPublicKey = walletResult.Result.PublicKey,
                notaryIsSignatory = true,
                tipPercentage = 0
            };

            var transactionData = new
            {
                network = network,
                manifest = System.Text.Json.JsonSerializer.Serialize(manifest),
                header = transactionHeader,
                message = new { kind = "None" }
            };

            // Submit transaction via Radix Gateway API
            var submitResult = await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
                _httpClient,
                $"{_config.HostUri}/core/lts/transaction/submit",
                transactionData);

            if (submitResult.IsError || submitResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to submit Radix burn transaction: {submitResult.Message}");
                return result;
            }

            result.Result = new TransactionResponse
            {
                TransactionResult = submitResult.Result.TransactionHash ?? "Token burn initiated"
            };
            result.IsError = false;
            result.Message = "Token burned successfully via Radix";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error burning token: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
    {
        return LockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }

            // Lock ONE NFT at TokenAddress (NFTTokenAddress) – no amount, single NFT
            var lockContractAddress = _config.OasisBlueprintAddress ?? request.TokenAddress;
            var fromWalletAddress = request.FromWalletAddress;

            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(
                request.LockedByAvatarId != Guid.Empty ? request.LockedByAvatarId : Guid.NewGuid(), 
                ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for locking");
                return result;
            }

            if (string.IsNullOrWhiteSpace(fromWalletAddress))
                fromWalletAddress = walletResult.Result.WalletAddress;

            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = lockContractAddress,
                        methodName = "lock",
                        args = new[]
                        {
                            new { kind = "Address", value = request.TokenAddress },
                            new { kind = "NonFungibleLocalId", value = "1" },
                            new { kind = "Address", value = fromWalletAddress }
                        }
                    }
                },
                blobs = new object[0]
            };

            // Get construction metadata and build transaction
            var metadataResult = await _httpClient.GetConstructionMetadataAsync(_config);
            if (metadataResult == null)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get Radix construction metadata");
                return result;
            }

            var transactionHeader = new
            {
                networkId = _config.NetworkId,
                startEpochInclusive = metadataResult.CurrentEpoch,
                endEpochExclusive = metadataResult.CurrentEpoch + 50,
                nonce = new Random().Next(),
                notaryPublicKey = walletResult.Result.PublicKey,
                notaryIsSignatory = true,
                tipPercentage = 0
            };

            var transactionData = new
            {
                network = network,
                manifest = System.Text.Json.JsonSerializer.Serialize(manifest),
                header = transactionHeader,
                message = new { kind = "None" }
            };

            // Submit transaction via Radix Gateway API
            var submitResult = await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
                _httpClient,
                $"{_config.HostUri}/core/lts/transaction/submit",
                transactionData);

            if (submitResult.IsError || submitResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to submit Radix lock transaction: {submitResult.Message}");
                return result;
            }

            result.Result = new TransactionResponse
            {
                TransactionResult = submitResult.Result.TransactionHash ?? "Token lock initiated"
            };
            result.IsError = false;
            result.Message = "Token locked successfully via Radix";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error locking token: {ex.Message}", ex);
        }
        return result;
    }

}

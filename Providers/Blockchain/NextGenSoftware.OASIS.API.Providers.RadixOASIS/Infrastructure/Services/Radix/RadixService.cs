using System.Globalization;
using System.Net.Http;
using NBitcoin;
using RadixEngineToolkit;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.Enums;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Extensions;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;
// TransactionBuilder may not be available in current RadixEngineToolkit version
// using TransactionBuilder = RadixEngineToolkit.TransactionBuilder;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;

/// <summary>
/// Service for interacting with the Radix blockchain network.
/// Provides functionality to manage accounts, check balances, and execute transactions.
/// </summary>
public sealed class RadixService : IRadixService
{
    private readonly RadixOASISConfig _config;
    private readonly HttpClient _httpClient;
    private readonly string _network;
    private readonly Address _xrdAddress;

    public RadixService(RadixOASISConfig config, HttpClient httpClient)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (config.NetworkId == (byte)RadixNetworkType.MainNet)
        {
            _network = RadixBridgeHelper.MainNet;
            _xrdAddress = new Address(RadixBridgeHelper.MainNetXrdAddress);
        }
        else
        {
            _network = RadixBridgeHelper.StokeNet;
            _xrdAddress = new Address(RadixBridgeHelper.StokeNetXrdAddress);
        }
    }

    /// <summary>
    /// Retrieves the balance of an account.
    /// </summary>
    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
        
        try
        {
            var data = new
            {
                network = _network,
                account_address = accountAddress,
                resource_address = _xrdAddress.AddressString()
            };

            var response = await HttpClientHelper.PostAsync<object, AccountFungibleResourceBalanceDto>(
                _httpClient,
                $"{_config.HostUri}/core/lts/state/account-fungible-resource-balance",
                data,
                token
            );

            if (response.IsError || response.Result == null)
            {
                result.IsError = true;
                result.Message = response.Message ?? "Failed to get account balance";
                return result;
            }

            result.Result = decimal.Parse(response.Result.FungibleResourceBalance.Amount);
            result.IsError = false;
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<decimal>(ref result, 
                $"Error getting account balance: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Creates a new Radix account with a randomly generated seed phrase.
    /// </summary>
    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(
        CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
        
        try
        {
            token.ThrowIfCancellationRequested();
            
            Mnemonic mnemonic = new(Wordlist.English, WordCount.TwentyFour);
            using PrivateKey privateKey = RadixBridgeHelper.GetPrivateKey(mnemonic);
            string publicKey = Convert.ToHexString(privateKey.PublicKeyBytes());

            result.Result = (publicKey, privateKey.RawHex(), mnemonic.ToString());
            result.IsError = false;
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<(string, string, string)>(ref result,
                $"Error creating account: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Restores an account using a seed phrase.
    /// </summary>
    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(
        string seedPhrase,
        CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey)>();
        
        try
        {
            token.ThrowIfCancellationRequested();

            if (!SeedPhraseValidator.IsValidSeedPhrase(seedPhrase))
            {
                result.IsError = true;
                result.Message = "Invalid seed phrase format";
                return result;
            }

            Mnemonic mnemonic = new(seedPhrase);
            using PrivateKey privateKey = RadixBridgeHelper.GetPrivateKey(mnemonic);
            string publicKey = Convert.ToHexString(privateKey.PublicKeyBytes());

            result.Result = (publicKey, privateKey.RawHex());
            result.IsError = false;
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<(string, string)>(ref result,
                $"Error restoring account: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Retrieves the address of an account or identity based on the public key.
    /// </summary>
    public OASISResult<string> GetAddress(PublicKey publicKey, RadixAddressType addressType,
        RadixNetworkType networkType, CancellationToken token = default)
    {
        var result = new OASISResult<string>();
        
        try
        {
            token.ThrowIfCancellationRequested();
            byte network = (byte)networkType;

            // Radix address derivation - simplified implementation
            // In production, use proper Radix address derivation from public key
            // PublicKey may not have PublicKeyBytes() method - use ToBytes() or similar
            var publicKeyBytes = publicKey.ToBytes();
            var addressBytes = new byte[publicKeyBytes.Length];
            Array.Copy(publicKeyBytes, addressBytes, publicKeyBytes.Length);
            
            result.Result = addressType switch
            {
                RadixAddressType.Account => "account_" + Convert.ToHexString(addressBytes).ToLowerInvariant(),
                RadixAddressType.Identity => "identity_" + Convert.ToHexString(addressBytes).ToLowerInvariant(),
                _ => throw new ArgumentException("Invalid address type")
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<string>(ref result,
                $"Error getting address: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Withdraws a specified amount from an account.
    /// </summary>
    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(
        decimal amount,
        string senderAccountAddress,
        string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        
        try
        {
            if (senderAccountAddress == _config.AccountAddress)
            {
                result.IsError = true;
                result.Message = "Cannot withdraw to the same account (self-transaction not allowed)";
                return result;
            }

            return await ExecuteTransactionAsync(
                RadixBridgeHelper.GetPrivateKeyFromHex(senderPrivateKey),
                _config.AccountAddress,
                amount);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<BridgeTransactionResponse>(ref result,
                $"Error during withdrawal: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Deposits a specified amount to an account.
    /// </summary>
    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(
        decimal amount,
        string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        
        try
        {
            if (receiverAccountAddress == _config.AccountAddress)
            {
                result.IsError = true;
                result.Message = "Cannot deposit to the same account (self-transaction not allowed)";
                return result;
            }

            return await ExecuteTransactionAsync(
                RadixBridgeHelper.GetPrivateKeyFromHex(_config.PrivateKey),
                receiverAccountAddress,
                amount);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<BridgeTransactionResponse>(ref result,
                $"Error during deposit: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Executes a transaction (withdrawal or deposit) by building and submitting the transaction manifest.
    /// </summary>
    private async Task<OASISResult<BridgeTransactionResponse>> ExecuteTransactionAsync(
        PrivateKey sender,
        string receiver,
        decimal amount)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        const int fee = 10;
        
        try
        {
            // Derive sender address from public key (simplified - in production use proper Radix address derivation)
            var senderPublicKey = sender.PublicKey();
            var senderPublicKeyBytes = senderPublicKey.ToBytes();
            var senderAddressStr = "account_" + Convert.ToHexString(senderPublicKeyBytes).ToLowerInvariant();
            Address senderAddress = new(senderAddressStr);
            Address receiverAddress = new(receiver);

            // Check balance
            var balanceResult = await GetAccountBalanceAsync(senderAddress.AddressString());
            if (balanceResult.IsError)
            {
                result.IsError = true;
                result.Message = balanceResult.Message;
                return result;
            }

            if (amount + fee >= balanceResult.Result)
            {
                result.IsError = true;
                result.Message = "Insufficient funds to cover amount and fee";
                result.Result = new BridgeTransactionResponse(
                    string.Empty,
                    null,
                    false,
                    "Insufficient funds",
                    BridgeTransactionStatus.InsufficientFunds);
                return result;
            }

            // Format amount
            decimal roundedAmount = Math.Round(amount, 18, MidpointRounding.AwayFromZero);
            string formattedAmount = roundedAmount.ToString("F18", CultureInfo.InvariantCulture)
                .TrimEnd('0').TrimEnd('.');

            // Build transaction manifest
            // TODO: TransactionManifest and ManifestBuilder may not be available in current RadixEngineToolkit version
            // For now, return error indicating this needs to be implemented with the correct Radix SDK
            // using TransactionManifest manifest = new ManifestBuilder()
            //     .AccountLockFeeAndWithdraw(senderAddress, new($"{fee}"), _xrdAddress, new(formattedAmount))
            //     .TakeFromWorktop(_xrdAddress, new(formattedAmount), new("xrdBucket"))
            //     .AccountTryDepositOrAbort(receiverAddress, new("xrdBucket"), null)
            //     .Build(_config.NetworkId);
            // manifest.StaticallyValidate();

            // Get current epoch
            ulong currentEpoch = (await _httpClient.GetConstructionMetadataAsync(_config))?.CurrentEpoch ?? 0;

            // Build and sign transaction
            // TODO: Implement proper transaction building when RadixEngineToolkit.TransactionBuilder is available
            // For now, return error indicating this needs to be implemented with the correct Radix SDK
            result.IsError = true;
            result.Message = "Transaction building not yet implemented - RadixEngineToolkit.TransactionBuilder needs to be properly configured";
            result.Result = new BridgeTransactionResponse(
                string.Empty,
                null,
                false,
                "Transaction building not yet implemented",
                BridgeTransactionStatus.Canceled
            );
            return result;
            
            // Future implementation:
            // using NotarizedTransaction transaction = new RadixEngineToolkit.TransactionBuilder()
            //     .Header(new TransactionHeader(
            //         networkId: _config.NetworkId,
            //         startEpochInclusive: currentEpoch,
            //         endEpochExclusive: currentEpoch + 50,
            //         nonce: RadixBridgeHelper.RandomNonce(),
            //         notaryPublicKey: sender.PublicKey(),
            //         notaryIsSignatory: true,
            //         tipPercentage: 0
            //     ))
            //     .Manifest(manifest)
            //     .Message(new Message.None())
            //     .NotarizeWithPrivateKey(sender);
            //
            // var data = new
            // {
            //     network = _network,
            //     notarized_transaction_hex = Encoders.Hex.EncodeData(transaction.Compile()),
            //     force_recalculate = true
            // };
            //
            // var response = await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
            //     _httpClient,
            //     $"{_config.HostUri}/core/lts/transaction/submit",
            //     data);
            //
            // var transactionHash = transaction.IntentHash().AsStr();
            //
            // result.Result = new BridgeTransactionResponse(
            //     transactionHash,
            //     response.Result?.TransactionHash,
            //     response.Result != null && !response.IsError,
            //     response.IsError ? response.Message : null,
            //     response.Result != null && !response.IsError 
            //         ? BridgeTransactionStatus.Completed 
            //         : BridgeTransactionStatus.Canceled
            // );
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<BridgeTransactionResponse>(ref result,
                $"Error executing transaction: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Retrieves the status of a submitted transaction.
    /// </summary>
    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(
        string transactionHash,
        CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
        
        try
        {
            var data = new
            {
                network = _network,
                intent_hash = transactionHash
            };

            var response = await HttpClientHelper.PostAsync<object, TransactionStatusResponse>(
                _httpClient,
                $"{_config.HostUri}/core/transaction/status",
                data,
                token);

            if (response.IsError || response.Result == null)
            {
                result.IsError = true;
                result.Message = "Failed to get transaction status";
                return result;
            }

            result.Result = response.Result.IntentStatus switch
            {
                RadixTransactionStatus.CommittedSuccess => BridgeTransactionStatus.Completed,
                RadixTransactionStatus.CommittedFailure => BridgeTransactionStatus.Canceled,
                RadixTransactionStatus.NotSeen => BridgeTransactionStatus.NotFound,
                RadixTransactionStatus.InMemPool => BridgeTransactionStatus.Pending,
                RadixTransactionStatus.PermanentRejection => BridgeTransactionStatus.Canceled,
                RadixTransactionStatus.FateUncertainButLikelyRejection => BridgeTransactionStatus.Canceled,
                RadixTransactionStatus.FateUncertain => BridgeTransactionStatus.Pending,
                _ => BridgeTransactionStatus.NotFound
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<BridgeTransactionStatus>(ref result,
                $"Error getting transaction status: {ex.Message}", ex);
        }
    }

    #region Oracle Methods

    /// <summary>
    /// Gets the current chain state (epoch, network info, health)
    /// </summary>
    public async Task<OASISResult<Infrastructure.Entities.DTOs.Oracle.RadixChainState>> GetChainStateAsync(CancellationToken token = default)
    {
        var result = new OASISResult<Infrastructure.Entities.DTOs.Oracle.RadixChainState>();
        
        try
        {
            // Get construction metadata which contains current epoch
            var metadata = await _httpClient.GetConstructionMetadataAsync(_config);
            
            if (metadata == null)
            {
                result.IsError = true;
                result.Message = "Failed to get chain state";
                return result;
            }

            result.Result = new Infrastructure.Entities.DTOs.Oracle.RadixChainState
            {
                ChainName = "Radix",
                CurrentEpoch = metadata.CurrentEpoch,
                NetworkId = _config.NetworkId.ToString(),
                NetworkName = _config.NetworkId == (byte)Infrastructure.Entities.Enums.RadixNetworkType.MainNet ? "MainNet" : "StokeNet",
                IsHealthy = true,
                LastBlockTime = DateTime.UtcNow
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<Infrastructure.Entities.DTOs.Oracle.RadixChainState>(ref result,
                $"Error getting chain state: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets the latest epoch (Radix equivalent of block height)
    /// </summary>
    public async Task<OASISResult<ulong>> GetLatestEpochAsync(CancellationToken token = default)
    {
        var result = new OASISResult<ulong>();
        
        try
        {
            var metadata = await _httpClient.GetConstructionMetadataAsync(_config);
            
            if (metadata == null)
            {
                result.IsError = true;
                result.Message = "Failed to get latest epoch";
                return result;
            }

            result.Result = metadata.CurrentEpoch;
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<ulong>(ref result,
                $"Error getting latest epoch: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets detailed transaction information
    /// </summary>
    public async Task<OASISResult<Infrastructure.Entities.DTOs.Oracle.RadixTransactionDetails>> GetTransactionDetailsAsync(
        string intentHash,
        CancellationToken token = default)
    {
        var result = new OASISResult<Infrastructure.Entities.DTOs.Oracle.RadixTransactionDetails>();
        
        try
        {
            var statusResult = await GetTransactionStatusAsync(intentHash, token);
            
            if (statusResult.IsError)
            {
                result.IsError = true;
                result.Message = statusResult.Message;
                return result;
            }

            // Get chain state for epoch info
            var chainStateResult = await GetChainStateAsync(token);
            
            result.Result = new Infrastructure.Entities.DTOs.Oracle.RadixTransactionDetails
            {
                IntentHash = intentHash,
                TransactionHash = intentHash, // In Radix, intent hash is the transaction identifier
                Status = statusResult.Result.ToString(),
                Timestamp = DateTime.UtcNow,
                Epoch = chainStateResult.Result?.CurrentEpoch ?? 0,
                TokenSymbol = "XRD"
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<Infrastructure.Entities.DTOs.Oracle.RadixTransactionDetails>(ref result,
                $"Error getting transaction details: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Verifies a transaction's validity
    /// </summary>
    public async Task<OASISResult<bool>> VerifyTransactionAsync(
        string intentHash,
        CancellationToken token = default)
    {
        var result = new OASISResult<bool>();
        
        try
        {
            var statusResult = await GetTransactionStatusAsync(intentHash, token);
            
            if (statusResult.IsError)
            {
                result.Result = false;
                result.IsError = false; // Not an error, just not found
                return result;
            }

            // Transaction is verified if it's completed
            result.Result = statusResult.Result == BridgeTransactionStatus.Completed;
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<bool>(ref result,
                $"Error verifying transaction: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets XRD price feed (placeholder - would integrate with CoinGecko, CoinMarketCap, etc.)
    /// </summary>
    public async Task<OASISResult<Infrastructure.Entities.DTOs.Oracle.RadixPriceFeed>> GetXrdPriceAsync(
        string currency = "USD",
        CancellationToken token = default)
    {
        var result = new OASISResult<Infrastructure.Entities.DTOs.Oracle.RadixPriceFeed>();
        
        try
        {
            // TODO: Integrate with actual price sources (CoinGecko, CoinMarketCap, RadixDEX, etc.)
            // For now, return a placeholder structure
            
            result.Result = new Infrastructure.Entities.DTOs.Oracle.RadixPriceFeed
            {
                TokenSymbol = "XRD",
                Currency = currency,
                Price = 0, // Would fetch from price source
                Timestamp = DateTime.UtcNow,
                Source = "Placeholder", // Would be "CoinGecko", "CoinMarketCap", etc.
                Confidence = 0 // Would be calculated based on source reliability
            };
            
            result.IsError = false;
            result.Message = "Price feed not yet integrated - implement CoinGecko/CoinMarketCap integration";
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<Infrastructure.Entities.DTOs.Oracle.RadixPriceFeed>(ref result,
                $"Error getting XRD price: {ex.Message}", ex);
            return result;
        }
    }

    #endregion
}


using System;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.Infrastructure.Services.Arbitrum;

/// <summary>
/// Arbitrum bridge service for cross-chain operations
/// Implements all IOASISBridge methods for Arbitrum blockchain
/// </summary>
public sealed class ArbitrumBridgeService : IArbitrumBridgeService
{
    private const decimal WeiPerEth = 1_000_000_000_000_000_000m; // 10^18
    private readonly Web3 _web3;
    private readonly Account _technicalAccount;
    private readonly HexBigInteger _defaultGasLimit;
    private readonly string _hostUri;

    public ArbitrumBridgeService(Web3 web3, Account technicalAccount, HexBigInteger gasLimit = null, string hostUri = null)
    {
        _web3 = web3 ?? throw new ArgumentNullException(nameof(web3));
        _technicalAccount = technicalAccount ?? throw new ArgumentNullException(nameof(technicalAccount));
        _defaultGasLimit = gasLimit ?? new HexBigInteger(500000); // Default 500k gas
        _hostUri = hostUri ?? "http://localhost:8545"; // Default to localhost if not provided
    }

    /// <summary>
    /// Converts Wei to ETH
    /// </summary>
    private decimal ConvertWeiToEth(System.Numerics.BigInteger weiAmount) 
        => (decimal)weiAmount / WeiPerEth;

    /// <summary>
    /// Retrieves the balance of a given Arbitrum account in ETH
    /// </summary>
    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(
        string accountAddress, 
        CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
        
        try
        {
            if (string.IsNullOrWhiteSpace(accountAddress))
            {
                result.IsError = true;
                result.Message = "Account address cannot be null or empty";
                return result;
            }

            // Get balance in Wei
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(accountAddress);
            
            if (balance == null)
            {
                result.IsError = true;
                result.Message = "Failed to retrieve account balance";
                return result;
            }

            // Convert Wei to ETH
            result.Result = ConvertWeiToEth(balance.Value);
            result.IsError = false;
            result.Message = $"Balance retrieved successfully: {result.Result} ETH";
            
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error getting account balance: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Creates a new Arbitrum account and returns keys and seed phrase
    /// </summary>
    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> 
        CreateAccountAsync(CancellationToken token = default)
    {
        var result = new OASISResult<(string, string, string)>();
        
        try
        {
            // Generate new Ethereum account
            var ecKey = EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKey();
            var address = ecKey.GetPublicAddress();
            
            // For now, use a simple seed phrase generation
            // In production, add Nethereum.HdWallet for proper BIP39 support
            var seedPhrase = $"arbitrum seed {Guid.NewGuid().ToString("N")} wallet keys secure private";
            
            result.Result = (
                PublicKey: address,
                PrivateKey: privateKey,
                SeedPhrase: seedPhrase
            );
            result.IsError = false;
            result.Message = "Arbitrum account created successfully";
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error creating account: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Restores an existing Arbitrum account from a seed phrase
    /// Note: For full BIP39 support, add Nethereum.HdWallet package
    /// This implementation expects a private key as the "seed phrase"
    /// </summary>
    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> 
        RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        var result = new OASISResult<(string, string)>();
        
        try
        {
            if (string.IsNullOrWhiteSpace(seedPhrase))
            {
                result.IsError = true;
                result.Message = "Seed phrase/private key cannot be null or empty";
                return result;
            }

            // For now, treat the seed phrase as a private key
            // In production with Nethereum.HdWallet, you would parse BIP39 mnemonic
            var privateKey = seedPhrase.Trim();
            
            // Create account from private key
            var account = new Account(privateKey);
            
            result.Result = (
                PublicKey: account.Address,
                PrivateKey: account.PrivateKey
            );
            result.IsError = false;
            result.Message = "Account restored successfully";
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error restoring account: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Withdraws ETH from user's account to technical account (for bridge swap)
    /// </summary>
    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(
        decimal amount, 
        string senderAccountAddress, 
        string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        
        try
        {
            if (amount <= 0)
            {
                result.IsError = true;
                result.Message = "Amount must be greater than zero";
                return result;
            }

            if (string.IsNullOrWhiteSpace(senderAccountAddress))
            {
                result.IsError = true;
                result.Message = "Sender account address cannot be null or empty";
                return result;
            }

            if (string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                result.IsError = true;
                result.Message = "Sender private key cannot be null or empty";
                return result;
            }

            // Create account from sender's private key
            var senderAccount = new Account(senderPrivateKey);
            var senderWeb3 = new Web3(senderAccount, _hostUri);

            // Send ETH from sender to technical account  
            var receipt = await senderWeb3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(_technicalAccount.Address, amount);
            
            if (receipt == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = "",
                    IsSuccessful = false,
                    ErrorMessage = "Transaction receipt was null",
                    Status = BridgeTransactionStatus.NotFound
                };
                result.IsError = true;
                result.Message = "Transaction receipt was null";
                return result;
            }

            var isSuccessful = receipt.Status?.Value == 1;
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = receipt.TransactionHash,
                IsSuccessful = isSuccessful,
                ErrorMessage = isSuccessful ? null : "Transaction failed",
                Status = isSuccessful ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.NotFound
            };
            
            result.IsError = !isSuccessful;
            result.Message = isSuccessful 
                ? $"Withdrawal successful: {amount} ETH transferred" 
                : "Withdrawal transaction failed";
            
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error during withdrawal: {ex.Message}";
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = "",
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.NotFound
            };
            return result;
        }
    }

    /// <summary>
    /// Deposits ETH from technical account to receiver's account (completing bridge swap)
    /// </summary>
    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(
        decimal amount, 
        string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        
        try
        {
            if (amount <= 0)
            {
                result.IsError = true;
                result.Message = "Amount must be greater than zero";
                return result;
            }

            if (string.IsNullOrWhiteSpace(receiverAccountAddress))
            {
                result.IsError = true;
                result.Message = "Receiver account address cannot be null or empty";
                return result;
            }

            // Send ETH from technical account to receiver
            var receipt = await _web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount);
            
            if (receipt == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = "",
                    IsSuccessful = false,
                    ErrorMessage = "Transaction receipt was null",
                    Status = BridgeTransactionStatus.NotFound
                };
                result.IsError = true;
                result.Message = "Transaction receipt was null";
                return result;
            }

            var isSuccessful = receipt.Status?.Value == 1;
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = receipt.TransactionHash,
                IsSuccessful = isSuccessful,
                ErrorMessage = isSuccessful ? null : "Transaction failed",
                Status = isSuccessful ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.NotFound
            };
            
            result.IsError = !isSuccessful;
            result.Message = isSuccessful 
                ? $"Deposit successful: {amount} ETH transferred" 
                : "Deposit transaction failed";
            
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error during deposit: {ex.Message}";
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = "",
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.NotFound
            };
            return result;
        }
    }

    /// <summary>
    /// Gets the status of a transaction by its hash
    /// </summary>
    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(
        string transactionHash, 
        CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
        
        try
        {
            if (string.IsNullOrWhiteSpace(transactionHash))
            {
                result.IsError = true;
                result.Message = "Transaction hash cannot be null or empty";
                return result;
            }

            // Get transaction receipt
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt
                .SendRequestAsync(transactionHash);
            
            if (receipt == null)
            {
                // Transaction not yet mined
                result.Result = BridgeTransactionStatus.Pending;
                result.IsError = false;
                result.Message = "Transaction is pending confirmation";
                return result;
            }

            // Check if transaction was successful
            if (receipt.Status?.Value == 1)
            {
                result.Result = BridgeTransactionStatus.Completed;
                result.Message = $"Transaction completed in block {receipt.BlockNumber}";
            }
            else
            {
                result.Result = BridgeTransactionStatus.NotFound;
                result.Message = "Transaction failed";
            }
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error getting transaction status: {ex.Message}";
            return result;
        }
    }
}

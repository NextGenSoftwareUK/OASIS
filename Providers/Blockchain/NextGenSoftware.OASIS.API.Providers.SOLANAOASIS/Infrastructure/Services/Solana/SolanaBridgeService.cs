using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using Solnet.Rpc;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using Solnet.Programs;
using System.Net;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

/// <summary>
/// Solana bridge service for cross-chain operations
/// </summary>
public sealed class SolanaBridgeService : ISolanaBridgeService
{
    private const decimal Lamports = 1_000_000_000m;
    private readonly Account _technicalAccount;
    private readonly IRpcClient _rpcClient;

    public SolanaBridgeService(Account technicalAccount, IRpcClient rpcClient)
    {
        _technicalAccount = technicalAccount ?? throw new ArgumentNullException(nameof(technicalAccount));
        _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
    }

    private ulong ConvertSolToLamports(decimal amountInSol)
        => (ulong)(amountInSol * Lamports);

    /// <summary>
    /// Retrieves the balance of a given Solana account in SOL.
    /// </summary>
    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
        
        try
        {
            RequestResult<ResponseValue<AccountInfo>> response = await _rpcClient.GetAccountInfoAsync(accountAddress);

            if (response.WasSuccessful && response.Result.Value?.Lamports != null)
            {
                result.Result = response.Result.Value.Lamports / Lamports;
                result.IsError = false;
                return result;
            }

            result.Result = 0m;
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error getting account balance: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Creates a new Solana account with a seed phrase and retrieves its details.
    /// </summary>
    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(
        CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
        
        try
        {
            Mnemonic mnemonic = new(WordList.English, WordCount.Twelve);
            Wallet wallet = new(mnemonic);

            string seedPhrase = string.Join(" ", mnemonic.Words);
            string publicKey = wallet.Account.PublicKey;
            string privateKey = Convert.ToBase64String(wallet.Account.PrivateKey);

            result.Result = (publicKey, privateKey, seedPhrase);
            result.IsError = false;
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error creating account: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Restores a Solana account using a seed phrase.
    /// </summary>
    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(
        string seedPhrase,
        CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey)>();
        
        try
        {
            if (string.IsNullOrWhiteSpace(seedPhrase))
            {
                result.IsError = true;
                result.Message = "Invalid seed phrase";
                return result;
            }

            Mnemonic mnemonic = new(seedPhrase);
            Wallet wallet = new(mnemonic);

            string publicKey = wallet.Account.PublicKey;
            string privateKey = Convert.ToBase64String(wallet.Account.PrivateKey);

            result.Result = (publicKey, privateKey);
            result.IsError = false;
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error restoring account: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Executes a withdrawal from a client account to the technical account.
    /// </summary>
    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(
        decimal amount,
        string senderAccountAddress,
        string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        
        try
        {
            if (senderAccountAddress == _technicalAccount.PublicKey)
            {
                result.IsError = true;
                result.Message = "Cannot withdraw to the same account (self-transaction not allowed)";
                return result;
            }

            ulong lamports = ConvertSolToLamports(amount);

            Account clientAccount = new(
                Convert.FromBase64String(senderPrivateKey),
                new PublicKey(senderAccountAddress)
            );

            return await ExecuteTransactionAsync(clientAccount, _technicalAccount, lamports);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error during withdrawal: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Executes a deposit from the technical account to a client account.
    /// </summary>
    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(
        decimal amount,
        string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        
        try
        {
            if (receiverAccountAddress == _technicalAccount.PublicKey)
            {
                result.IsError = true;
                result.Message = "Cannot deposit to the same account (self-transaction not allowed)";
                return result;
            }

            ulong lamports = ConvertSolToLamports(amount);
            PublicKey receiverAccount = new(receiverAccountAddress);

            return await ExecuteTransactionAsync(_technicalAccount, receiverAccount, lamports);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error during deposit: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Executes a transaction between two accounts.
    /// </summary>
    private async Task<OASISResult<BridgeTransactionResponse>> ExecuteTransactionAsync(
        Account sender,
        PublicKey receiver,
        ulong lamports)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        
        try
        {
            // Check balance
            var balanceResult = await GetAccountBalanceAsync(sender.PublicKey);
            if (balanceResult.IsError)
            {
                result.IsError = true;
                result.Message = balanceResult.Message;
                return result;
            }

            decimal balanceSol = balanceResult.Result;
            if (lamports / Lamports >= balanceSol)
            {
                result.IsError = true;
                result.Message = "Insufficient funds in account";
                result.Result = new BridgeTransactionResponse(
                    string.Empty,
                    null,
                    false,
                    "Insufficient funds",
                    BridgeTransactionStatus.InsufficientFunds);
                return result;
            }

            // Get latest blockhash
            RequestResult<ResponseValue<LatestBlockHash>> latestBlockHashResult =
                await _rpcClient.GetLatestBlockHashAsync();
                
            if (!latestBlockHashResult.WasSuccessful || latestBlockHashResult.Result?.Value == null)
            {
                result.IsError = true;
                result.Message = $"Failed to get latest blockhash: {latestBlockHashResult.Reason}";
                return result;
            }

            string recentBlockHash = latestBlockHashResult.Result.Value.Blockhash;

            // Build transaction
            Transaction transaction = new()
            {
                RecentBlockHash = recentBlockHash,
                FeePayer = sender.PublicKey
            };

            transaction.Add(SystemProgram.Transfer(sender.PublicKey, receiver, lamports));
            transaction.Sign(sender);

            // Send transaction
            RequestResult<string> sendResult = await _rpcClient.SendTransactionAsync(transaction.Serialize());
            
            if (!sendResult.WasSuccessful)
            {
                result.IsError = true;
                result.Message = $"Transaction failed: {sendResult.Reason}";
                result.Result = new BridgeTransactionResponse(
                    string.Empty,
                    null,
                    false,
                    sendResult.Reason,
                    BridgeTransactionStatus.Canceled);
                return result;
            }

            result.Result = new BridgeTransactionResponse(
                sendResult.Result,
                sendResult.Result,
                true,
                null,
                BridgeTransactionStatus.Completed
            );
            result.IsError = false;
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error executing transaction: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Retrieves the status of a Solana transaction using its hash.
    /// </summary>
    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(
        string transactionHash,
        CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
        
        try
        {
            Commitment commitment = Commitment.Confirmed;
            RequestResult<TransactionMetaSlotInfo> transactionStatusResult =
                await _rpcClient.GetTransactionAsync(transactionHash, commitment);

            if (transactionStatusResult == null)
            {
                result.IsError = true;
                result.Message = "Transaction not found";
                result.Result = BridgeTransactionStatus.NotFound;
                return result;
            }

            if (!transactionStatusResult.WasSuccessful || transactionStatusResult.Result == null)
            {
                if (transactionStatusResult.HttpStatusCode == HttpStatusCode.BadRequest)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = false;
                    return result;
                }
                
                result.IsError = true;
                result.Message = "Failed to get transaction status";
                return result;
            }

            BridgeTransactionStatus status = transactionStatusResult.Result?.Meta?.Error == null
                ? BridgeTransactionStatus.Completed
                : BridgeTransactionStatus.Canceled;

            result.Result = status;
            result.IsError = false;
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error getting transaction status: {ex.Message}", ex);
            return result;
        }
    }
}


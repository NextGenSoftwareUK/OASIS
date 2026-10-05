using Application.Contracts;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Messages;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using System.Text;

namespace Infrastructure.Blockchain.Solana;

/// <summary>
/// Solana implementation of on-chain funding rate publisher using PDAs
/// </summary>
public class SolanaOnChainFundingPublisher : IOnChainFundingPublisher
{
    private readonly IRpcClient _rpcClient;
    private readonly Account _signerAccount;
    private readonly SolanaPdaManager _pdaManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SolanaOnChainFundingPublisher> _logger;
    private readonly PublicKey _programId;

    public BlockchainProviderType ProviderType => BlockchainProviderType.Solana;

    public SolanaOnChainFundingPublisher(
        IRpcClient rpcClient,
        IConfiguration configuration,
        SolanaPdaManager pdaManager,
        ILogger<SolanaOnChainFundingPublisher> logger)
    {
        _rpcClient = rpcClient;
        _configuration = configuration;
        _pdaManager = pdaManager;
        _logger = logger;

        // Load signer account from configuration
        // Support both base64 encoded and raw hex strings
        var privateKey = configuration["Blockchain:Solana:PrivateKey"] 
            ?? configuration["SolanaTechnicalAccountBridgeOptions:PrivateKey"];
        var publicKey = configuration["Blockchain:Solana:PublicKey"] 
            ?? configuration["SolanaTechnicalAccountBridgeOptions:PublicKey"];

        if (string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(publicKey))
        {
            throw new InvalidOperationException(
                "Solana private key and public key must be configured in Blockchain:Solana:PrivateKey/PublicKey or SolanaTechnicalAccountBridgeOptions");
        }

        // Try to decode as base64, if that fails, assume it's a hex string
        byte[] privateKeyBytes;
        try
        {
            privateKeyBytes = Convert.FromBase64String(privateKey);
        }
        catch
        {
            // If base64 decode fails, try hex
            privateKeyBytes = Convert.FromHexString(privateKey);
        }

        _signerAccount = new Account(privateKeyBytes, publicKey);
        
        // Load program ID
        var programIdString = configuration["Blockchain:Solana:FundingRateProgramId"] 
            ?? "Fg6PaFpoGXkYsidMpWTK6W2BeZ7FEfcYkg476zPFsLnS";
        _programId = new PublicKey(programIdString);
    }

    public async Task<OnChainPublishResult> PublishFundingRateAsync(
        string symbol,
        FundingRateResponse rate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Publishing funding rate for {Symbol} on Solana", symbol);

            // 1. Get or create PDA for symbol
            var (pda, bump) = await _pdaManager.GetOrCreateFundingRatePdaAsync(symbol, cancellationToken);

            // 2. Check if account exists, if not initialize it
            var accountInfo = await _rpcClient.GetAccountInfoAsync(pda.Key);
            if (!accountInfo.WasSuccessful || accountInfo.Result?.Value == null)
            {
                _logger.LogInformation("Funding rate account does not exist for {Symbol}, initializing...", symbol);
                var initResult = await InitializeFundingRateAccountAsync(symbol, cancellationToken);
                if (!initResult)
                {
                    return new OnChainPublishResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to initialize funding rate account",
                        ProviderType = ProviderType
                    };
                }
            }

            // 3. Build update instruction
            var instruction = BuildUpdateFundingRateInstruction(pda, bump, rate);

            // 4. Build and send transaction
            var transactionBytes = await BuildTransactionAsync(instruction, cancellationToken);
            var signature = await SendTransactionAsync(transactionBytes, cancellationToken);

            // 5. Wait for confirmation
            await ConfirmTransactionAsync(signature, cancellationToken);

            _logger.LogInformation("Successfully published funding rate for {Symbol}, tx: {TransactionHash}", 
                symbol, signature);

            return new OnChainPublishResult
            {
                Success = true,
                TransactionHash = signature,
                AccountAddress = pda.Key,
                PublishedAt = DateTime.UtcNow,
                Confirmations = 1,
                ProviderType = ProviderType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish funding rate for {Symbol} on Solana", symbol);
            return new OnChainPublishResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProviderType = ProviderType
            };
        }
    }

    public async Task<Dictionary<string, OnChainPublishResult>> PublishBatchFundingRatesAsync(
        Dictionary<string, FundingRateResponse> rates,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, OnChainPublishResult>();

        foreach (var (symbol, rate) in rates)
        {
            results[symbol] = await PublishFundingRateAsync(symbol, rate, cancellationToken);
        }

        return results;
    }

    public async Task<OnChainFundingRate?> GetOnChainFundingRateAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (pda, _) = _pdaManager.DeriveFundingRatePda(symbol);
            
            var accountInfo = await _rpcClient.GetAccountInfoAsync(pda.Key);
            if (!accountInfo.WasSuccessful || accountInfo.Result?.Value?.Data == null)
            {
                return null;
            }

            return ParseFundingRateAccount(accountInfo.Result.Value.Data, symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get on-chain funding rate for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<Dictionary<string, OnChainFundingRate>> GetOnChainFundingRatesAsync(
        List<string> symbols,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, OnChainFundingRate>();

        foreach (var symbol in symbols)
        {
            var rate = await GetOnChainFundingRateAsync(symbol, cancellationToken);
            if (rate != null)
            {
                results[symbol] = rate;
            }
        }

        return results;
    }

    public async Task<bool> InitializeFundingRateAccountAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (pda, bump) = _pdaManager.DeriveFundingRatePda(symbol);

            // Check if already exists
            var accountInfo = await _rpcClient.GetAccountInfoAsync(pda.Key);
            if (accountInfo.WasSuccessful && accountInfo.Result?.Value != null)
            {
                _logger.LogInformation("Funding rate account already exists for {Symbol}", symbol);
                return true;
            }

            // Build initialize instruction
            var instruction = BuildInitializeFundingRateInstruction(pda, bump, symbol);

            // Build and send transaction
            var transaction = await BuildTransactionAsync(instruction, cancellationToken);
            var signature = await SendTransactionAsync(transaction, cancellationToken);

            // Wait for confirmation
            await ConfirmTransactionAsync(signature, cancellationToken);

            _logger.LogInformation("Initialized funding rate account for {Symbol}, tx: {TransactionHash}", 
                symbol, signature);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize funding rate account for {Symbol}", symbol);
            return false;
        }
    }

    public async Task<bool> IsFundingRateAccountInitializedAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (pda, _) = _pdaManager.DeriveFundingRatePda(symbol);
            var accountInfo = await _rpcClient.GetAccountInfoAsync(pda.Key);
            return accountInfo.WasSuccessful && accountInfo.Result?.Value != null;
        }
        catch
        {
            return false;
        }
    }

    public Task<string?> GetFundingRateAccountAddressAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var (pda, _) = _pdaManager.DeriveFundingRatePda(symbol);
        return Task.FromResult<string?>(pda.Key);
    }

    private TransactionInstruction BuildInitializeFundingRateInstruction(
        PublicKey pda,
        byte bump,
        string symbol)
    {
        // This is a simplified version - in production, you'd use Anchor's instruction builder
        // or generate client code from the IDL
        
        var symbolBytes = Encoding.UTF8.GetBytes(symbol);
        var seeds = new List<byte[]>
        {
            Encoding.UTF8.GetBytes("funding_rate"),
            symbolBytes
        };

        var accounts = new List<AccountMeta>
        {
            AccountMeta.Writable(pda, false),
            AccountMeta.ReadOnly(_signerAccount.PublicKey, true),
            AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false)
        };

        var instructionData = new List<byte>();
        // Anchor discriminator for initialize_funding_rate (first 8 bytes of sha256("global:initialize_funding_rate"))
        // For now, we'll use a placeholder - this should be generated from the IDL
        instructionData.AddRange(new byte[8]); // Discriminator
        instructionData.AddRange(BitConverter.GetBytes(symbolBytes.Length));
        instructionData.AddRange(symbolBytes);

        return new TransactionInstruction
        {
            ProgramId = _programId,
            Keys = accounts,
            Data = instructionData.ToArray()
        };
    }

    private TransactionInstruction BuildUpdateFundingRateInstruction(
        PublicKey pda,
        byte bump,
        FundingRateResponse rate)
    {
        // Convert rates to basis points (multiply by 10000)
        var rateBasisPoints = (long)(rate.Rate * 10000);
        var hourlyRateBasisPoints = (long)(rate.HourlyRate * 10000);
        
        // Convert prices to smallest unit (multiply by 10^8)
        var markPriceScaled = (ulong)(rate.MarkPrice * 100_000_000);
        var spotPriceScaled = (ulong)(rate.SpotPrice * 100_000_000);
        
        // Premium can be negative
        var premiumScaled = (long)(rate.Premium * 100_000_000);
        
        // Valid until as Unix timestamp
        var validUntil = ((DateTimeOffset)rate.ValidUntil).ToUnixTimeSeconds();

        var accounts = new List<AccountMeta>
        {
            AccountMeta.Writable(pda, false),
            AccountMeta.ReadOnly(_signerAccount.PublicKey, true)
        };

        var instructionData = new List<byte>();
        // Anchor discriminator for update_funding_rate (placeholder - should be from IDL)
        instructionData.AddRange(new byte[8]);
        instructionData.AddRange(BitConverter.GetBytes(rateBasisPoints));
        instructionData.AddRange(BitConverter.GetBytes(hourlyRateBasisPoints));
        instructionData.AddRange(BitConverter.GetBytes(markPriceScaled));
        instructionData.AddRange(BitConverter.GetBytes(spotPriceScaled));
        instructionData.AddRange(BitConverter.GetBytes(premiumScaled));
        instructionData.AddRange(BitConverter.GetBytes(validUntil));

        return new TransactionInstruction
        {
            ProgramId = _programId,
            Keys = accounts,
            Data = instructionData.ToArray()
        };
    }

    private async Task<byte[]> BuildTransactionAsync(
        TransactionInstruction instruction,
        CancellationToken cancellationToken)
    {
        var blockHashResult = await _rpcClient.GetLatestBlockHashAsync();
        if (!blockHashResult.WasSuccessful)
        {
            throw new Exception($"Failed to get latest blockhash: {blockHashResult.Reason}");
        }

        var transaction = new TransactionBuilder()
            .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
            .SetFeePayer(_signerAccount.PublicKey)
            .AddInstruction(instruction)
            .Build(_signerAccount);

        return transaction;
    }

    private async Task<string> SendTransactionAsync(
        byte[] transactionBytes,
        CancellationToken cancellationToken)
    {
        var sendResult = await _rpcClient.SendTransactionAsync(transactionBytes);

        if (!sendResult.WasSuccessful)
        {
            throw new Exception($"Transaction send failed: {sendResult.Reason}");
        }

        return sendResult.Result;
    }

    private async Task ConfirmTransactionAsync(
        string signature,
        CancellationToken cancellationToken)
    {
        var confirmation = await _rpcClient.ConfirmTransaction(
            signature,
            Commitment.Confirmed);

        if (confirmation.Result?.Value?.Err != null)
        {
            throw new Exception($"Transaction failed: {confirmation.Result.Value.Err}");
        }
    }

    private OnChainFundingRate ParseFundingRateAccount(
        object accountData,
        string symbol)
    {
        // This is a placeholder - in production, use Anchor's deserialization
        // or generated client code from the IDL
        // For now, return a basic structure
        
        return new OnChainFundingRate
        {
            Symbol = symbol,
            ProviderType = ProviderType,
            AccountAddress = _pdaManager.DeriveFundingRatePda(symbol).Item1.Key
            // Other fields would be parsed from accountData
        };
    }
}


using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Starknet;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.Bridges;

/// <summary>
/// Minimal Starknet bridge that talks to the configured RPC endpoint.
/// </summary>
public class StarknetBridge : IOASISBridge
{
    private readonly string _network;
    private readonly IStarknetRpcClient _rpcClient;

    public StarknetBridge(string network, IStarknetRpcClient rpcClient)
    {
        _network = network;
        _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
    }

    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        var balanceResult = await _rpcClient.GetBalanceAsync(accountAddress);
        if (balanceResult.IsError)
        {
            return new OASISResult<decimal> { IsError = true, Message = balanceResult.Message };
        }

        return new OASISResult<decimal>
        {
            Result = balanceResult.Result,
            IsError = false,
            Message = $"Balance retrieved for {accountAddress} on {_network}"
        };
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
    {
        try
        {
            // Generate a new Starknet account
            // In production, this would use a Starknet SDK like StarknetSharp or similar
            // For now, we generate deterministic keys based on a seed phrase
            
            var seedPhrase = GenerateSeedPhrase();
            var (publicKey, privateKey) = DeriveKeysFromSeed(seedPhrase);

            var result = new OASISResult<(string, string, string)>
            {
                Result = (publicKey, privateKey, seedPhrase),
                IsError = false,
                Message = $"Starknet account created on {_network}"
            };

            return result;
        }
        catch (Exception ex)
        {
            return new OASISResult<(string, string, string)>
            {
                IsError = true,
                Message = $"Failed to create Starknet account: {ex.Message}",
                Exception = ex
            };
        }
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(seedPhrase))
        {
            return new OASISResult<(string, string)>
            {
                IsError = true,
                Message = "Seed phrase is required"
            };
        }

        try
        {
            // Derive keys from seed phrase
            var (publicKey, privateKey) = DeriveKeysFromSeed(seedPhrase);

            var result = new OASISResult<(string, string)>
            {
                Result = (publicKey, privateKey),
                IsError = false,
                Message = $"Starknet account restored from seed on {_network}"
            };

            return result;
        }
        catch (Exception ex)
        {
            return new OASISResult<(string, string)>
            {
                IsError = true,
                Message = $"Failed to restore Starknet account: {ex.Message}",
                Exception = ex
            };
        }
    }

    private string GenerateSeedPhrase()
    {
        // Generate a deterministic seed phrase
        // In production, use a proper BIP39 or similar mnemonic generation
        var words = new[]
        {
            "abandon", "ability", "able", "about", "above", "absent", "absorb", "abstract",
            "absurd", "abuse", "access", "accident", "account", "accuse", "achieve", "acid"
        };

        var random = new Random();
        var seedWords = new List<string>();
        for (int i = 0; i < 12; i++)
        {
            seedWords.Add(words[random.Next(words.Length)]);
        }

        return string.Join(" ", seedWords);
    }

    private (string PublicKey, string PrivateKey) DeriveKeysFromSeed(string seedPhrase)
    {
        // Derive keys from seed phrase
        // In production, use proper cryptographic key derivation (e.g., BIP32/BIP44)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var seedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(seedPhrase));
        
        // Generate private key (64 hex chars = 32 bytes)
        var privateKey = $"0x{BitConverter.ToString(seedBytes).Replace("-", "").ToLowerInvariant()}";
        
        // Derive public key from private key (simplified - in production use proper EC operations)
        var publicKeyBytes = sha256.ComputeHash(seedBytes);
        var publicKey = $"0x{BitConverter.ToString(publicKeyBytes).Replace("-", "").ToLowerInvariant()}";

        return (publicKey, privateKey);
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        var response = new BridgeTransactionResponse(Guid.NewGuid().ToString(), null, true, null, BridgeTransactionStatus.Pending);

        if (string.IsNullOrWhiteSpace(senderAccountAddress))
        {
            return new OASISResult<BridgeTransactionResponse>
            {
                IsError = true,
                Message = "Sender address is required"
            };
        }

        var balance = await _rpcClient.GetBalanceAsync(senderAccountAddress);
        if (balance.IsError)
        {
            return new OASISResult<BridgeTransactionResponse> { IsError = true, Message = balance.Message };
        }

        if (balance.Result < amount)
        {
            return new OASISResult<BridgeTransactionResponse>
            {
                IsError = true,
                Message = $"Insufficient Starknet funds ({balance.Result}) for withdraw {amount}"
            };
        }

        var txResult = await _rpcClient.SubmitTransactionAsync(new StarknetTransactionPayload
        {
            From = senderAccountAddress,
            To = string.Empty,
            Amount = amount
        });

        if (txResult.IsError)
        {
            return new OASISResult<BridgeTransactionResponse> { IsError = true, Message = txResult.Message };
        }

        response.TransactionId = txResult.Result;
        response.Status = BridgeTransactionStatus.Pending;

        return new OASISResult<BridgeTransactionResponse>
        {
            Result = response,
            IsError = false,
            Message = $"Withdrawal submitted (tx {txResult.Result})"
        };
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        if (string.IsNullOrWhiteSpace(receiverAccountAddress))
        {
            return new OASISResult<BridgeTransactionResponse>
            {
                IsError = true,
                Message = "Receiver address is required"
            };
        }

        var txResult = await _rpcClient.SubmitTransactionAsync(new StarknetTransactionPayload
        {
            From = string.Empty,
            To = receiverAccountAddress,
            Amount = amount
        });

        if (txResult.IsError)
        {
            return new OASISResult<BridgeTransactionResponse> { IsError = true, Message = txResult.Message };
        }

        var response = new BridgeTransactionResponse(txResult.Result, null, true, null, BridgeTransactionStatus.Pending);

        return new OASISResult<BridgeTransactionResponse>
        {
            Result = response,
            IsError = false,
            Message = $"Deposit submitted (tx {txResult.Result})"
        };
    }

    public Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
    {
        return _rpcClient.GetTransactionStatusAsync(transactionHash);
    }
}


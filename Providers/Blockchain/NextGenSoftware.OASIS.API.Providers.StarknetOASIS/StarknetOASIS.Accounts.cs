using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

/// <summary>Real Starknet account material and deployment through the pinned starknet.js bridge.</summary>
public sealed record StarknetAccountStatus(string Address, bool Deployed, string ClassHash, string Nonce);
public sealed record StarknetDeployFee(string Address, string OverallFee, string Unit);
public sealed record StarknetAccountDeployment(string Address, string TransactionHash, string ExecutionStatus, string FinalityStatus);

public sealed partial class StarknetOASIS
{
    // ── key material (offline) ──────────────────────────────────────────────────────────────────

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair() => GenerateKeyPairAsync().GetAwaiter().GetResult();

    /// <summary>A fresh Stark-curve key pair from a new BIP-39 mnemonic, with its OpenZeppelin account address.</summary>
    public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
    {
        var result = new OASISResult<IKeyPairAndWallet>();
        try
        {
            var created = await Sdk.InvokeAsync(new { operation = "createAccount", classHash = _accountClassHash });
            result.Result = new KeyHelper.KeyPairAndWallet
            {
                PrivateKey = Text(created, "privateKey"),
                PublicKey = Text(created, "publicKey"),
                WalletAddressLegacy = Text(created, "address")
            };
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Starknet key generation failed: {ex.Message}", ex);
        }
        return result;
    }

    /// <summary>A new account: BIP-39 mnemonic, Stark-curve key pair (m/44'/9004'/0'/0/0, ground) and public key.</summary>
    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
        try
        {
            var created = await Sdk.InvokeAsync(new { operation = "createAccount", classHash = _accountClassHash }, token);
            result.Result = (Text(created, "publicKey"), Text(created, "privateKey"), Text(created, "mnemonic"));
            result.Message = $"Starknet account {Text(created, "address")} created for {_network}; deploy it once it holds STRK for fees.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Starknet account creation failed: {ex.Message}", ex);
        }
        return result;
    }

    /// <summary>Restores the first account (index 0) from a BIP-39 mnemonic.</summary>
    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey)>();
        if (string.IsNullOrWhiteSpace(seedPhrase))
        {
            OASISErrorHandling.HandleError(ref result, "A BIP-39 seed phrase is required.");
            return result;
        }
        try
        {
            var restored = await Sdk.InvokeAsync(new { operation = "restoreAccount", mnemonic = seedPhrase, index = 0, classHash = _accountClassHash }, token);
            result.Result = (Text(restored, "publicKey"), Text(restored, "privateKey"));
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Starknet account restore failed: {ex.Message}", ex);
        }
        return result;
    }

    /// <summary>The OpenZeppelin account address controlled by a Stark private key.</summary>
    public async Task<OASISResult<string>> GetAccountAddressAsync(string privateKey, CancellationToken token = default)
    {
        var result = new OASISResult<string>();
        try
        {
            var derived = await Sdk.InvokeAsync(new { operation = "deriveFromPrivateKey", privateKey, classHash = _accountClassHash }, token);
            result.Result = Text(derived, "address");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Starknet address derivation failed: {ex.Message}", ex);
        }
        return result;
    }

    // ── on-chain account lifecycle ──────────────────────────────────────────────────────────────

    public async Task<OASISResult<StarknetAccountStatus>> GetAccountStatusAsync(string address, CancellationToken token = default)
    {
        var result = new OASISResult<StarknetAccountStatus>();
        try
        {
            var status = await Sdk.InvokeAsync(new { operation = "accountStatus", endpoint = _rpcEndpoint, chainId = _chainId, address }, token);
            var deployed = status.GetProperty("deployed").GetBoolean();
            result.Result = new StarknetAccountStatus(Text(status, "address"), deployed,
                deployed ? Text(status, "classHash") : null, deployed ? Text(status, "nonce") : null);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Starknet account status failed: {ex.Message}", ex);
        }
        return result;
    }

    /// <summary>Fee (in FRI, 10^-18 STRK) to deploy the account for this key; the address must hold at least this before deploying.</summary>
    public async Task<OASISResult<StarknetDeployFee>> EstimateAccountDeployFeeAsync(string privateKey, CancellationToken token = default)
    {
        var result = new OASISResult<StarknetDeployFee>();
        try
        {
            var fee = await Sdk.InvokeAsync(new { operation = "estimateDeployFee", endpoint = _rpcEndpoint, chainId = _chainId, classHash = _accountClassHash, privateKey }, token);
            result.Result = new StarknetDeployFee(Text(fee, "address"), Text(fee, "overallFee"), Text(fee, "unit"));
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Starknet deploy fee estimate failed: {ex.Message}", ex);
        }
        return result;
    }

    /// <summary>
    /// Deploys the OpenZeppelin account for this key (the address must already hold STRK for the fee). Submits once.
    /// If the transaction was submitted but its outcome is unknown, the result carries a reconciliation-required
    /// exception: check <see cref="GetAccountStatusAsync"/> for the deterministic address before any retry.
    /// </summary>
    public async Task<OASISResult<StarknetAccountDeployment>> DeployAccountAsync(string privateKey, CancellationToken token = default)
    {
        var result = new OASISResult<StarknetAccountDeployment>();
        try
        {
            var deployed = await Sdk.InvokeAsync(new { operation = "deployAccount", endpoint = _rpcEndpoint, chainId = _chainId, classHash = _accountClassHash, privateKey }, token);
            result.Result = new StarknetAccountDeployment(Text(deployed, "address"), Text(deployed, "transactionHash"),
                Text(deployed, "executionStatus"), Text(deployed, "finalityStatus"));
            result.IsSaved = true;
        }
        catch (StarknetTransactionReconciliationException ex)
        {
            OASISErrorHandling.HandleError(ref result, ex.Message, ex);
        }
        catch (OperationCanceledException ex)
        {
            // The bridge may have submitted before cancellation; the address is deterministic, so say how to reconcile.
            OASISErrorHandling.HandleError(ref result,
                "Starknet account deployment was interrupted; check the account status for this key before retrying.",
                new StarknetTransactionReconciliationException(string.Empty, ex));
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Starknet account deployment failed: {ex.Message}", ex);
        }
        return result;
    }

    /// <summary>Deploys the account for the avatar's single Starknet key held in the OASIS key store.</summary>
    public async Task<OASISResult<StarknetAccountDeployment>> DeployAvatarAccountAsync(Guid avatarId, CancellationToken token = default)
    {
        var result = new OASISResult<StarknetAccountDeployment>();
        var keys = ResolveAccountKeys(avatarId);
        if (keys.IsError)
        {
            OASISErrorHandling.HandleError(ref result, keys.Message, keys.Exception);
            result.ErrorCode = keys.ErrorCode;
            return result;
        }
        var distinct = (keys.Result ?? new List<string>()).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (distinct.Count != 1)
        {
            OASISErrorHandling.HandleError(ref result, distinct.Count == 0
                ? "The avatar has no Starknet key; create or import one first."
                : "The avatar has several Starknet keys; deploy a specific key's account instead.");
            return result;
        }
        return await DeployAccountAsync(distinct[0], token);
    }

    internal OASISResult<List<string>> ResolveAccountKeys(Guid avatarId) => ResolveAccountKeysCore(avatarId);
    internal Func<Guid, OASISResult<List<string>>> ResolveAccountKeysCore { get; set; } =
        avatarId => KeyManager.Instance.GetProviderPrivateKeysForAvatarById(avatarId, Core.Enums.ProviderType.StarknetOASIS);

    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()
            : throw new System.IO.InvalidDataException($"Starknet SDK reply is missing '{name}'.");
}

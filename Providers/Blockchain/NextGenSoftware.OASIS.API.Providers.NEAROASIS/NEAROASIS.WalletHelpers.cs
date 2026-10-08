using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<decimal>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await EnsureSdkActiveAsync(result)) return result;
            var yocto = BigInteger.Parse(await _sdk.GetBalanceYoctoAsync(accountAddress));
            result.Result = (decimal)yocto / 1_000_000_000_000_000_000_000_000m;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error reading NEAR account balance: {ex.Message}", ex); }
        return result;
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<(string, string, string)>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = await _sdk.GenerateKeyPairAsync();
            result.Result = (key.PublicKey, key.PrivateKey, string.Empty);
            result.Message = $"Generated NEAR implicit account {key.ImplicitAccountId}; fund it before use. NEAR uses an Ed25519 secret key rather than a mnemonic.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error generating NEAR account key: {ex.Message}", ex); }
        return result;
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<(string, string)>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(seedPhrase)) throw new ArgumentException("A NEAR Ed25519 secret key is required.", nameof(seedPhrase));
            var key = await _sdk.DerivePublicKeyAsync(seedPhrase);
            result.Result = (key.PublicKey, key.PrivateKey);
            result.Message = $"Restored NEAR implicit account {key.ImplicitAccountId} from its Ed25519 secret key.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error restoring NEAR account key: {ex.Message}", ex); }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount,
        string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                throw new ArgumentException("Positive amount, sender account, and sender private key are required.");
            var yocto = new BigInteger(decimal.Round(amount * 1_000_000_000_000_000_000_000_000m,
                0, MidpointRounding.AwayFromZero)).ToString();
            var tx = await _sdk.TransferAsync(_accountId, yocto, senderAccountAddress, senderPrivateKey);
            await RecordWalletTransactionAsync(tx, senderAccountAddress, _accountId, amount, "NEAR bridge withdrawal");
            result.Result = new BridgeTransactionResponse(tx, null, true, null, BridgeTransactionStatus.Completed);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error withdrawing NEAR: {ex.Message}", ex); }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount,
        string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(receiverAccountAddress))
                throw new ArgumentException("Positive amount and receiver account are required.");
            var yocto = new BigInteger(decimal.Round(amount * 1_000_000_000_000_000_000_000_000m,
                0, MidpointRounding.AwayFromZero)).ToString();
            var tx = await _sdk.TransferAsync(receiverAccountAddress, yocto);
            await RecordWalletTransactionAsync(tx, _accountId, receiverAccountAddress, amount, "NEAR bridge deposit");
            result.Result = new BridgeTransactionResponse(tx, null, true, null, BridgeTransactionStatus.Completed);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error depositing NEAR: {ex.Message}", ex); }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = await _sdk.GetTransactionStatusAsync(transactionHash);
            result.Result = status is "FINAL" or "EXECUTED" or "EXECUTED_OPTIMISTIC"
                ? BridgeTransactionStatus.Completed
                : BridgeTransactionStatus.Pending;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error reading NEAR transaction status: {ex.Message}", ex); }
        return result;
    }
}

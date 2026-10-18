using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

public partial class CosmosBlockChainOASIS
{
    private readonly CosmosTokenEscrowConfiguration _tokenEscrow;

    private async Task<OASISResult<ITransactionResponse>> ChangeTokenEscrowAsync(string symbol, bool locking,
        Guid actor, Guid lockId, decimal amount, string requestedEscrow, string suppliedKey = null, string from = null)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (_tokenEscrow == null)
            {
                // Original issuer freeze is distinct; explicit deposits must never downgrade to it.
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.AggregateTokenEscrowRequest.Validate(
                    amount, lockId, requestedEscrow, _contractAddress);
                return await ChangeTokenLockAsync(symbol, locking, actor, suppliedKey, from);
            }
            if (string.IsNullOrWhiteSpace(symbol) || symbol == _nativeDenom || actor == Guid.Empty
                || lockId == Guid.Empty || requestedEscrow != _tokenEscrow.ContractAddress || locking && amount <= 0)
                throw new ArgumentException("Custom token escrow requires avatar, original deposit ID, matching reviewed contract and positive lock amount.");
            var units = locking ? AssetUnits(amount) : null;
            var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lockId.ToString("N")))).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(suppliedKey))
            {
                var wallet = await LoadSigningWalletAsync(actor);
                if (wallet.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Core.Interfaces.IProviderWallet, ITransactionResponse>(wallet);
                suppliedKey = wallet.Result.PrivateKey;
            }
            var key = await SigningKeyAsync(actor, suppliedKey);
            var owner = await SignerAddressAsync(key);
            if (string.IsNullOrWhiteSpace(owner) || from != null && from != owner)
                throw new ArgumentException("Escrow signer must own the requested sender.");
            var info = await _sdkBackend.InvokeAsync("tokenEscrowInfo", new {
                targetContractAddress = _tokenEscrow.ContractAddress, codeSha256 = _tokenEscrow.CodeSha256 });
            if (info.GetProperty("contractAddress").GetString() != _tokenEscrow.ContractAddress
                || info.GetProperty("codeSha256").GetString() != _tokenEscrow.CodeSha256 || info.GetProperty("protocol").GetInt32() != 1)
                throw new InvalidDataException("Escrow contract identity or protocol differs from reviewed configuration.");
            var query = new { token_deposit = new { deposit_id = id } };
            var original = await QueryAssetAsync(query, _tokenEscrow.ContractAddress);
            if (locking)
            {
                if (original.ValueKind != JsonValueKind.Null) throw new InvalidOperationException("Deposit ID already used; reconcile original operation instead of replacing it.");
            }
            else
            {
                units = original.GetProperty("amount").GetString();
                ValidateDeposit(original, id, symbol, owner, units, false);
            }
            object message = locking
                ? new { token_deposit = new { deposit_id = id, symbol, amount = units } }
                : new { token_release = new { deposit_id = id, symbol, amount = units } };
            var hash = await ExecuteAssetAsync(message, key, owner, _tokenEscrow.ContractAddress);
            try
            {
                var saved = await QueryAssetAsync(query, _tokenEscrow.ContractAddress);
                ValidateDeposit(saved, id, symbol, owner, units, !locking);
            }
            catch (Exception ex)
            {
                throw new CosmosNativeMutationReconciliationException("Escrow committed but original deposit readback failed; reconcile before retry.", hash, true, ex);
            }
            result.Result = new CosmosIdBoundEscrowResponse(lockId, hash);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    private static void ValidateDeposit(JsonElement saved, string id, string symbol, string owner, string units, bool released)
    {
        if (ParseCustomTokenBalanceUnits(units).IsZero || saved.GetProperty("deposit_id").GetString() != id
            || saved.GetProperty("symbol").GetString() != symbol || saved.GetProperty("owner").GetString() != owner
            || saved.GetProperty("amount").GetString() != units || saved.GetProperty("released").GetBoolean() != released)
            throw new InvalidDataException("Exact original owned token deposit and release state required.");
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

public partial class SuiOASIS
{
    private readonly SuiTokenEscrowConfiguration _tokenEscrow;
    private async Task<OASISResult<ITransactionResponse>> TokenEscrowAsync(bool locking, string token, Guid actor,
        Guid id, decimal amount, string escrow, string key = null, string from = null)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            ValidateCoinType(token);
            if (_tokenEscrow == null)
            {
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.AggregateTokenEscrowRequest.Validate(amount, id, escrow, _contractAddress);
                return await TokenCallAsync(locking ? "tokenLock" : "tokenUnlock", async () => new {
                    privateKey = await TokenSigningKeyAsync(actor, key), fromWalletAddress = from });
            }
            if (actor == Guid.Empty || id == Guid.Empty || escrow != _tokenEscrow.EscrowObjectId || locking && amount <= 0)
                throw new ArgumentException("Per-deposit custody requires avatar, original deposit ID, matching shared escrow and positive lock amount.");
            var units = locking ? TokenUnits(amount) : null;
            var depositId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("N")))).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(key))
            {
                var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(actor, Core.Enums.ProviderType.SuiOASIS, showPrivateKeys: true);
                if (wallet.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Core.Interfaces.IProviderWallet, ITransactionResponse>(wallet);
                if (wallet.Result == null || wallet.Result.AvatarId != actor || string.IsNullOrWhiteSpace(wallet.Result.PrivateKey))
                    throw new InvalidOperationException("Exact requesting avatar signing wallet is required.");
                key = wallet.Result.PrivateKey;
            }
            var signer = await TokenSignerAsync(actor, key);
            if (string.IsNullOrWhiteSpace(signer.Address) || from != null && !SameSuiAddress(from, signer.Address))
                throw new ArgumentException("Escrow signer does not own requested sender.");
            var reply = await InvokeSdkAsync(locking ? "tokenEscrowLock" : "tokenEscrowRelease", new {
                privateKey = signer.PrivateKey, fromWalletAddress = signer.Address, depositId, amountUnits = units,
                escrowObjectId = _tokenEscrow.EscrowObjectId });
            var hash = reply.GetProperty("transactionHash").GetString();
            try
            {
                var returned = reply.GetProperty("amountUnits").GetString();
                if (string.IsNullOrWhiteSpace(hash) || reply.GetProperty("depositId").GetString() != depositId
                    || !SameSuiAddress(reply.GetProperty("owner").GetString(), signer.Address)
                    || !SameSuiAddress(reply.GetProperty("escrowObjectId").GetString(), _tokenEscrow.EscrowObjectId)
                    || reply.GetProperty("released").GetBoolean() != !locking
                    || !ulong.TryParse(returned, NumberStyles.None, CultureInfo.InvariantCulture, out var exact) || exact == 0
                    || returned != exact.ToString(CultureInfo.InvariantCulture) || locking && returned != units)
                    throw new InvalidDataException("SDK reply differs from exact original owned token deposit.");
            }
            catch (Exception ex)
            {
                throw new SuiNativeMutationReconciliationException("Escrow reply is inconsistent; reconcile original transaction before retry.", hash, true, ex);
            }
            result.Result = new SuiIdBoundEscrowResponse(id, hash);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }
}

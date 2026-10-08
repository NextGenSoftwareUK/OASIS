using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using static NextGenSoftware.Utilities.KeyHelper;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

public partial class SuiOASIS
{
    private async Task<OASISResult<ITransactionResponse>> SendSuiAsync(string fromWalletAddress, string toWalletAddress,
        decimal amount, string privateKey, string memoText = "")
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (string.IsNullOrWhiteSpace(fromWalletAddress) || string.IsNullOrWhiteSpace(toWalletAddress)
                || string.IsNullOrWhiteSpace(privateKey)) throw new ArgumentException("Sender, recipient and signing key are required.");
            if (!string.IsNullOrEmpty(memoText))
                throw new ArgumentException("Native SUI transfers have no memo field; omit memoText rather than silently dropping it.");
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Positive SUI amount is required.");
            var units = checked(amount * 1000000000m);
            if (units != decimal.Truncate(units) || units > ulong.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must fit u64 MIST without precision loss.");
            var committed = await InvokeSdkAsync("transfer", new
            {
                fromWalletAddress, toWalletAddress, privateKey,
                amountUnits = units.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
            });
            var hash = committed.GetProperty("transactionHash").GetString();
            if (string.IsNullOrWhiteSpace(hash)) throw new InvalidOperationException("Sui returned no committed transaction digest.");
            result.Result = new TransactionResponse { TransactionResult = hash };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    private Task<JsonElement> InvokeSdkAsync(string operation, object args = null, CancellationToken cancellationToken = default)
        => ((SuiStorageBackend)Backend).Client.InvokeAsync(operation, args, cancellationToken);

    public async Task<OASISResult<IKeyPairAndWallet>> RestoreKeyPairAsync(string privateKey)
    {
        var result = new OASISResult<IKeyPairAndWallet>();
        try
        {
            var key = await InvokeSdkAsync("restoreKey", new { privateKey });
            result.Result = new KeyPairAndWallet
            {
                PrivateKey = key.GetProperty("privateKey").GetString(), PublicKey = key.GetProperty("publicKey").GetString(),
                WalletAddressLegacy = key.GetProperty("address").GetString()
            };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }
}

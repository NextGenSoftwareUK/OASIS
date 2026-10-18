using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    /// <summary>Independently verifies original signed bytes and decoded native call. No signing, submission or fee permission.</summary>
    public Task<OASISResult<JsonElement>> VerifyNativeTokenTransferPreparationAsync(JsonElement preparation)
        => VerifyNativeTokenTransferPreparationAsync(preparation, CancellationToken.None);

    public Task<OASISResult<JsonElement>> VerifyNativeTokenTransferPreparationAsync(JsonElement preparation, CancellationToken cancellationToken)
        => VerifyOriginalPreparationAsync(preparation, false, cancellationToken);

    private async Task<OASISResult<JsonElement>> VerifyOriginalPreparationAsync(JsonElement preparation, bool asset, CancellationToken cancellationToken)
    {
        var result = new OASISResult<JsonElement>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_isActivated) throw new InvalidOperationException("Activate the verified network before checking original preparation.");
            var original = preparation.Clone();
            var identity = await InvokePolkadotSdkAsync(new { operation = asset ? "verifyAssetPreparation" : "verifyNativePreparation", endpoint = _rpcEndpoint,
                genesisHash = ReportedGenesisHash, preparation = original }, cancellationToken);
            if (identity.ValueKind != JsonValueKind.Object || !identity.TryGetProperty("verified", out var verified)
                || verified.ValueKind != JsonValueKind.True
                || identity.GetProperty("transactionHash").GetString() != original.GetProperty("transactionHash").GetString()
                || identity.GetProperty("sender").GetString() != original.GetProperty("sender").GetString()
                || identity.GetProperty("chainId").GetString() != ReportedGenesisHash)
                throw new InvalidDataException("SDK verification did not bind the exact original transaction, sender and configured chain.");
            result.Result = identity;
            result.Message = "Original signed operation verified; no submission or fee authorization.";
        }
        catch (Exception failure) { OASISErrorHandling.HandleError(ref result, failure.Message, failure); }
        return result;
    }

    /// <summary>Creates verified signed SCALE bytes and a runtime fee estimate. NEVER broadcasts,
    /// reserves funds or claims finality. Keep signed bytes in the durable execution owner before any later submission.</summary>
    public Task<OASISResult<JsonElement>> PrepareNativeTokenTransferAsync(ISendWeb3TokenRequest request)
        => PrepareNativeTokenTransferAsync(request, CancellationToken.None);

    public async Task<OASISResult<JsonElement>> PrepareNativeTokenTransferAsync(ISendWeb3TokenRequest request, CancellationToken cancellationToken)
    {
        var result = new OASISResult<JsonElement>();
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            if (!_isActivated) throw new InvalidOperationException("Activate and verify the Polkadot network before preparing a transfer.");
            // Capture the whole signing intent before the first await. No configured service-key fallback.
            var intent = new { operation = "prepareTransfer", endpoint = _rpcEndpoint, genesisHash = ReportedGenesisHash,
                sender = request.FromWalletAddress, recipient = request.ToWalletAddress,
                amount = request.Amount.ToString(CultureInfo.InvariantCulture), tokenAddress = request.FromTokenAddress,
                tokenId = request.Web3TokenId == Guid.Empty ? null : request.Web3TokenId.ToString("D"),
                memo = request.MemoText, publicKey = request.OwnerPublicKey,
                privateKey = request.OwnerPrivateKey, mnemonic = request.OwnerSeedPhrase };
            result.Result = await InvokePolkadotSdkAsync(intent, cancellationToken);
            result.Message = "Signed native transfer prepared and fee estimated; not submitted.";
        }
        catch (Exception failure) { OASISErrorHandling.HandleError(ref result, failure.Message, failure); }
        return result;
    }
}

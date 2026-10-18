using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    /// <summary>Creates verified signed SCALE bytes and a runtime fee estimate. NEVER broadcasts,
    /// reserves funds or claims finality. Keep signed bytes in the durable execution owner before any later submission.</summary>
    public async Task<OASISResult<JsonElement>> PrepareNativeTokenTransferAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<JsonElement>();
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!_isActivated) throw new InvalidOperationException("Activate and verify the Polkadot network before preparing a transfer.");
            // Capture the whole signing intent before the first await. No configured service-key fallback.
            var intent = new { operation = "prepareTransfer", endpoint = _rpcEndpoint, genesisHash = ReportedGenesisHash,
                sender = request.FromWalletAddress, recipient = request.ToWalletAddress,
                amount = request.Amount.ToString(CultureInfo.InvariantCulture), tokenAddress = request.FromTokenAddress,
                tokenId = request.Web3TokenId == Guid.Empty ? null : request.Web3TokenId.ToString("D"),
                memo = request.MemoText, publicKey = request.OwnerPublicKey,
                privateKey = request.OwnerPrivateKey, mnemonic = request.OwnerSeedPhrase };
            result.Result = await InvokePolkadotSdkAsync(intent);
            result.Message = "Signed native transfer prepared and fee estimated; not submitted.";
        }
        catch (Exception failure) { OASISErrorHandling.HandleError(ref result, failure.Message, failure); }
        return result;
    }
}

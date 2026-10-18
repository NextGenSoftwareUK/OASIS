using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    /// <summary>Reads an exact finalized block's dispatch and native fee events. Does not broadcast,
    /// scan an incomplete history, infer asset-denominated fees, or claim a failed dispatch succeeded.</summary>
    public async Task<OASISResult<JsonElement>> ReadFinalizedNativeReceiptAsync(string blockHash,
        string transactionHash, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<JsonElement>();
        try
        {
            if (!_isActivated) throw new InvalidOperationException("Activate the verified network before reading a receipt.");
            var receipt = await InvokePolkadotSdkAsync(new { operation = "finalizedReceipt", endpoint = _rpcEndpoint,
                genesisHash = ReportedGenesisHash, blockHash, transactionHash }, cancellationToken);
            if (!receipt.GetProperty("finalized").GetBoolean()
                || !string.Equals(receipt.GetProperty("blockHash").GetString(), blockHash, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(receipt.GetProperty("transactionHash").GetString(), transactionHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("SDK receipt does not identify the requested finalized transaction.");
            result.Result = receipt;
            result.Message = "Finalized dispatch and native fee evidence read; inspect dispatchSucceeded for the execution outcome.";
        }
        catch (Exception failure) { OASISErrorHandling.HandleError(ref result, failure.Message, failure); }
        return result;
    }
}

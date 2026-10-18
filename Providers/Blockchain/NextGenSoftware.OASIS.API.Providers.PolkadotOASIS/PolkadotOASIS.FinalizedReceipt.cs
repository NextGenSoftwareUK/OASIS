using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    /// <summary>Read-only bounded recovery of the exact signed original. A missing result is unknown,
    /// never rejection/expiry/replay permission. Retain nextBlockNumber and resume without broadcasting.</summary>
    public async Task<OASISResult<JsonElement>> ReadOriginalNativeOutcomeAsync(JsonElement preparation,
        string fromBlockNumber, int maximumBlocks = 16, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<JsonElement>();
        try
        {
            if (!_isActivated) throw new InvalidOperationException("Activate the verified network before original outcome recovery.");
            var original = preparation.Clone();
            var response = await InvokePolkadotSdkAsync(new { operation = "originalOutcome", endpoint = _rpcEndpoint,
                genesisHash = ReportedGenesisHash, preparation = original, fromBlockNumber, maximumBlocks }, cancellationToken);
            var found = response.GetProperty("found").GetBoolean();
            var observed = found ? response.GetProperty("receipt") : response;
            if (!string.Equals(observed.GetProperty("transactionHash").GetString(), original.GetProperty("transactionHash").GetString(), StringComparison.Ordinal)
                || !response.GetProperty("reconciliationRequired").GetBoolean()
                || observed.GetProperty("finalized").GetBoolean() != found)
                throw new InvalidDataException("SDK outcome does not bind the same original transaction and finality state.");
            result.Result = response;
            result.Message = found ? "Original finalized dispatch/fee evidence found; billing settlement remains separate."
                : "Original not found in the specified finalized range; outcome remains unknown. Resume the returned cursor, never resubmit.";
        }
        catch (Exception failure) { OASISErrorHandling.HandleError(ref result, failure.Message, failure); }
        return result;
    }

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

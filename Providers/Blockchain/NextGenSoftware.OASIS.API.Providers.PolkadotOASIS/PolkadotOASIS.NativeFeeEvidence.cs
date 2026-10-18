using System;
using System.IO;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS : IPreparedNativeFeeEvidenceReader
{
    string IPreparedNativeFeeEvidenceReader.NativeFeeProvider => "PolkadotOASIS";
    string IPreparedNativeFeeEvidenceReader.NativeFeeChainId => ReportedGenesisHash;
    NativeFeeCoverage IPreparedNativeFeeEvidenceReader.DeclaredFeeCoverage => NativeFeeCoverage.NetFinal;

    /// <summary>Independently verifies the retained native-transfer original and final FRAME native fee event.
    /// No price fetching, settlement, replay or inferred zero fee. Runtime/network fee-model review remains host policy.</summary>
    public async Task<OASISResult<PreparedNativeFeeEvidence>> ReadPreparedNativeFeeEvidenceAsync(
        UsageProviderAttempt original, CancellationToken token = default)
    {
        var result = new OASISResult<PreparedNativeFeeEvidence>();
        try
        {
            token.ThrowIfCancellationRequested();
            if (!_isActivated) throw new InvalidOperationException("Activate the pinned network before original native fee lookup.");
            var snapshot = NativePreparedAttemptSnapshot.Capture(original, "PolkadotOASIS", ReportedGenesisHash);
            var preparation = new { transactionHash = snapshot.ProviderRequestId, chainId = snapshot.ChainId,
                sender = snapshot.Sender, signedTransactionBase64 = snapshot.SignedTransactionBase64,
                signDocBase64 = snapshot.SignDocBase64, signature = snapshot.Signature };
            var receipt = await InvokePolkadotSdkAsync(new { operation = "originalNativeFee", endpoint = _rpcEndpoint,
                genesisHash = ReportedGenesisHash, preparation }, token);
            if (!receipt.GetProperty("finalized").GetBoolean()
                || receipt.GetProperty("transactionHash").GetString() != snapshot.ProviderRequestId
                || receipt.GetProperty("feeCoverage").GetString() != "finalized-native-transaction-payment-event-tip-included")
                throw new InvalidDataException("Native fee receipt does not bind the exact finalized original and coverage.");
            var units = receipt.GetProperty("actualFeeUnits").GetString();
            if (string.IsNullOrEmpty(units) || (units.Length > 1 && units[0] == '0')
                || !BigInteger.TryParse(units, NumberStyles.None, CultureInfo.InvariantCulture, out var amount)
                || amount < 0 || amount >= (BigInteger.One << 128))
                throw new InvalidDataException("Native fee receipt units must be canonical unsigned u128.");
            // SDK independently binds canonical AccountId32 payer to the original signer before replying.
            result.Result = new PreparedNativeFeeEvidence("PolkadotOASIS", snapshot.ChainId, snapshot.ProviderRequestId,
                receipt.GetProperty("payer").GetString(), receipt.GetProperty("dispatchSucceeded").GetBoolean(),
                NativeFeeCoverage.NetFinal, new[] { new NativeFeeComponent("native-base-unit", units) },
                "verified-frame-native-fee-paid-tip-included");
        }
        catch (Exception failure) { OASISErrorHandling.HandleError(ref result, failure.Message, failure); }
        return result;
    }
}

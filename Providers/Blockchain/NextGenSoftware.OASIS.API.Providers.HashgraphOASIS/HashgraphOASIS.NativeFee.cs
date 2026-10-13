using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS : Core.Interfaces.IOASISNativeFeeReceiptProvider
    {
        // Mirror reads neither activate charge-bearing storage nor require signing keys.
        public async Task<OASISResult<JsonElement>> GetNativeFeeReceiptAsync(string transactionId,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<JsonElement>();
            try
            {
                var id = NativeFeeMirrorId(transactionId);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(20));
                using var response = await _httpClient.GetAsync("transactions/" + Uri.EscapeDataString(id), timeout.Token);
                response.EnsureSuccessStatusCode();
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
                result.Result = MapNativeFeeReceipt(document.RootElement, transactionId, _hederaNetwork);
                result.Message = "Indexed native transaction fee; not a quote, fiat valuation or subscription settlement.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Hedera native fee receipt read failed: " + ex.Message, ex);
            }
            return result;
        }

        private static string NativeFeeMirrorId(string id)
        {
            if (!Regex.IsMatch(id ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+@[0-9]+\.[0-9]{9}\z"))
                throw new ArgumentException("An unscheduled native Hedera SDK transaction ID is required.");
            return id.Replace('@', '-').Remove(id.LastIndexOf('.'), 1).Insert(id.LastIndexOf('.'), "-");
        }

        private static JsonElement MapNativeFeeReceipt(JsonElement root, string transactionId, string network)
        {
            var id = NativeFeeMirrorId(transactionId);
            var rows = root.GetProperty("transactions");
            if (rows.GetArrayLength() == 0)
                throw new InvalidOperationException("No indexed fee records are available.");
            var timestamps = new HashSet<string>(StringComparer.Ordinal);
            var records = new List<JsonElement>();
            long total = 0;
            JsonElement? primary = null;
            foreach (var tx in rows.EnumerateArray())
            {
                if (tx.GetProperty("transaction_id").GetString() != id || tx.GetProperty("nonce").GetInt64() != 0
                    || tx.GetProperty("scheduled").GetBoolean()
                    || tx.GetProperty("parent_consensus_timestamp").ValueKind != JsonValueKind.Null)
                    throw new InvalidOperationException("Receipt identity is not the requested unscheduled parent transaction.");
                // Ethereum/batch/scheduled execution can have different or multiple fee payers.
                var operation = tx.GetProperty("name").GetString();
                if (operation is not ("CRYPTOTRANSFER" or "CONTRACTCREATEINSTANCE" or "CONTRACTCALL"
                    or "FILECREATE" or "FILEAPPEND" or "FILEUPDATE" or "FILEDELETE"
                    or "TOKENCREATION" or "TOKENMINT" or "TOKENBURN" or "TOKENDELETION" or "TOKENUPDATENFTS"
                    or "TOKENASSOCIATE" or "TOKENUPDATE" or "CRYPTOAPPROVEALLOWANCE"))
                    throw new InvalidOperationException("Native fee payer attribution is not supported for this operation.");
                var nativeStatus = tx.GetProperty("result").GetString();
                var status = nativeStatus == "DUPLICATE_TRANSACTION" ? BridgeTransactionStatus.Failed : MapReceiptStatus(nativeStatus);
                if (status is BridgeTransactionStatus.Pending or BridgeTransactionStatus.NotFound)
                    throw new InvalidOperationException("Receipt does not establish a terminal consensus outcome.");
                var timestamp = tx.GetProperty("consensus_timestamp").GetString();
                if (!Regex.IsMatch(timestamp ?? "", @"\A[0-9]+\.[0-9]{9}\z")
                    || decimal.Parse(timestamp, CultureInfo.InvariantCulture) <= 0 || !timestamps.Add(timestamp))
                    throw new InvalidOperationException("Invalid consensus timestamp.");
                var fee = tx.GetProperty("charged_tx_fee").GetInt64();
                if (fee < 0) throw new InvalidOperationException("Charged fee cannot be negative.");
                total = checked(total + fee);
                if (nativeStatus != "DUPLICATE_TRANSACTION")
                {
                    if (primary.HasValue) throw new InvalidOperationException("Multiple nonduplicate parent outcomes cannot establish one mint result.");
                    primary = tx;
                }
                records.Add(JsonSerializer.SerializeToElement(new
                {
                    nativeStatus,
                    consensusTimestamp = timestamp,
                    chargedUnits = fee.ToString(CultureInfo.InvariantCulture)
                }));
            }
            if (!primary.HasValue) throw new InvalidOperationException("Duplicate records alone do not establish the original outcome.");
            var original = primary.Value;
            return JsonSerializer.SerializeToElement(new
            {
                transactionId,
                network,
                payer = transactionId.Substring(0, transactionId.IndexOf('@')),
                currency = "HBAR",
                unit = "tinybar",
                netUnits = total.ToString(CultureInfo.InvariantCulture),
                success = original.GetProperty("result").GetString() == "SUCCESS",
                nativeStatus = original.GetProperty("result").GetString(),
                consensusTimestamp = original.GetProperty("consensus_timestamp").GetString(),
                records,
                basis = "mirror-charged-tx-fee",
                coverage = "indexed-native-parent-records"
            });
        }
    }
}

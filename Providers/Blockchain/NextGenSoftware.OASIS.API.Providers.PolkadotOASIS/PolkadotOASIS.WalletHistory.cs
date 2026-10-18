using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    private async Task<OASISResult<IList<IWalletTransaction>>> ReadWalletHistoryAsync(IGetWeb3TransactionsRequest request)
    {
        var result = new OASISResult<IList<IWalletTransaction>>();
        try {
            ArgumentNullException.ThrowIfNull(request);
            if (!_isActivated) throw new InvalidOperationException("Activate the verified network before wallet history.");
            var address = request.WalletAddress;
            var first = request.FromBlock ?? 0;
            var last = request.ToBlock;
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("WalletAddress is required.");
            if (last.HasValue && (last.Value < first || last.Value - first >= 128))
                throw new ArgumentException("Choose an inclusive finalized history range of at most128blocks.");
            var response = await InvokePolkadotSdkAsync(new { operation = "walletHistory", endpoint = _rpcEndpoint,
                genesisHash = ReportedGenesisHash, address, fromBlock = first.ToString(CultureInfo.InvariantCulture),
                toBlock = last?.ToString(CultureInfo.InvariantCulture) }).ConfigureAwait(false);
            var from = ulong.Parse(response.GetProperty("fromBlock").GetString(), CultureInfo.InvariantCulture);
            var to = ulong.Parse(response.GetProperty("toBlock").GetString(), CultureInfo.InvariantCulture);
            var ceiling = ulong.Parse(response.GetProperty("finalizedSnapshotBlock").GetString(), CultureInfo.InvariantCulture);
            if (!response.GetProperty("finalized").GetBoolean() || response.GetProperty("genesisHash").GetString() != ReportedGenesisHash
                || from != first || to < from || to - from >= 128 || to > ceiling || (last.HasValue && to != last.Value)
                || (!last.HasValue && to != ceiling))
                throw new InvalidDataException("History response does not bind the requested finalized range/network.");
            var wallet = response.GetProperty("address").GetString(); // SDK canonicalizes SS58/AccountId32 before reading.
            if (!IsGenesisHash(wallet)) throw new InvalidDataException("Canonical history AccountId32 is missing.");
            var rows = new List<IWalletTransaction>();
            foreach (var item in response.GetProperty("rows").EnumerateArray()) {
                var transaction = item.GetProperty("transactionHash").GetString();
                var block = item.GetProperty("blockHash").GetString();
                var number = ulong.Parse(item.GetProperty("blockNumber").GetString(), CultureInfo.InvariantCulture);
                var source = item.GetProperty("from").GetString();
                var target = item.GetProperty("to").GetString();
                var signer = item.GetProperty("signer").GetString();
                var eventIndex = item.GetProperty("eventIndex");
                if (!IsGenesisHash(transaction) || !IsGenesisHash(block) || number < from || number > to
                    || (source != wallet && target != wallet && signer != wallet)
                    || item.GetProperty("extrinsicIndex").GetInt32() < 0
                    || (eventIndex.ValueKind != JsonValueKind.Null && eventIndex.GetInt32() < 0))
                    throw new InvalidDataException("History row identity/range/wallet is invalid.");
                var raw = item.GetProperty("rawAmount").GetString();
                var kind = item.GetProperty("amountKind").GetString();
                var applied = kind == "applied-event";
                if (kind is not ("applied-event" or "not-interpreted") || applied != (raw != null)
                    || applied != (eventIndex.ValueKind != JsonValueKind.Null))
                    throw new InvalidDataException("History amount must identify actual event evidence or remain uninterpreted.");
                rows.Add(new WalletTransaction {
                    TransactionId = CreateDeterministicGuid($"{ReportedGenesisHash}:{block}:{transaction}:{(eventIndex.ValueKind == JsonValueKind.Null ? "signed" : eventIndex.GetInt32().ToString(CultureInfo.InvariantCulture))}"),
                    NativeTransactionId = transaction, FromWalletAddress = source, ToWalletAddress = target,
                    RawAmount = raw, AmountUnit = item.GetProperty("amountUnit").GetString(), Token = item.GetProperty("token").GetString(),
                    AmountEvidence = kind, NativeEvent = item.GetProperty("effect").GetString(), NativeBlockHash = block,
                    NativeBlockNumber = number, NativeExtrinsicIndex = item.GetProperty("extrinsicIndex").GetInt32(),
                    NativeEventIndex = eventIndex.ValueKind == JsonValueKind.Null ? null : eventIndex.GetInt32(),
                    Amount = applied ? UnitsToDisplay(raw, item.GetProperty("decimals").GetInt32()) : 0,
                    NativeOperation = item.GetProperty("operation").GetString(),
                    NativeExecutionResult = item.GetProperty("dispatchSucceeded").GetBoolean() ? "succeeded" : "failed",
                    NativeError = item.GetProperty("dispatchError").GetString(),
                    ProviderType = Core.Enums.ProviderType.PolkadotOASIS,
                    CreatedDate = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(item.GetProperty("timestampMilliseconds").GetString(), NumberStyles.None, CultureInfo.InvariantCulture)).UtcDateTime,
                    TransactionType = source == wallet ? Core.Enums.TransactionType.Debit : Core.Enums.TransactionType.Credit,
                    TransactionCategory = Core.Enums.TransactionCategory.Other,
                    Description = applied ? $"Finalized {item.GetProperty("effect").GetString()} event; top-level dispatch status, fees excluded."
                        : "Finalized signed extrinsic; monetary effects not interpreted (display0 is not a verified zero balance change or fee)."
                });
            }
            result.Result = rows; result.IsLoaded = true; result.LoadedCount = rows.Count;
            result.Message = $"Finalized signed extrinsics and native/Assets transfer, mint and burn events in inclusive blocks {from}-{to}; other pallet effects/memos/fees are not interpreted. Raw event units are exact; double display amounts are approximate.";
        }
        catch (Exception failure) { OASISErrorHandling.HandleError(ref result, "Polkadot wallet history failed: " + failure.Message, failure); }
        return result;
    }
}

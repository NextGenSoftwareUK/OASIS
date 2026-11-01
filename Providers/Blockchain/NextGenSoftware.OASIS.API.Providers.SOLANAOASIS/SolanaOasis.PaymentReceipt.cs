using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.Common;
using Solnet.Rpc.Models;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS : IOASISPaymentReceiptProvider
{
    public async Task<OASISResult<NativePaymentReceipt>> GetPaymentReceiptAsync(string transactionId, CancellationToken token = default)
    {
        var result = new OASISResult<NativePaymentReceipt>();
        try
        {
            RequireNativeBase58(transactionId, 64);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            var observed = await Infrastructure.SolanaTransactionReader.ReadFeeAsync(_rpcEndpoint, transactionId, _nativeFeeReadHandler, timeout.Token);
            var identity = MapSolanaNativeFeeReceipt(transactionId, observed.Genesis, observed.Transaction);
            if (!identity.GetProperty("success").GetBoolean()) throw new InvalidOperationException("Finalized transaction execution failed.");
            var tx = Infrastructure.SolanaTransactionReader.Map(transactionId, observed.Transaction);
            return new OASISResult<NativePaymentReceipt> { Result = MapPaymentReceipt(transactionId, observed.Genesis, tx, observed.Transaction.GetProperty("blockTime").GetInt64()), IsLoaded = true };
        }
        catch (Exception cause) { OASISErrorHandling.HandleError(ref result, cause.Message, cause); }
        return result;
    }

    internal static NativePaymentReceipt MapPaymentReceipt(string signature, string genesis, TransactionMetaSlotInfo tx, long blockTime)
    {
        RequireNativeBase58(signature, 64); RequireNativeBase58(genesis, 32);
        if (tx?.Meta == null || tx.Meta.Error != null || tx.Slot == 0) throw new InvalidOperationException("A successful finalized native transaction is required.");
        var created = DateTimeOffset.FromUnixTimeSeconds(blockTime).UtcDateTime;
        if (created.Year < 2020 || created > DateTime.UtcNow.AddMinutes(5)) throw new InvalidOperationException("Native block time is invalid.");
        var keys = tx.Transaction.Message.AccountKeys;
        var instructions = NativeHistoryInstructions(tx).Select(x => x.Instruction).ToList();
        var memos = instructions.Where(i => keys[i.ProgramIdIndex] == "MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr")
            .Select(i => new UTF8Encoding(false, true).GetString(Encoders.Base58.DecodeData(i.Data))).ToList();
        if (memos.Count != 1) throw new InvalidOperationException("Exactly one unambiguous native memo is required for payment evidence.");
        var status = new SignatureStatusInfo { Signature = signature, Slot = tx.Slot };
        var summary = new WalletTransaction { CreatedDate = created, NativeExecutionResult = "succeeded" };
        var transfers = keys.SelectMany(wallet => MapNativeHistoryEffects(status, wallet, summary, tx).Where(t => t.FromWalletAddress == wallet))
            .Where(t => t.RawAmount != null && t.AmountUnit == "lamports")
            .Select(t => new NativePaymentTransfer(t.FromWalletAddress, t.ToWalletAddress, t.RawAmount, "lamports", "SOL"))
            .ToList();
        return new NativePaymentReceipt { ProviderType = Core.Enums.ProviderType.SolanaOASIS, ChainId = genesis, TransactionId = signature,
            Finalized = true, Succeeded = true, CreatedUtc = created, Memo = memos[0], Transfers = transfers };
    }
}

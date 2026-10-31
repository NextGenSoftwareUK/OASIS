using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>SDK-independent EVM receipt observation. Adapters supply real receipt/block RPCs.
/// No submission, polling, latest-tag fallback or chain-specific settlement claim.</summary>
public static class EvmFinalizedStatus
{
    public sealed record Receipt(string TransactionHash,string BlockHash,BigInteger? BlockNumber,BigInteger? ExecutionStatus);
    public sealed record Block(string Hash,BigInteger? Number);

    public static async Task<BridgeTransactionStatus> ObserveAsync(string transactionHash,
        Func<CancellationToken,Task<Receipt>> receipt,
        Func<CancellationToken,Task<Block>> finalized,
        Func<BigInteger,CancellationToken,Task<Block>> canonical,
        CancellationToken cancellationToken=default)
    {
        ValidateHash(transactionHash);ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(finalized);ArgumentNullException.ThrowIfNull(canonical);
        cancellationToken.ThrowIfCancellationRequested();
        var observed=await receipt(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if(observed==null)return BridgeTransactionStatus.Pending;
        if(!string.Equals(observed.TransactionHash,transactionHash,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Receipt transaction identity mismatch.");
        ValidateHash(observed.BlockHash);
        if(observed.BlockNumber==null||observed.BlockNumber<0||observed.ExecutionStatus==null||
            (observed.ExecutionStatus!=BigInteger.Zero&&observed.ExecutionStatus!=BigInteger.One))
            throw new InvalidDataException("Receipt must contain a nonnegative block and explicit execution status0or1.");
        var checkpoint=await finalized(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if(checkpoint==null||checkpoint.Number==null||checkpoint.Number<0)throw new InvalidDataException("Explicit finalized block is required.");
        ValidateHash(checkpoint.Hash);
        if(observed.BlockNumber>checkpoint.Number)return BridgeTransactionStatus.Pending;
        var block=await canonical(observed.BlockNumber.Value,cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if(block==null||block.Number!=observed.BlockNumber)throw new InvalidDataException("Canonical receipt block is missing or has the wrong height.");
        ValidateHash(block.Hash);
        if(!string.Equals(block.Hash,observed.BlockHash,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Receipt block is not canonical; outcome requires reconciliation.");
        if(block.Number==checkpoint.Number&&!string.Equals(block.Hash,checkpoint.Hash,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Canonical and finalized block identities disagree.");
        cancellationToken.ThrowIfCancellationRequested();
        return observed.ExecutionStatus==BigInteger.One?BridgeTransactionStatus.Completed:BridgeTransactionStatus.Failed;
    }

    public static void ValidateHash(string value)
    {
        if(value==null||value.Length!=66||!value.StartsWith("0x",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("A32byte hexadecimal EVM hash is required.");
        for(var i=2;i<value.Length;i++)if(!Uri.IsHexDigit(value[i]))throw new ArgumentException("Invalid EVM hash encoding.");
    }
}

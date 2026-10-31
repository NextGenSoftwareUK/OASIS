using System;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>Original signed submission evidence, never a replacement transaction or inferred ledger outcome.</summary>
public sealed class TronBroadcastOutcomeUnknownException : InvalidOperationException,
    Core.Interfaces.INativeMutationReconciliationRequired
{
    public string TransactionId { get; }
    public string OriginalTransactionId => TransactionId;
    public string ChainGenesisId { get; }
    public long TransactionTimestampMilliseconds { get; }
    public long TransactionExpirationMilliseconds { get; }
    // Untrusted node diagnostics, not ledger proof. Base64 avoids injecting control text into logs.
    public int? BroadcastResponseCode { get; }
    public string? BroadcastResponseMessageBase64 { get; }
    public bool BroadcastResponseMessageTruncated { get; }
    public bool HasOriginalValidityWindow => TransactionTimestampMilliseconds > 0
        && TransactionExpirationMilliseconds > TransactionTimestampMilliseconds;

    public TronBroadcastOutcomeUnknownException(string transactionId, string chainGenesisId,
        long timestampMilliseconds, long expirationMilliseconds, Exception innerException)
        : base($"TRON broadcast outcome for {transactionId} is unknown. Track this original transaction before any retry.", innerException)
    {
        if (!Regex.IsMatch(transactionId ?? "", @"\A[0-9a-f]{64}\z")
            || !Regex.IsMatch(chainGenesisId ?? "", @"\A[0-9a-fA-F]{64}\z"))
            throw new ArgumentException("Unknown broadcast evidence requires exact original transaction and chain identity.");
        TransactionId = transactionId;
        ChainGenesisId = chainGenesisId.ToLowerInvariant();
        TransactionTimestampMilliseconds = timestampMilliseconds;
        TransactionExpirationMilliseconds = expirationMilliseconds;
    }

    public TronBroadcastOutcomeUnknownException(string transactionId, string chainGenesisId,
        long timestampMilliseconds, long expirationMilliseconds, Exception innerException,
        int responseCode, ReadOnlySpan<byte> responseMessage)
        : this(transactionId,chainGenesisId,timestampMilliseconds,expirationMilliseconds,innerException)
    {
        BroadcastResponseCode=responseCode;
        BroadcastResponseMessageTruncated=responseMessage.Length>1024;
        BroadcastResponseMessageBase64=Convert.ToBase64String(responseMessage[..Math.Min(responseMessage.Length,1024)]);
    }
}

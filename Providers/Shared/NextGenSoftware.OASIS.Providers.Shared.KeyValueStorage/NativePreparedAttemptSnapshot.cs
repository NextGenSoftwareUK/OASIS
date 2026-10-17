using System;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Immutable identity captured before asynchronous verification/recovery. This validates the
/// persisted envelope, not its signature, native outcome, fee, or authority to spend.</summary>
public sealed record NativePreparedAttemptSnapshot(string ProviderRequestId,string ChainId,string Sender,
    string SignedTransactionBase64,string Signature,string SignDocBase64)
{
    public static NativePreparedAttemptSnapshot Capture(UsageProviderAttempt attempt,string provider,string chainId)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(chainId);
        var original=attempt.NativePreparation;
        var requestId=attempt.ProviderRequestId;
        if(attempt.Provider!=provider||original==null||string.IsNullOrWhiteSpace(requestId)||requestId.Length>256)
            throw new InvalidDataException("Prepared attempt must bind this provider and a bounded original transaction identity.");
        var snapshot=new NativePreparedAttemptSnapshot(requestId,original.ChainId,original.Sender,
            original.SignedTransactionBase64,original.Signature,original.SignDocBase64);
        if(snapshot.ChainId!=chainId)
            throw new InvalidDataException("Prepared attempt must bind the configured chain or network.");
        new UsageNativePreparation {ChainId=snapshot.ChainId,Sender=snapshot.Sender,
            SignedTransactionBase64=snapshot.SignedTransactionBase64,Signature=snapshot.Signature,
            SignDocBase64=snapshot.SignDocBase64}.ValidateAndMeasure();
        return snapshot;
    }
}

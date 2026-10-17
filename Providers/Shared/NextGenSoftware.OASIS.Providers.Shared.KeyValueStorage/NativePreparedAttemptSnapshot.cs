using System;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Immutable identity captured before asynchronous verification/recovery. This validates the
/// persisted envelope, not its signature, native outcome, fee, or authority to spend.</summary>
public sealed record NativePreparedAttemptSnapshot(string ProviderRequestId,string ChainId,string Sender,
    string SignedTransactionBase64,string Signature,string SignDocBase64,NativeSigningAuthority SigningAuthority)
{
    public NativePreparedAttemptSnapshot(string providerRequestId,string chainId,string sender,
        string signedTransactionBase64,string signature,string signDocBase64)
        :this(providerRequestId,chainId,sender,signedTransactionBase64,signature,signDocBase64,null) { }

    public void Deconstruct(out string providerRequestId,out string chainId,out string sender,
        out string signedTransactionBase64,out string signature,out string signDocBase64)
    {
        providerRequestId=ProviderRequestId;chainId=ChainId;sender=Sender;
        signedTransactionBase64=SignedTransactionBase64;signature=Signature;signDocBase64=SignDocBase64;
    }
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
            original.SignedTransactionBase64,original.Signature,original.SignDocBase64,original.SigningAuthority);
        if(snapshot.ChainId!=chainId)
            throw new InvalidDataException("Prepared attempt must bind the configured chain or network.");
        new UsageNativePreparation {ChainId=snapshot.ChainId,Sender=snapshot.Sender,
            SignedTransactionBase64=snapshot.SignedTransactionBase64,Signature=snapshot.Signature,
            SignDocBase64=snapshot.SignDocBase64,SigningAuthority=snapshot.SigningAuthority}.ValidateAndMeasure();
        return snapshot;
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using Google.Protobuf;
using TronNet.Protocol;
using Nethereum.Signer;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

public static class TronPreparationVerifier
{
    public static void VerifyOriginal(string signedTransactionBase64,string expectedTransactionId,byte[] expectedSender)
        => VerifyOriginal(signedTransactionBase64,expectedTransactionId,expectedSender,long.MaxValue);
    public static void VerifyOriginal(string signedTransactionBase64,string expectedTransactionId,byte[] expectedSender,long maximumFeeLimitSun)
        => VerifyCore(signedTransactionBase64,expectedTransactionId,expectedSender,maximumFeeLimitSun,null);
    public static void VerifyWithAccountAuthority(string signedTransactionBase64,string expectedTransactionId,byte[] expectedOwner,long maximumFeeLimitSun,Account verifiedAccount)
    {
        ArgumentNullException.ThrowIfNull(verifiedAccount);
        VerifyCore(signedTransactionBase64,expectedTransactionId,expectedOwner,maximumFeeLimitSun,verifiedAccount);
    }
    /// <summary>Read-only verification of an already prepared original. Current execution limits must
    /// not retroactively prevent receipt recovery; this does not authorize a new submission.</summary>
    public static void VerifyHistoricalWithAccountAuthority(string signedTransactionBase64,string expectedTransactionId,byte[] expectedOwner,Account verifiedOriginalAccount)
        =>VerifyWithAccountAuthority(signedTransactionBase64,expectedTransactionId,expectedOwner,long.MaxValue,verifiedOriginalAccount);
    private static void VerifyCore(string signedTransactionBase64,string expectedTransactionId,byte[] expectedSender,long maximumFeeLimitSun,Account verifiedAccount)
    {
        if(maximumFeeLimitSun<0)throw new ArgumentOutOfRangeException(nameof(maximumFeeLimitSun));
        if(string.IsNullOrEmpty(signedTransactionBase64)||signedTransactionBase64.Length>1048576)
            throw new InvalidDataException("Bounded original TRON signed bytes required.");
        var bytes=Convert.FromBase64String(signedTransactionBase64);
        if(Convert.ToBase64String(bytes)!=signedTransactionBase64)throw new InvalidDataException("Canonical TRON base64 required.");
        var transaction=Transaction.Parser.ParseFrom(bytes);
        if(transaction.RawData==null||transaction.Signature.Count==0||(verifiedAccount==null&&transaction.Signature.Count!=1))
            throw new InvalidDataException("One original recoverable TRON signature required.");
        if(transaction.RawData.Timestamp<=0||transaction.RawData.Expiration<=transaction.RawData.Timestamp||transaction.RawData.FeeLimit<0)
            throw new InvalidDataException("Original TRON validity window and nonnegative fee limit required.");
        if(transaction.RawData.FeeLimit>maximumFeeLimitSun)throw new InvalidDataException("TRON signed fee limit exceeds the authorised ceiling.");
        var digest=SHA256.HashData(transaction.RawData.ToByteArray());
        if(Convert.ToHexString(digest).ToLowerInvariant()!=expectedTransactionId)throw new InvalidDataException("TRON original identity mismatch.");
        if(expectedSender==null||expectedSender.Length!=21||expectedSender[0]!=0x41)throw new ArgumentException("Trusted 21-byte TRON sender required.",nameof(expectedSender));
        if(transaction.RawData.Contract.Count!=1)throw new InvalidDataException("One original TRON operation contract required.");
        var contract=transaction.RawData.Contract[0];
        if(contract.PermissionId!=0&&verifiedAccount==null)throw new InvalidDataException("TRON active permission requires independent on-chain permission authority verification.");
        ByteString owner;
        try{owner=TronNativeTransferClient.OriginalOperationOwner(contract);}
        catch(InvalidOperationException error){throw new InvalidDataException("Prepared TRON operation owner mapping is not supported.",error);}
        if(!CryptographicOperations.FixedTimeEquals(owner.Span,expectedSender))throw new InvalidDataException("TRON operation owner differs from trusted sender.");
        var recoveredSigners=new System.Collections.Generic.List<ByteString>();
        foreach(var encodedSignature in transaction.Signature){
            if(encodedSignature.Length!=65)throw new InvalidDataException("Recoverable TRON signature length required.");
            var signature=encodedSignature.ToByteArray();
            if(signature[64]>1)throw new InvalidDataException("Canonical TRON recovery parity required.");
            var parsed=EthECDSASignatureFactory.FromComponents(signature.AsSpan(0,32).ToArray(),signature.AsSpan(32,32).ToArray());
            var recovered=EthECKey.RecoverFromSignature(parsed,signature[64],digest).GetPublicAddressAsBytes();
            var address=new byte[21];address[0]=0x41;recovered.CopyTo(address,1);recoveredSigners.Add(ByteString.CopyFrom(address));
            if(verifiedAccount==null&&!CryptographicOperations.FixedTimeEquals(expectedSender.AsSpan(1),recovered))throw new InvalidDataException("TRON original signature sender mismatch.");
        }
        if(verifiedAccount!=null)TronPermissionVerifier.Verify(verifiedAccount,ByteString.CopyFrom(expectedSender),contract.PermissionId,(int)contract.Type,recoveredSigners);
    }
}

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
    {
        if(string.IsNullOrEmpty(signedTransactionBase64)||signedTransactionBase64.Length>1048576)
            throw new InvalidDataException("Bounded original TRON signed bytes required.");
        var bytes=Convert.FromBase64String(signedTransactionBase64);
        if(Convert.ToBase64String(bytes)!=signedTransactionBase64)throw new InvalidDataException("Canonical TRON base64 required.");
        var transaction=Transaction.Parser.ParseFrom(bytes);
        if(transaction.RawData==null||transaction.Signature.Count!=1||transaction.Signature[0].Length!=65)
            throw new InvalidDataException("One original recoverable TRON signature required.");
        if(transaction.RawData.Timestamp<=0||transaction.RawData.Expiration<=transaction.RawData.Timestamp||transaction.RawData.FeeLimit<0)
            throw new InvalidDataException("Original TRON validity window and nonnegative fee limit required.");
        var digest=SHA256.HashData(transaction.RawData.ToByteArray());
        if(Convert.ToHexString(digest).ToLowerInvariant()!=expectedTransactionId)throw new InvalidDataException("TRON original identity mismatch.");
        if(expectedSender==null||expectedSender.Length!=21||expectedSender[0]!=0x41)throw new ArgumentException("Trusted 21-byte TRON sender required.",nameof(expectedSender));
        if(transaction.RawData.Contract.Count!=1)throw new InvalidDataException("One original TRON operation contract required.");
        var contract=transaction.RawData.Contract[0];
        var owner=contract.Type switch{
            Transaction.Types.Contract.Types.ContractType.TransferContract=>contract.Parameter.Unpack<TransferContract>().OwnerAddress,
            Transaction.Types.Contract.Types.ContractType.AccountCreateContract=>contract.Parameter.Unpack<AccountCreateContract>().OwnerAddress,
            Transaction.Types.Contract.Types.ContractType.AccountUpdateContract=>contract.Parameter.Unpack<AccountUpdateContract>().OwnerAddress,
            Transaction.Types.Contract.Types.ContractType.TriggerSmartContract=>contract.Parameter.Unpack<TriggerSmartContract>().OwnerAddress,
            Transaction.Types.Contract.Types.ContractType.CreateSmartContract=>contract.Parameter.Unpack<CreateSmartContract>().OwnerAddress,
            _=>throw new InvalidDataException("Prepared TRON operation owner mapping is not supported.")
        };
        if(!CryptographicOperations.FixedTimeEquals(owner.Span,expectedSender))throw new InvalidDataException("TRON operation owner differs from trusted sender.");
        var signature=transaction.Signature[0].ToByteArray();
        if(signature[64]>1)throw new InvalidDataException("Canonical TRON recovery parity required.");
        var parsed=EthECDSASignatureFactory.FromComponents(signature.AsSpan(0,32).ToArray(),signature.AsSpan(32,32).ToArray());
        var recovered=EthECKey.RecoverFromSignature(parsed,signature[64],digest).GetPublicAddressAsBytes();
        if(!CryptographicOperations.FixedTimeEquals(expectedSender.AsSpan(1),recovered))throw new InvalidDataException("TRON original signature sender mismatch.");
    }
}

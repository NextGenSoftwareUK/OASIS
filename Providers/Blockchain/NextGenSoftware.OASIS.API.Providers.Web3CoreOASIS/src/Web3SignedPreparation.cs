using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
using Nethereum.Signer;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

internal static class Web3SignedPreparation
{
    internal static void Verify(byte[] signed,string hash,BigInteger chain,string sender,string contract,byte[] data)
    {
        if(signed==null || signed.Length==0 || chain<=0) throw new InvalidDataException("Original signed EVM bytes and positive chain identity required.");
        var transaction=TransactionFactory.CreateTransaction(signed);
        if(!CryptographicOperations.FixedTimeEquals(transaction.GetRLPEncoded(),signed)
            || !TransactionVerificationAndRecovery.VerifyTransaction(transaction))
            throw new InvalidDataException("EVM preparation is not a canonical valid signed transaction.");
        var actualHash="0x"+Convert.ToHexString(Nethereum.Util.Sha3Keccack.Current.CalculateHash(signed)).ToLowerInvariant();
        if(!string.Equals(hash,actualHash,StringComparison.OrdinalIgnoreCase)
            || !string.Equals(sender,TransactionVerificationAndRecovery.GetSenderAddress(transaction),StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Signed EVM preparation hash or recovered signer differs from expected identity.");
        BigInteger actualChain,value;byte[] target,callData;
        switch(transaction)
        {
            case LegacyTransactionChainId legacy:
                actualChain=new BigInteger(legacy.ChainId,isUnsigned:true,isBigEndian:true);
                value=new BigInteger(legacy.Value,isUnsigned:true,isBigEndian:true);target=legacy.ReceiveAddress;callData=legacy.Data;break;
            case Transaction1559 dynamicFee:
                actualChain=dynamicFee.ChainId;value=dynamicFee.Amount.GetValueOrDefault();
                target=dynamicFee.ReceiverAddress.HexToByteArray();callData=dynamicFee.Data.HexToByteArray();break;
            default: throw new InvalidDataException("Prepared EVM transaction type requires an explicit verified decoder.");
        }
        if(actualChain!=chain || value!=BigInteger.Zero || !CryptographicOperations.FixedTimeEquals(target,contract.HexToByteArray())
            || !CryptographicOperations.FixedTimeEquals(callData,data))
            throw new InvalidDataException("Signed EVM preparation chain, contract, value or calldata differs from admitted mutation.");
    }
}

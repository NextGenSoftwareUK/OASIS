using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
using Nethereum.Signer;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public class Web3SignedPreparationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void OriginalSignatureBindsChainSignerContractAndCalldata(bool dynamicFee)
    {
        // Public deterministic unit-only key, never funded or used outside tests.
        var key="1".PadLeft(64,'0');var sender=new EthECKey(key).GetPublicAddress();
        const string contract="0x1111111111111111111111111111111111111111";
        var signed=dynamicFee
            ? new Transaction1559Signer().SignTransaction(key,new Transaction1559(31337,0,1,2,3000000,contract,0,"0x0102",new List<AccessListItem>()))
            : new LegacyTransactionSigner().SignTransaction(key,new System.Numerics.BigInteger(31337),contract,0,0,1,3000000,"0x0102");
        var bytes=signed.HexToByteArray();var data=new byte[] {1,2};
        var hash="0x"+Convert.ToHexString(Nethereum.Util.Sha3Keccack.Current.CalculateHash(bytes)).ToLowerInvariant();
        Web3SignedPreparation.Verify(bytes,hash,31337,sender,contract,data);
        Assert.ThrowsException<InvalidDataException>(()=>Web3SignedPreparation.Verify(bytes,hash,1,sender,contract,data));
        Assert.ThrowsException<InvalidDataException>(()=>Web3SignedPreparation.Verify(bytes,hash,31337,contract,contract,data));
        Assert.ThrowsException<InvalidDataException>(()=>Web3SignedPreparation.Verify(bytes,hash,31337,sender,sender,data));
        Assert.ThrowsException<InvalidDataException>(()=>Web3SignedPreparation.Verify(bytes,hash,31337,sender,contract,new byte[] {3}));
        Assert.ThrowsException<InvalidDataException>(()=>Web3SignedPreparation.Verify(bytes,"0xwrong",31337,sender,contract,data));
        Assert.ThrowsException<InvalidDataException>(()=>Web3SignedPreparation.Verify(Array.Empty<byte>(),hash,31337,sender,contract,data));
    }
}

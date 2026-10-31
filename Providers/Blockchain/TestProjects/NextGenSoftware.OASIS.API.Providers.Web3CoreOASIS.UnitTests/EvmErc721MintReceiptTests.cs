using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using NextGenSoftware.OASIS.Providers.Shared.Evm;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public class EvmErc721MintReceiptTests
{
    private const string Contract = "0x1111111111111111111111111111111111111111";
    private const string Recipient = "0x2222222222222222222222222222222222222222";

    [DataTestMethod, DataRow("0"), DataRow("9223372036854775808"),
        DataRow("115792089237316195423570985008687907853269984665640564039457584007913129639935")]
    public void NativeMintIdRetainsFullUint256AndMatchesExactIntent(string value)
    {
        var id = BigInteger.Parse(value); var receipt = Receipt(id);
        Assert.AreEqual(id, EvmErc721MintReceipt.Verify(receipt, Contract, Recipient));
        Assert.AreEqual(id, EvmErc721MintReceipt.Verify(receipt, Contract.ToUpperInvariant().Replace("0X", "0x"), Recipient, id));
        Assert.ThrowsException<InvalidDataException>(() => EvmErc721MintReceipt.Verify(receipt, Contract, Recipient, id + 1));
    }

    [DataTestMethod, DataRow("status"), DataRow("block"), DataRow("hash"), DataRow("logs"),
        DataRow("foreign"), DataRow("sender"), DataRow("recipient"), DataRow("signature"),
        DataRow("short-topic"), DataRow("bad-hex"), DataRow("duplicate"), DataRow("erc20")]
    public void SuccessfulReceiptAloneCannotProveMint(string defect)
    {
        var receipt = Receipt(BigInteger.One); var log = receipt.Logs[0];
        switch (defect)
        {
            case "status": receipt.Status = new HexBigInteger(0); break;
            case "block": receipt.BlockNumber = null; break;
            case "hash": receipt.TransactionHash = "not-a-native-hash"; break;
            case "logs": receipt.Logs = null; break;
            case "foreign": log.Address = Recipient; break;
            case "sender": log.Topics[1] = log.Topics[2]; break;
            case "recipient": log.Topics[2] = "0x" + new string('0', 64); break;
            case "signature": log.Topics[0] = "0x" + new string('0', 64); break;
            case "short-topic": log.Topics[3] = "0x01"; break;
            case "bad-hex": log.Topics[3] = "0x" + new string('z', 64); break;
            case "duplicate": receipt.Logs = new[] { log, log }; break;
            case "erc20": log.Topics = log.Topics.Take(3).ToArray(); break;
        }
        Assert.ThrowsException<InvalidDataException>(() => EvmErc721MintReceipt.Verify(receipt, Contract, Recipient));
    }

    [DataTestMethod, DataRow("none"), DataRow("burn"), DataRow("from"), DataRow("to"),
        DataRow("contract"), DataRow("id"), DataRow("topics"), DataRow("logs")]
    public void DirectionalTransferProofBindsEveryNativeField(string defect)
    {
        var receipt = Receipt(BigInteger.One); var log = receipt.Logs[0];
        log.Topics[1] = "0x" + Contract[2..].PadLeft(64,'0');
        var to = defect == "burn" ? "0x" + new string('0',40) : Recipient;
        if (defect == "burn") log.Topics[2] = "0x" + new string('0',64);
        if (defect == "from") log.Topics[1] = "0x" + new string('0',64);
        if (defect == "to") log.Topics[2] = log.Topics[1];
        if (defect == "contract") log.Address = Recipient;
        if (defect == "id") log.Topics[3] = "0x" + new string('0',64);
        if (defect == "topics") log.Topics = log.Topics.Take(3).ToArray();
        if (defect == "logs") receipt.Logs = null;
        if (defect is "none" or "burn") EvmErc721TransferReceipt.Verify(receipt, Contract, Contract, to, BigInteger.One);
        else Assert.ThrowsException<InvalidDataException>(() => EvmErc721TransferReceipt.Verify(receipt, Contract, Contract, to, BigInteger.One));
    }

    [DataTestMethod, DataRow("0"), DataRow("115792089237316195423570985008687907853269984665640564039457584007913129639935")]
    public void TransferProofPreservesZeroAndMaximumNativeTokenIds(string value)
    {
        var id = BigInteger.Parse(value); var receipt = Receipt(id);
        receipt.Logs[0].Topics[1] = "0x" + Contract[2..].PadLeft(64,'0');
        EvmErc721TransferReceipt.Verify(receipt, Contract, Contract, Recipient, id);
    }

    private static TransactionReceipt Receipt(BigInteger id) => new()
    {
        Status = new HexBigInteger(1), BlockNumber = new HexBigInteger(7), TransactionHash = "0x" + new string('a', 64),
        Logs = new[] { new FilterLog { Address = Contract, Topics = new object[] {
            "0x" + Sha3Keccack.Current.CalculateHash("Transfer(address,address,uint256)"),
            "0x" + new string('0', 64), "0x" + Recipient.Substring(2).PadLeft(64, '0'),
            "0x" + Convert.ToHexString(id.ToByteArray(isUnsigned:true, isBigEndian:true)).PadLeft(64, '0') } } }
    };
}

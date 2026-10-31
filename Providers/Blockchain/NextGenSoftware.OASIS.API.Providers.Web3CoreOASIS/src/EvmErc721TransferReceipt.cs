using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;

namespace NextGenSoftware.OASIS.Providers.Shared.Evm;

// Native ERC-721 event proof; shared/source-linked, never a submission or retry owner.
internal static class EvmErc721TransferReceipt
{
    internal static void RequireMined(TransactionReceipt receipt)
    {
        if (receipt?.Status?.Value != BigInteger.One || receipt.BlockNumber == null
            || receipt.TransactionHash?.Length != 66 || !receipt.TransactionHash.StartsWith("0x", StringComparison.Ordinal)
            || !receipt.TransactionHash.Skip(2).All(Uri.IsHexDigit))
            throw new InvalidDataException("NFT receipt lacks an authoritative successful mined outcome.");
    }

    internal static void Verify(TransactionReceipt receipt, string contract, string from, string to, BigInteger id)
    {
        RequireMined(receipt);
        if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(contract)
            || !AddressUtil.Current.IsValidEthereumAddressHexFormat(from)
            || !AddressUtil.Current.IsValidEthereumAddressHexFormat(to)
            || id < 0 || id >= (BigInteger.One << 256))
            throw new InvalidDataException("NFT transfer proof requires exact contract/accounts and uint256 token ID.");
        var signature = "0x" + Sha3Keccack.Current.CalculateHash("Transfer(address,address,uint256)");
        var fromTopic = "0x" + from.Substring(2).PadLeft(64, '0');
        var toTopic = "0x" + to.Substring(2).PadLeft(64, '0');
        var token = "0x" + Convert.ToHexString(id.ToByteArray(isUnsigned:true, isBigEndian:true)).PadLeft(64, '0');
        if (receipt.Logs == null || !receipt.Logs.Any(log => log != null
            && string.Equals(log.Address, contract, StringComparison.OrdinalIgnoreCase)
            && log.Topics is { Length:4 } topics
            && string.Equals(topics[0]?.ToString(), signature, StringComparison.OrdinalIgnoreCase)
            && string.Equals(topics[1]?.ToString(), fromTopic, StringComparison.OrdinalIgnoreCase)
            && string.Equals(topics[2]?.ToString(), toTopic, StringComparison.OrdinalIgnoreCase)
            && string.Equals(topics[3]?.ToString(), token, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("NFT receipt lacks the exact contract/from/to/token ERC-721 transfer event.");
    }
}

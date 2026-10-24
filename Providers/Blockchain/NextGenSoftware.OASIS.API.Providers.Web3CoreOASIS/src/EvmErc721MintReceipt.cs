using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Globalization;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;

namespace NextGenSoftware.OASIS.Providers.Shared.Evm;

// Shared/source-linked by EVM providers, not an alternative submission implementation.
internal static class EvmErc721MintReceipt
{
    internal static BigInteger Verify(TransactionReceipt receipt, string contract, string recipient, BigInteger? expectedId = null)
    {
        if (receipt?.Status?.Value != BigInteger.One || receipt.BlockNumber == null
            || receipt.TransactionHash?.Length != 66 || !receipt.TransactionHash.StartsWith("0x", StringComparison.Ordinal)
            || !receipt.TransactionHash.Skip(2).All(Uri.IsHexDigit))
            throw new InvalidDataException("NFT mint receipt lacks an authoritative mined outcome.");
        if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(contract)
            || !AddressUtil.Current.IsValidEthereumAddressHexFormat(recipient)
            || string.Equals(recipient, "0x" + new string('0', 40), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("NFT mint proof requires an exact contract and nonzero recipient.");

        var signature = "0x" + Sha3Keccack.Current.CalculateHash("Transfer(address,address,uint256)");
        var zero = "0x" + new string('0', 64);
        var to = "0x" + recipient.Substring(2).PadLeft(64, '0');
        var matches = receipt.Logs?.Where(log =>
            string.Equals(log.Address, contract, StringComparison.OrdinalIgnoreCase)
            && log.Topics is { Length: 4 } topics
            && string.Equals(topics[0]?.ToString(), signature, StringComparison.OrdinalIgnoreCase)
            && string.Equals(topics[1]?.ToString(), zero, StringComparison.OrdinalIgnoreCase)
            && string.Equals(topics[2]?.ToString(), to, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches == null || matches.Length != 1)
            throw new InvalidDataException("NFT mint receipt requires exactly one zero-to-recipient ERC-721 transfer event.");
        var token = matches[0].Topics[3]?.ToString();
        if (token?.Length != 66 || !token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            || !token.Skip(2).All(Uri.IsHexDigit))
            throw new InvalidDataException("NFT mint event token ID is not an exact uint256 topic.");
        var id = BigInteger.Parse("0" + token.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        if (expectedId.HasValue && id != expectedId.Value)
            throw new InvalidDataException("NFT mint event does not match the submitted native token ID.");
        return id;
    }
}

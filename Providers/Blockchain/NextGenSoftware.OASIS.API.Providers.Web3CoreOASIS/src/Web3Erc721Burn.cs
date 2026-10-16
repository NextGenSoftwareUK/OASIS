using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Nethereum.RPC.Eth.DTOs;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

internal static class Web3Erc721Burn
{
    // Explicit ERC-721 burn(uint256) extension, not a fallback ABI for storage.
    internal const string Abi = "[{\"type\":\"function\",\"name\":\"ownerOf\",\"inputs\":[{\"name\":\"id\",\"type\":\"uint256\"}],\"outputs\":[{\"name\":\"owner\",\"type\":\"address\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"burn\",\"inputs\":[{\"name\":\"id\",\"type\":\"uint256\"}],\"outputs\":[],\"stateMutability\":\"nonpayable\"}]";

    internal static void VerifyReceipt(TransactionReceipt receipt,string contract,string owner,BigInteger id)
    {
        if(receipt?.Status?.Value != BigInteger.One || receipt.BlockNumber == null
            || receipt.TransactionHash?.Length != 66 || !receipt.TransactionHash.StartsWith("0x",StringComparison.Ordinal)
            || !receipt.TransactionHash.Skip(2).All(Uri.IsHexDigit))
            throw new InvalidDataException("NFT burn receipt lacks an authoritative mined outcome.");
        var signature="0x"+Nethereum.Util.Sha3Keccack.Current.CalculateHash("Transfer(address,address,uint256)");
        var from="0x"+owner.Substring(2).PadLeft(64,'0');
        var zero="0x"+new string('0',64);
        var token="0x"+Convert.ToHexString(id.ToByteArray(isUnsigned:true,isBigEndian:true)).PadLeft(64,'0');
        if(receipt.Logs == null || !receipt.Logs.Any(log=>
            string.Equals(log.Address,contract,StringComparison.OrdinalIgnoreCase)
            && log.Topics is { Length:4 } topics
            && string.Equals(topics[0]?.ToString(),signature,StringComparison.OrdinalIgnoreCase)
            && string.Equals(topics[1]?.ToString(),from,StringComparison.OrdinalIgnoreCase)
            && string.Equals(topics[2]?.ToString(),zero,StringComparison.OrdinalIgnoreCase)
            && string.Equals(topics[3]?.ToString(),token,StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("NFT burn receipt lacks the exact owner-to-zero transfer event.");
    }
}

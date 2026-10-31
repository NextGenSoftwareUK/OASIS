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
        NextGenSoftware.OASIS.Providers.Shared.Evm.EvmErc721TransferReceipt.Verify(
            receipt, contract, owner, "0x" + new string('0',40), id);
    }
}

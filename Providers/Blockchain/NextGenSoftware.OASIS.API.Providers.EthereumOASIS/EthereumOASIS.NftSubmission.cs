using System;
using System.IO;
using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS
{
    // One submission/receipt identity owner for mint, transfer and burn.
    // SDK polling stays in the existing PollTokenReceiptAsync boundary.
    private async Task<TransactionReceipt> SubmitNftAsync(Web3 web3, Function function,
        string sender, HexBigInteger gas, params object[] arguments)
    {
        string hash = null;
        try
        {
            var acknowledged = await function.SendTransactionAsync(sender, gas, null, arguments);
            if (!IsStorageTransactionIdentity(acknowledged))
                throw new InvalidDataException("NFT submission returned no valid native transaction identity.");
            hash = acknowledged;
            var receipt = await PollTokenReceiptAsync(web3, hash);
            if (receipt == null || !string.Equals(receipt.TransactionHash, hash, StringComparison.OrdinalIgnoreCase)
                || receipt.BlockNumber == null || receipt.Status == null
                || (receipt.Status.Value != 0 && receipt.Status.Value != 1))
                throw new InvalidDataException("NFT receipt lacks a matching authoritative mined execution outcome.");
            return receipt;
        }
        catch (Exception cause)
        {
            throw new EthereumNftMutationReconciliationException(hash, cause);
        }
    }
}

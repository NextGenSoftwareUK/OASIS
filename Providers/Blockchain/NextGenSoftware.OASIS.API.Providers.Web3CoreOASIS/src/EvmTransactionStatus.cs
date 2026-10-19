using System;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Hex.HexTypes;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.Providers.Shared.Evm;

// Same source-link pattern as EvmWalletBalance/EvmWalletHistory; no extra SDK/backend.
internal static class EvmTransactionStatus
{
    internal static async Task<OASISResult<BridgeTransactionStatus>> ReadAsync(string transactionHash,
        Func<Task<OASISResult<Web3>>> initialize,CancellationToken token=default)
    {
        var result=new OASISResult<BridgeTransactionStatus>{Result=BridgeTransactionStatus.Pending};
        try{
            token.ThrowIfCancellationRequested();EvmFinalizedStatus.ValidateHash(transactionHash);
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var ready=await initialize().WaitAsync(deadline.Token);
            if(ready.IsError){OASISErrorHandling.HandleError(ref result,ready.Message,ready.Exception);result.ErrorCode=ready.ErrorCode;return result;}
            var client=ready.Result??throw new InvalidOperationException("EVM status client was not initialized.");
            result.Result=await EvmFinalizedStatus.ObserveAsync(transactionHash,
                async ct=>{
                    var receipt=await client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash).WaitAsync(ct);
                    return receipt==null?null:new(receipt.TransactionHash,receipt.BlockHash,receipt.BlockNumber?.Value,receipt.Status?.Value);
                },async ct=>{
                    var block=await client.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(BlockParameter.CreateFinalized()).WaitAsync(ct);
                    return block==null?null:new(block.BlockHash,block.Number?.Value);
                },async (height,ct)=>{
                    var block=await client.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(new BlockParameter(new HexBigInteger(height))).WaitAsync(ct);
                    return block==null?null:new(block.BlockHash,block.Number?.Value);
                },deadline.Token);
            result.Message="Canonical finalized EVM execution only; not proof of rollup settlement, bridge release or fee recovery.";
        }catch(Exception cause){OASISErrorHandling.HandleError(ref result,"EVM status observation failed: "+cause.Message,cause);}
        return result;
    }
}

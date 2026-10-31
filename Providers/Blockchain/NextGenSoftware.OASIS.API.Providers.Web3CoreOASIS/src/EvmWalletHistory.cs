using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.Evm;

// Single maintained source, also compiled by EthereumOASIS via an explicit source link.
internal static class EvmWalletHistory
{
    private static bool Hash(string value)=>value?.Length==66 && value.StartsWith("0x",StringComparison.Ordinal) && value.Skip(2).All(Uri.IsHexDigit);

    internal static async Task<OASISResult<IList<IWalletTransaction>>> ReadAsync(IGetWeb3TransactionsRequest request,Func<Task<OASISResult<Web3>>> initialize)
    {
        var result=new OASISResult<IList<IWalletTransaction>>();
        try
        {
            if(request==null || !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(request.WalletAddress)) throw new ArgumentException("A valid EVM wallet address is required.");
            var first=new BigInteger(request.FromBlock??0);
            if(request.ToBlock.HasValue && (request.ToBlock.Value<first || new BigInteger(request.ToBlock.Value)-first>=1000))
                throw new ArgumentOutOfRangeException(nameof(request),"Choose an inclusive range of at most 1000 blocks.");
            var ready=await initialize();
            if(ready.IsError){OASISErrorHandling.HandleError(ref result,ready.Message,ready.Exception);result.ErrorCode=ready.ErrorCode;return result;}
            var client=ready.Result??throw new InvalidOperationException("EVM history client was not initialized.");
            var tipResponse=await client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            if(tipResponse==null || tipResponse.Value<0) throw new InvalidDataException("EVM history head is missing or invalid.");
            var tip=tipResponse.Value;
            var last=request.ToBlock.HasValue ? new BigInteger(request.ToBlock.Value):tip;
            if(first>last || last>tip || last-first>=1000) throw new ArgumentOutOfRangeException(nameof(request),"Choose an available inclusive range of at most 1000 blocks using FromBlock and ToBlock.");
            var rows=new List<IWalletTransaction>();string previousHash=null;
            for(var height=first;height<=last;height++)
            {
                var block=await client.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(new HexBigInteger(height)));
                if(block?.Number?.Value!=height || !Hash(block.BlockHash) || block.Transactions==null || block.Timestamp==null || block.Timestamp.Value<0 || block.Timestamp.Value>253402300799)
                    throw new InvalidDataException("EVM history block is incomplete.");
                if(previousHash!=null && !string.Equals(block.ParentHash,previousHash,StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("EVM history range changed during the read.");
                foreach(var transaction in block.Transactions)
                {
                    if(transaction==null || !Hash(transaction.TransactionHash) || transaction.Value==null || transaction.Value.Value<0 || transaction.Value.Value>=(BigInteger.One<<256)
                        || !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(transaction.From)
                        || transaction.To!=null && !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(transaction.To))
                        throw new InvalidDataException("EVM history transaction identity/value is malformed.");
                    if(!string.Equals(transaction.From,request.WalletAddress,StringComparison.OrdinalIgnoreCase) && !string.Equals(transaction.To,request.WalletAddress,StringComparison.OrdinalIgnoreCase)) continue;
                    var receipt=await client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction.TransactionHash);
                    if(receipt?.Status==null || (receipt.Status.Value!=0 && receipt.Status.Value!=1) || receipt.BlockNumber?.Value!=height
                        || !string.Equals(receipt.BlockHash,block.BlockHash,StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(receipt.TransactionHash,transaction.TransactionHash,StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("EVM history receipt does not match its block/transaction or execution status.");
                    rows.Add(new WalletTransaction{NativeTransactionId=transaction.TransactionHash,FromWalletAddress=transaction.From,ToWalletAddress=transaction.To??receipt.ContractAddress,
                        RawAmount=transaction.Value.Value.ToString(CultureInfo.InvariantCulture),AmountUnit="wei",Amount=EvmNativeAmount.ToDisplayDouble(transaction.Value.Value),
                        NativeOperation="top-level transaction",NativeExecutionResult=receipt.Status.Value==1 ? "succeeded":"reverted",
                        NativeError=receipt.Status.Value==1 ? null:"EVM execution reverted; requested value was not applied.",
                        CreatedDate=DateTimeOffset.FromUnixTimeSeconds(checked((long)block.Timestamp.Value)).UtcDateTime,
                        TransactionType=string.Equals(transaction.From,request.WalletAddress,StringComparison.OrdinalIgnoreCase) ? TransactionType.Debit:TransactionType.Credit,
                        TransactionCategory=TransactionCategory.Other,Description="Mined top-level EVM transaction; excludes internal calls and token events."});
                }
                previousHash=block.BlockHash;
            }
            var anchor=await client.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(new HexBigInteger(last)));
            if(anchor?.Number?.Value!=last || !string.Equals(anchor.BlockHash,previousHash,StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("EVM history anchor changed during the read.");
            result.Result=rows;result.IsLoaded=true;result.LoadedCount=rows.Count;
            result.Message=$"Top-level mined transactions in inclusive blocks {first}-{last}; excludes internal calls and token events.";
        }
        catch(Exception cause){OASISErrorHandling.HandleError(ref result,"EVM transaction history failed: "+cause.Message,cause);}
        return result;
    }
}

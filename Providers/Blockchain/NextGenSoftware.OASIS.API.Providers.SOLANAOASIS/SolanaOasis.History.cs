using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using Solnet.Programs;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    private async Task<List<IWalletTransaction>> ReadNativeHistoryEffectsAsync(SignatureStatusInfo signature,string wallet,WalletTransaction summary)
    {
        var response=await _rpcClient.GetTransactionAsync(signature.Signature,Commitment.Finalized);
        if(!response.WasSuccessful) throw new InvalidOperationException($"Solana transaction RPC failed: {response.Reason}");
        var tx=response.Result;
        if(tx?.Transaction?.Message?.AccountKeys==null || tx.Transaction.Message.Instructions==null || tx.Meta==null)
            throw new InvalidDataException("Solana transaction details are missing; history is incomplete.");
        if(tx.Slot!=signature.Slot || tx.Transaction.Signatures==null || tx.Transaction.Signatures.FirstOrDefault()!=signature.Signature
            || (tx.Meta.Error==null)!=(signature.Error==null))
            throw new InvalidDataException("Solana transaction details do not match the native signature, slot or execution result.");
        var rows=new List<IWalletTransaction>();var message=tx.Transaction.Message;
        for(var index=0;index<message.Instructions.Length;index++)
        {
            var instruction=message.Instructions[index];
            if(instruction.ProgramIdIndex<0 || instruction.ProgramIdIndex>=message.AccountKeys.Length)
                throw new InvalidDataException("Solana instruction program index is invalid.");
            if(message.AccountKeys[instruction.ProgramIdIndex]!=SystemProgram.ProgramIdKey.Key) continue;
            var data=Encoders.Base58.DecodeData(instruction.Data);
            if(data.Length<4) throw new InvalidDataException("Solana System instruction is truncated.");
            if(System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data)!=2) continue;
            if(data.Length!=12 || instruction.Accounts==null || instruction.Accounts.Length!=2
                || instruction.Accounts.Any(key=>key<0 || key>=message.AccountKeys.Length))
                throw new InvalidDataException("Solana native transfer instruction is malformed.");
            var from=message.AccountKeys[instruction.Accounts[0]];var to=message.AccountKeys[instruction.Accounts[1]];
            if(from!=wallet && to!=wallet) continue;
            var raw=System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(4));
            rows.Add(new WalletTransaction {
                NativeTransactionId=signature.Signature,NativeOperation="SystemProgram.Transfer",
                NativeExecutionResult=summary.NativeExecutionResult,NativeError=summary.NativeError,
                FromWalletAddress=from,ToWalletAddress=to,RawAmount=raw.ToString(CultureInfo.InvariantCulture),AmountUnit="lamports",
                Amount=(double)(raw/1_000_000_000m),CreatedDate=summary.CreatedDate,
                Description=$"Finalized Solana slot {signature.Slot}, top-level instruction {index}; requested SOL transfer, execution status determines whether applied."
            });
        }
        if(rows.Count==0) rows.Add(summary);
        return rows;
    }
}

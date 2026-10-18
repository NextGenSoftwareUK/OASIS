using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

internal sealed record HederaTokenEscrowIntent(Guid LockId, string DepositId, string Amount)
{
    internal string AssociationTokenId { get; init; }
    internal static HederaTokenEscrowIntent CaptureAssociation(HederaTokenEscrowConfiguration config,string token,string requestedEscrow)
    {
        config.Validate();
        if(config.Protocol!=HederaTokenEscrowProtocol.IdBoundV1 || requestedEscrow!=config.ContractId || string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Association requires explicit id-bound protocol, token and matching reviewed escrow.");
        return new(Guid.Empty,null,null){AssociationTokenId=token};
    }
    internal static HederaTokenEscrowIntent Capture(HederaTokenEscrowConfiguration config, bool locking,
        Guid id, decimal amount, string requestedEscrow)
    {
        config.Validate();
        if(config.Protocol==HederaTokenEscrowProtocol.AggregateV1)
        {
            NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.AggregateTokenEscrowRequest.Validate(amount,id,requestedEscrow,config.ContractId);
            return new(Guid.Empty,null,null);
        }
        if(id==Guid.Empty || (locking && amount<=0) || (!locking && amount!=0) || requestedEscrow!=config.ContractId)
            throw new ArgumentException("Per-deposit escrow requires nonempty identity, positive lock amount and matching configured contract; release uses the identified whole deposit.");
        return new(id,"0x"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("N")))).ToLowerInvariant(),
            amount.ToString("0.############################",CultureInfo.InvariantCulture));
    }

    internal void AppendTo(IDictionary<string,object> request)
    {
        request["escrowProtocol"]=DepositId==null&&AssociationTokenId==null?"aggregate-v1":"id-bound-v1";
        if(DepositId!=null){request["depositId"]=DepositId;request["amount"]=Amount;}
    }

    internal void ValidateReply(JsonElement reply)
    {
        if(AssociationTokenId!=null){
            if(!reply.GetProperty("associated").GetBoolean() || reply.GetProperty("tokenId").GetString()!=AssociationTokenId)
                throw new System.IO.InvalidDataException("Association reply does not establish the requested token association.");
            return;
        }
        if(!long.TryParse(reply.GetProperty("amountUnits").GetString(),NumberStyles.None,CultureInfo.InvariantCulture,out var units) || units<=0)
            throw new System.IO.InvalidDataException("Escrow SDK response lacks exact positive int64 token units.");
        if(DepositId!=null && reply.GetProperty("depositId").GetString()!=DepositId)
            throw new System.IO.InvalidDataException("Escrow SDK response deposit identity differs from original intent.");
    }
}

public sealed class HederaIdBoundEscrowResponse : ITransactionResponse
{
    public string TransactionResult { get; set; }
    public Guid LockId { get; }
    public IReadOnlyList<string> NativeTransactions { get; }
    internal HederaIdBoundEscrowResponse(Guid id,string mutation,string approval)
    {
        LockId=id;TransactionResult=mutation;
        NativeTransactions=Array.AsReadOnly(approval==null?new[]{mutation}:new[]{approval,mutation});
    }
}

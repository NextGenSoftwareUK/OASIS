using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS : IHolonWritePreparationStorageProvider
{
    private const string PreparationTable="OasisHolonWritePreparations";
    public OASISResult<HolonWritePreparation> ClaimHolonWritePreparation(HolonWritePreparation candidate)
        =>ClaimHolonWritePreparationAsync(candidate).GetAwaiter().GetResult();

    public async Task<OASISResult<HolonWritePreparation>> ClaimHolonWritePreparationAsync(
        HolonWritePreparation candidate,CancellationToken cancellationToken=default)
    {
        var dispatched=false;string? requestToken=null;var key=string.Empty;
        try {
            cancellationToken.ThrowIfCancellationRequested();ArgumentNullException.ThrowIfNull(candidate);
            if(candidate.Authority!=StorageProviderType)return PreparationFailure("COSMIC_AUTHORITY_MISMATCH","The preparation belongs to another authority.");
            if(_client.Config.MaxErrorRetry!=0)throw new InvalidOperationException("Preparation claims require single-attempt SDK configuration.");
            // Native list/map attributes preserve opaque envelope text without a second JSON codec.
            if(candidate.Records.Count>99)
                throw new ArgumentException("DynamoDB protected preparation exceeds the99-record/400KiB native item limit.");
            key=StorageProviderType+":"+candidate.OperationId.ToString("N");
            var item=new Dictionary<string,AttributeValue> {
                ["id"]=new(){S=key},["actor"]=new(){S=candidate.ActorId.ToString("D")},
                ["authority"]=new(){S=StorageProviderType.ToString()},
                ["records"]=new(){L=candidate.Records.Select(record=>new AttributeValue{M=new() {
                    ["id"]=new(){S=record.HolonId.ToString("D")},["envelope"]=new(){S=record.Envelope}
                }}).ToList()}
            };
            if(DynamoItemSize.MinimumBytes(item)>DynamoItemSize.MaximumItemBytes)
                throw new ArgumentException("DynamoDB protected preparation known native bytes exceed400KiB.");
            var prior=await ReadPreparationWinnerAsync(candidate,key,cancellationToken);
            if(prior!=null)return new(prior);
            requestToken=Guid.NewGuid().ToString("N");
            var transaction=new TransactWriteItemsRequest {ClientRequestToken=requestToken,TransactItems=new() {
                new(){ConditionCheck=new ConditionCheck {TableName=ConditionalReceiptTable,Key=new(){{"id",new(){S=key}}},
                    ConditionExpression="attribute_not_exists(#id)",ExpressionAttributeNames=new(){{"#id","id"}}}},
                new(){Put=new Put {TableName=PreparationTable,Item=item,ConditionExpression="attribute_not_exists(#id)",
                    ExpressionAttributeNames=new(){{"#id","id"}}}}
            }};
            cancellationToken.ThrowIfCancellationRequested();dispatched=true;
            try {
                var response=await _client.TransactWriteItemsAsync(transaction,cancellationToken);
                if(response?.HttpStatusCode!=HttpStatusCode.OK)throw new InvalidDataException("Preparation claim lacks an authoritative native acknowledgement.");
            } catch(TransactionCanceledException cause) when(cause.CancellationReasons?.Any(reason=>reason.Code=="ConditionalCheckFailed")==true
                &&cause.CancellationReasons.All(reason=>reason.Code=="None"||reason.Code=="ConditionalCheckFailed")) {
                dispatched=false;
                var winner=await ReadPreparationWinnerAsync(candidate,key,cancellationToken);
                if(winner!=null)return new(winner);
                var receipt=await _client.GetItemAsync(new GetItemRequest {TableName=ConditionalReceiptTable,
                    Key=new(){{"id",new(){S=key}}},ConsistentRead=true},cancellationToken);
                RequireItemRead(receipt);
                if(receipt.Item.Count>0)return PreparationFailure("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",
                    "This operation has a write receipt but no original protected preparation.",
                    new DynamoDBMutationReconciliationException(PreparationTable,key,new InvalidDataException("Original protected preparation is unavailable.")));
                return PreparationFailure("COSMIC_PREPARATION_FAILED","The native preparation claim was rejected without a readable winner.",cause);
            } catch(Amazon.DynamoDBv2.AmazonDynamoDBException cause) when(IsNativeValidationRejection(cause)) {
                dispatched=false;throw;
            }
            return new(candidate);
        } catch(PreparationActorMismatchException cause) {
            return PreparationFailure("COSMIC_PREPARATION_ACTOR_MISMATCH",cause.Message,cause);
        } catch(Exception cause) {
            return PreparationFailure(dispatched?"COSMIC_PREPARATION_RECONCILIATION_REQUIRED":
                cause is OperationCanceledException?"COSMIC_PREPARATION_CANCELLED":"COSMIC_PREPARATION_FAILED",
                "The protected preparation claim failed.",dispatched?
                    new DynamoDBMutationReconciliationException(PreparationTable,key,cause,requestToken):cause);
        }
    }
    private async Task<HolonWritePreparation?> ReadPreparationWinnerAsync(HolonWritePreparation candidate,string key,CancellationToken token)
    {
        var response=await _client.GetItemAsync(new GetItemRequest {TableName=PreparationTable,
            Key=new(){{"id",new(){S=key}}},ConsistentRead=true},token);
        RequireItemRead(response);var item=response.Item;
        if(item.Count==0)return null;
        if(!item.TryGetValue("id",out var id)||id.S!=key||!item.TryGetValue("authority",out var authority)||authority.S!=StorageProviderType.ToString()
            ||!item.TryGetValue("actor",out var actor)||!Guid.TryParseExact(actor.S,"D",out var owner)||owner==Guid.Empty||actor.S!=owner.ToString("D"))
            throw new InvalidDataException("Stored preparation ownership is invalid.");
        if(owner!=candidate.ActorId)throw new PreparationActorMismatchException();
        if(!item.TryGetValue("records",out var rows)||rows.L==null||rows.L.Count<1||rows.L.Count>99)
            throw new InvalidDataException("Stored preparation record count is invalid.");
        if(DynamoItemSize.MinimumBytes(item)>DynamoItemSize.MaximumItemBytes)
            throw new InvalidDataException("Stored preparation exceeds its native transport limit.");
        var records=new List<ProtectedHolonPreparation>();
        foreach(var row in rows.L) {
            var fields=row.M;
            if(fields==null||fields.Count!=2||!fields.TryGetValue("id",out var identity)||!Guid.TryParseExact(identity.S,"D",out var holonId)
                ||identity.S!=holonId.ToString("D")||!fields.TryGetValue("envelope",out var envelope)||envelope.S==null)
                throw new InvalidDataException("Stored preparation record schema is invalid.");
            records.Add(new ProtectedHolonPreparation(holonId,envelope.S));
        }
        return new HolonWritePreparation(candidate.OperationId,owner,StorageProviderType,records);
    }
    private static OASISResult<HolonWritePreparation> PreparationFailure(string code,string message,Exception? cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
    private sealed class PreparationActorMismatchException:Exception
    {
        public PreparationActorMismatchException():base("The operation is owned by another actor."){}
    }
}

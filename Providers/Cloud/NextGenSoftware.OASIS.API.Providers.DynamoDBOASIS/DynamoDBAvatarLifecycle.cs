using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS : IAvatarTombstoneStorageProvider
{
    public OASISResult<bool> ApplyAvatarTombstone(AvatarTombstone tombstone)
        => ApplyAvatarTombstoneAsync(tombstone).GetAwaiter().GetResult();

    public Task<OASISResult<bool>> ApplyAvatarTombstoneAsync(AvatarTombstone tombstone)
        => tombstone == null ? Task.FromResult(new OASISResult<bool> {IsError=true,ErrorCode="HYPERDRIVE_PAYLOAD_INVALID",Message="Committed tombstone required."})
            : ApplyAvatarLifecycleAsync(tombstone.AvatarId,true,tombstone);

    private static Dictionary<string,AttributeValue> LifecycleKey(Guid id)
        => new() {{"id",new AttributeValue {S=id.ToString()}}};

    private async Task<Dictionary<string,AttributeValue>> ReadLifecycleItemAsync(string table,Guid id)
    {
        var read=await _client.GetItemAsync(new GetItemRequest {TableName=table,Key=LifecycleKey(id),ConsistentRead=true});
        return read.Item ?? new Dictionary<string,AttributeValue>();
    }

    private static string LifecycleJson(Dictionary<string,AttributeValue> item,Guid id)
    {
        if(!item.TryGetValue("id",out var key) || key.S!=id.ToString()
            || !item.TryGetValue("data_json",out var json) || string.IsNullOrWhiteSpace(json.S))
            throw new InvalidDataException("DynamoDB lifecycle snapshot/key is invalid.");
        return json.S;
    }

    private static TransactWriteItem LifecycleWrite(string table,Guid id,string expected,string updated)
    {
        var values=new Dictionary<string,AttributeValue> {{":expected",new AttributeValue {S=expected}}};
        if(updated == null) return new TransactWriteItem {Delete=new Delete {TableName=table,Key=LifecycleKey(id),
            ConditionExpression="data_json = :expected",ExpressionAttributeValues=values}};
        values[":json"]=new AttributeValue {S=updated};values[":deleted"]=new AttributeValue {BOOL=true};
        return new TransactWriteItem {Update=new Update {TableName=table,Key=LifecycleKey(id),
            ConditionExpression="data_json = :expected",UpdateExpression="SET data_json = :json, is_deleted = :deleted",ExpressionAttributeValues=values}};
    }

    private async Task<OASISResult<bool>> ApplyAvatarLifecycleAsync(Guid id,bool softDelete,AvatarTombstone supplied=null)
    {
        var result=new OASISResult<bool>();var dispatched=false;
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("Avatar identity required.");
            if(!IsProviderActivated || _clientDisposed) throw new InvalidOperationException("DynamoDB provider is not activated.");
            var profileItem=await ReadLifecycleItemAsync("OasisAvatars",id);
            if(profileItem.Count==0) return new OASISResult<bool>(false) {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Avatar not found in DynamoDB."};
            var profileJson=LifecycleJson(profileItem,id);
            var profile=Des<Avatar>(profileJson) ?? throw new InvalidDataException("Avatar snapshot is null.");
            if(profile.Id!=id) throw new InvalidDataException("Avatar snapshot identity differs from its key.");
            if(supplied!=null && profile.IsDeleted && (profile.DeletedDate!=supplied.DeletedAtUtc || profile.DeletedByAvatarId!=supplied.DeletedByAvatarId))
                throw new StorageLifecycleConflictException("Avatar has a conflicting committed tombstone.");
            var detailItem=await ReadLifecycleItemAsync("OasisAvatarDetails",id);
            if(supplied!=null && detailItem.Count==0) throw new StorageLifecycleConflictException("Committed tombstone requires an existing avatar detail.");
            AvatarTombstone tombstone=null;
            if(softDelete)
            {
                var date=supplied?.DeletedAtUtc ?? (profile.IsDeleted ? profile.DeletedDate:DateTime.UtcNow);
                profile.IsActive=false;profile.DeletedDate=date;
                if(supplied!=null) profile.DeletedByAvatarId=supplied.DeletedByAvatarId;
                tombstone=new AvatarTombstone(id,date,profile.DeletedByAvatarId);
            }
            var writes=new List<TransactWriteItem> {LifecycleWrite("OasisAvatars",id,profileJson,softDelete ? Ser(profile):null)};
            if(detailItem.Count==0)
                writes.Add(new TransactWriteItem {ConditionCheck=new ConditionCheck {TableName="OasisAvatarDetails",Key=LifecycleKey(id),ConditionExpression="attribute_not_exists(id)"}});
            else
            {
                var detailJson=LifecycleJson(detailItem,id);
                var detail=Des<AvatarDetail>(detailJson) ?? throw new InvalidDataException("Avatar detail snapshot is null.");
                if(detail.Id!=id) throw new InvalidDataException("Avatar detail snapshot identity differs from its key.");
                if(softDelete)
                {
                    if(detail.IsDeleted && (detail.DeletedDate!=tombstone.DeletedAtUtc || detail.DeletedByAvatarId!=tombstone.DeletedByAvatarId))
                        throw new StorageLifecycleConflictException("Avatar/detail committed tombstones conflict.");
                    detail.IsActive=false;detail.DeletedDate=tombstone.DeletedAtUtc;detail.DeletedByAvatarId=tombstone.DeletedByAvatarId;
                }
                writes.Add(LifecycleWrite("OasisAvatarDetails",id,detailJson,softDelete ? Ser(detail):null));
            }
            dispatched=true;
            var response=await _client.TransactWriteItemsAsync(new TransactWriteItemsRequest {TransactItems=writes,ClientRequestToken=Guid.NewGuid().ToString("N")});
            if(response?.HttpStatusCode!=HttpStatusCode.OK) throw new InvalidDataException("DynamoDB lifecycle transaction lacks an authoritative acknowledgement.");
            result.Result=true;result.IsSaved=true;result.IsDeleted=true;
            if(tombstone!=null) AvatarTombstoneEvidence.Attach(result,tombstone);
        }
        catch(TransactionCanceledException cause) when(cause.CancellationReasons?.Any(r=>r.Code=="ConditionalCheckFailed")==true
            && cause.CancellationReasons.All(r=>r.Code=="None" || r.Code=="ConditionalCheckFailed"))
        {
            result.IsError=true;result.Exception=cause;result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
            result.Message="DynamoDB avatar/detail changed before lifecycle commit; transaction canceled atomically.";
        }
        catch(Exception cause)
        {
            result.IsError=true;result.Exception=dispatched ? new DynamoDBMutationReconciliationException("OasisAvatars+OasisAvatarDetails",id.ToString(),cause):cause;
            result.Message=result.Exception.Message;
            if(cause is StorageLifecycleConflictException) result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
            else if(!dispatched && cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}

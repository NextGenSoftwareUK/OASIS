using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS : IConditionalHolonStorageProvider
{
    private const string ConditionalReceiptTable="OasisHolonWriteReceipts";
    public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
        =>ExecuteConditionalHolonWriteAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(
        ConditionalHolonWriteRequest request,CancellationToken cancellationToken=default)
    {
        var dispatched=false;
        string? token=null;
        ConditionalHolonPlan? plan=null;
        try {
            cancellationToken.ThrowIfCancellationRequested();
            plan=ConditionalHolonPlan.Create(request,StorageProviderType);
            if(plan.Nodes.Count>99)throw new ArgumentException("DynamoDB atomic graph allows at most99 nodes plus its receipt.");
            if(_client.Config.MaxErrorRetry!=0)throw new InvalidOperationException("Atomic graph writes require single-attempt SDK configuration.");
            var receipt=await ReadGraphReceiptAsync(plan,cancellationToken);
            if(receipt!=null)return GraphAcknowledgement(receipt,request.Operation);
            var originals=new Dictionary<Guid,Dictionary<string,AttributeValue>>();
            var previous=new Dictionary<Guid,Holon?>();
            var conflicts=new List<CosmicConcurrencyConflict>();
            foreach(var node in plan.Nodes) {
                var id=node.Snapshot.Id;
                var observed=await _client.GetItemAsync(new GetItemRequest {
                    TableName="OasisHolons",Key=LifecycleKey(id),ConsistentRead=true
                },cancellationToken);
                RequireItemRead(observed);
                var old=observed.Item.Count==0?null:ReadStoredIdentity<Holon>(observed.Item,id);
                previous.Add(id,old);originals.Add(id,observed.Item);
                var actual=old==null?(Guid?)null:CosmicConcurrency.GetProviderRevisionId(old,StorageProviderType)??Guid.Empty;
                if(actual!=plan.ExpectedRevisions[id])conflicts.Add(new CosmicConcurrencyConflict {
                    HolonId=id,ExpectedRevisionId=plan.ExpectedRevisions[id],ActualRevisionId=actual,
                    ActualVersionId=old?.VersionId,ActualVersion=old?.Version
                });
            }
            if(conflicts.Count>0) {
                // Another contender may have committed this very operation after the first receipt read.
                receipt=await ReadGraphReceiptAsync(plan,cancellationToken);
                if(receipt!=null)return GraphAcknowledgement(receipt,request.Operation);
                throw new CosmicConcurrencyException(conflicts);
            }
            if(request.Operation==ConditionalHolonOperation.Save)
                foreach(var node in plan.Nodes)HolonSaveLifecycle.RequireCompatibleSave(previous[node.Snapshot.Id],node.Snapshot);
            var actions=new List<TransactWriteItem>();
            var acknowledged=new Dictionary<Guid,IHolon>();
            var sizes=new List<long>();
            foreach(var node in plan.Nodes) {
                var old=previous[node.Snapshot.Id];
                var proposal=request.Operation==ConditionalHolonOperation.Save?node.Snapshot:
                    Core.Serialization.StoredRecordJson.Project<Holon>(old!);
                proposal.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(node,
                    request.Operation==ConditionalHolonOperation.Save?HolonChildRoleWriteKind.SourceSave:HolonChildRoleWriteKind.SourceDelete,
                    old!=null,old?.ChildRoleManifestJson);
                proposal.Version=old==null?1:checked(old.Version+1);
                proposal.PreviousVersionId=old?.VersionId??Guid.Empty;proposal.VersionId=Guid.NewGuid();
                var now=DateTime.UtcNow;
                if(old==null) {
                    proposal.CreatedDate=now;proposal.CreatedByAvatarId=request.Write.ActorId;
                    proposal.CreatedProviderType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(StorageProviderType);
                    proposal.ModifiedDate=DateTime.MinValue;proposal.ModifiedByAvatarId=Guid.Empty;
                } else {
                    proposal.CreatedDate=old.CreatedDate;proposal.CreatedByAvatarId=old.CreatedByAvatarId;
                    proposal.CreatedProviderType=old.CreatedProviderType;
                    proposal.ModifiedDate=now;proposal.ModifiedByAvatarId=request.Write.ActorId;
                }
                proposal.InstanceSavedOnProviderType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(StorageProviderType);
                if(request.Operation!=ConditionalHolonOperation.Save) {
                    proposal.IsActive=false;proposal.DeletedDate=now;proposal.DeletedByAvatarId=request.Write.ActorId;
                }
                proposal.ProviderUniqueStorageKey??=new();
                proposal.ProviderUniqueStorageKey[StorageProviderType]=proposal.Id.ToString();
                var observed=originals[proposal.Id];
                var prior=old==null?null:OasisJson.ParseStoredObject(observed["data_json"].S);
                var staged=await StageRecordAsync(proposal,prior);
                var json=staged.ToString(Newtonsoft.Json.Formatting.None);
                acknowledged.Add(proposal.Id,OasisJson.DeserializeStored<Holon>(json));
                var names=new Dictionary<string,string>{{"#id","id"}};
                var condition="attribute_not_exists(#id)";
                Dictionary<string,AttributeValue>? values=null;
                if(old!=null) {
                    names=new(){{"#data","data_json"}};
                    condition="#data = :observed";values=new(){{":observed",new(){S=observed["data_json"].S}}};
                }
                if(request.Operation==ConditionalHolonOperation.HardDelete) {
                    sizes.Add(MeasureGraphItem(observed));
                    actions.Add(new(){Delete=new Delete {TableName="OasisHolons",Key=LifecycleKey(proposal.Id),
                        ConditionExpression=condition,ExpressionAttributeNames=names,ExpressionAttributeValues=values}});
                } else {
                    var item=new Dictionary<string,AttributeValue> {
                        ["id"]=new(){S=proposal.Id.ToString()},["data_json"]=new(){S=json},
                        ["parentHolonId"]=new(){S=proposal.ParentHolonId.ToString()},
                        ["holon_type"]=new(){N=((int)proposal.HolonType).ToString(System.Globalization.CultureInfo.InvariantCulture)},
                        ["is_deleted"]=new(){BOOL=proposal.IsDeleted}
                    };
                    sizes.Add(Math.Max(MeasureGraphItem(item),old==null?0:MeasureGraphItem(observed)));
                    actions.Add(new(){Put=new Put {TableName="OasisHolons",Item=item,ConditionExpression=condition,
                        ExpressionAttributeNames=names,ExpressionAttributeValues=values}});
                }
            }
            foreach(var edge in plan.ChildIds)acknowledged[edge.Key].Children=edge.Value.Select(id=>acknowledged[id]).ToList();
            var roots=plan.RootIds.Select(id=>acknowledged[id]).ToList();
            var receiptJson=ConditionalHolonReceipt.Write(roots,plan);
            var validated=ValidateGraphReceipt(receiptJson,plan);
            var receiptItem=new Dictionary<string,AttributeValue> {
                ["id"]=new(){S=GraphReceiptKey(plan)},["request_hash"]=new(){S=plan.Fingerprint},["result_json"]=new(){S=receiptJson}
            };
            sizes.Add(MeasureGraphItem(receiptItem));
            if(sizes.Sum()>4L*1024*1024)throw new ArgumentException("DynamoDB atomic graph exceeds the4MiB transaction limit.");
            actions.Add(new(){Put=new Put {TableName=ConditionalReceiptTable,Item=receiptItem,ConditionExpression="attribute_not_exists(#id)",
                ExpressionAttributeNames=new(){{"#id","id"}}}});
            cancellationToken.ThrowIfCancellationRequested();
            token=Guid.NewGuid().ToString("N");
            dispatched=true;
            try {
                var ack=await _client.TransactWriteItemsAsync(new TransactWriteItemsRequest {
                    TransactItems=actions,ClientRequestToken=token
                },cancellationToken);
                if(ack?.HttpStatusCode!=HttpStatusCode.OK)throw new InvalidDataException("DynamoDB atomic graph lacks an authoritative acknowledgement.");
            } catch(TransactionCanceledException cause) when(cause.CancellationReasons?.Any(r=>r.Code=="ConditionalCheckFailed")==true
                &&cause.CancellationReasons.All(r=>r.Code=="None"||r.Code=="ConditionalCheckFailed")) {
                dispatched=false; // Native all-or-nothing rejection; none of this transaction executed.
                receipt=await ReadGraphReceiptAsync(plan,cancellationToken);
                if(receipt!=null)return GraphAcknowledgement(receipt,request.Operation);
                return GraphFailure("COSMIC_CONCURRENCY_CONFLICT","DynamoDB atomic graph conditions changed before commit.",cause);
            } catch(Amazon.DynamoDBv2.AmazonDynamoDBException cause) when(IsNativeValidationRejection(cause)) {
                dispatched=false;throw;
            }
            return GraphAcknowledgement(validated,request.Operation);
        } catch(StorageLifecycleConflictException cause) {
            return GraphFailure(StorageLifecycleConflictException.ErrorCode,cause.Message,cause);
        } catch(CosmicConcurrencyException cause) {
            return GraphFailure(CosmicConcurrencyException.Code,cause.Message,cause);
        } catch(GraphOperationReuseException cause) {
            return GraphFailure("COSMIC_OPERATION_ID_REUSED",cause.Message,cause);
        } catch(Exception cause) {
            return GraphFailure(dispatched?"COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED":"COSMIC_CONDITIONAL_WRITE_FAILED",
                "DynamoDB atomic graph operation failed.",dispatched?
                    new DynamoDBMutationReconciliationException(ConditionalReceiptTable,plan==null?string.Empty:GraphReceiptKey(plan),cause,token):cause);
        }
    }

    private string GraphReceiptKey(ConditionalHolonPlan plan)=>StorageProviderType+":"+plan.OperationId.ToString("N");
    private async Task<IReadOnlyList<IHolon>?> ReadGraphReceiptAsync(ConditionalHolonPlan plan,CancellationToken token)
    {
        var key=GraphReceiptKey(plan);
        var response=await _client.GetItemAsync(new GetItemRequest {TableName=ConditionalReceiptTable,
            Key=new(){{"id",new(){S=key}}},ConsistentRead=true},token);
        RequireItemRead(response);
        if(response.Item.Count==0)return null;
        if(!response.Item.TryGetValue("id",out var id)||id.S!=key
            ||!response.Item.TryGetValue("request_hash",out var hash)||string.IsNullOrEmpty(hash.S)
            ||!response.Item.TryGetValue("result_json",out var result)||string.IsNullOrEmpty(result.S))
            throw new InvalidDataException("DynamoDB operation receipt is incomplete or has a different native key.");
        if(hash.S!=plan.Fingerprint)throw new GraphOperationReuseException();
        return ValidateGraphReceipt(result.S,plan);
    }
    private IReadOnlyList<IHolon> ValidateGraphReceipt(string json,ConditionalHolonPlan plan)
    {
        var roots=ConditionalHolonReceipt.Read(json,plan);
        var graph=HolonGraphWritePlan.Create(roots,true,true,0,0);
        foreach(var node in graph.Nodes)
            if(node.Source.ProviderUniqueStorageKey==null||!node.Source.ProviderUniqueStorageKey.TryGetValue(StorageProviderType,out var key)
                ||key!=node.Source.Id.ToString())throw new InvalidDataException("DynamoDB receipt provider key differs from its acknowledged identity.");
        return roots;
    }
    private static long MeasureGraphItem(Dictionary<string,AttributeValue> item)
    {
        var size=DynamoItemSize.MinimumBytes(item);
        if(size>DynamoItemSize.MaximumItemBytes)throw new ArgumentException("DynamoDB atomic graph item or receipt known bytes exceed400KiB.");
        return size;
    }
    private static OASISResult<IEnumerable<IHolon>> GraphAcknowledgement(IEnumerable<IHolon> roots,ConditionalHolonOperation operation)
        =>ConditionalHolonAcknowledgement.Create(roots,operation);
    private static OASISResult<IEnumerable<IHolon>> GraphFailure(string code,string message,Exception cause)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
    private sealed class GraphOperationReuseException:Exception
    {
        public GraphOperationReuseException():base("Operation ID is bound to different request content."){}
    }
}

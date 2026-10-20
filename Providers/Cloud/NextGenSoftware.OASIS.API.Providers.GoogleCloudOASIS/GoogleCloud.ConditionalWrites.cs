using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS : IConditionalHolonStorageProvider
{
    private const string ConditionalReceiptCollection="cosmicWriteReceipts";
    private const string ConditionalFenceCollection="cosmicOperationFences";
    public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
        =>ExecuteConditionalHolonWriteAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(
        ConditionalHolonWriteRequest request,CancellationToken cancellationToken=default)
    {
        var dispatched=false;var path=string.Empty;
        try {
            cancellationToken.ThrowIfCancellationRequested();
            var plan=ConditionalHolonPlan.Create(request,Core.Enums.ProviderType.GoogleCloudOASIS);
            if(!_enableFirestore)throw new NotSupportedException("Atomic graph writes require enabled Firestore.");
            var ready=await EnsureDetailStoreAsync(0);
            if(ready.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IEnumerable<IHolon>>(ready);
            RequireSingleAttemptFirestoreCommit(_firestoreDb);
            var key=ConditionalOperationKey(plan.OperationId);
            var receiptReference=_firestoreDb.Collection(ConditionalReceiptCollection).Document(key);path=receiptReference.Path;
            var priorReceipt=await ReadFirestoreConditionalReceiptAsync(receiptReference,plan,cancellationToken);
            if(priorReceipt!=null)return FirestoreGraphAcknowledgement(priorReceipt,plan.Operation);
            var fenceReference=_firestoreDb.Collection(ConditionalFenceCollection).Document(key);
            var fence=await fenceReference.GetSnapshotAsync(cancellationToken);
            ValidateOperationFence(fence,plan.OperationId);
            var snapshots=new Dictionary<Guid,DocumentSnapshot>();
            var previous=new Dictionary<Guid,Holon>();
            var conflicts=new List<CosmicConcurrencyConflict>();
            foreach(var node in plan.Nodes) {
                var id=node.Snapshot.Id;
                var snapshot=await _firestoreDb.Collection("holons").Document(id.ToString()).GetSnapshotAsync(cancellationToken);
                snapshots.Add(id,snapshot);
                var old=snapshot.Exists?await ReadFirestoreHistoryAsync<Holon>(snapshot.ToDictionary(),id.ToString(),0):null;
                previous.Add(id,old);
                var actual=old==null?(Guid?)null:CosmicConcurrency.GetProviderRevisionId(old,Core.Enums.ProviderType.GoogleCloudOASIS)??Guid.Empty;
                if(actual!=plan.ExpectedRevisions[id])conflicts.Add(new CosmicConcurrencyConflict {
                    HolonId=id,ExpectedRevisionId=plan.ExpectedRevisions[id],ActualRevisionId=actual,
                    ActualVersionId=old?.VersionId,ActualVersion=old?.Version
                });
            }
            if(conflicts.Count>0) {
                priorReceipt=await ReadFirestoreConditionalReceiptAsync(receiptReference,plan,cancellationToken);
                if(priorReceipt!=null)return FirestoreGraphAcknowledgement(priorReceipt,plan.Operation);
                throw new CosmicConcurrencyException(conflicts);
            }
            var batch=_firestoreDb.StartBatch();var acknowledged=new Dictionary<Guid,IHolon>();
            long requestBytes=0;var writes=0;
            foreach(var node in plan.Nodes) {
                var snapshot=snapshots[node.Snapshot.Id];var old=previous[node.Snapshot.Id];
                var proposal=plan.Operation==ConditionalHolonOperation.Save?node.Snapshot:Core.Serialization.StoredRecordJson.Project<Holon>(old);
                proposal.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(node,
                    plan.Operation==ConditionalHolonOperation.Save?HolonChildRoleWriteKind.SourceSave:HolonChildRoleWriteKind.SourceDelete,
                    old!=null,old?.ChildRoleManifestJson);
                proposal.Version=old==null?1:checked(old.Version+1);
                proposal.PreviousVersionId=old?.VersionId??Guid.Empty;proposal.VersionId=Guid.NewGuid();
                var now=DateTime.UtcNow;
                if(old==null) {
                    proposal.CreatedDate=now;proposal.CreatedByAvatarId=request.Write.ActorId;
                    proposal.CreatedProviderType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.GoogleCloudOASIS);
                    proposal.ModifiedDate=DateTime.MinValue;proposal.ModifiedByAvatarId=Guid.Empty;
                } else {
                    proposal.CreatedDate=old.CreatedDate;proposal.CreatedByAvatarId=old.CreatedByAvatarId;
                    proposal.CreatedProviderType=old.CreatedProviderType;proposal.ModifiedDate=now;proposal.ModifiedByAvatarId=request.Write.ActorId;
                }
                proposal.InstanceSavedOnProviderType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.GoogleCloudOASIS);
                if(plan.Operation!=ConditionalHolonOperation.Save) {
                    proposal.IsActive=false;proposal.DeletedDate=now;proposal.DeletedByAvatarId=request.Write.ActorId;
                }
                proposal.ProviderUniqueStorageKey??=new();
                proposal.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS]=proposal.Id.ToString();
                var staged=await StageFirestoreDocumentAsync(proposal,snapshot.Exists?snapshot.ToDictionary():null,proposal.Id.ToString(),IsVersionControlEnabled);
                var json=staged.ToString(Newtonsoft.Json.Formatting.None);
                var committed=OasisJson.DeserializeStored<Holon>(json);acknowledged.Add(committed.Id,committed);
                if(plan.Operation==ConditionalHolonOperation.HardDelete) {
                    batch.Delete(snapshot.Reference,ObservedFirestoreRevision(snapshot));
                    requestBytes+=1024+Encoding.UTF8.GetByteCount(snapshot.Reference.Path);
                } else {
                    var fields=CreateFirestoreHolonFields(committed,json);
                    requestBytes+=MeasureFirestoreGraphDocument(snapshot.Reference,fields);
                    if(snapshot.Exists) {
                        var updates=fields.ToDictionary(pair=>new FieldPath(pair.Key),pair=>pair.Value);
                        foreach(var name in snapshot.ToDictionary().Keys.Where(name=>!fields.ContainsKey(name))) {
                            updates[new FieldPath(name)]=FieldValue.Delete;requestBytes+=64+Encoding.UTF8.GetByteCount(name);
                        }
                        batch.Update(snapshot.Reference,updates,ObservedFirestoreRevision(snapshot));
                    } else batch.Create(snapshot.Reference,fields);
                }
                writes++;
            }
            foreach(var edge in plan.ChildIds)acknowledged[edge.Key].Children=edge.Value.Select(id=>acknowledged[id]).ToList();
            var roots=plan.RootIds.Select(id=>acknowledged[id]).ToList();
            var receiptJson=ConditionalHolonReceipt.Write(roots,plan);
            var validated=ValidateFirestoreConditionalReceipt(receiptJson,plan);
            var receiptFields=new Dictionary<string,object> {
                ["operationId"]=plan.OperationId.ToString("D"),["requestHash"]=plan.Fingerprint,["resultJson"]=receiptJson
            };
            requestBytes+=MeasureFirestoreGraphDocument(receiptReference,receiptFields);
            batch.Create(receiptReference,receiptFields);writes++;
            var fenceFields=new Dictionary<string,object> {
                ["operationId"]=plan.OperationId.ToString("D"),["revision"]=Guid.NewGuid().ToString("D")
            };
            requestBytes+=MeasureFirestoreGraphDocument(fenceReference,fenceFields);
            if(fence.Exists)batch.Update(fenceReference,fenceFields,ObservedFirestoreRevision(fence));
            else batch.Create(fenceReference,fenceFields);
            writes++;
            if(requestBytes>10L*1024*1024)throw new ArgumentException("Firestore atomic graph exceeds its conservative10MiB request budget.");
            cancellationToken.ThrowIfCancellationRequested();dispatched=true;
            try {
                var results=await CommitFirestoreConditionalBatchAsync(batch,cancellationToken);
                if(results==null||results.Count!=writes)throw new InvalidDataException("Firestore atomic graph lacks complete write acknowledgements.");
                foreach(var result in results)RequireFirestoreAcknowledgement(result);
            } catch(RpcException cause) when(cause.StatusCode is StatusCode.AlreadyExists or StatusCode.FailedPrecondition or StatusCode.NotFound or StatusCode.Aborted) {
                dispatched=false; // Native atomic commit rejected: none of this batch executed.
                priorReceipt=await ReadFirestoreConditionalReceiptAsync(receiptReference,plan,cancellationToken);
                if(priorReceipt!=null)return FirestoreGraphAcknowledgement(priorReceipt,plan.Operation);
                return FirestoreGraphFailure(CosmicConcurrencyException.Code,"Firestore graph conditions changed before commit.",cause);
            } catch(RpcException cause) when(cause.StatusCode==StatusCode.InvalidArgument) {dispatched=false;throw;}
            return FirestoreGraphAcknowledgement(validated,plan.Operation);
        } catch(CosmicConcurrencyException cause) {
            return FirestoreGraphFailure(CosmicConcurrencyException.Code,cause.Message,cause);
        } catch(StorageLifecycleConflictException cause) {
            return FirestoreGraphFailure(StorageLifecycleConflictException.ErrorCode,cause.Message,cause);
        } catch(FirestoreOperationReuseException cause) {
            return FirestoreGraphFailure("COSMIC_OPERATION_ID_REUSED",cause.Message,cause);
        } catch(Exception cause) {
            return FirestoreGraphFailure(dispatched?"COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED":"COSMIC_CONDITIONAL_WRITE_FAILED",
                "Firestore atomic graph operation failed.",dispatched?new GoogleFirestoreMutationReconciliationException(path,cause):cause);
        }
    }
    protected virtual Task<IList<WriteResult>> CommitFirestoreConditionalBatchAsync(WriteBatch batch,CancellationToken token)
        =>batch.CommitAsync(token);
    private static string ConditionalOperationKey(Guid operation)=>"GoogleCloudOASIS:"+operation.ToString("N");
    private static Precondition ObservedFirestoreRevision(DocumentSnapshot snapshot)
    {
        if(!snapshot.Exists||!snapshot.UpdateTime.HasValue||snapshot.UpdateTime.Value.Equals(default(Timestamp)))
            throw new InvalidDataException("Firestore observed document lacks a native revision.");
        return Precondition.LastUpdated(snapshot.UpdateTime.Value);
    }
    private static void ValidateOperationFence(DocumentSnapshot snapshot,Guid operation)
    {
        if(!snapshot.Exists)return;
        var fields=snapshot.ToDictionary();
        if(!fields.TryGetValue("operationId",out var id)||!Equals(id,operation.ToString("D"))
            ||!fields.TryGetValue("revision",out var value)||value is not string text
            ||!Guid.TryParseExact(text,"D",out var revision)||revision==Guid.Empty||text!=revision.ToString("D"))
            throw new InvalidDataException("Firestore operation fence has invalid identity/revision.");
        _=ObservedFirestoreRevision(snapshot);
    }
    private async Task<IReadOnlyList<IHolon>> ReadFirestoreConditionalReceiptAsync(DocumentReference reference,ConditionalHolonPlan plan,CancellationToken token)
    {
        var snapshot=await reference.GetSnapshotAsync(token);if(!snapshot.Exists)return null;
        var fields=snapshot.ToDictionary();
        if(snapshot.Id!=ConditionalOperationKey(plan.OperationId)||!fields.TryGetValue("operationId",out var id)||!Equals(id,plan.OperationId.ToString("D"))
            ||!fields.TryGetValue("requestHash",out var hash)||hash is not string fingerprint||string.IsNullOrEmpty(fingerprint)
            ||!fields.TryGetValue("resultJson",out var value)||value is not string json||string.IsNullOrEmpty(json))
            throw new InvalidDataException("Firestore operation receipt is incomplete or has a different identity.");
        if(fingerprint!=plan.Fingerprint)throw new FirestoreOperationReuseException();
        return ValidateFirestoreConditionalReceipt(json,plan);
    }
    private static IReadOnlyList<IHolon> ValidateFirestoreConditionalReceipt(string json,ConditionalHolonPlan plan)
    {
        var roots=ConditionalHolonReceipt.Read(json,plan);
        foreach(var node in HolonGraphWritePlan.Create(roots,true,true,0,0).Nodes)
            if(node.Source.ProviderUniqueStorageKey==null||!node.Source.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.GoogleCloudOASIS,out var key)
                ||key!=node.Source.Id.ToString())throw new InvalidDataException("Firestore receipt provider key differs from its acknowledged identity.");
        return roots;
    }
    private static long MeasureFirestoreGraphDocument(DocumentReference reference,Dictionary<string,object> fields)
    {
        long size=1024+Encoding.UTF8.GetByteCount(reference.Path);
        foreach(var pair in fields) {
            size+=64+Encoding.UTF8.GetByteCount(pair.Key);
            if(pair.Value is string text)size+=Encoding.UTF8.GetByteCount(text);
            else if(pair.Value!=null&&pair.Value is not (bool or int or long or double or Timestamp))
                throw new ArgumentException("Firestore graph fields must use the existing scalar projection.");
        }
        if(size>1024L*1024)throw new ArgumentException("Firestore graph document or receipt exceeds its conservative1MiB budget.");
        return size;
    }
    private static OASISResult<IEnumerable<IHolon>> FirestoreGraphAcknowledgement(IEnumerable<IHolon> roots,ConditionalHolonOperation operation)
        =>ConditionalHolonAcknowledgement.Create(roots,operation);
    private static OASISResult<IEnumerable<IHolon>> FirestoreGraphFailure(string code,string message,Exception cause)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
    private sealed class FirestoreOperationReuseException:Exception
    {
        public FirestoreOperationReuseException():base("Operation ID is bound to different request content."){}
    }
}

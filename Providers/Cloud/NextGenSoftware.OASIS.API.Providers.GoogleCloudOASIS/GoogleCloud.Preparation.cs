using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS : IHolonWritePreparationStorageProvider
{
    private const string PreparationCollection="cosmicWritePreparations";
    public OASISResult<HolonWritePreparation> ClaimHolonWritePreparation(HolonWritePreparation candidate)
        =>ClaimHolonWritePreparationAsync(candidate).GetAwaiter().GetResult();
    public async Task<OASISResult<HolonWritePreparation>> ClaimHolonWritePreparationAsync(
        HolonWritePreparation candidate,CancellationToken cancellationToken=default)
    {
        var dispatched=false;var path=string.Empty;
        try {
            cancellationToken.ThrowIfCancellationRequested();ArgumentNullException.ThrowIfNull(candidate);
            if(candidate.Authority!=Core.Enums.ProviderType.GoogleCloudOASIS)
                return FirestorePreparationFailure("COSMIC_AUTHORITY_MISMATCH","The preparation belongs to another authority.");
            if(!_enableFirestore)throw new NotSupportedException("Protected preparation claims require enabled Firestore.");
            var payload=ProtectedPreparationJson.Write(candidate);
            if(Encoding.UTF8.GetByteCount(payload)+2048L>1024L*1024)
                throw new ArgumentException("Firestore protected preparation exceeds its conservative1MiB document budget.");
            var ready=await EnsureDetailStoreAsync(0);
            if(ready.IsError)return Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,HolonWritePreparation>(ready);
            RequireSingleAttemptFirestoreCommit(_firestoreDb);
            var key=ConditionalOperationKey(candidate.OperationId);
            var reference=_firestoreDb.Collection(PreparationCollection).Document(key);path=reference.Path;
            var prior=await ReadFirestorePreparationWinnerAsync(reference,candidate,cancellationToken);
            if(prior!=null)return new(prior);
            var fenceReference=_firestoreDb.Collection(ConditionalFenceCollection).Document(key);
            var receiptReference=_firestoreDb.Collection(ConditionalReceiptCollection).Document(key);
            var fence=await fenceReference.GetSnapshotAsync(cancellationToken);
            ValidateOperationFence(fence,candidate.OperationId);
            if((await receiptReference.GetSnapshotAsync(cancellationToken)).Exists)
                return MissingFirestorePreparation(candidate.OperationId,path);
            var fields=new Dictionary<string,object> {
                ["operationId"]=candidate.OperationId.ToString("D"),["actorId"]=candidate.ActorId.ToString("D"),
                ["authority"]=candidate.Authority.ToString(),["payloadJson"]=payload
            };
            _=MeasureFirestoreGraphDocument(reference,fields);
            var fenceFields=new Dictionary<string,object> {
                ["operationId"]=candidate.OperationId.ToString("D"),["revision"]=Guid.NewGuid().ToString("D")
            };
            var batch=_firestoreDb.StartBatch();
            batch.Create(reference,fields);
            if(fence.Exists)batch.Update(fenceReference,fenceFields,ObservedFirestoreRevision(fence));
            else batch.Create(fenceReference,fenceFields);
            cancellationToken.ThrowIfCancellationRequested();dispatched=true;
            try {
                var results=await CommitFirestoreConditionalBatchAsync(batch,cancellationToken);
                if(results==null||results.Count!=2)throw new InvalidDataException("Firestore preparation lacks both native write acknowledgements.");
                foreach(var result in results)RequireFirestoreAcknowledgement(result);
            } catch(RpcException cause) when(cause.StatusCode is StatusCode.AlreadyExists or StatusCode.FailedPrecondition or StatusCode.NotFound or StatusCode.Aborted) {
                dispatched=false;
                prior=await ReadFirestorePreparationWinnerAsync(reference,candidate,cancellationToken);
                if(prior!=null)return new(prior);
                if((await receiptReference.GetSnapshotAsync(cancellationToken)).Exists)
                    return MissingFirestorePreparation(candidate.OperationId,path);
                return FirestorePreparationFailure("COSMIC_PREPARATION_CONCURRENT_CLAIM",
                    "Native operation coordination changed; no preparation was committed by this attempt.",cause);
            } catch(RpcException cause) when(cause.StatusCode==StatusCode.InvalidArgument) {dispatched=false;throw;}
            return new(candidate);
        } catch(FirestorePreparationActorException cause) {
            return FirestorePreparationFailure("COSMIC_PREPARATION_ACTOR_MISMATCH",cause.Message,cause);
        } catch(Exception cause) {
            return FirestorePreparationFailure(dispatched?"COSMIC_PREPARATION_RECONCILIATION_REQUIRED":
                cause is OperationCanceledException?"COSMIC_PREPARATION_CANCELLED":"COSMIC_PREPARATION_FAILED",
                "Firestore protected preparation claim failed.",dispatched?new GoogleFirestoreMutationReconciliationException(path,cause):cause);
        }
    }
    private static OASISResult<HolonWritePreparation> MissingFirestorePreparation(Guid operation,string path)
        =>FirestorePreparationFailure("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",
            "This operation has a receipt but no original protected preparation.",
            new GoogleFirestoreMutationReconciliationException(path,new InvalidDataException("Original protected preparation is unavailable for operation "+operation.ToString("D")+".")));
    private static async Task<HolonWritePreparation> ReadFirestorePreparationWinnerAsync(
        DocumentReference reference,HolonWritePreparation candidate,CancellationToken token)
    {
        var snapshot=await reference.GetSnapshotAsync(token);if(!snapshot.Exists)return null;
        var fields=snapshot.ToDictionary();
        if(snapshot.Id!=ConditionalOperationKey(candidate.OperationId)
            ||!fields.TryGetValue("operationId",out var operation)||!Equals(operation,candidate.OperationId.ToString("D"))
            ||!fields.TryGetValue("authority",out var authority)||!Equals(authority,candidate.Authority.ToString())
            ||!fields.TryGetValue("actorId",out var actor)||actor is not string text
            ||!Guid.TryParseExact(text,"D",out var owner)||owner==Guid.Empty||text!=owner.ToString("D"))
            throw new InvalidDataException("Firestore preparation ownership is invalid.");
        if(owner!=candidate.ActorId)throw new FirestorePreparationActorException();
        if(!fields.TryGetValue("payloadJson",out var value)||value is not string payload
            ||payload.Length>ProtectedPreparationJson.MaximumPayloadCharacters)
            throw new InvalidDataException("Firestore protected preparation payload is invalid.");
        return ProtectedPreparationJson.Read(candidate,payload);
    }
    private static OASISResult<HolonWritePreparation> FirestorePreparationFailure(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
    private sealed class FirestorePreparationActorException:Exception
    {
        public FirestorePreparationActorException():base("The operation is owned by another actor."){}
    }
}

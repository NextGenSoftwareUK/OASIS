using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Protobuf;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Native=Google.Cloud.Firestore.V1;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    private static readonly Google.Api.Gax.Grpc.CallSettings IdentityTransactionCallSettings=
        Google.Api.Gax.Grpc.CallSettings.FromRetry(Google.Api.Gax.Grpc.RetrySettings.FromConstantBackoff(1,TimeSpan.Zero,_=>false));

    // Explicit SDK protocol ownership: no callback re-execution on an expired transaction.
    // Both identity documents are read in profile/detail order before any write.
    private static async Task<T> WriteFirestoreIdentityAsync<T>(DocumentReference reference,Dictionary<string,object> fields,T proposed,bool history,Action validateCaller) where T:class,IHolonBase
    {
        var database=reference.Database;
        return await RunFirestoreIdentityTransactionAsync<T>(database,reference.Path,async transaction=>{
            var profile=await ReadFirestoreIdentityDocumentAsync(database.Collection("avatars").Document(reference.Id),transaction);
            var detail=await ReadFirestoreIdentityDocumentAsync(database.Collection("avatarDetails").Document(reference.Id),transaction);
            var own=reference.Parent.Id=="avatars"?profile:detail;
            var related=reference.Parent.Id=="avatars"?detail:profile;
            if(related!=null){
                IHolonBase relatedRecord=reference.Parent.Id=="avatars"
                    ?await ReadFirestoreHistoryAsync<AvatarDetail>(DecodeFirestoreIdentityFields(related),reference.Id,0)
                    :await ReadFirestoreHistoryAsync<Avatar>(DecodeFirestoreIdentityFields(related),reference.Id,0);
                if(relatedRecord.IsDeleted&&!proposed.IsDeleted)
                    throw new StorageLifecycleConflictException("Active save conflicts with a committed related Firestore identity tombstone.");
            }
            var committed=await StageFirestoreFieldsAsync(proposed,own==null?null:DecodeFirestoreIdentityFields(own),reference.Id,history,fields);
            var document=new Native.Document{Name=reference.Path};
            foreach(var field in fields)document.Fields.Add(field.Key,EncodeFirestoreIdentityValue(field.Value));
            var condition=own==null?new Native.Precondition{Exists=false}:new Native.Precondition{UpdateTime=own.UpdateTime};
            return (committed,new[]{new Native.Write{Update=document,CurrentDocument=condition}});
        },validateCaller);
    }

    // One protocol owner for identity saves and paired lifecycle writes. Preparation executes ONCE.
    private static async Task<T> RunFirestoreIdentityTransactionAsync<T>(FirestoreDb database,string paths,
        Func<ByteString,Task<(T Result,IReadOnlyList<Native.Write> Writes)>> prepare,Action validateCaller=null)
    {
        RequireSingleAttemptFirestoreCommit(database);
        var client=database.Client;var databasePath=$"projects/{database.ProjectId}/databases/{database.DatabaseId}";
        var opened=await client.BeginTransactionAsync(new Native.BeginTransactionRequest{Database=databasePath},IdentityTransactionCallSettings);
        var transaction=opened?.Transaction;
        if(transaction==null||transaction.IsEmpty)throw new InvalidDataException("Firestore identity transaction lacks its native transaction ID.");
        var dispatched=false;var rejected=false;var rollbackAttempted=false;
        try{
            var prepared=await prepare(transaction);
            if(prepared.Writes==null)throw new InvalidDataException("Firestore identity preparation returned no write plan.");
            validateCaller?.Invoke();
            if(prepared.Writes.Count==0){
                rollbackAttempted=true;
                await client.RollbackAsync(new Native.RollbackRequest{Database=databasePath,Transaction=transaction},IdentityTransactionCallSettings);
                return prepared.Result;
            }
            var request=new Native.CommitRequest{Database=databasePath,Transaction=transaction};request.Writes.Add(prepared.Writes);
            dispatched=true;
            Native.CommitResponse acknowledgement;
            try{acknowledgement=await client.CommitAsync(request,IdentityTransactionCallSettings);}
            catch(RpcException cause)when(cause.StatusCode is StatusCode.AlreadyExists or StatusCode.FailedPrecondition or StatusCode.NotFound or StatusCode.Aborted or StatusCode.InvalidArgument){rejected=true;throw;}
            if(acknowledgement?.CommitTime==null||Timestamp.FromProto(acknowledgement.CommitTime).Equals(default(Timestamp))
                ||acknowledgement.WriteResults.Count!=prepared.Writes.Count)
                throw new InvalidDataException("Firestore identity transaction lacks complete native commit acknowledgement.");
            for(var index=0;index<acknowledgement.WriteResults.Count;index++){
                var result=acknowledgement.WriteResults[index];
                // Firestore omits update_time on deletes; their individual result plus CommitTime acknowledges deletion.
                if(result.UpdateTime==null&&prepared.Writes[index].OperationCase==Native.Write.OperationOneofCase.Delete)continue;
                if(result.UpdateTime==null||Timestamp.FromProto(result.UpdateTime).Equals(default(Timestamp)))
                    throw new InvalidDataException("Firestore identity transaction lacks a native write acknowledgement timestamp.");
            }
            return prepared.Result;
        }catch(Exception cause){
            if(dispatched&&!rejected)
                throw new GoogleFirestoreMutationReconciliationException(paths,cause,transaction.ToBase64());
            if(rollbackAttempted)throw;
            try{await client.RollbackAsync(new Native.RollbackRequest{Database=databasePath,Transaction=transaction},IdentityTransactionCallSettings);}
            catch(Exception rollback){throw new AggregateException("Firestore identity operation failed and native rollback was not acknowledged.",cause,rollback);}
            throw;
        }
    }

    private static async Task<Native.Document> ReadFirestoreIdentityDocumentAsync(DocumentReference reference,ByteString transaction)
    {
        var database=reference.Database;
        var request=new Native.BatchGetDocumentsRequest{Database=$"projects/{database.ProjectId}/databases/{database.DatabaseId}",Transaction=transaction,Mask=new Native.DocumentMask()};
        request.Documents.Add(reference.Path);
        request.Mask.FieldPaths.Add(new[]{"id","data_json","version","isActive","isDeleted","username","email","previousVersionId","nextVersionId"});
        // GAX server-streaming calls prohibit Retry settings entirely; there is no automatic stream replay.
        using var call=database.Client.BatchGetDocuments(request);
        var stream=call.GetResponseStream();Native.Document document=null;var observed=false;
        while(await stream.MoveNextAsync()){
            var response=stream.Current;
            if(observed)throw new InvalidDataException("Firestore identity point read returned multiple documents.");
            if(response.ResultCase==Native.BatchGetDocumentsResponse.ResultOneofCase.Found){
                document=response.Found;
                if(document.Name!=reference.Path||document.UpdateTime==null||Timestamp.FromProto(document.UpdateTime).Equals(default(Timestamp)))
                    throw new InvalidDataException("Firestore identity point read lacks matching identity/revision.");
            }else if(response.ResultCase!=Native.BatchGetDocumentsResponse.ResultOneofCase.Missing||response.Missing!=reference.Path)
                throw new InvalidDataException("Firestore identity point read lacks a matching found/missing result.");
            observed=true;
        }
        if(!observed)throw new InvalidDataException("Firestore identity point read returned no result.");
        return document;
    }

    // Only native scalar projections are adapted here; full records retain the shared canonical codec.
    private static Dictionary<string,object> DecodeFirestoreIdentityFields(Native.Document document)
    {
        var fields=new Dictionary<string,object>();
        foreach(var field in document.Fields)fields.Add(field.Key,field.Value.ValueTypeCase switch{
            Native.Value.ValueTypeOneofCase.NullValue=>null,
            Native.Value.ValueTypeOneofCase.StringValue=>field.Value.StringValue,
            Native.Value.ValueTypeOneofCase.BooleanValue=>field.Value.BooleanValue,
            Native.Value.ValueTypeOneofCase.IntegerValue=>field.Value.IntegerValue,
            Native.Value.ValueTypeOneofCase.DoubleValue=>field.Value.DoubleValue,
            Native.Value.ValueTypeOneofCase.TimestampValue=>Timestamp.FromProto(field.Value.TimestampValue),
            _=>throw new InvalidDataException("Firestore identity projection contains a non-scalar native value.")
        });
        return fields;
    }
    private static Native.Value EncodeFirestoreIdentityValue(object value)=>value switch{
        null=>new(){NullValue=Google.Protobuf.WellKnownTypes.NullValue.NullValue},
        string text=>new(){StringValue=text},bool flag=>new(){BooleanValue=flag},
        int number=>new(){IntegerValue=number},long number=>new(){IntegerValue=number},double number=>new(){DoubleValue=number},
        Timestamp timestamp=>new(){TimestampValue=timestamp.ToProto()},
        _=>throw new InvalidDataException("Firestore identity proposal contains a non-scalar native value.")
    };
}

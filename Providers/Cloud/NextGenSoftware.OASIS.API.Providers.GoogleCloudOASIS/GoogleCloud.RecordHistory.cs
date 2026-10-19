using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Google.Cloud.Firestore;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    private static void HandleFirestoreSaveError<T>(ref NextGenSoftware.OASIS.Common.OASISResult<T> result,string message,Exception cause)
    {
        NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,message,cause);
        // A definite create/revision rejection is not provider unavailability. Keep the original SDK cause.
        if(cause is Grpc.Core.RpcException rpc && rpc.StatusCode is Grpc.Core.StatusCode.AlreadyExists or Grpc.Core.StatusCode.FailedPrecondition or Grpc.Core.StatusCode.NotFound)
            result.ErrorCode=NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode;
    }
    private static readonly EmbeddedRecordHistory FirestoreHistory=new();

    // The existing transaction owns native atomicity. Never discard an existing archive during soft deletion.
    private static async Task<Dictionary<string,object>> StageFirestoreLifecycleUpdateAsync<T>(T proposed,Dictionary<string,object> previous,string documentId,bool createHistory) where T:class,IHolonBase
    {
        var current=CanonicalFirestoreHistoryDocument<T>(previous,documentId);
        await FirestoreHistory.ReadAsync(Guid.Parse(documentId),current,0,OasisJson.DeserializeStored<T>);
        if(proposed.Id.ToString()!=documentId || proposed.IsActive || !proposed.IsDeleted)
            throw new InvalidDataException("Firestore lifecycle stage requires the matching soft-deleted record.");
        var staged=createHistory||current.Property(EmbeddedRecordHistory.PropertyName)!=null
            ? await StageFirestoreHistoryAsync(proposed,previous,documentId)
            : JObject.Parse(OasisJson.Serialize(proposed));
        var record=OasisJson.DeserializeStored<T>(staged.ToString(Newtonsoft.Json.Formatting.None));
        var update=new Dictionary<string,object>{["isActive"]=false,["isDeleted"]=true,["data_json"]=staged.ToString(Newtonsoft.Json.Formatting.None)};
        if(previous.ContainsKey("version") || staged.Property(EmbeddedRecordHistory.PropertyName)!=null)update["version"]=record.Version;
        if(previous.ContainsKey("previousVersionId"))update["previousVersionId"]=record.PreviousVersionId.ToString();
        if(previous.ContainsKey("nextVersionId"))update["nextVersionId"]=record.VersionId.ToString();
        return update;
    }

    // One conditional writer for ordinary saves; native acceptance is required before publishing this batch.
    private static async Task<T> WriteFirestoreRecordAsync<T>(DocumentReference reference,Dictionary<string,object> proposal,bool createHistory) where T:class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(reference);ArgumentNullException.ThrowIfNull(proposal);
        RequireSingleAttemptFirestoreCommit(reference.Database);
        var collection=reference.Parent.Id;
        var expected=collection=="avatars"?typeof(Core.Holons.Avatar):collection=="avatarDetails"?typeof(Core.Holons.AvatarDetail):collection=="holons"?typeof(Core.Holons.Holon):null;
        if(expected!=typeof(T))throw new ArgumentException("Firestore canonical record kind differs from collection.");
        // Canonical projections consist only of immutable native scalar values. Reject, don't shallow-copy mutable maps.
        var fields=new Dictionary<string,object>();
        foreach(var pair in proposal){
            var value=pair.Value;
            if(value!=null && value is not (string or bool or int or long or double or Timestamp))
                throw new ArgumentException("Canonical Firestore projection must use immutable native scalar values.");
            fields.Add(pair.Key,value);
        }
        _=CanonicalFirestoreHistoryDocument<T>(fields,reference.Id);
        var proposed=OasisJson.DeserializeStored<T>((string)fields["data_json"]);
        var snapshot=await reference.GetSnapshotAsync();
        var previous=snapshot.Exists?snapshot.ToDictionary():null;
        if(previous!=null)await ReadFirestoreHistoryAsync<T>(previous,reference.Id,0);
        var hasHistory=previous!=null && JObject.Parse((string)previous["data_json"]).Property(EmbeddedRecordHistory.PropertyName)!=null;
        var staged=createHistory||hasHistory
            ? await StageFirestoreHistoryAsync(proposed,previous,reference.Id)
            : JObject.Parse(OasisJson.Serialize(proposed));
        var committed=OasisJson.DeserializeStored<T>(staged.ToString(Newtonsoft.Json.Formatting.None));
        fields["data_json"]=staged.ToString(Newtonsoft.Json.Formatting.None);
        fields["version"]=committed.Version;
        if(fields.ContainsKey("previousVersionId"))fields["previousVersionId"]=committed.PreviousVersionId.ToString();
        if(fields.ContainsKey("nextVersionId"))fields["nextVersionId"]=committed.VersionId.ToString();
        Dictionary<FieldPath,object> replacement=null;
        if(snapshot.Exists){
            if(!snapshot.UpdateTime.HasValue || snapshot.UpdateTime.Value.Equals(default(Timestamp)))
                throw new InvalidDataException("Firestore observed document lacks an authoritative revision.");
            replacement=fields.ToDictionary(pair=>new FieldPath(pair.Key),pair=>pair.Value);
            // Update with an exact observed revision and delete removed fields, preserving Set's replacement semantics.
            foreach(var key in previous.Keys.Where(key=>!fields.ContainsKey(key)))replacement[new FieldPath(key)]=FieldValue.Delete;
        }
        try{
            RequireFirestoreAcknowledgement(snapshot.Exists
                ? await reference.UpdateAsync(replacement,Precondition.LastUpdated(snapshot.UpdateTime.Value))
                : await reference.CreateAsync(fields));
            return committed;
        }
        // Requires production single-attempt Commit settings. No native mutation retry or merge here.
        catch(Grpc.Core.RpcException cause) when(cause.StatusCode is Grpc.Core.StatusCode.AlreadyExists or Grpc.Core.StatusCode.FailedPrecondition or Grpc.Core.StatusCode.NotFound){throw;}
        catch(Exception cause){throw new GoogleFirestoreMutationReconciliationException(reference.Path,cause);}
    }

    private static void RequireSingleAttemptFirestoreCommit(FirestoreDb database)
    {
        if(database?.Client.Settings?.CommitSettings?.Retry?.MaxAttempts!=1)
            throw new InvalidOperationException("Conditional Firestore writes require explicit single-attempt Commit settings before native I/O.");
    }

    private static Google.Cloud.Firestore.V1.FirestoreSettings CreateFirestoreSettings()
    {
        var settings=Google.Cloud.Firestore.V1.FirestoreSettings.GetDefault();
        // Commit's SDK default retries Unavailable/ResourceExhausted up to five attempts.
        // An unknown first commit must not be replayed and confused with a later CAS rejection.
        settings.CommitSettings=Google.Api.Gax.Grpc.CallSettingsExtensions.WithRetry(settings.CommitSettings,
            Google.Api.Gax.Grpc.RetrySettings.FromConstantBackoff(1,TimeSpan.Zero,_=>false));
        return settings;
    }

    // Pure detached staging; native snapshot conditions/commit acknowledgement belong to the SDK writer.
    // Ordinary saves preserve existing archives; positive public versions remain gated pending native acceptance.
    private static async Task<JObject> StageFirestoreHistoryAsync<T>(T proposed,Dictionary<string,object> previous,string documentId) where T:class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(proposed);
        if(proposed.Id==Guid.Empty || proposed.Id.ToString()!=documentId)
            throw new InvalidDataException("Firestore proposed identity differs from document key.");
        JObject prior=null;
        if(previous!=null){
            prior=CanonicalFirestoreHistoryDocument<T>(previous,documentId);
            // Validate the complete existing archive even before a new stage is attempted.
            await FirestoreHistory.ReadAsync(proposed.Id,prior,0,OasisJson.DeserializeStored<T>);
        }
        return await FirestoreHistory.StageAsync(proposed,prior,OasisJson.DeserializeStored<T>);
    }

    private static JObject CanonicalFirestoreHistoryDocument<T>(Dictionary<string,object> fields,string documentId) where T:class,IHolonBase
    {
        if(!Guid.TryParse(documentId,out var identity)||identity==Guid.Empty
            ||!fields.TryGetValue("id",out var nativeId)||!Equals(nativeId,documentId)
            ||!fields.TryGetValue("data_json",out var payload)||payload is not string json)
            throw new InvalidDataException("Firestore canonical identity/payload is invalid.");
        var record=OasisJson.DeserializeStored<T>(json);
        if(record==null||record.Id!=identity)throw new InvalidDataException("Firestore canonical record differs from document key.");
        // Optional native projections may be absent on canonical legacy rows, but must never contradict JSON.
        if(fields.TryGetValue("version",out var projectedVersion)){
            var version=projectedVersion is int integer?(long)integer:projectedVersion is long nativeInteger?nativeInteger:(long?)null;
            if(version!=record.Version)throw new InvalidDataException("Firestore native version projection differs from canonical record.");
        }
        if(fields.TryGetValue("isActive",out var active) && (active is not bool activeValue || activeValue!=record.IsActive))
            throw new InvalidDataException("Firestore native active projection differs from canonical record.");
        if(fields.TryGetValue("isDeleted",out var deleted) && (deleted is not bool deletedValue || deletedValue!=record.IsDeleted))
            throw new InvalidDataException("Firestore native deletion projection differs from canonical record.");
        var document=JObject.Parse(json);
        if(document["id"]!=null&&document["id"].Value<string>()!=documentId)
            throw new InvalidDataException("Firestore embedded identity differs from document key.");
        document["id"]=documentId;
        return document;
    }

    private static async Task<T> ReadFirestoreHistoryAsync<T>(Dictionary<string,object> fields,string documentId,int version) where T:class,IHolonBase
    {
        var document=CanonicalFirestoreHistoryDocument<T>(fields,documentId);
        return await FirestoreHistory.ReadAsync(Guid.Parse(documentId),document,version,OasisJson.DeserializeStored<T>);
    }
}

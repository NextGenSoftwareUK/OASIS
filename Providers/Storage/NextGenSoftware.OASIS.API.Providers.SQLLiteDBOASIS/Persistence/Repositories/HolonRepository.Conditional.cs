using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    // Microsoft.Data.Sqlite performs native I/O synchronously, including its async APIs.
    // Both public surfaces therefore share one transaction owner; no background replay.
    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(
        ConditionalHolonWriteRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ExecuteAtomicHolonWrite(request,null,cancellationToken));
    }

    public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
        =>ExecuteAtomicHolonWrite(request,null,CancellationToken.None);

    private OASISResult<IEnumerable<IHolon>> ExecuteAtomicHolonWrite(ConditionalHolonWriteRequest request,
        CommittedHolonReplicaRequest replicaRequest,CancellationToken token,HolonReplicationEnrollment enrollment=null)
    {
        var embedded=request?.Write.ReplicationEnrollment;
        if(enrollment!=null && embedded!=null && (enrollment.SourceId!=embedded.SourceId || !enrollment.TargetIds.SequenceEqual(embedded.TargetIds)))
            return ConditionalError("COSMIC_REPLICATION_ENROLLMENT_MISMATCH","The explicit and embedded replication enrollments differ.");
        enrollment??=embedded;
        PreparedGraph graph=null;SqliteTransaction native=null;
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction enlisted=null;
        var commitEntered=false;var opened=false;var rollbackFailed=false;var knownAcknowledgement=false;var admissionComplete=false;
        NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.CommittedHolonReplicaPlan replica=null;
        var operation=ConditionalHolonOperation.Save;var operationId=Guid.Empty;
        OASISResult<IEnumerable<IHolon>> result=null;Exception failure=null;
        void RecordFailure(Exception cause){failure=failure==null ? result?.IsError==true && result.Exception!=null
            ? new AggregateException(result.Exception,cause):cause:new AggregateException(failure,cause);}
        try{result=ApplyInTransaction();}catch(Exception cause){failure=cause;}
        if(native!=null && !commitEntered)
        {
            try{native.Rollback();}catch(Exception cause){rollbackFailed=true;RecordFailure(cause);}
        }
        foreach(var resource in new IDisposable[]{enlisted,native})
        {try{resource?.Dispose();}catch(Exception cause){RecordFailure(cause);}}
        if(opened){try{_dbContext.Database.CloseConnection();}catch(Exception cause){RecordFailure(cause);}}
        if(failure!=null)
        {
            if(knownAcknowledgement){result.IsWarning=true;result.WarningCount++;result.Exception=failure;
                result.Message="The atomic holon acknowledgement is known, but SQLite cleanup failed.";}
            else
            {
                if(graph!=null)foreach(var entry in _dbContext.ChangeTracker.Entries<HolonModel>().Where(entry=>graph.Models.Any(model=>model.Id==entry.Entity.Id)).ToArray())entry.State=EntityState.Detached;
                var uncertain=commitEntered || rollbackFailed;
                if(result?.IsError==true && !uncertain)result.Exception=failure;
                else result=ConditionalError(uncertain ? "COSMIC_WRITE_RECONCILIATION_REQUIRED"
                    : failure is SqliteReplicaConflictException ? "COSMIC_REPLICA_LINEAGE_CONFLICT"
                    : failure is OperationCanceledException ? replicaRequest!=null ? "COSMIC_REPLICA_WRITE_CANCELLED":"COSMIC_CONDITIONAL_WRITE_CANCELLED"
                    : replicaRequest!=null && !admissionComplete && failure is ArgumentException ? "COSMIC_REPLICA_WRITE_INVALID"
                    : replicaRequest!=null ? "COSMIC_REPLICA_WRITE_FAILED":"COSMIC_CONDITIONAL_WRITE_FAILED",
                    uncertain ? "Reconcile the original durable operation receipt before another mutation.":"The atomic write did not commit.",
                    uncertain ? replicaRequest!=null ? new SqliteReplicaReconciliationException(replicaRequest.OperationId,replicaRequest.SourceId,failure)
                        :new SqliteMutationReconciliationException(graph?.Models.Select(model=>Guid.Parse(model.Id)) ?? Array.Empty<Guid>(),failure):failure);
            }
        }
        if(result!=null && operationId!=Guid.Empty){result.MetaData??=new Dictionary<string,string>();result.MetaData["COSMIC.OperationId"]=operationId.ToString("D");}
        return result;

        OASISResult<IEnumerable<IHolon>> ApplyInTransaction()
        {
            token.ThrowIfCancellationRequested();
            if(replicaRequest!=null)replica=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.CommittedHolonReplicaPlan.Create(replicaRequest,Core.Enums.ProviderType.SQLLiteDBOASIS,
                source=>Context.DataContext.ValidateDeletionUtc(source.DeletedDate));
            if (request == null && replica == null) throw new ArgumentNullException(nameof(request));
            if (request != null && request.Write.Authority != Core.Enums.ProviderType.SQLLiteDBOASIS)
                return ConditionalError("COSMIC_AUTHORITY_MISMATCH", "The conditional request belongs to another provider authority.");
            if (request != null && request.Operation != ConditionalHolonOperation.Save && request.Write.Conditions.Any(condition => !condition.ExpectedRevisionId.HasValue))
                return ConditionalError("COSMIC_DELETE_REQUIRES_REVISION", "Conditional deletion requires an existing revision for every selected record.");
            if (_dbContext.ChangeTracker.HasChanges())
                return ConditionalError("COSMIC_CONTEXT_HAS_PENDING_CHANGES", "A conditional transaction cannot commit unrelated pending context changes.");
            graph = replica==null ? PrepareGraph(request.Roots, request.SaveChildren, request.Recursive, request.MaxChildDepth)
                : new PreparedGraph(replica.Nodes.Select(node=>CreateHolonModel(node.Snapshot,node.Snapshot.Id)).ToList(),replica.Nodes.ToList(),
                    replica.RootIds.Select(id=>id.ToString()).ToHashSet(),replica.ChildIds);
            operation=replica?.Operation ?? request.Operation;
            operationId=replica?.OperationId ?? request.Write.OperationId;
            var conditions = request?.Write.Conditions.ToDictionary(condition => condition.HolonId.ToString());
            if (conditions!=null && !graph.Models.Select(model => model.Id).ToHashSet().SetEquals(conditions.Keys))
                return ConditionalError("COSMIC_CONDITIONS_INCOMPLETE", "Exactly one condition is required for every selected record.");
            var fingerprint = replica?.Fingerprint ?? ConditionalFingerprint(request, graph);
            if(enrollment!=null)fingerprint=ReplicationFingerprint(fingerprint,enrollment);
            var ids=graph.Models.Select(model=>model.Id).ToArray();
            foreach (var entry in _dbContext.ChangeTracker.Entries<HolonModel>().ToArray())
                if (ids.Contains(entry.Entity.Id)) entry.State = EntityState.Detached;
            admissionComplete=true;
            _dbContext.Database.OpenConnection();
            opened = true;
            var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
            // IMMEDIATE acquires the native writer reservation BEFORE reading revisions.
            native = connection.BeginTransaction(deferred: false);
            enlisted = _dbContext.Database.UseTransaction(native);
            token.ThrowIfCancellationRequested();
            using (var receipt = connection.CreateCommand())
            {
                receipt.Transaction = native;
                receipt.CommandText = "SELECT RequestHash, ResultJson FROM CosmicWriteReceipt WHERE OperationId = $id";
                receipt.Parameters.AddWithValue("$id", operationId.ToString("D"));
                using var reader = receipt.ExecuteReader();
                if (reader.Read())
                {
                    if (reader.GetString(0) != fingerprint)
                        return ConditionalError("COSMIC_OPERATION_ID_REUSED", "This operation ID is already bound to different write content or actor.");
                    var replay=replica==null ? ReadConditionalReceipt(reader.GetString(1))
                        : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonReceipt.Read(reader.GetString(1),replica);
                    result=ConditionalSuccess(replay,operation);knownAcknowledgement=true;return result;
                }
            }
            var persisted = _dbContext.Holons.AsNoTracking().Where(row => ids.Contains(row.Id)).ToDictionary(row => row.Id);
            var conflicts = (conditions ?? new Dictionary<string,HolonWriteCondition>()).Select(pair => {
                persisted.TryGetValue(pair.Key, out var actual);
                return (Condition: pair.Value, Actual: actual);
            }).Where(pair => pair.Condition.ExpectedRevisionId.HasValue
                ? pair.Actual == null || pair.Actual.ProviderRevisionId != pair.Condition.ExpectedRevisionId.Value
                : pair.Actual != null).Select(pair => new CosmicConcurrencyConflict {
                    HolonId = pair.Condition.HolonId, ExpectedRevisionId = pair.Condition.ExpectedRevisionId,
                    ActualRevisionId = pair.Actual?.ProviderRevisionId, ActualVersionId = pair.Actual?.VersionId, ActualVersion = pair.Actual?.Version }).ToArray();
            if (conflicts.Length != 0)
                return ConditionalError(CosmicConcurrencyException.Code, "The persisted revisions changed; reload and resolve the conflict.", new CosmicConcurrencyException(conflicts));
            token.ThrowIfCancellationRequested();
            if(replica!=null)PrepareReplicaModels(connection,native,graph,persisted,replica);
            var now = DateTime.UtcNow;
            var archiveRevisions=_versionControlEnabled();
            for (var index = 0; index < graph.Models.Count; index++)
            {
                var model = graph.Models[index];
                persisted.TryGetValue(model.Id, out var previous);
                if(previous!=null && archiveRevisions)ArchiveHolonRevision(connection,native,previous);
                if(replica==null)
                {
                    if (request.Operation != ConditionalHolonOperation.Save)
                        model = CreateHolonModel(GetHolonFromEntity(previous), Guid.Parse(previous.Id));
                    model.Version = previous == null ? 1 : checked(previous.Version + 1);
                    model.PreviousVersionId = previous?.VersionId ?? Guid.Empty;
                    model.VersionId = Guid.NewGuid();
                    // Physical deletion has no AddOrUpdate write; retain its acknowledgement token in the receipt.
                    if (operation == ConditionalHolonOperation.HardDelete) model.ProviderRevisionId = Guid.NewGuid();
                    model.ModifiedDate = previous == null ? DateTime.MinValue : now;
                    model.ModifiedByAvatarId = previous == null ? Guid.Empty.ToString() : request.Write.ActorId.ToString();
                    if (previous == null) { model.CreatedDate = now; model.CreatedByAvatarId = request.Write.ActorId.ToString(); }
                    if (request.Operation != ConditionalHolonOperation.Save)
                    {
                        model.IsActive = false; model.DeletedDate = now;
                        model.DeletedByAvatarId = request.Write.ActorId.ToString();
                    }
                }
                if(replica==null && operation==ConditionalHolonOperation.Save)
                {
                    var extension=JObject.Parse(model.PersistentValueJson);
                    var field=nameof(Holon.ChildRoleManifestJson);
                    if(request.SubmittedRelationships.TryGetValue(Guid.Parse(model.Id),out var manifest))
                        extension[field]=HolonChildRoleManifestJson.Serialize(manifest);
                    else
                    {
                        var stored=previous==null ? null:JObject.Parse(previous.PersistentValueJson).Property(field);
                        if(stored==null)extension.Remove(field);else extension[field]=stored.Value.DeepClone();
                    }
                    model.PersistentValueJson=extension.ToString(Formatting.None);
                }
                if (operation == ConditionalHolonOperation.HardDelete)
                {
                    if(previous!=null)_dbContext.Holons.Remove(previous);
                    graph.Models[index] = model;
                }
                else graph.Models[index] = AddOrUpdateHolon(model,replica!=null);
            }
            _dbContext.SaveChanges();
            // Materialize and encode the exact acknowledgement before COMMIT. Replay never re-reads a newer record.
            var acknowledged = CommittedGraph(graph);
            var acknowledgement=ConditionalSuccess(acknowledged,operation);
            var receiptJson = replica==null ? WriteConditionalReceipt(acknowledged)
                : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonReceipt.Write(acknowledged,replica);
            if(replica!=null){_ = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonReceipt.Read(receiptJson,replica);
                WriteReplicaHeads(connection,native,acknowledged,replica);}
            if(enrollment!=null)WriteReplicationObligations(connection,native,request,enrollment,acknowledged,now);
            using (var receipt = connection.CreateCommand())
            {
                receipt.Transaction = native;
                receipt.CommandText = "INSERT INTO CosmicWriteReceipt(OperationId, RequestHash, ResultJson, CommittedAtUtc) VALUES ($id, $hash, $json, $at)";
                receipt.Parameters.AddWithValue("$id", operationId.ToString("D"));
                receipt.Parameters.AddWithValue("$hash", fingerprint);
                receipt.Parameters.AddWithValue("$json", receiptJson);
                receipt.Parameters.AddWithValue("$at", now.ToString("O"));
                receipt.ExecuteNonQuery();
            }
            token.ThrowIfCancellationRequested();
            commitEntered = true;
            enlisted.Commit();
            result=acknowledgement;knownAcknowledgement=true;return result;
        }
    }

    private static OASISResult<IEnumerable<IHolon>> ConditionalError(string code, string message, Exception exception = null)
        => new() { IsError = true, ErrorCount = 1, IsSaved = false, ErrorCode = code, Message = message, Exception = exception };
    private static OASISResult<IEnumerable<IHolon>> ConditionalSuccess(IEnumerable<IHolon> records, ConditionalHolonOperation operation)
    {
        var roots=records.ToList();var selected=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphWritePlan.Create(roots,true,true,0,0);
        foreach(var node in selected.Nodes)node.Source.IsNewHolon=false;
        return new(roots) { IsSaved = operation == ConditionalHolonOperation.Save,
            IsDeleted = operation != ConditionalHolonOperation.Save,
            SavedCount=operation == ConditionalHolonOperation.Save ? selected.Nodes.Count:0,
            DeletedCount = operation == ConditionalHolonOperation.Save ? 0 : selected.Nodes.Count, HasAnyHolonsChanged = true };
    }

    private static string ConditionalFingerprint(ConditionalHolonWriteRequest request, PreparedGraph graph)
    {
        var nodes = new JArray();
        foreach (var node in graph.Nodes)
        {
            var values = JObject.Parse(node.CapturedIntentJson ?? throw new System.IO.InvalidDataException("Selected holon intent was not captured."));
            values.Remove(nameof(Holon.ChildRoleManifestJson));
            // These values are assigned by persistence, rather than part of the logical write intent.
            foreach (var field in new[] { "Version", "VersionId", "PreviousVersionId", "CreatedDate", "ModifiedDate",
                "CreatedByAvatarId", "ModifiedByAvatarId", "CreatedProviderType", "InstanceSavedOnProviderType",
                "ProviderUniqueStorageKey", "PreviousVersionProviderUniqueStorageKey" }) values.Remove(field);
            if (values["ProviderMetaData"] is JObject providerFields)
                foreach (var provider in providerFields.Properties().ToArray())
                    if (provider.Value is JObject fields)
                    {
                        fields.Remove(CosmicConcurrency.ProviderRevisionMetadataKey);
                        if (!fields.Properties().Any()) provider.Remove();
                    }
            values["Id"] = node.Snapshot.Id; values["ParentHolonId"] = node.Snapshot.ParentHolonId;
            nodes.Add(values);
        }
        var content = new JObject { ["actor"] = request.Write.ActorId, ["operation"] = request.Operation.ToString(),
            ["children"] = request.SaveChildren, ["recursive"] = request.Recursive, ["depth"] = request.MaxChildDepth,
            ["roots"] = new JArray(request.Roots.Select(root => root.Id)), ["nodes"] = nodes,
            ["authority"] = request.Write.Authority.ToString(),
            ["conditions"] = JArray.FromObject(request.Write.Conditions.OrderBy(condition => condition.HolonId)) };
        if(request.SubmittedRelationships.Count>0)
        {
            content["intentFormat"]=2;
            content["relationships"]=new JArray(request.SubmittedRelationships.OrderBy(pair=>pair.Key)
                .Select(pair=>JObject.Parse(HolonChildRoleManifestJson.Serialize(pair.Value))));
        }
        JToken Canonical(JToken token) => token is JObject obj
            ? new JObject(obj.Properties().OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => new JProperty(property.Name, Canonical(property.Value))))
            : token is JArray array ? new JArray(array.Select(Canonical)) : token.DeepClone();
        using var hash = SHA256.Create();
        return Convert.ToHexString(hash.ComputeHash(Encoding.UTF8.GetBytes(Canonical(content).ToString(Formatting.None))));
    }

    private static string WriteConditionalReceipt(IEnumerable<IHolon> roots)
    {
        var rootList = roots.ToList(); var nodes = new JArray(); var visited = new HashSet<Guid>();
        void Visit(IHolon record)
        {
            if (!visited.Add(record.Id)) throw new InvalidOperationException("Invalid duplicate receipt identity.");
            nodes.Add(new JObject { ["id"] = record.Id, ["record"] = StoredRecordJson.Serialize<IHolon>(record),
                ["children"] = new JArray(record.Children.Select(child => child.Id)) });
            foreach (var child in record.Children) Visit(child);
        }
        foreach (var root in rootList) Visit(root);
        return new JObject { ["format"] = 1, ["roots"] = new JArray(rootList.Select(root => root.Id)), ["nodes"] = nodes }.ToString(Formatting.None);
    }

    private static IEnumerable<IHolon> ReadConditionalReceipt(string json)
    {
        var receipt = JObject.Parse(json);
        if (receipt.Value<int>("format") != 1) throw new InvalidOperationException("Unsupported conditional receipt format.");
        var nodes = (JArray)receipt["nodes"];
        var records = nodes.ToDictionary(node => node["id"].ToObject<Guid>(), node => StoredRecordJson.DeserializeStoredHolon((string)node["record"]) as IHolon);
        foreach (var node in nodes)
        {
            records[node["id"].ToObject<Guid>()].Children = node["children"].Select(id => records[id.ToObject<Guid>()]).ToList();
            records[node["id"].ToObject<Guid>()].IsNewHolon = false;
        }
        return receipt["roots"].Select(id => records[id.ToObject<Guid>()]).ToList();
    }
}

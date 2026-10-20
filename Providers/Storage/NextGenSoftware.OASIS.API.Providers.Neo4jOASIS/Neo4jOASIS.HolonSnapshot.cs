using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.ExceptionServices;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    internal static void ApplyPlannedChildRoleManifest(IHolon record,PlannedHolonWrite plan,IHolon existing,bool save,bool committedReplica=false)
    {
        // Replica logical content is already committed at the source; never regenerate its roles.
        if(committedReplica)return;
        if(record is not Holon target)throw new InvalidDataException("Canonical Neo4j holon snapshots must support child-role observations.");
        target.ChildRoleManifestJson=save && plan.SelectsChildren && plan.RelationshipManifest!=null
            ? Core.Serialization.HolonChildRoleManifestJson.Serialize(plan.RelationshipManifest)
            :existing is Holon observed ? observed.ChildRoleManifestJson:target.ChildRoleManifestJson;
    }

    private async Task<OASISResult<IHolon>> ReadHolonRootAsync(string field, string value, bool children, bool recursive, int maximumDepth, bool continueOnError,int version)
    {
        var result = new OASISResult<IHolon>();
        var failures = new List<string>();
        try
        {
            if (field != "Id" && field != "ProviderKey") throw new ArgumentException("Unknown holon selector.");
            if (maximumDepth < 0) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
            if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
            if (!IsProviderActivated || _driver == null) throw new InvalidOperationException("Neo4j provider is not activated.");
            using (var session = _driver.AsyncSession())
            {
                var rows = await (await session.RunAsync("MATCH (h:Holon {" + field + ":$value}) RETURN h", new { value })).ToListAsync();
                if (rows.Count == 0) return new OASISResult<IHolon> { IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Holon not found in Neo4j." };
                if (rows.Count != 1) throw new Core.Helpers.StorageLifecycleConflictException("Neo4j holon identity is not unique.");
                var root = ReadHolonNode(rows[0]["h"].As<Neo4j.Driver.INode>());
                root=await ReadStoredVersionAsync(session,"Holon",root,version,json=>OasisJson.DeserializeStored<Holon>(json));
                if(root==null)return new OASISResult<IHolon>{IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Requested Neo4j holon version was not found."};
                if (children) await HydrateNativeChildrenAsync(session, root, recursive, maximumDepth, 0, new HashSet<Guid> { root.Id }, continueOnError, failures);
                result.Result = root;
            }
            result.IsLoaded = true;
        }
        catch (Exception cause)
        {
            result.IsError = true; result.Exception = cause; result.Message = cause.Message;
            if (cause is InvalidDataException || cause is Core.Helpers.StorageLifecycleConflictException)
                result.ErrorCode = Core.Helpers.StorageLifecycleConflictException.ErrorCode;
            else if (cause is ArgumentException) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
        }
        return MarkNativePartialRead(result, failures);
    }

    private static OASISResult<T> MarkNativePartialRead<T>(OASISResult<T> result, List<string> failures)
    {
        if (result.IsError || failures.Count == 0) return result;
        result.IsWarning = true;
        result.Message = $"Returned partial holon data; {failures.Count} descendant reads failed.";
        result.MetaData ??= new Dictionary<string, string>();
        result.MetaData["partialHolonReads"] = "true";
        result.MetaData["failedHolonReadCount"] = failures.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        result.MetaData["failedHolonReads"] = OasisJson.Serialize(failures.ToArray());
        return result;
    }

    private static async Task HydrateNativeChildrenAsync(IAsyncSession session, IHolon parent,
        bool recursive, int maximumDepth, long depth, HashSet<Guid> visited,
        bool continueOnError = false, List<string> failures = null)
    {
        if(maximumDepth!=0 && depth>=maximumDepth)return;
        async Task<List<IRecord>> ReadRows(IHolon node)
        {
            try
            {
                return await (await session.RunAsync("MATCH (p:Holon {Id:$id})-[:HAS_CHILD]->(h:Holon) RETURN h",
                    new{id=node.Id.ToString()})).ToListAsync();
            }
            catch(Exception cause) when(continueOnError && failures!=null && cause is not InvalidDataException
                && (cause is IOException || cause is TimeoutException || cause is ServiceUnavailableException || cause is SessionExpiredException))
            {
                failures.Add(node.Id+" ("+cause.GetType().Name+")");return null;
            }
        }
        var rows=await ReadRows(parent);if(rows==null)return;
        var pending=new Stack<(IHolon Parent,long Depth,List<IRecord> Rows,int Next,List<IHolon> Selected)>();
        pending.Push((parent,depth,rows,0,new List<IHolon>()));
        while(pending.Count>0)
        {
            var frame=pending.Pop();
            if(frame.Next==frame.Rows.Count){frame.Parent.Children=frame.Selected;continue;}
            var child=ReadHolonNode(frame.Rows[frame.Next]["h"].As<Neo4j.Driver.INode>());
            if(!visited.Add(child.Id))throw new Core.Helpers.StorageLifecycleConflictException("Neo4j selected graph has a repeated identity or cycle.");
            if(child.ParentHolonId!=frame.Parent.Id)throw new Core.Helpers.StorageLifecycleConflictException("Neo4j child snapshot conflicts with its native parent edge.");
            frame.Selected.Add(child);
            pending.Push((frame.Parent,frame.Depth,frame.Rows,frame.Next+1,frame.Selected));
            if(recursive && (maximumDepth==0 || frame.Depth+1<maximumDepth))
            {
                var descendants=await ReadRows(child);
                if(descendants!=null)pending.Push((child,frame.Depth+1,descendants,0,new List<IHolon>()));
            }
        }
    }

    private static Holon ReadHolonNode(Neo4j.Driver.INode node)
    {
        if (!node.Properties.TryGetValue("SnapshotJson", out var json) || json is not string snapshot || string.IsNullOrWhiteSpace(snapshot))
            throw new InvalidDataException("Neo4j holon canonical snapshot is missing.");
        var holon = OasisJson.DeserializeStored<Holon>(snapshot);
        if (holon == null || holon.Id != ReadNativeIdentity(node))
            throw new InvalidDataException("Neo4j holon snapshot identity differs from its index.");
        ValidateNativeLookupProjection(node,"Name",holon.Name);
        ValidateNativeLookupProjection(node,"Description",holon.Description);
        ValidateNativeLookupProjection(node,"HolonType",holon.HolonType.ToString());
        var revision=Guid.Empty;
        if(node.Properties.TryGetValue("ProviderRevisionId",out var physical)
            && (physical is not string value || !Guid.TryParse(value,out revision)))
            throw new InvalidDataException("Neo4j holon provider revision is invalid.");
        CosmicConcurrency.SetProviderRevisionId(holon,Core.Enums.ProviderType.Neo4jOASIS,revision);
        holon.IsNewHolon=false;
        return holon;
    }

    private async Task<OASISResult<IHolon>> SaveOneHolonSnapshotAsync(IHolon holon, bool children, bool recursive, int maximumDepth)
    {
        var batch = await SaveHolonSnapshotsAsync(new[] { holon }, children, recursive, maximumDepth, 0);
        return new OASISResult<IHolon> { IsError = batch.IsError, IsSaved = batch.IsSaved,
            IsWarning=batch.IsWarning, WarningCount=batch.WarningCount, ErrorCount=batch.ErrorCount, SavedCount=batch.SavedCount,
            MetaData=batch.MetaData, Result = batch.IsError ? null : batch.Result.Single(), ErrorCode = batch.ErrorCode, Exception = batch.Exception, Message = batch.Message };
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonSnapshotsAsync(IEnumerable<IHolon> holons,
        bool children, bool recursive, int maximumDepth, int initialDepth, NeoAtomicHolonWrite conditional = null)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        var state = (NeoHolonTransactionState)conditional ?? new NeoHolonTransactionState();
        var archiveVersions = IsVersionControlEnabled;
        try
        {
            ArgumentNullException.ThrowIfNull(holons);
            if (!IsProviderActivated || _driver == null) throw new InvalidOperationException("Neo4j provider is not activated.");
            if (maximumDepth < 0 || initialDepth < 0) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
            var roots = holons.ToList();
            if (roots.Count == 0) { result.Result = Array.Empty<IHolon>(); return result; }
            var plan = conditional == null ? HolonGraphWritePlan.Create(roots, children, recursive, maximumDepth, initialDepth)
                : (Roots:roots,Nodes:conditional.Plan.Nodes.ToList());
            var rootSources = new HashSet<IHolon>(plan.Roots, ReferenceEqualityComparer.Instance);
            var sources = plan.Nodes.Select(node => node.Source).ToList();
            var staged = plan.Nodes.Select(node => node.Snapshot).ToList();
            var ids = new HashSet<Guid>(staged.Select(record => record.Id));
            var rootIds = plan.Nodes.Where(node => rootSources.Contains(node.Source)).Select(node => node.Snapshot.Id).ToList();
            var edges = plan.Nodes.Where(node => !rootSources.Contains(node.Source)).Select(node =>
                new Dictionary<string, object> { ["parent"] = node.Snapshot.ParentHolonId.ToString(), ["child"] = node.Snapshot.Id.ToString() }).ToList();
            foreach (var record in staged)
            {
                record.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = record.Id.ToString();
                record.IsNewHolon = false;
                CosmicConcurrency.SetProviderRevisionId(record,Core.Enums.ProviderType.Neo4jOASIS,Guid.NewGuid());
            }
            List<IHolon> acknowledgedRoots;
            IAsyncSession session=null;
            IAsyncTransaction transaction=null;
            Exception transactionFailure=null;
            try
            {
                session=_driver.AsyncSession();
                transaction = conditional == null ? await session.BeginTransactionAsync().ConfigureAwait(false)
                    : await session.BeginTransactionAsync(config=>config.WithMetadata(new Dictionary<string,object>{
                        ["COSMIC.OperationId"]=conditional.Plan.OperationId.ToString("D")})).ConfigureAwait(false);
                try
                {
                    if (conditional != null)
                    {
                        conditional.Cancellation.ThrowIfCancellationRequested();
                        var replay = await ReadConditionalReceiptAsync(transaction,conditional).ConfigureAwait(false);
                        if (replay != null)
                        {
                            SetConditionalAcknowledgement(result,replay,conditional);
                            conditional.KnownCommitted=true;
                            await transaction.RollbackAsync().ConfigureAwait(false);
                        }
                        else staged = await PrepareConditionalHolonsAsync(transaction,staged,conditional,archiveVersions).ConfigureAwait(false);
                    }
                    if(!state.KnownCommitted)
                    {
                        // Lock existing membership observations before preparing snapshots/receipts.
                        var membershipRows=await(await transaction.RunAsync("UNWIND $ids AS id WITH id ORDER BY id MATCH (h:Holon {Id:id}) "
                            +"WHERE $claim IS NULL OR h.__cosmicCreatedBy IS NULL OR h.__cosmicCreatedBy<>$claim "
                            +"SET h.__cosmicMembershipLock=true REMOVE h.__cosmicMembershipLock RETURN h",
                            new{ids=staged.Select(record=>record.Id.ToString()).ToArray(),claim=conditional?.Claim}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
                        var memberships=membershipRows.Select(row=>ReadHolonNode(row["h"].As<Neo4j.Driver.INode>())).ToDictionary(record=>record.Id);
                        var plannedMemberships=plan.Nodes.ToDictionary(node=>node.Snapshot.Id);
                        foreach(var record in staged)
                        {
                            memberships.TryGetValue(record.Id,out var existingMembership);
                            ApplyPlannedChildRoleManifest(record,plannedMemberships[record.Id],existingMembership,
                                conditional==null || conditional.Plan.Operation==ConditionalHolonOperation.Save,conditional?.Plan is CommittedHolonReplicaPlan);
                        }
                        var rows=staged.Select(HolonSnapshotRow).ToList();
                        if(archiveVersions && conditional==null)
                        {
                            var byId=rows.ToDictionary(row=>(string)row["id"]);
                            foreach(var record in staged.OrderBy(record=>record.Id))
                            {
                                var row=byId[record.Id.ToString()];
                                row["expected"]=await StampRecordHistoryAsync(transaction,"Holon",record,ReadHolonNode).ConfigureAwait(false);
                                row["writeId"]=Guid.NewGuid().ToString("N");
                                row["snapshot"]=OasisJson.Serialize(record);
                            }
                        }
                        var query="UNWIND $rows AS row MERGE (h:Holon {Id:row.id}) ";
                        if(archiveVersions && conditional==null)query+="ON CREATE SET h.__oasisCreatedBy=row.writeId "
                            + "SET h.__oasisHolonLock=true REMOVE h.__oasisHolonLock WITH h,row "
                            + "WHERE (row.expected IS NULL AND h.__oasisCreatedBy=row.writeId) OR h.SnapshotJson=row.expected REMOVE h.__oasisCreatedBy ";
                        query+="SET h.ProviderKey=row.id, h.Name=row.name, h.Description=row.description, h.HolonType=row.type, h.SnapshotJson=row.snapshot, h.MetaLookupTokens=row.metadataTokens, h.ProviderRevisionId=row.revision ";
                        if(conditional!=null)query+="REMOVE h.__cosmicCreatedBy ";
                        query+="RETURN h";
                        var records=await(await transaction.RunAsync(query,new{rows}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
                        var acknowledged=records.Select(row=>ReadHolonNode(row["h"].As<Neo4j.Driver.INode>())).ToDictionary(record=>record.Id);
                        if(acknowledged.Count!=staged.Count || ids.Any(id=>!acknowledged.ContainsKey(id)))
                            throw new InvalidDataException("Neo4j holon batch acknowledgement differs from submitted identities.");
                        foreach(var record in staged)RequireAcknowledgedRecord(record,acknowledged[record.Id]);
                        if(conditional?.Plan.Operation!=ConditionalHolonOperation.HardDelete)
                        {
                            // ParentHolonId belongs to the selected child, including a child saved as a root.
                            // An omitted child is not an unlink instruction. Logical parents may live on another
                            // provider; only locally present parents have a native relationship projection.
                            var incoming=staged.Select(record=>new Dictionary<string,object> {
                                ["parent"]=record.ParentHolonId.ToString(),["child"]=record.Id.ToString() }).ToList();
                            await(await transaction.RunAsync("UNWIND $incoming AS edge MATCH (c:Holon {Id:edge.child}) "
                                +"MATCH (old:Holon)-[r:HAS_CHILD]->(c) WHERE old.Id <> edge.parent DELETE r",new{incoming}).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
                            var linked=await(await transaction.RunAsync("UNWIND $incoming AS edge MATCH (c:Holon {Id:edge.child}) "
                                +"OPTIONAL MATCH (p:Holon {Id:edge.parent}) "
                                +"FOREACH (parent IN CASE WHEN p IS NULL THEN [] ELSE [p] END | MERGE (parent)-[:HAS_CHILD]->(c)) "
                                +"RETURN count(c) AS linked",new{incoming}).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
                            if(linked["linked"].As<long>()!=staged.Count)throw new InvalidDataException("Neo4j graph edge acknowledgement is incomplete.");
                            var cycle=await(await transaction.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids "
                                +"AND EXISTS { MATCH (h)<-[:HAS_CHILD*1..]-(h) } RETURN count(h) AS count",
                                new{ids=ids.Select(id=>id.ToString()).ToArray()}).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
                            if(cycle["count"].As<long>()!=0)throw new Core.Helpers.StorageLifecycleConflictException("Neo4j parent assignment creates a cycle.");
                        }
                        foreach(var record in acknowledged.Values)
                            record.Children=edges.Where(edge=>(string)edge["parent"]==record.Id.ToString())
                                .Select(edge=>(IHolon)acknowledged[Guid.Parse((string)edge["child"])]).ToList();
                        acknowledgedRoots=rootIds.Select(id=>(IHolon)acknowledged[id]).ToList();
                        if(conditional!=null)
                        {
                            if(conditional.Plan.Operation==ConditionalHolonOperation.HardDelete)
                            {
                                var removed=await(await transaction.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h RETURN count(h) AS count",
                                    new{ids=ids.Select(id=>id.ToString()).ToArray()}).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
                                if(removed["count"].As<long>()!=ids.Count)throw new InvalidDataException("Neo4j conditional deletion acknowledgement is incomplete.");
                                await(await transaction.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v",
                                    new{ids=ids.Select(id=>id.ToString()).ToArray()}).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
                            }
                            if(conditional.Plan is CommittedHolonReplicaPlan replica)
                                await WriteCommittedReplicaHeadsAsync(transaction,replica,acknowledged.Values).ConfigureAwait(false);
                            if(conditional.Plan is ConditionalHolonPlan {ReplicationEnrollment:not null} enrolled)
                                await WritePrimaryHolonObligationsAsync(transaction,enrolled,conditional.Actor,acknowledgedRoots).ConfigureAwait(false);
                            await WriteConditionalReceiptAsync(transaction,conditional,acknowledgedRoots).ConfigureAwait(false);
                            conditional.Cancellation.ThrowIfCancellationRequested();
                        }
                        state.CommitDispatched=true;
                        await transaction.CommitAsync().ConfigureAwait(false);
                        if(conditional!=null)
                        {
                            SetConditionalAcknowledgement(result,acknowledgedRoots,conditional);
                            conditional.KnownCommitted=true;
                        }
                        else
                        {
                            result.Result=acknowledgedRoots;result.IsSaved=true;result.SavedCount=staged.Count;
                            state.KnownCommitted=true;
                            for(var index=0;index<sources.Count;index++)
                            {
                                sources[index].Id=staged[index].Id;
                                sources[index].ProviderUniqueStorageKey??=new Dictionary<Core.Enums.ProviderType,string>();
                                sources[index].ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS]=staged[index].Id.ToString();
                                sources[index].IsNewHolon=false;sources[index].ParentHolonId=staged[index].ParentHolonId;
                                if(archiveVersions){sources[index].Version=staged[index].Version;sources[index].VersionId=staged[index].VersionId;sources[index].PreviousVersionId=staged[index].PreviousVersionId;}
                            }
                        }
                    }
                }
                catch(Exception cause)
                {
                    if(!state.CommitDispatched && !state.KnownCommitted)
                    {
                        try {await transaction.RollbackAsync().ConfigureAwait(false);}
                        catch(Exception rollback) {state.RollbackFailed=true;throw new AggregateException(cause,rollback);}
                    }
                    throw;
                }
            }
            catch(Exception cause){transactionFailure=cause;}
            finally
            {
                foreach(var resource in new IDisposable[]{transaction,session})
                {
                    try{resource?.Dispose();}
                    catch(Exception cleanup){transactionFailure=transactionFailure==null ? cleanup:new AggregateException(transactionFailure,cleanup);}
                }
            }
            if(transactionFailure!=null)ExceptionDispatchInfo.Capture(transactionFailure).Throw();
        }
        catch(Exception cause)
        {
            if(conditional!=null) return ConditionalNeoFailure(result,conditional,cause);
            if(state.KnownCommitted)
            {
                result.IsWarning=true;result.WarningCount++;result.Exception=cause;
                result.Message="The holon write committed, but Neo4j acknowledgement publication or cleanup failed.";
                return result;
            }
            var uncertain=state.CommitDispatched || state.RollbackFailed;
            result.IsError=true;result.ErrorCount++;result.IsSaved=false;result.Result=null;result.SavedCount=0;
            result.Exception=uncertain ? new Neo4jMutationReconciliationException(cause):cause;
            result.ErrorCode=uncertain ? "COSMIC_WRITE_RECONCILIATION_REQUIRED"
                :cause is Core.Helpers.StorageLifecycleConflictException ? Core.Helpers.StorageLifecycleConflictException.ErrorCode
                :cause is ArgumentException || cause is InvalidDataException ? "HYPERDRIVE_PAYLOAD_INVALID":"COSMIC_WRITE_FAILED";
            result.Message=result.Exception.Message;
        }
        return result;
    }

    private static Dictionary<string,object> HolonSnapshotRow(Holon record)=>new() {
        ["id"]=record.Id.ToString(),["name"]=record.Name,["description"]=record.Description,["type"]=record.HolonType.ToString(),
        ["snapshot"]=OasisJson.Serialize(record),["metadataTokens"]=MetadataLookupTokens(record),
        ["revision"]=CosmicConcurrency.GetProviderRevisionId(record,Core.Enums.ProviderType.Neo4jOASIS)!.Value.ToString("D") };
}

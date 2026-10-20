using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

public partial class PostgreSQLOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> WriteSelectedGraphAsync(IEnumerable<IHolon> roots,bool children,bool recursive,int depth,int initial,bool continueOnError)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        var acknowledged=new HashSet<Guid>();var committedRoots=new List<IHolon>();
        var originalIds=new List<Guid>();var failures=new List<Exception>();
        try{
            var guards=new Dictionary<IHolon,StoredRecordPublicationSnapshot<IHolon>>(ReferenceEqualityComparer.Instance);
            var plan=HolonGraphWritePlan.Create(roots,children,recursive,depth,initial,source=>guards.Add(source,StoredRecordPublicationSnapshot<IHolon>.Capture(source)));
            var identities=new Dictionary<IHolon,Guid>(ReferenceEqualityComparer.Instance);
            foreach(var node in plan.Nodes)identities.Add(node.Source,node.Snapshot.Id);
            originalIds.AddRange(plan.Nodes.Select(node=>node.Snapshot.Id));
            void ValidateNode(PlannedHolonWrite node){
                guards[node.Source].ValidateUnchanged();
                if(node.SelectsChildren){
                    var current=HolonChildCollectionState.Capture(node.Source).CreateManifest(node.Snapshot.Id,child=>identities[child]);
                    if(HolonChildRoleManifestJson.Serialize(current)!=HolonChildRoleManifestJson.Serialize(node.RelationshipManifest))
                        throw new InvalidOperationException("Caller child membership changed after graph capture.");
                }
            }
            void Validate(IEnumerable<PlannedHolonWrite> selected){foreach(var node in selected)ValidateNode(node);}
            var groups=new List<List<PlannedHolonWrite>>();
            var rootSources=new HashSet<IHolon>(plan.Roots,ReferenceEqualityComparer.Instance);
            if(!continueOnError)groups.Add(plan.Nodes);
            else{
                foreach(var node in plan.Nodes){if(rootSources.Contains(node.Source))groups.Add(new());groups[^1].Add(node);}
            }
            var failed=new HashSet<Guid>();
            foreach(var group in groups){
                try{
                    if(failed.Contains(group[0].Snapshot.ParentHolonId))throw new InvalidOperationException("Selected root depends on a failed graph parent.");
                    Validate(group);
                    var stored=await WriteGraphTransactionAsync(group,ValidateNode,()=>Validate(group));
                    acknowledged.UnionWith(stored.Keys);
                    // Validate the complete group before publishing any caller state.
                    Validate(group);
                    foreach(var node in group){
                        PublishAcknowledgedIdentity(node.Source,stored[node.Snapshot.Id],guards[node.Source]);
                        node.Source.ParentHolonId=stored[node.Snapshot.Id].ParentHolonId;
                        if(node.Source is Holon owner)owner.ChildRoleManifestJson=((Holon)stored[node.Snapshot.Id]).ChildRoleManifestJson;
                    }
                    committedRoots.AddRange(group.Where(node=>rootSources.Contains(node.Source)).Select(node=>stored[node.Snapshot.Id]));
                }catch(Exception cause){
                    if(cause is PostgreSqlGraphReconciliationException uncertain)acknowledged.UnionWith(uncertain.AcknowledgedRecordIds);
                    failures.Add(cause);failed.UnionWith(group.Select(node=>node.Snapshot.Id));
                    if(!continueOnError||NativeMutationReconciliation.IsRequired(cause)||acknowledged.Overlaps(group.Select(node=>node.Snapshot.Id)))break;
                }
            }
            if(failures.Count==0){result.Result=plan.Roots;result.IsSaved=true;}
            else{
                var cause=failures.Count==1?failures[0]:new AggregateException(failures);
                result.Exception=acknowledged.Count>0||failures.Any(NativeMutationReconciliation.IsRequired)
                    ?new PostgreSqlGraphReconciliationException(originalIds,acknowledged,cause):cause;
                result.Result=committedRoots;OASISErrorHandling.HandleError(ref result,"PostgreSQL selected graph save failed.");
            }
        }catch(Exception cause){result.Exception=acknowledged.Count>0?new PostgreSqlGraphReconciliationException(originalIds,acknowledged,cause):cause;OASISErrorHandling.HandleError(ref result,"PostgreSQL graph admission or publication failed.");}
        result.SavedCount=acknowledged.Count;return result;
    }

    private async Task<Dictionary<Guid,IHolon>> WriteGraphTransactionAsync(IReadOnlyList<PlannedHolonWrite> group,Action<PlannedHolonWrite> validateNode,Action validateCaller)
    {
        var stored=new Dictionary<Guid,IHolon>();var committing=false;var committed=false;var mutationStarted=false;
        try{
            await using var connection=new NpgsqlConnection(_connectionString);await connection.OpenAsync();
            await using var transaction=await connection.BeginTransactionAsync(IsolationLevel.Serializable);
            // Stable lock ordering across overlapping graphs; no native write retries.
            foreach(var node in group.OrderBy(node=>node.Snapshot.Id)){
                var proposal=node.Snapshot;proposal.ProviderUniqueStorageKey??=new();proposal.ProviderUniqueStorageKey[ProviderType.Value]=proposal.Id.ToString();
                const string sql=@"INSERT INTO oasis_holons(id,parent_holon_id,holon_type,is_deleted,data_json)
VALUES(@id,@parent_holon_id,@holon_type,@is_deleted,@data_json)
ON CONFLICT(id) DO UPDATE SET parent_holon_id=EXCLUDED.parent_holon_id,holon_type=EXCLUDED.holon_type,
is_deleted=EXCLUDED.is_deleted,modified_date=NOW(),data_json=EXCLUDED.data_json";
                await using var command=new NpgsqlCommand(sql,connection,transaction);
                command.Parameters.AddWithValue("id",proposal.Id);
                command.Parameters.AddWithValue("parent_holon_id",proposal.ParentHolonId==Guid.Empty?(object)DBNull.Value:proposal.ParentHolonId);
                command.Parameters.AddWithValue("holon_type",(int)proposal.HolonType);command.Parameters.AddWithValue("is_deleted",proposal.IsDeleted);
                command.Parameters.AddWithValue("data_json",string.Empty);
                stored.Add(proposal.Id,await WriteRecordInTransactionAsync<IHolon>(command,"oasis_holons",proposal,()=>validateNode(node),node,()=>mutationStarted=true));
            }
            validateCaller();committing=true;await transaction.CommitAsync();committed=true;
            validateCaller();return stored;
        }catch(Exception cause)when(committed||mutationStarted&&cause is NpgsqlException and not PostgresException||committing&&cause is not PostgresException){
            throw new PostgreSqlGraphReconciliationException(group.Select(node=>node.Snapshot.Id),committed?stored.Keys:Array.Empty<Guid>(),cause);
        }
    }
}

public sealed class PostgreSqlGraphReconciliationException:Exception,INativeMutationReconciliationRequired
{
    public IReadOnlyList<Guid> OriginalRecordIds{get;}
    public IReadOnlyList<Guid> AcknowledgedRecordIds{get;}
    public string OriginalTransactionId=>string.Empty;
    public PostgreSqlGraphReconciliationException(IEnumerable<Guid> original,IEnumerable<Guid> acknowledged,Exception cause)
        :base("PostgreSQL graph has partial or uncertain committed outcomes; reconcile original records before replay.",cause)
    {OriginalRecordIds=Array.AsReadOnly(original.Distinct().ToArray());AcknowledgedRecordIds=Array.AsReadOnly(acknowledged.Distinct().ToArray());}
}

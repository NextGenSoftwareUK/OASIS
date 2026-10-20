using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS : IConditionalHolonStorageProvider
{
    // Uses the same transaction owner for direct and interface dispatch; no weaker save fallback.
    public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
        =>ExecuteConditionalHolonWriteAsync(request).GetAwaiter().GetResult();
    public async Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(ConditionalHolonWriteRequest request,CancellationToken cancellationToken=default)
    {
        var commitEntered=false;
        try{
            cancellationToken.ThrowIfCancellationRequested();var plan=ConditionalHolonPlan.Create(request,Core.Enums.ProviderType.OracleDBOASIS);
            await using var connection=new OracleConnection(_connectionString);await connection.OpenAsync(cancellationToken);
            await using var transaction=(OracleTransaction)await connection.BeginTransactionAsync(cancellationToken);
            try{
                await using var claim=CreateCommand("INSERT INTO OASIS_WRITE_RECEIPTS(OPERATION_ID,REQUEST_HASH) VALUES(:Id,:Hash)",connection);claim.Transaction=transaction;
                claim.Parameters.Add("Id",OracleDbType.Raw).Value=request.Write.OperationId.ToByteArray();claim.Parameters.Add("Hash",OracleDbType.Varchar2).Value=plan.Fingerprint;
                if(await claim.ExecuteNonQueryAsync(cancellationToken)!=1)throw new InvalidDataException("Oracle operation claim did not insert exactly one row.");
            }catch(OracleException cause)when(cause.Number==1){
                // Unique-key contention resolves against the ORIGINAL committed operation; never resubmit its writes.
                await using var receipt=CreateCommand("SELECT REQUEST_HASH,RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",connection);receipt.Transaction=transaction;receipt.Parameters.Add("Id",OracleDbType.Raw).Value=request.Write.OperationId.ToByteArray();
                await using var reader=await receipt.ExecuteReaderAsync(cancellationToken);
                if(!await reader.ReadAsync(cancellationToken)||reader.IsDBNull(1))throw new InvalidDataException("Oracle original operation receipt is absent or incomplete.");
                if(reader.GetString(0)!=plan.Fingerprint)return ConditionalFailure("COSMIC_OPERATION_ID_REUSED","Operation ID is bound to different request content.");
                return ConditionalAcknowledgement(ReadConditionalReceipt(reader.GetString(1),plan),request.Operation);
            }
            var previous=new Dictionary<Guid,Holon?>();var originals=new Dictionary<Guid,string?>();var conflicts=new List<CosmicConcurrencyConflict>();
            var conditions=request.Write.Conditions.ToDictionary(c=>c.HolonId);
            foreach(var node in plan.Nodes.OrderBy(n=>n.Snapshot.Id)){
                await using var read=CreateCommand("SELECT DATA_JSON,HOLON_TYPE,IS_DELETED,PARENT_HOLON_ID FROM OASIS_HOLONS WHERE ID=:Id FOR UPDATE",connection);read.Transaction=transaction;read.Parameters.Add("Id",OracleDbType.Raw).Value=node.Snapshot.Id.ToByteArray();
                await using var reader=await read.ExecuteReaderAsync(cancellationToken);Holon? stored=null;string? json=null;
                if(await reader.ReadAsync(cancellationToken)){
                    json=reader.GetString(0);stored=await ReadRecordVersionAsync<Holon>(node.Snapshot.Id,json,0)??throw new InvalidDataException("Oracle current conditional record is missing.");
                    var nativeParent=reader.IsDBNull(3)?Guid.Empty:new Guid((byte[])reader.GetValue(3));
                    ValidateNativeHolonProjection(stored,reader.GetInt32(1),reader.GetInt32(2),nativeParent);
                }
                previous.Add(node.Snapshot.Id,stored);originals.Add(node.Snapshot.Id,json);
                var expected=conditions[node.Snapshot.Id].ExpectedRevisionId;
                var actual=stored==null?(Guid?)null:CosmicConcurrency.GetProviderRevisionId(stored,Core.Enums.ProviderType.OracleDBOASIS)??Guid.Empty;
                if(expected!=actual)conflicts.Add(new CosmicConcurrencyConflict{HolonId=node.Snapshot.Id,ExpectedRevisionId=expected,ActualRevisionId=actual,ActualVersionId=stored?.VersionId,ActualVersion=stored?.Version});
            }
            if(conflicts.Count>0)throw new CosmicConcurrencyException(conflicts);
            var acknowledged=new Dictionary<Guid,IHolon>();
            foreach(var node in plan.Nodes){
                cancellationToken.ThrowIfCancellationRequested();var old=previous[node.Snapshot.Id];
                var proposal=request.Operation==ConditionalHolonOperation.Save?node.Snapshot:Core.Serialization.StoredRecordJson.Project<Holon>(old!);
                proposal.Version=old==null?1:checked(old.Version+1);proposal.PreviousVersionId=old?.VersionId??Guid.Empty;proposal.VersionId=Guid.NewGuid();
                var now=DateTime.UtcNow;
                if(old==null){proposal.CreatedDate=now;proposal.CreatedByAvatarId=request.Write.ActorId;proposal.CreatedProviderType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.OracleDBOASIS);proposal.ModifiedDate=DateTime.MinValue;proposal.ModifiedByAvatarId=Guid.Empty;}
                else{proposal.CreatedDate=old.CreatedDate;proposal.CreatedByAvatarId=old.CreatedByAvatarId;proposal.CreatedProviderType=old.CreatedProviderType;proposal.ModifiedDate=now;proposal.ModifiedByAvatarId=request.Write.ActorId;}
                proposal.InstanceSavedOnProviderType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.OracleDBOASIS);
                if(request.Operation!=ConditionalHolonOperation.Save){proposal.IsActive=false;proposal.DeletedDate=now;proposal.DeletedByAvatarId=request.Write.ActorId;}
                proposal.ProviderUniqueStorageKey??=new Dictionary<Core.Enums.ProviderType,string>();proposal.ProviderUniqueStorageKey[Core.Enums.ProviderType.OracleDBOASIS]=proposal.Id.ToString();
                var json=await StageRecordHistoryAsync(proposal,originals[proposal.Id]);var staged=Deserialize<Holon>(json);
                await using var command=request.Operation==ConditionalHolonOperation.HardDelete?CreateCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id",connection):CreateCanonicalWriteCommand(staged,json,"OASIS_HOLONS",old==null,connection,transaction);
                command.Transaction=transaction;if(request.Operation==ConditionalHolonOperation.HardDelete)command.Parameters.Add("Id",OracleDbType.Raw).Value=proposal.Id.ToByteArray();
                try{
                    if(await command.ExecuteNonQueryAsync(cancellationToken)!=1)throw new InvalidDataException("Oracle conditional graph write did not affect exactly one row.");
                }catch(OracleException cause)when(old==null&&cause.Number==1){
                    // A missing-key SELECT cannot lock absence. Read the actual winner after native uniqueness resolves.
                    await using var winner=CreateCommand("SELECT DATA_JSON FROM OASIS_HOLONS WHERE ID=:Id",connection);winner.Transaction=transaction;winner.Parameters.Add("Id",OracleDbType.Raw).Value=proposal.Id.ToByteArray();
                    var value=await winner.ExecuteScalarAsync(cancellationToken);
                    if(value==null||value==DBNull.Value)throw new InvalidDataException("Oracle create conflict has no readable original winner.",cause);
                    var actual=await ReadRecordVersionAsync<Holon>(proposal.Id,value.ToString()!,0)??throw new InvalidDataException("Oracle create conflict winner is invalid.");
                    throw new CosmicConcurrencyException(new[]{new CosmicConcurrencyConflict{HolonId=proposal.Id,ExpectedRevisionId=null,ActualRevisionId=CosmicConcurrency.GetProviderRevisionId(actual,Core.Enums.ProviderType.OracleDBOASIS)??Guid.Empty,ActualVersionId=actual.VersionId,ActualVersion=actual.Version}});
                }
                acknowledged.Add(staged.Id,staged);
            }
            foreach(var pair in plan.ChildIds)acknowledged[pair.Key].Children=pair.Value.Select(id=>acknowledged[id]).ToList();
            var roots=plan.RootIds.Select(id=>acknowledged[id]).ToList();var receiptJson=ConditionalHolonReceipt.Write(roots,plan);
            var validated=ReadConditionalReceipt(receiptJson,plan);
            await using(var receipt=CreateCommand("UPDATE OASIS_WRITE_RECEIPTS SET RESULT_JSON=:Json,COMMITTED_AT=SYSTIMESTAMP WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash",connection)){
                receipt.Transaction=transaction;receipt.Parameters.Add("Json",OracleDbType.Clob).Value=receiptJson;receipt.Parameters.Add("Id",OracleDbType.Raw).Value=request.Write.OperationId.ToByteArray();receipt.Parameters.Add("Hash",OracleDbType.Varchar2).Value=plan.Fingerprint;
                if(await receipt.ExecuteNonQueryAsync(cancellationToken)!=1)throw new InvalidDataException("Oracle conditional receipt claim changed unexpectedly.");
            }
            cancellationToken.ThrowIfCancellationRequested();commitEntered=true;await CommitLifecycleTransactionAsync(transaction);
            return ConditionalAcknowledgement(validated,request.Operation);
        }catch(CosmicConcurrencyException cause){return ConditionalFailure(CosmicConcurrencyException.Code,cause.Message,cause);}
        catch(OracleException cause)when(!commitEntered&&cause.Number==1013&&cancellationToken.IsCancellationRequested){
            // ODP.NET can report caller cancellation as ORA-01013. Preserve the native cause,
            // but expose cancellation to the shared router rather than an eligible transport failure.
            return ConditionalFailure("COSMIC_CONDITIONAL_WRITE_FAILED","Oracle conditional operation was cancelled.",new OperationCanceledException("Oracle conditional operation was cancelled.",cause,cancellationToken));
        }
        catch(Exception cause){return ConditionalFailure(commitEntered?"COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED":"COSMIC_CONDITIONAL_WRITE_FAILED","Oracle conditional operation failed.",commitEntered?new OracleConditionalReconciliationException(request.Write.OperationId,cause):cause);}
    }
    private static IReadOnlyList<IHolon> ReadConditionalReceipt(string json,ConditionalHolonPlan plan)
    {
        var roots=ConditionalHolonReceipt.Read(json,plan);var graph=HolonGraphWritePlan.Create(roots,true,true,0,0);
        foreach(var node in graph.Nodes)if(node.Source.ProviderUniqueStorageKey==null||!node.Source.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.OracleDBOASIS,out var key)||key!=node.Source.Id.ToString())
            throw new InvalidDataException("Oracle receipt provider key differs from acknowledged identity.");
        return roots;
    }
    private static OASISResult<IEnumerable<IHolon>> ConditionalAcknowledgement(IEnumerable<IHolon> roots,ConditionalHolonOperation operation)
        =>new(){Result=roots,IsSaved=operation==ConditionalHolonOperation.Save,IsDeleted=operation!=ConditionalHolonOperation.Save};
    private static OASISResult<IEnumerable<IHolon>> ConditionalFailure(string code,string message,Exception? cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
}

public sealed class OracleConditionalReconciliationException:Exception,INativeMutationReconciliationRequired
{
    public Guid OperationId{get;}public string OriginalTransactionId=>string.Empty;
    public OracleConditionalReconciliationException(Guid operationId,Exception cause):base("Oracle conditional operation requires original receipt reconciliation before replay.",cause)=>OperationId=operationId;
}

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
public partial class HolonRepository
{
    public async Task<OASISResult<CosmicProjectedSetPacket>> ExecuteCosmicProjectedSetAsync<T,TResult>(
        CosmicProjectedSetPlan<T,TResult> plan,CosmicProjectedSetExecutionLimits limits,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        OASISResult<CosmicProjectedSetPacket> Error(string code,string message,Exception cause=null)
            =>new(){IsError=true,ErrorCode=code,Message=message,Exception=cause};
        try
        {
            if(plan==null || limits==null || !ReferenceEquals(plan.Options,limits.Options))
                return Error("CQL_PLAN_INVALID","The selected-set snapshot must retain its admitted limits.");
            var admitted=CosmicProjectedSetPlanner.Plan(plan.Query,ProviderType.SQLLiteDBOASIS,
                new CosmicProjectionCompositionTarget(ProviderType.SQLLiteDBOASIS,SqliteCqlContract.Capabilities,
                    SQLLiteDBOASIS.CqlProjectionCapabilities,SQLLiteDBOASIS.CqlCompositionCapabilities),
                SQLLiteDBOASIS.CqlProjectedSetCapabilities,plan.Options);
            if(admitted.IsError)return Error(admitted.ErrorCode,admitted.Message,admitted.Exception);
            plan=admitted.Result;
            CosmicProjectionCompositionExecutionLimits InputLimits(CosmicProjectionCompositionOptions options)
                =>new(options.MaximumInputRows,options.MaximumInputPayloadBytes,options.MaximumStageRows,
                    options.MaximumProofPayloadBytes,10000,options.MaximumInputPayloadBytes);
            var leftLimits=InputLimits(plan.Left.Options);var rightLimits=InputLimits(plan.Right.Options);
            var parameters=new List<SqliteParameter>();
            var leftSql=CompileSelectedSql(plan.Query.Left,plan.Left.Options,leftLimits,selectedSetInput:true,sharedParameters:parameters);
            var rightSql=CompileSelectedSql(plan.Query.Right,plan.Right.Options,rightLimits,selectedSetInput:true,sharedParameters:parameters);
            var setSql=CompileProjectedSetSql(plan.Query,leftSql,rightSql,parameters);
            cancellationToken.ThrowIfCancellationRequested();
            using var exclusive=_dbContext.GetService<IConcurrencyDetector>().EnterCriticalSection();
            var connection=(SqliteConnection)_dbContext.Database.GetDbConnection();
            var opened=connection.State!=ConnectionState.Open;
            if(opened)await _dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var budget=new SqliteCqlReadBudget(connection,cancellationToken);
                using var traversal=leftSql.HasRelationship || rightSql.HasRelationship
                    ? new SqliteCqlTraversalGuard(connection,budget.Check):null;
                void Check(){budget.Check();traversal?.Check();}
                if(_dbContext.Database.CurrentTransaction!=null &&
                    _dbContext.Database.CurrentTransaction.GetDbTransaction().IsolationLevel!=IsolationLevel.Serializable)
                    return Error("CQL_CONSISTENCY_UNSUPPORTED","SQLite selected sets require one serializable read owner.");
                using var transaction=_dbContext.Database.CurrentTransaction==null
                    ? connection.BeginTransaction(IsolationLevel.Serializable,deferred:true):null;
                using var owner=transaction==null?null:_dbContext.Database.UseTransaction(transaction);
                try
                {
                    var left=await ReadSelectedPacketAsync(plan.Query.Left,leftSql,leftLimits,connection,budget,traversal,
                        Check,cancellationToken,plan.Left,selectedSetInput:true).ConfigureAwait(false);
                    var right=await ReadSelectedPacketAsync(plan.Query.Right,rightSql,rightLimits,connection,budget,traversal,
                        Check,cancellationToken,plan.Right,selectedSetInput:true).ConfigureAwait(false);
                    var members=new List<CosmicProjectedSetOccurrence>();
                    var stages=Enumerable.Range(0,plan.Query.Output.ResultStages.Count)
                        .Select(_=>new List<CosmicProjectedSetOccurrence>()).ToArray();
                    var seen=Enumerable.Range(0,stages.Length+1).Select(_=>new HashSet<CosmicProjectedSetOccurrence>()).ToArray();
                    bool header=false;long work=0,proofBytes=0;
                    using(var command=connection.CreateCommand())
                    {
                        command.Transaction=_dbContext.Database.CurrentTransaction?.GetDbTransaction() as SqliteTransaction;
                        command.CommandText=setSql;
                        foreach(var parameter in parameters)
                            command.Parameters.Add(new SqliteParameter(parameter.ParameterName,parameter.SqliteType){Value=parameter.Value});
                        await using var reader=await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        while(await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            Check();var phase=reader.GetInt64(0);
                            if(phase==-2)
                            {
                                if(header)throw new InvalidDataException("Native selected-set header is duplicated.");
                                work=reader.GetInt64(4);header=true;
                                if(work<0 || work>plan.Options.MaximumStageRows)
                                    throw new SqliteCqlResourceException("Native selected-set work exceeds its admitted bound.");
                                continue;
                            }
                            if(!header || phase< -1 || phase>=stages.Length)
                                throw new InvalidDataException("Native selected-set proof phase is invalid.");
                            var side=reader.GetInt64(1);var index=reader.GetInt64(2);
                            if(side is not (0 or 1) || index<0 || index>=(side==0?left.Rows.Count:right.Rows.Count))
                                throw new InvalidDataException("Native selected-set occurrence is outside its complete input.");
                            var occurrence=new CosmicProjectedSetOccurrence(side==1,checked((int)index));
                            if(!seen[checked((int)phase+1)].Add(occurrence))
                                throw new InvalidDataException("Native selected-set occurrence repeats within one stage.");
                            proofBytes=checked(proofBytes+5);
                            if(proofBytes>plan.Options.MaximumProofPayloadBytes)
                                throw new SqliteCqlResourceException("Native selected-set stage proof exceeds its byte budget.");
                            (phase==-1?members:stages[(int)phase]).Add(occurrence);
                        }
                    }
                    if(!header)throw new InvalidDataException("Native selected-set header is missing.");
                    var final=stages.Length==0?members:stages[stages.Length-1];
                    if(final.Count>limits.MaximumResults)
                        throw new SqliteCqlResourceException("Native selected-set result count exceeds its admitted bound.");
                    var output=new List<CosmicProjectionRow>();long outputBytes=0;
                    foreach(var occurrence in final)
                    {
                        Check();
                        var source=occurrence.IsRight?right.Rows[occurrence.Index]:left.Rows[occurrence.Index];
                        var indices=occurrence.IsRight?plan.Query.Right.OutputColumnIndices:plan.Query.Left.OutputColumnIndices;
                        var cells=indices.Select(index=>source.Row.Values[index]).ToArray();
                        outputBytes=checked(outputBytes+16);
                        foreach(var value in cells)Reserve(value,ref outputBytes,limits.MaximumPayloadBytes);
                        output.Add(new CosmicProjectionRow(cells));
                    }
                    var packet=new CosmicProjectedSetPacket(left,right,new(members),
                        stages.Select(stage=>new CosmicProjectedSetStageProof(stage)).ToArray(),output,work);
                    Check();transaction?.Commit();Check();
                    return new OASISResult<CosmicProjectedSetPacket>(packet){IsLoaded=true};
                }
                catch(SqliteException cause) when(cause.SqliteErrorCode==9){budget.Check();throw;}
            }
            finally{if(opened)await _dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);}
        }
        catch(InvalidDataException cause){return Error("CQL_DATA_INVALID",cause.Message,cause);}
        catch(OverflowException cause){return Error("CQL_DATA_INVALID",cause.Message,cause);}
        catch(SqliteCqlDeadlineException cause){return Error("CQL_QUERY_TIMEOUT",cause.Message,cause);}
        catch(SqliteCqlResourceException cause){return Error("CQL_QUERY_LIMIT",cause.Message,cause);}
        catch(CqlTranslationLimitException cause){return Error("CQL_QUERY_LIMIT",cause.Message,cause);}
        catch(NotSupportedException cause){return Error("CQL_CAPABILITY_UNSUPPORTED",cause.Message,cause);}
        catch(OperationCanceledException cause){return Error("CQL_QUERY_CANCELLED",cause.Message,cause);}
        catch(Exception cause){return Error("CQL_QUERY_FAILED","SQLite native selected-set snapshot did not complete.",cause);}
    }
}

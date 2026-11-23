using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
public partial class HolonRepository
{
    public async Task<OASISResult<CosmicSetProjectionCompositionPacket>> ExecuteCosmicSetProjectionCompositionAsync<T,TResult>(
        CosmicSetProjectionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken token) where T:IHolon
    {
        OASISResult<CosmicSetProjectionCompositionPacket> Error(string code,string message,Exception cause=null)
            =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
        if(plan==null || limits==null || !plan.RequiresSelectedComposition)
            return Error("CQL_PLAN_INVALID","A late entity-set selected-stage plan and limits are required.");
        var actual=CosmicSetProjectionPlanner.Plan(plan.Query,ProviderType.SQLLiteDBOASIS,
            new CosmicProjectionTarget(ProviderType.SQLLiteDBOASIS,SqliteCqlContract.Capabilities,
                SQLLiteDBOASIS.CqlProjectionCapabilities),SQLLiteDBOASIS.CqlSetCapabilities,
            plan.Source.Options,SQLLiteDBOASIS.CqlCompositionCapabilities);
        if(actual.IsError)return Error(actual.ErrorCode,actual.Message,actual.Exception);
        var admitted=limits.Admit(actual.Result.SelectedOptions);
        if(admitted.IsError)return Error(admitted.ErrorCode,admitted.Message,admitted.Exception);
        // Retain the exact captured columns/materializer through the public plan;
        // the repository base read consumes cells without constructing TResult.
        var basePlan=CosmicSetProjectionPlanner.PlanBase(actual.Result,
            new CosmicProjectionTarget(ProviderType.SQLLiteDBOASIS,SqliteCqlContract.Capabilities,
                SQLLiteDBOASIS.CqlProjectionCapabilities),SQLLiteDBOASIS.CqlSetCapabilities);
        if(basePlan.IsError)return Error(basePlan.ErrorCode,basePlan.Message,basePlan.Exception);
        CosmicSetProjectionCompositionPacket packet=null;
        var read=await ExecuteProjectionReadAsync(basePlan.Result.Selection,limits.MaximumInputRows,
            limits.MaximumInputPayloadBytes,token,setProjection:basePlan.Result,
            selectedContinuation:async(connection,budget,traversal,input)=>{
                packet=await ReadSetSelectedOccurrencesAsync(actual.Result,limits,connection,budget,traversal,input,token)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
        if(read.IsError)return Error(read.ErrorCode,read.Message,read.Exception);
        if(!read.IsLoaded || packet==null)
            return Error("CQL_PROVIDER_RESULT_INVALID","SQLite selected set snapshot returned no complete packet.");
        return new OASISResult<CosmicSetProjectionCompositionPacket>(packet){IsLoaded=true,LoadedCount=packet.Rows.Count};
    }

    private async Task<CosmicSetProjectionCompositionPacket> ReadSetSelectedOccurrencesAsync<T,TResult>(
        CosmicSetProjectionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
        SqliteConnection connection,SqliteCqlReadBudget budget,SqliteCqlTraversalGuard traversal,
        IReadOnlyList<CosmicProjectionRow> input,CancellationToken token) where T:IHolon
    {
        void Check(){token.ThrowIfCancellationRequested();budget.Check();traversal?.Check();}
        var set=CompileSetSql(plan.Source);var entity=set.Result;var translator=entity.SourceTranslator;
        var order="Holon."+entity.Position;
        var cells=new List<string>{"row_number() OVER (ORDER BY "+order+") AS position"};
        for(var index=0;index<plan.Query.Selection.Columns.Count;index++)
        {
            var cell=translator.ProjectionOperand(plan.Query.Selection.Columns[index],"setSelected"+index,comparable:true);
            cells.Add(cell.State+" AS v"+index+"s");cells.Add(cell.Value+" AS v"+index+"v");
        }
        var source="SELECT "+string.Join(",",cells)+" FROM ("+entity.Rows+") AS Holon ORDER BY "+order
            +" LIMIT "+translator.Bind((long)limits.MaximumInputRows+1);
        var ctes=new List<string>{"c0 AS MATERIALIZED ("+source+")"};
        var selected=CompileSelectedStages(plan.Query.Selection,translator,ctes);
        var header="SELECT -1 AS phase,0 AS position,0 AS ordinal,(SELECT count(*) FROM c0) AS sourceCount,"
            +(selected.Work.Count==0?"0":string.Join("+",selected.Work))+" AS stageRows";
        var sql="WITH "+string.Join(",",ctes)+" "+header+" UNION ALL "+string.Join(" UNION ALL ",selected.Proof)
            +" ORDER BY phase,ordinal,position";
        var stages=Enumerable.Range(0,plan.Query.Selection.ResultStages.Count).Select(_=>new List<int>()).ToArray();
        var seen=stages.Select(_=>new HashSet<int>()).ToArray();
        bool acknowledged=false;long work=0,proofBytes=checked(4L*input.Count);
        using(var command=connection.CreateCommand())
        {
            command.Transaction=_dbContext.Database.CurrentTransaction?.GetDbTransaction() as SqliteTransaction;
            command.CommandText=sql;
            foreach(var parameter in translator.Parameters)
                command.Parameters.Add(new SqliteParameter(parameter.ParameterName,parameter.SqliteType){Value=parameter.Value});
            await using var reader=await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            while(await reader.ReadAsync(token).ConfigureAwait(false))
            {
                Check();var phase=reader.GetInt64(0);
                if(phase==-1)
                {
                    if(acknowledged || reader.GetInt64(3)!=input.Count)
                        throw new InvalidDataException("Selected set native source count disagrees with its complete input.");
                    work=reader.GetInt64(4);acknowledged=true;
                    if(work<0 || work>limits.MaximumStageRows)
                        throw new SqliteCqlResourceException("Selected set cumulative native work exceeds its limit.");
                    continue;
                }
                if(!acknowledged || phase<0 || phase>=stages.Length)
                    throw new InvalidDataException("Selected set native stage phase is invalid.");
                var position=reader.GetInt64(1)-1;
                if(position<0 || position>=input.Count || !seen[(int)phase].Add((int)position))
                    throw new InvalidDataException("Selected set native stage has an invalid or repeated occurrence.");
                proofBytes=checked(proofBytes+4);
                if(proofBytes>limits.MaximumProofPayloadBytes)
                    throw new SqliteCqlResourceException("Selected set native occurrence proof exceeds its limit.");
                stages[(int)phase].Add((int)position);
            }
        }
        Check();
        if(!acknowledged)throw new InvalidDataException("Selected set native header is missing.");
        var final=stages[stages.Length-1];
        if(final.Count>limits.MaximumResults)
            throw new SqliteCqlResourceException("Selected set native output exceeds its limit.");
        // Native ordinals choose already validated detached cells; no managed
        // filtering, sorting or expression evaluation occurs in this publication.
        var rows=final.Select(index=>input[index]).ToArray();
        return new CosmicSetProjectionCompositionPacket(input,
            stages.Select(stage=>new CosmicSetProjectionSelectedStageProof(stage)).ToArray(),rows,work);
    }
}

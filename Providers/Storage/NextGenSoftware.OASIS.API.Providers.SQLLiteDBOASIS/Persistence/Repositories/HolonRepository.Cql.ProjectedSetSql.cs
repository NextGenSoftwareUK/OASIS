using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
public partial class HolonRepository
{
    private static string CompileProjectedSetSql<T,TResult>(CosmicProjectedSetQuery<T,TResult> query,
        CompositionSql left,CompositionSql right,List<SqliteParameter> parameters) where T:IHolon
    {
        var definitions=new List<string>();
        void Input(string name,CosmicProjectionQuery<T,TResult> operand,CompositionSql compiled)
        {
            var fields=new List<string>{"row_number() OVER (ORDER BY chosen."+compiled.Rank+")-1 AS occurrenceIndex"};
            for(var index=0;index<operand.OutputColumnIndices.Count;index++)
            {
                var original=operand.OutputColumnIndices[index];
                fields.Add("chosen.v"+original+"s AS v"+index+"s");
                fields.Add("chosen.v"+original+"v AS v"+index+"v");
            }
            definitions.Add(name+" AS MATERIALIZED (SELECT "+string.Join(",",fields)+" FROM ("+compiled.Rows+") AS chosen)");
        }
        Input("setLeft",query.Left,left);Input("setRight",query.Right,right);
        definitions.Add("setOccurrences AS MATERIALIZED (SELECT 0 AS side,* FROM setLeft UNION ALL SELECT 1 AS side,* FROM setRight)");
        var tuple=query.ValueTypes.Select((type,index)=>"v"+index+"v"+CqlSqlTranslator.SelectedCollation(type)).ToArray();
        var match=string.Join(" AND ",query.ValueTypes.Select((type,index)=>
            "candidate.v"+index+"v"+CqlSqlTranslator.SelectedCollation(type)+" IS rhs.v"+index+"v"+CqlSqlTranslator.SelectedCollation(type)));
        var from="setOccurrences";var filter="";
        if(query.Kind!=CosmicSetQueryKind.Concat)
        {
            definitions.Add("setRanked AS MATERIALIZED (SELECT *,row_number() OVER (PARTITION BY "+
                string.Join(",",tuple)+" ORDER BY side,occurrenceIndex) AS duplicateRank FROM setOccurrences)");
            from="setRanked";filter=" WHERE duplicateRank=1";
            if(query.Kind==CosmicSetQueryKind.Intersect || query.Kind==CosmicSetQueryKind.Except)
                filter+=" AND side=0 AND "+(query.Kind==CosmicSetQueryKind.Except?"NOT ":"")+
                    "EXISTS (SELECT 1 FROM setRight AS rhs WHERE "+match+")";
            else if(query.Kind!=CosmicSetQueryKind.Union)throw new NotSupportedException("Unknown selected-set operation.");
        }
        definitions.Add("c0 AS MATERIALIZED (SELECT candidate.*,row_number() OVER (ORDER BY side,occurrenceIndex) AS position FROM "+
            from+" AS candidate"+filter+")");
        var translator=new CqlSqlTranslator("Holon",parameters,query.Output.Source.ReadAccess);
        var selected=CompileSelectedStages(query.Output,translator,definitions);
        var proofs=new List<string>{"SELECT -1 AS phase,side,occurrenceIndex,position AS ordinal,0 AS stageRows FROM c0"};
        for(var index=0;index<query.Output.ResultStages.Count;index++)
            proofs.Add("SELECT "+index+" AS phase,side,occurrenceIndex,r"+(index+1)+" AS ordinal,0 AS stageRows FROM c"+(index+1));
        var work="(SELECT count(*) FROM setLeft)+(SELECT count(*) FROM setRight)";
        if(selected.Work.Count!=0)work+="+"+string.Join("+",selected.Work);
        return "WITH "+string.Join(",",definitions)+" SELECT -2 AS phase,0 AS side,0 AS occurrenceIndex,0 AS ordinal,"+
            work+" AS stageRows UNION ALL "+string.Join(" UNION ALL ",proofs)+" ORDER BY phase,ordinal,side,occurrenceIndex";
    }
}

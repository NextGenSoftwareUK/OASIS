using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS:ICosmicSetQueryStorageProvider
{
    public CosmicSetQueryCapabilities SetQueryCapabilities=>new(
        CosmicSetQueryFeatures.Union|CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Intersect|CosmicSetQueryFeatures.Except,
        EntityCompositionCapabilities,supportsNestedSets:true);
    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicSetQueryAsync<T>(
        CosmicSetQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        =>ProjectCqlRowsAsync(ExecuteCqlReadAsync(plan?.Anchor,maximumResults,cancellationToken,set:plan));

    private static string CompileCqlSet<T>(NeoCqlSource left,CosmicSetQueryPlan<T> plan,int maximumResults,
        IReadOnlyList<CosmicProjectionColumn> projection=null,
        int maximumProjectionBytes=CosmicProjectionResults.MaximumPayloadLimit,bool countOnly=false) where T:IHolon
    {
        if(plan.Query.IsNested)return CompileCqlSetTree(left,plan,maximumResults,projection,maximumProjectionBytes,countOnly);
        var translator=left.Translator;var parameters=translator.Parameters;
        var right=CompileCqlSource(plan.Query.Right,parameters);
        parameters.Add("setLeftBound",plan.Options.MaximumLeftInputRows);
        parameters.Add("setRightBound",plan.Options.MaximumRightInputRows);
        parameters.Add("setOrdinalScale",checked(plan.Options.MaximumLeftInputRows+plan.Options.MaximumRightInputRows+1));
        parameters.Add("entityWorkBound",32000);
        parameters.Add("entityOutputTake",maximumResults+1);
        string Input(NeoCqlSource compiled,CosmicQuery<T> query,string name,int maximum)
        {
            parameters.Add(name+"Offset",query.Offset);
            parameters.Add(name+"Take",Math.Min(query.Limit??maximum+1,maximum+1));
            var keys=compiled.Translator.TranslateOrdering(query.Orderings);
            var projected=string.Concat(keys.Select((key,i)=>","+key.Expression+" AS "+name+"Sort"+i));
            var ordering=string.Join(",",keys.Select((key,i)=>name+"Sort"+i+(key.Descending?" DESC":" ASC"))
                .Concat(new[]{"p.f_Id ASC"}));
            var errors=compiled.Translator.TraversalClauses.Count==0
                ? "WITH setSchemaInvalid,false AS sourceCycle,false AS sourceLimit "
                : "CALL { "+compiled.EligibleSource+"WHERE "+compiled.Cycle+" OR "+compiled.TraversalLimit+
                    " RETURN coalesce(max(CASE WHEN "+compiled.Cycle+" THEN 1 ELSE 0 END),0)>0 AS sourceCycle,"+
                    "coalesce(max(CASE WHEN "+compiled.TraversalLimit+" THEN 1 ELSE 0 END),0)>0 AS sourceLimit } ";
            return "CALL { WITH setSchemaInvalid "+errors+"CALL { "+compiled.EligibleSource+
                "WHERE ("+compiled.DescendantPredicate+") AND NOT("+compiled.Cycle+" OR "+compiled.TraversalLimit+
                ") WITH h,p"+projected+" ORDER BY "+ordering+" SKIP $"+name+"Offset LIMIT $"+name+"Take"+
                " RETURN collect({h:h,p:p,valid:true,cycle:false,limit:false}) AS candidates } "+
                "RETURN CASE WHEN setSchemaInvalid OR sourceCycle OR sourceLimit "+
                "THEN [{h:null,p:null,valid:false,cycle:sourceCycle,limit:sourceLimit}] ELSE candidates END AS "+name+"Input } ";
        }
        const string invalid="null AS id,null AS revision,false AS valid,null AS snapshot,";
        var positionName=projection==null?"setEntryPosition":"entityPosition";
        var fields="id,revision,valid,snapshot,traversalCycle,traversalLimit,entityInput"+(countOnly?",entityCount":"")+","+positionName
            +(projection==null?"":",payloadLimit,cells");
        var prefix="CALL { "+left.Source+"WHERE NOT valid WITH h LIMIT 1 RETURN count(h)>0 AS setSchemaInvalid } "+
            Input(left,plan.Query.Left,"setLeft",plan.Options.MaximumLeftInputRows)+
            Input(right,plan.Query.Right,"setRight",plan.Options.MaximumRightInputRows)+
            "WITH setLeftInput AS leftInput,setRightInput AS rightInput ";
        var text=new StringBuilder("WITH leftInput,rightInput WHERE all(item IN leftInput+rightInput WHERE item.valid) "+
            "CALL { WITH leftInput,rightInput CALL { "+
            "WITH leftInput UNWIND CASE WHEN size(leftInput)=0 THEN [] ELSE range(0,size(leftInput)-1) END AS setInputPosition "+
            "WITH leftInput[setInputPosition] AS item,setInputPosition "+
            "RETURN item.h AS h,item.p AS p,0 AS setSide,setInputPosition "+
            "UNION ALL WITH rightInput UNWIND CASE WHEN size(rightInput)=0 THEN [] ELSE range(0,size(rightInput)-1) END AS setInputPosition "+
            "WITH rightInput[setInputPosition] AS item,setInputPosition "+
            "RETURN item.h AS h,item.p AS p,1 AS setSide,setInputPosition } ");
        if(plan.Query.Kind==CosmicSetQueryKind.Concat)
            text.Append("WITH h,p,setSide*$setOrdinalScale+setInputPosition AS setOrdinal ");
        else
        {
            text.Append("WITH h,p,collect(DISTINCT setSide) AS setSides,min(setSide*$setOrdinalScale+setInputPosition) AS setOrdinal ");
            if(plan.Query.Kind==CosmicSetQueryKind.Intersect)text.Append("WHERE 0 IN setSides AND 1 IN setSides ");
            else if(plan.Query.Kind==CosmicSetQueryKind.Except)text.Append("WHERE 0 IN setSides AND NOT 1 IN setSides ");
            else if(plan.Query.Kind!=CosmicSetQueryKind.Union)throw new NotSupportedException("Unknown native set operation.");
        }
        text.Append("WITH h,p,setOrdinal ORDER BY setOrdinal RETURN collect({h:h,p:p}) AS selected } "+
            "WITH leftInput+rightInput AS entitySource,selected,false AS entityCycle,"+
            "size(leftInput)>$setLeftBound OR size(rightInput)>$setRightBound AS entityLimit,0 AS entityWork ");
        AppendCqlEntityStages(text,plan.Query.Output.Source,plan.Query.Output.Stages,translator);
        AppendCqlEntityPublication(text,countOnly,projection,maximumProjectionBytes,translator,orderedOccurrences:true);
        return prefix+"CALL { WITH leftInput,rightInput WITH leftInput,rightInput "+
            "WHERE any(item IN leftInput+rightInput WHERE NOT item.valid) RETURN "+invalid+
            "any(item IN leftInput+rightInput WHERE item.cycle) AS traversalCycle,"+
            "any(item IN leftInput+rightInput WHERE item.limit) AS traversalLimit,false AS entityInput,"+(countOnly?"null AS entityCount,":"")+"-1 AS "+positionName+
            (projection==null?" ":",false AS payloadLimit,[] AS cells ")+
            "UNION ALL WITH leftInput,rightInput "+text+" } RETURN "+fields+" ORDER BY "+positionName;
    }
}

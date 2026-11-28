using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS
{
    // Each subquery returns its selected occurrences AND all source proofs. A window
    // can change the former but cannot erase a source or intermediate failure.
    private static string CompileCqlSetTree<T>(NeoCqlSource anchor,CosmicSetQueryPlan<T> root,int maximumResults,
        IReadOnlyList<CosmicProjectionColumn> projection,int maximumProjectionBytes,bool countOnly) where T:IHolon
    {
        var parameters=anchor.Translator.Parameters;
        parameters.Add("entityWorkBound",32000);
        parameters.Add("entityOutputTake",maximumResults+1);
        var ordinal=0;
        string Leaf(CosmicQuery<T> query,int maximum,string name)
        {
            var compiled=CompileCqlSource(query,parameters);
            parameters.Add(name+"Offset",query.Offset);
            parameters.Add(name+"Take",Math.Min(query.Limit??maximum+1,maximum+1));
            var keys=compiled.Translator.TranslateOrdering(query.Orderings);
            var fields=string.Concat(keys.Select((key,i)=>","+key.Expression+" AS sort"+i));
            var order=string.Join(",",keys.Select((key,i)=>"sort"+i+(key.Descending?" DESC":" ASC")).Concat(new[]{"p.f_Id ASC"}));
            var errors=compiled.Translator.TraversalClauses.Count==0
                ? "WITH setSchemaInvalid,false AS sourceCycle,false AS sourceLimit "
                : "CALL { "+compiled.EligibleSource+"WHERE "+compiled.Cycle+" OR "+compiled.TraversalLimit+
                    " RETURN coalesce(max(CASE WHEN "+compiled.Cycle+" THEN 1 ELSE 0 END),0)>0 AS sourceCycle,"+
                    "coalesce(max(CASE WHEN "+compiled.TraversalLimit+" THEN 1 ELSE 0 END),0)>0 AS sourceLimit } ";
            return "CALL { WITH setSchemaInvalid "+errors+"CALL { "+compiled.EligibleSource+
                "WHERE ("+compiled.DescendantPredicate+") AND NOT("+compiled.Cycle+" OR "+compiled.TraversalLimit+
                ") WITH h,p"+fields+" ORDER BY "+order+" SKIP $"+name+"Offset LIMIT $"+name+"Take"+
                " RETURN collect({h:h,p:p}) AS candidates } RETURN {rows:candidates,proof:candidates,"+
                "invalid:setSchemaInvalid,cycle:sourceCycle,limit:sourceLimit,work:0} AS "+name+" } ";
        }
        string Node(CosmicSetQueryPlan<T> plan,string name)
        {
            var leftName="tree"+(ordinal++);var rightName="tree"+(ordinal++);
            var prefix=plan.LeftSet==null?Leaf(plan.Query.Left,plan.Options.MaximumLeftInputRows,leftName):Node(plan.LeftSet,leftName);
            prefix+=plan.RightSet==null?Leaf(plan.Query.Right,plan.Options.MaximumRightInputRows,rightName):Node(plan.RightSet,rightName);
            parameters.Add(name+"LeftBound",plan.Options.MaximumLeftInputRows);
            parameters.Add(name+"RightBound",plan.Options.MaximumRightInputRows);
            parameters.Add(name+"Scale",checked(plan.Options.MaximumLeftInputRows+plan.Options.MaximumRightInputRows+1));
            var text=new StringBuilder("CALL { WITH setSchemaInvalid "+prefix+
                "WITH "+leftName+" AS l,"+rightName+" AS r "+
                "CALL { WITH l,r CALL { WITH l UNWIND CASE WHEN size(l.rows)=0 THEN [] ELSE range(0,size(l.rows)-1) END AS pos "+
                "WITH l.rows[pos] AS item,pos RETURN item.h AS h,item.p AS p,0 AS side,pos "+
                "UNION ALL WITH r UNWIND CASE WHEN size(r.rows)=0 THEN [] ELSE range(0,size(r.rows)-1) END AS pos "+
                "WITH r.rows[pos] AS item,pos RETURN item.h AS h,item.p AS p,1 AS side,pos } ");
            if(plan.Query.Kind==CosmicSetQueryKind.Concat)
                text.Append("WITH h,p,side*$"+name+"Scale+pos AS position ");
            else
            {
                text.Append("WITH h,p,collect(DISTINCT side) AS sides,min(side*$"+name+"Scale+pos) AS position ");
                if(plan.Query.Kind==CosmicSetQueryKind.Intersect)text.Append("WHERE 0 IN sides AND 1 IN sides ");
                else if(plan.Query.Kind==CosmicSetQueryKind.Except)text.Append("WHERE 0 IN sides AND NOT 1 IN sides ");
                else if(plan.Query.Kind!=CosmicSetQueryKind.Union)throw new NotSupportedException("Unknown native set operation.");
            }
            text.Append("WITH h,p,position ORDER BY position RETURN collect({h:h,p:p}) AS combined } "+
                "CALL { WITH l,r,combined WITH l.proof+r.proof AS entitySource,combined AS selected,"+
                "l.cycle OR r.cycle AS entityCycle,l.limit OR r.limit OR size(l.rows)>$"+name+"LeftBound OR size(r.rows)>$"+name+"RightBound AS entityLimit,"+
                "l.work+r.work AS entityWork ");
            AppendCqlEntityStages(text,plan.Query.Output.Source,plan.Query.Output.Stages,anchor.Translator,name);
            text.Append("RETURN entitySource,selected,entityCycle,entityLimit,entityWork } RETURN "+
                "{rows:selected,proof:entitySource,invalid:l.invalid OR r.invalid,cycle:entityCycle,limit:entityLimit,work:entityWork} AS "+name+" } ");
            return text.ToString();
        }
        var output=new StringBuilder("CALL { "+anchor.Source+"WHERE NOT valid WITH h LIMIT 1 RETURN count(h)>0 AS setSchemaInvalid } "+Node(root,"treeRoot"));
        var positionName=projection==null?"setEntryPosition":"entityPosition";
        var fields="id,revision,valid,snapshot,traversalCycle,traversalLimit,entityInput"+(countOnly?",entityCount":"")+","+positionName+(projection==null?"":",payloadLimit,cells");
        output.Append("CALL { WITH treeRoot WITH treeRoot WHERE treeRoot.invalid RETURN null AS id,null AS revision,false AS valid,null AS snapshot,"+
            "treeRoot.cycle AS traversalCycle,treeRoot.limit AS traversalLimit,false AS entityInput,"+(countOnly?"null AS entityCount,":"")+"-1 AS "+positionName+
            (projection==null?" ":",false AS payloadLimit,[] AS cells ")+"UNION ALL WITH treeRoot WITH treeRoot WHERE NOT treeRoot.invalid "+
            "WITH treeRoot.proof AS entitySource,treeRoot.rows AS selected,treeRoot.cycle AS entityCycle,treeRoot.limit AS entityLimit,treeRoot.work AS entityWork ");
        AppendCqlEntityPublication(output,countOnly,projection,maximumProjectionBytes,anchor.Translator,orderedOccurrences:true);
        return output+" } RETURN "+fields+" ORDER BY "+positionName;
    }
}

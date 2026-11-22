using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
public partial class HolonRepository
{
    private sealed class SetSql
    {
        internal EntityCompositionSql Result;
        internal CqlSqlTranslator RightTranslator;
        internal string RightPredicate;
        internal IReadOnlyList<EntityCompositionSource> Sources;
    }
    private static SetSql CompileSetSql<T>(CosmicSetQueryPlan<T> plan) where T:IHolon
    {
        var left=plan.Query.Left;var right=plan.Query.Right;
        var translator=new CqlSqlTranslator(left.ReadAccess);
        var predicate=translator.TranslateSource(left.Predicates);
        var leftOrder=translator.TranslateOrdering(left.Orderings);
        var rightTranslator=new CqlSqlTranslator("Holon",translator.Parameters,right.ReadAccess);
        var rightPredicate=rightTranslator.TranslateSource(right.Predicates);
        var rightOrder=rightTranslator.TranslateOrdering(right.Orderings);
        const string inputPosition="__cosmicSetInputPosition";
        const string side="__cosmicSetSide";
        const string position="__cosmicEntityPosition0";
        var definitions=new List<string>();
        void Input(string name,CosmicQuery<T> query,CqlSqlTranslator native,string test,string order,int maximum)
        {
            // Read at most one over each input bound; validation rejects excess
            // before final set selection, even when its outer page is empty.
            var fetch=Math.Min(query.Limit??maximum+1,maximum+1);
            definitions.Add(name+" AS MATERIALIZED (SELECT Holon.*,row_number() OVER (ORDER BY "+order+
                ") AS "+inputPosition+" FROM Holon WHERE "+test+" ORDER BY "+order+
                " LIMIT "+native.Bind(fetch)+" OFFSET "+native.Bind(query.Offset)+")");
        }
        Input("setLeft",left,translator,predicate,leftOrder,plan.Options.MaximumLeftInputRows);
        Input("setRight",right,rightTranslator,rightPredicate,rightOrder,plan.Options.MaximumRightInputRows);
        string Prefix()=>"WITH "+string.Join(",",definitions)+" ";
        var leftSource=Prefix()+"SELECT * FROM setLeft ORDER BY "+inputPosition;
        var rightSource=Prefix()+"SELECT * FROM setRight ORDER BY "+inputPosition;
        definitions.Add("setOccurrences AS MATERIALIZED (SELECT Holon.*,0 AS "+side+
            " FROM setLeft AS Holon UNION ALL SELECT Holon.*,1 AS "+side+" FROM setRight AS Holon)");
        if(plan.Query.Kind!=CosmicSetQueryKind.Concat)
        {
            var operation=plan.Query.Kind switch {
                CosmicSetQueryKind.Union=>"UNION",
                CosmicSetQueryKind.Intersect=>"INTERSECT",
                CosmicSetQueryKind.Except=>"EXCEPT",
                _=>throw new NotSupportedException("Unknown native set operation.")};
            definitions.Add("setMemberIds AS MATERIALIZED (SELECT Id FROM setLeft "+operation+" SELECT Id FROM setRight)");
            definitions.Add("setRanked AS MATERIALIZED (SELECT Holon.*,row_number() OVER (PARTITION BY Id ORDER BY "+
                side+","+inputPosition+") AS __cosmicSetDuplicate FROM setOccurrences AS Holon)");
            definitions.Add("set0 AS MATERIALIZED (SELECT Holon.*,row_number() OVER (ORDER BY "+side+","+inputPosition+
                ") AS "+position+" FROM setRanked AS Holon WHERE __cosmicSetDuplicate=1 AND Id IN (SELECT Id FROM setMemberIds))");
        }
        else
            definitions.Add("set0 AS MATERIALIZED (SELECT Holon.*,row_number() OVER (ORDER BY "+side+","+inputPosition+
                ") AS "+position+" FROM setOccurrences AS Holon)");
        var sourceSql=Prefix()+"SELECT * FROM set0 ORDER BY "+position;
        var result=CompileEntityStages(plan.Query.Output.Source,plan.Query.Output.Stages,translator,predicate,
            definitions,"set0",position,sourceSql,translator.HasRelationship||rightTranslator.HasRelationship);
        return new SetSql {Result=result,RightTranslator=rightTranslator,RightPredicate=rightPredicate,
            Sources=new[]{new EntityCompositionSource(leftSource,plan.Options.MaximumLeftInputRows),
                new EntityCompositionSource(rightSource,plan.Options.MaximumRightInputRows)}};
    }
}

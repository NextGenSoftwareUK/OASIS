using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
public partial class HolonRepository
{
    private sealed class SetSql
    {
        internal EntityCompositionSql Result;
        internal IReadOnlyList<EntityCompositionSource> Sources;
    }
    private static SetSql CompileSetSql<T>(CosmicSetQueryPlan<T> plan,List<SqliteParameter> parameters=null) where T:IHolon
    {
        var anchor=plan.Query.Anchor;
        var translator=parameters==null?new CqlSqlTranslator(anchor.ReadAccess):new CqlSqlTranslator("Holon",parameters,anchor.ReadAccess);
        var predicate=translator.TranslateSource(anchor.Predicates);
        var rightAnchor=plan.Query.RightInput.Anchor;
        var rightTranslator=new CqlSqlTranslator("Holon",translator.Parameters,rightAnchor.ReadAccess);
        var rightPredicate=rightTranslator.TranslateSource(rightAnchor.Predicates);
        var leftChild=plan.LeftSet==null?null:CompileSetSql(plan.LeftSet,translator.Parameters);
        var rightChild=plan.RightSet==null?null:CompileSetSql(plan.RightSet,translator.Parameters);
        const string inputPosition="__cosmicSetInputPosition";
        const string side="__cosmicSetSide";
        const string position="__cosmicEntityPosition0";
        var definitions=new List<string>();
        void Input(string name,CosmicQuery<T> query,SetSql child,CqlSqlTranslator native,string test,int maximum)
        {
            if(child!=null)
            {
                // Rejoin by native identity in the same read snapshot to strip child-only
                // stage columns while retaining every ordered duplicate occurrence.
                var order="nested."+child.Result.Position;
                definitions.Add(name+" AS MATERIALIZED (SELECT stored.*,row_number() OVER (ORDER BY "+order+
                    ") AS "+inputPosition+" FROM Holon AS stored INNER JOIN ("+child.Result.Rows+
                    ") AS nested ON stored.Id=nested.Id ORDER BY "+order+" LIMIT "+native.Bind(maximum+1)+")");
                return;
            }
            var ordering=native.TranslateOrdering(query.Orderings);
            var fetch=Math.Min(query.Limit??maximum+1,maximum+1);
            definitions.Add(name+" AS MATERIALIZED (SELECT Holon.*,row_number() OVER (ORDER BY "+ordering+
                ") AS "+inputPosition+" FROM Holon WHERE "+test+" ORDER BY "+ordering+
                " LIMIT "+native.Bind(fetch)+" OFFSET "+native.Bind(query.Offset)+")");
        }
        Input("setLeft",plan.Query.Left,leftChild,translator,predicate,plan.Options.MaximumLeftInputRows);
        Input("setRight",plan.Query.Right,rightChild,rightTranslator,rightPredicate,plan.Options.MaximumRightInputRows);
        string Prefix()=>"WITH "+string.Join(",",definitions)+" ";
        var leftSource=Prefix()+"SELECT * FROM setLeft ORDER BY "+inputPosition;
        var rightSource=Prefix()+"SELECT * FROM setRight ORDER BY "+inputPosition;
        EntityCompositionSource Proof(string sql,int maximum,SetSql child,CosmicSetQueryPlan<T> childPlan,CqlSqlTranslator native,string test)
            =>new(sql,maximum,child==null?native:null,child==null?test:null,child?.Result,
                child==null?null:new CosmicEntityCompositionOptions(Math.Max(childPlan.Options.MaximumLeftInputRows,
                    childPlan.Options.MaximumRightInputRows),childPlan.Options.MaximumInputPayloadBytes),child?.Sources);
        var sources=new[]{Proof(leftSource,plan.Options.MaximumLeftInputRows,leftChild,plan.LeftSet,translator,predicate),
            Proof(rightSource,plan.Options.MaximumRightInputRows,rightChild,plan.RightSet,rightTranslator,rightPredicate)};
        definitions.Add("setOccurrences AS MATERIALIZED (SELECT Holon.*,0 AS "+side+
            " FROM setLeft AS Holon UNION ALL SELECT Holon.*,1 AS "+side+" FROM setRight AS Holon)");
        if(plan.Query.Kind!=CosmicSetQueryKind.Concat)
        {
            var operation=plan.Query.Kind switch {CosmicSetQueryKind.Union=>"UNION",CosmicSetQueryKind.Intersect=>"INTERSECT",
                CosmicSetQueryKind.Except=>"EXCEPT",_=>throw new NotSupportedException("Unknown native set operation.")};
            definitions.Add("setMemberIds AS MATERIALIZED (SELECT Id FROM setLeft "+operation+" SELECT Id FROM setRight)");
            definitions.Add("setRanked AS MATERIALIZED (SELECT Holon.*,row_number() OVER (PARTITION BY Id ORDER BY "+
                side+","+inputPosition+") AS __cosmicSetDuplicate FROM setOccurrences AS Holon)");
            definitions.Add("set0 AS MATERIALIZED (SELECT Holon.*,row_number() OVER (ORDER BY "+side+","+inputPosition+
                ") AS "+position+" FROM setRanked AS Holon WHERE __cosmicSetDuplicate=1 AND Id IN (SELECT Id FROM setMemberIds))");
        }
        else definitions.Add("set0 AS MATERIALIZED (SELECT Holon.*,row_number() OVER (ORDER BY "+side+","+inputPosition+
            ") AS "+position+" FROM setOccurrences AS Holon)");
        var sourceSql=Prefix()+"SELECT * FROM set0 ORDER BY "+position;
        var result=CompileEntityStages(plan.Query.Output.Source,plan.Query.Output.Stages,translator,predicate,definitions,
            "set0",position,sourceSql,translator.HasRelationship||rightTranslator.HasRelationship
                ||leftChild?.Result.HasRelationship==true||rightChild?.Result.HasRelationship==true);
        return new SetSql{Result=result,Sources=sources};
    }
}

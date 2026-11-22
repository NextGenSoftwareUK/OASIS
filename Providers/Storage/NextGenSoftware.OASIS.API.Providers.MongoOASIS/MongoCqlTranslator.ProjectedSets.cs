using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
internal sealed class MongoCqlProjectedSetCommand
{
    internal MongoCqlCompositionCommand Left,Right;
    internal BsonDocument[] Members;
    internal BsonDocument[][] Stages;
}
internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlProjectedSetCommand CompileProjectedSet<T,TResult>(CosmicProjectedSetPlan<T,TResult> plan,
        CosmicProjectionCompositionExecutionLimits leftLimits,CosmicProjectionCompositionExecutionLimits rightLimits,
        string collection) where T:IHolon
    {
        var left=CompileSelected(plan.Query.Left,leftLimits);
        var right=CompileSelected(plan.Query.Right,rightLimits);
        List<BsonDocument> Input(MongoCqlCompositionCommand command,CosmicProjectionQuery<T,TResult> query,int side)
        {
            var rows=command.Rows.Select(stage=>(BsonDocument)stage.DeepClone()).ToList();
            rows.Add(new("$setWindowFields",new BsonDocument{{"sortBy",new BsonDocument("rank",1)},
                {"output",new BsonDocument("occurrence",CompositionRank())}}));
            var fields=new BsonDocument{{"_id",0},{"side",Literal(side)},
                {"index",new BsonDocument("$subtract",new BsonArray{"$occurrence",1L})}};
            for(var index=0;index<query.OutputColumnIndices.Count;index++)
                fields.Add("k"+index,"$k"+query.OutputColumnIndices[index]);
            rows.Add(new("$project",fields));return rows;
        }
        var current=Input(left,plan.Query.Left,0);
        current.Add(new("$unionWith",new BsonDocument{{"coll",collection},
            {"pipeline",new BsonArray(Input(right,plan.Query.Right,1))}}));
        if(plan.Query.Kind!=CosmicSetQueryKind.Concat)
        {
            var tuple=new BsonDocument();
            for(var index=0;index<plan.Query.ValueTypes.Count;index++)
                tuple.Add("v"+index,new BsonDocument("$ifNull",new BsonArray{"$k"+index+".value",BsonNull.Value}));
            current.Add(new("$sort",new BsonDocument{{"side",1},{"index",1}}));
            current.Add(new("$group",new BsonDocument{{"_id",tuple},{"firstRow",new BsonDocument("$first","$$ROOT")},
                {"left",new BsonDocument("$max",new BsonDocument("$cond",new BsonArray{
                    new BsonDocument("$eq",new BsonArray{"$side",0}),1,0}))},
                {"right",new BsonDocument("$max",new BsonDocument("$cond",new BsonArray{
                    new BsonDocument("$eq",new BsonArray{"$side",1}),1,0}))}}));
            if(plan.Query.Kind==CosmicSetQueryKind.Intersect)
                current.Add(new("$match",new BsonDocument{{"left",1},{"right",1}}));
            else if(plan.Query.Kind==CosmicSetQueryKind.Except)
                current.Add(new("$match",new BsonDocument{{"left",1},{"right",0}}));
            else if(plan.Query.Kind!=CosmicSetQueryKind.Union)throw new NotSupportedException("Unknown selected-set operation.");
            current.Add(new("$replaceRoot",new BsonDocument("newRoot","$firstRow")));
        }
        current.Add(new("$setWindowFields",new BsonDocument{{"sortBy",new BsonDocument{{"side",1},{"index",1}}},
            {"output",new BsonDocument{{"rank",CompositionRank()},{"stageCount",CompositionCount()}}}}));
        current.Add(new("$sort",new BsonDocument("rank",1)));
        var members=current.Select(stage=>(BsonDocument)stage.DeepClone()).ToList();
        members.Add(new("$project",new BsonDocument{{"_id",0},{"side",1},{"index",1},{"rank",1},{"stageCount",1}}));
        ValidatePipeline(members);
        var stages=CompileSelectedStages(plan.Query.Output,current,occurrences:true);
        return new(){Left=left,Right=right,Members=members.ToArray(),Stages=stages};
    }
}

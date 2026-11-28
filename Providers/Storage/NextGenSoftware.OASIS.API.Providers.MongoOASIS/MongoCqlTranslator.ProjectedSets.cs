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
    internal MongoCqlProjectedSetCommand LeftSet,RightSet;
    internal BsonDocument[] Rows;
    internal BsonDocument[] Members;
    internal BsonDocument[][] Stages;
}
internal sealed partial class MongoCqlTranslator
{
    internal static BsonDocument[] CompileProjectedSetCount(BsonDocument[] input)
    {
        var stages=input.Select(stage=>(BsonDocument)stage.DeepClone()).ToList();
        stages.Add(new("$facet",new BsonDocument("count",new BsonArray{new BsonDocument("$count","value")})));
        stages.Add(new("$project",new BsonDocument{{"_id",0},{"nativeCount",new BsonDocument("$ifNull",new BsonArray{
            new BsonDocument("$arrayElemAt",new BsonArray{"$count.value",0}),0L})}}));
        ValidatePipeline(stages);return stages.ToArray();
    }
    internal BsonDocument[] CompileProjectedSetNumeric(BsonDocument[] input,CosmicProjectedNumericIntent intent)
    {
        var rows=input.Select(stage=>(BsonDocument)stage.DeepClone()).ToList();
        rows.Add(new("$project",new BsonDocument{{"_id",0},{"rank",1},{"values",new BsonArray(
            Enumerable.Range(0,intent.ColumnIndex+1).Select(index=>(BsonValue)("$k"+index+".value")))}}));
        return SelectedIntegerReduction(rows,intent);
    }
    internal static CosmicProjectionCompositionExecutionLimits ProjectedSetInputLimits(CosmicProjectionCompositionOptions options)
        =>new(options.MaximumInputRows,options.MaximumInputPayloadBytes,options.MaximumStageRows,
            options.MaximumProofPayloadBytes,10000,options.MaximumInputPayloadBytes);
    internal MongoCqlProjectedSetCommand CompileProjectedSet<T,TResult>(CosmicProjectedSetPlan<T,TResult> plan,
        CosmicProjectionCompositionExecutionLimits leftLimits,CosmicProjectionCompositionExecutionLimits rightLimits,
        string collection) where T:IHolon
    {
        var left=plan.LeftSet==null?CompileSelected(plan.Query.Left,leftLimits):null;
        var right=plan.RightSet==null?CompileSelected(plan.Query.Right,rightLimits):null;
        MongoCqlProjectedSetCommand Child(CosmicProjectedSetPlan<T,TResult> child)
            =>child==null?null:CompileProjectedSet(child,ProjectedSetInputLimits(child.LeftInputOptions),
                ProjectedSetInputLimits(child.RightInputOptions),collection);
        var leftSet=Child(plan.LeftSet);var rightSet=Child(plan.RightSet);
        List<BsonDocument> Input(BsonDocument[] pipeline,CosmicProjectionQuery<T,TResult> query,int side)
        {
            var rows=pipeline.Select(stage=>(BsonDocument)stage.DeepClone()).ToList();
            rows.Add(new("$setWindowFields",new BsonDocument{{"sortBy",new BsonDocument("rank",1)},
                {"output",new BsonDocument("occurrence",CompositionRank())}}));
            var fields=new BsonDocument{{"_id",0},{"side",Literal(side)},
                {"index",new BsonDocument("$subtract",new BsonArray{"$occurrence",1L})}};
            for(var index=0;index<query.OutputColumnIndices.Count;index++)
                fields.Add("k"+index,"$k"+query.OutputColumnIndices[index]);
            rows.Add(new("$project",fields));return rows;
        }
        var current=Input(leftSet?.Rows??left.Rows,plan.Query.LeftInput.Anchor,0);
        current.Add(new("$unionWith",new BsonDocument{{"coll",collection},
            {"pipeline",new BsonArray(Input(rightSet?.Rows??right.Rows,plan.Query.RightInput.Anchor,1))}}));
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
        var rawRows=current.Select(stage=>(BsonDocument)stage.DeepClone()).ToArray();
        ValidatePipeline(current);
        return new(){Left=left,Right=right,LeftSet=leftSet,RightSet=rightSet,Rows=rawRows,Members=members.ToArray(),Stages=stages};
    }
}

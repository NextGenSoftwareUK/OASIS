using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
internal sealed class MongoCqlSetProjectionCompositionCommand
{
    internal BsonDocument[] Input;
    internal BsonDocument[][] Stages;
    internal BsonDocument[] Rows;
}
internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlSetProjectionCompositionCommand CompileSetProjectionComposition<T,TResult>(
        CosmicSetProjectionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,bool terminalSource=false) where T:IHolon
    {
        if(plan==null || limits==null || !terminalSource&&!plan.RequiresSelectedComposition || limits.Admit(plan.SelectedOptions).IsError)
            throw new ArgumentException("One admitted late entity-set selected pipeline is required.");
        var source=CompileSet(plan.Source,Math.Max(1,limits.MaximumInputRows));
        var current=source.Pipeline.Take(source.Pipeline.Length-1)
            .Select(stage=>(BsonDocument)stage.DeepClone()).ToList();
        current.Add(new("$limit",limits.MaximumInputRows+1));
        // Preserve the already compiled complete entity-stage occurrence order.
        // Never use entity Id as occurrence identity: Concat may repeat it.
        current.Add(new("$group",new BsonDocument{{"_id",BsonNull.Value},{"rows",new BsonDocument("$push","$$ROOT")}}));
        current.Add(new("$unwind",new BsonDocument{{"path","$rows"},{"includeArrayIndex","occurrence"}}));
        current.Add(new("$replaceRoot",new BsonDocument("newRoot",new BsonDocument("$mergeObjects",new BsonArray{
            "$rows",new BsonDocument("__cosmicSelectedOccurrence","$occurrence")}))));
        var fields=new BsonDocument{{"_id",0},{"side",Literal(0)},{"index","$__cosmicSelectedOccurrence"},
            {"rank",new BsonDocument("$add",new BsonArray{"$__cosmicSelectedOccurrence",1L})}};
        var cells=new BsonArray();
        for(var index=0;index<plan.Query.Selection.Columns.Count;index++)
        {
            fields.Add("k"+index,OrderingKey(plan.Query.Selection.Columns[index],"setSelected"+index));
            cells.Add(ProjectionCell(plan.Query.Selection.Columns[index],"setValue"+index));
        }
        fields.Add("values",cells);
        current.Add(new("$project",fields));
        current.Add(new("$setWindowFields",new BsonDocument{{"sortBy",new BsonDocument("rank",1)},
            {"output",new BsonDocument("stageCount",CompositionCount())}}));
        current.Add(new("$sort",new BsonDocument("rank",1)));
        var input=current.Select(stage=>(BsonDocument)stage.DeepClone()).ToList();
        input.Add(new("$project",new BsonDocument{{"_id",0},{"side",1},{"index",1},{"rank",1},{"stageCount",1}}));
        ValidatePipeline(input);
        var stages=CompileSelectedStages(plan.Query.Selection,current,occurrences:true);
        ValidatePipeline(current);
        return new(){Input=input.ToArray(),Stages=stages,Rows=current.Select(stage=>(BsonDocument)stage.DeepClone()).ToArray()};
    }
}

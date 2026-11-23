using System;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlProjectionCommand CompileSetProjection<T,TResult>(
        CosmicSetProjectionPlan<T,TResult> plan,int maximumResults) where T:IHolon
    {
        if(plan==null || maximumResults<0 || maximumResults>CosmicProjectionResults.MaximumResultLimit)
            throw new ArgumentException("An admitted bounded late set projection is required.");
        var set=CompileSet(plan.Source,Math.Max(1,maximumResults));
        // Keep both input guards and the complete set/entity sequence. Only replace
        // the final publication sentinel; selected domains are evaluated afterwards.
        var pipeline=set.Pipeline.Take(set.Pipeline.Length-1)
            .Select(stage=>(BsonDocument)stage.DeepClone()).ToList();
        pipeline.Add(new("$limit",maximumResults+1));
        var cells=new BsonArray();
        for(var index=0;index<plan.Selection.Query.Columns.Count;index++)
            cells.Add(ProjectionCell(plan.Selection.Query.Columns[index],"cqlsetp"+index));
        pipeline.Add(new("$project",new BsonDocument{{"_id",0},{"cells",cells}}));
        ValidatePipeline(pipeline);
        // Existing projection repository validates all set/entity guards then reads
        // these cells in the same Mongo snapshot session, before any DTO publication.
        return new MongoCqlProjectionCommand(pipeline.ToArray(),set.RequiresMembership,
            true,false,false,set.EntityComposition);
    }
}

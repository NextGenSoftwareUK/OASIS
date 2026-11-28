using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
internal sealed partial class MongoCqlTranslator
{
    private const string SetPositionField="__cosmicSetPosition";
    internal MongoCqlCommand CompileSet<T>(CosmicSetQueryPlan<T> plan,int maximumResults,bool applyPublicationLimit=true) where T:IHolon
    {
        if(plan==null || maximumResults<1 || maximumResults>10000)
            throw new ArgumentException("An admitted bounded native set plan is required.");
        MongoCqlEntityInput Input(CosmicQuery<T> query,CosmicSetQueryPlan<T> subtree,int maximum)
        {
            if(subtree!=null)return new MongoCqlEntityInput(CompileSet(subtree,maximum),maximum);
            var pipeline=CompileSource(query,out var envelope);
            var fetch=Math.Min(query.Limit??maximum+1,maximum+1);
            var page=SelectedWindow(query,fetch);
            if(envelope)FinishDescendantEnvelope(pipeline,page);else pipeline.AddRange(page);
            ValidatePipeline(pipeline);
            return new MongoCqlEntityInput(new MongoCqlCommand(pipeline.ToArray(),fetch,
                query.Predicates.Any(HasRelationships),envelope),maximum);
        }
        var left=Input(plan.Query.Left,plan.LeftSet,plan.Options.MaximumLeftInputRows);
        var right=Input(plan.Query.Right,plan.RightSet,plan.Options.MaximumRightInputRows);
        List<BsonDocument> Occurrences(MongoCqlEntityInput input,int side)
        {
            var current=input.Command.Pipeline.Select(x=>(BsonDocument)x.DeepClone()).ToList();
            if(input.Command.HasTraversalEnvelope)
            {
                // Both complete protected inputs are validated by the same
                // snapshot owner before this combined pipeline is read.
                current.Add(new("$match",new BsonDocument("error",0)));
                current.Add(new("$replaceRoot",new BsonDocument("newRoot","$row")));
            }
            current.Add(new("$group",new BsonDocument {{"_id",BsonNull.Value},{"rows",new BsonDocument("$push","$$ROOT")}}));
            current.Add(new("$unwind",new BsonDocument {{"path","$rows"},{"includeArrayIndex","position"}}));
            current.Add(new("$project",new BsonDocument {
                {"_id",0},{"row","$rows"},{"position",1},{"side",Literal(side)}}));
            return current;
        }
        var pipeline=Occurrences(left,0);
        pipeline.Add(new("$unionWith",new BsonDocument {{"coll","Holon"},{"pipeline",new BsonArray(Occurrences(right,1))}}));
        pipeline.Add(new("$sort",new BsonDocument {{"side",1},{"position",1}}));
        if(plan.Query.Kind!=CosmicSetQueryKind.Concat)
        {
            pipeline.Add(new("$group",new BsonDocument {
                {"_id","$row.HolonId"},{"row",new BsonDocument("$first","$row")},
                {"side",new BsonDocument("$first","$side")},{"position",new BsonDocument("$first","$position")},
                {"sides",new BsonDocument("$addToSet","$side")}}));
            var inLeft=Op("$in",Literal(0),"$sides");
            var inRight=Op("$in",Literal(1),"$sides");
            if(plan.Query.Kind==CosmicSetQueryKind.Intersect)
                pipeline.Add(new("$match",new BsonDocument("$expr",Op("$and",inLeft,inRight))));
            else if(plan.Query.Kind==CosmicSetQueryKind.Except)
                pipeline.Add(new("$match",new BsonDocument("$expr",Op("$and",inLeft,Op("$not",inRight)))));
            else if(plan.Query.Kind!=CosmicSetQueryKind.Union)
                throw new NotSupportedException("Unknown native set operation.");
            pipeline.Add(new("$sort",new BsonDocument {{"side",1},{"position",1}}));
        }
        // Occurrence order is provider-owned and overwritten, never taken from
        // stored/user sort state. It is removed from every decoded proof/result.
        pipeline.Add(new("$replaceRoot",new BsonDocument("newRoot",new BsonDocument("$mergeObjects",new BsonArray {
            "$row",new BsonDocument(SetPositionField,new BsonDocument {{"side","$side"},{"position","$position"}})}))));
        ValidatePipeline(pipeline);
        var maximum=checked(plan.Options.MaximumLeftInputRows+plan.Options.MaximumRightInputRows);
        var membership=left.Command.RequiresMembership||right.Command.RequiresMembership;
        var clean=pipeline.Select(x=>(BsonDocument)x.DeepClone()).ToList();
        clean.Add(new("$unset",SetPositionField));
        var source=new MongoCqlCommand(clean.ToArray(),maximum+1,membership);
        var options=new CosmicEntityCompositionOptions(Math.Min(maximum,10000),plan.Options.MaximumInputPayloadBytes);
        return CompileEntityStages(source,plan.Query.Output.Source,plan.Query.Output.Stages,options,maximumResults,
            pipeline.ToArray(),SetPositionField,new[]{left,right},maximum,applyPublicationLimit);
    }
}

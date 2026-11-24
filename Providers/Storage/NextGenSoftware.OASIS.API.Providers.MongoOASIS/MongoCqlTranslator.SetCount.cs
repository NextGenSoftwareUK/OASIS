using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlScalarCommand CompileSetCount<T>(CosmicSetQueryPlan<T> plan) where T:IHolon
    {
        var entity=CompileSet(plan,10000);
        // Native source/stage proof commands run under the scalar snapshot owner
        // before this reduction. Remove only the row-publication sentinel cap.
        var pipeline=entity.Pipeline.Take(entity.Pipeline.Length-1).Select(stage=>(BsonDocument)stage.DeepClone()).ToList();
        pipeline.Add(new("$count","count"));
        pipeline.Add(new("$project",new BsonDocument { { "_id",0 }, { "count",1 }, { "error",Literal(0) } }));
        ValidatePipeline(pipeline);
        return new MongoCqlScalarCommand(pipeline.ToArray(),entity.RequiresMembership,true,false,
            entityComposition:entity.EntityComposition);
    }

}

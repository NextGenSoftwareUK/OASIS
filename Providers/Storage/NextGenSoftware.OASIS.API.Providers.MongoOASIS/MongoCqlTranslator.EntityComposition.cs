using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed class MongoCqlEntityCompositionCommand
{
    internal MongoCqlCommand Source { get; }
    internal MongoCqlCommand[] Stages { get; }
    internal CosmicEntityCompositionOptions Options { get; }
    internal MongoCqlEntityCompositionCommand(MongoCqlCommand source, MongoCqlCommand[] stages, CosmicEntityCompositionOptions options)
    { Source = source; Stages = stages; Options = options; }
}

internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlScalarCommand CompileEntityCount<T>(CosmicEntityCompositionPlan<T> plan) where T:IHolon
    {
        var entity=CompileEntityComposition(plan,10000);
        // Native source/stage proof commands run under the scalar snapshot owner
        // before this reduction. Remove only the row-publication sentinel cap.
        var pipeline=entity.Pipeline.Take(entity.Pipeline.Length-1).Select(stage=>(BsonDocument)stage.DeepClone()).ToList();
        pipeline.Add(new("$count","count"));
        pipeline.Add(new("$project",new BsonDocument { { "_id",0 }, { "count",1 }, { "error",Literal(0) } }));
        ValidatePipeline(pipeline);
        return new MongoCqlScalarCommand(pipeline.ToArray(),entity.RequiresMembership,true,false,
            entityComposition:entity.EntityComposition);
    }

    internal MongoCqlCommand CompileEntityComposition<T>(CosmicEntityCompositionPlan<T> plan, int maximumResults) where T : IHolon
    {
        if (plan == null || plan.Query.Stages.Count == 0 || maximumResults < 1 || maximumResults > 10000)
            throw new ArgumentException("A bounded admitted native entity pipeline is required.");
        // The input bound belongs to composition, not the final graph budget:
        // a stage may narrow many source roots to one expanded result.
        var sourceQuery = plan.Query.Source;
        var sourcePipeline = CompileSource(sourceQuery, out var sourceEnvelope);
        var fetch = sourceQuery.Limit.HasValue ? Math.Min(sourceQuery.Limit.Value, plan.Options.MaximumInputRows + 1)
            : plan.Options.MaximumInputRows + 1;
        var page = SelectedWindow(sourceQuery, fetch);
        if (sourceEnvelope) FinishDescendantEnvelope(sourcePipeline, page); else sourcePipeline.AddRange(page);
        ValidatePipeline(sourcePipeline);
        var source = new MongoCqlCommand(sourcePipeline.ToArray(), fetch,
            sourceQuery.Predicates.Any(HasRelationships), sourceEnvelope);
        var current = source.Pipeline.Select(stage => (BsonDocument)stage.DeepClone()).ToList();
        if (source.HasTraversalEnvelope)
        {
            // The snapshot owner validates the complete source command before
            // any stage runs. These outcomes cannot change between commands.
            current.Add(new("$match", new BsonDocument("error", 0)));
            current.Add(new("$replaceRoot", new BsonDocument("newRoot", "$row")));
        }
        var stages = new List<MongoCqlCommand>();
        var membership = source.RequiresMembership;
        foreach (var stage in plan.Query.Stages)
        {
            var envelope = false;
            if (stage is CosmicEntityFilterStage<T> filter)
            {
                var relationships = filter.Predicate.Predicates.Any(HasRelationships);
                var errors = new List<BsonValue>();
                var matched = AppendAllPredicate(current, filter.Predicate.Predicates, plan.Query.Source.ReadAccess, errors);
                current.Add(new("$project", new BsonDocument
                {
                    { "_id", 0 }, { "row", relationships ? "$row" : "$$ROOT" },
                    { "matched", matched }, { "error", AllErrorCode(errors) }
                }));
                // Never discard a protected failure merely because the Boolean
                // predicate is false. Complete stage proof precedes later pages.
                current.Add(new("$match", new BsonDocument("$expr", Op("$or",
                    Op("$ne", "$error", Literal(0)), "$matched"))));
                envelope = true;
                membership |= relationships;
            }
            else if (stage is CosmicEntityOrderStage ordered)
            {
                var keys = new BsonDocument(); var sort = new BsonDocument();
                for (var index = 0; index < ordered.Orderings.Count; index++)
                {
                    var item = ordered.Orderings[index]; var key = "entitySort" + index;
                    keys.Add(key, OrderingKey(item.Path, key));
                    sort.Add(key + ".rank", item.Descending ? -1 : 1);
                    sort.Add(key + ".value", item.Descending ? -1 : 1);
                }
                // Wrap native records so user fields cannot shadow sort state.
                var wrapper = new BsonDocument { { "_id", 0 }, { "row", "$$ROOT" } };
                foreach (var key in keys) wrapper.Add(key);
                current.Add(new("$project", wrapper));
                sort.Add("row.HolonId", 1);
                current.Add(new("$sort", sort));
                current.Add(new("$replaceRoot", new BsonDocument("newRoot", "$row")));
            }
            else if (stage is CosmicEntityWindowStage window)
            {
                if (window.Kind == CosmicSelectedWindowKind.Take)
                    current.Add(window.Count == 0 ? new("$match", new BsonDocument("$expr", Literal(false))) : new("$limit", window.Count));
                else if (window.Count > 0) current.Add(new("$skip", window.Count));
            }
            else throw new NotSupportedException("Unknown native entity stage.");
            ValidatePipeline(current);
            stages.Add(new MongoCqlCommand(current.Select(step => (BsonDocument)step.DeepClone()).ToArray(),
                plan.Options.MaximumInputRows + 1, membership, envelope));
            if (envelope)
            {
                current.Add(new("$match", new BsonDocument("error", 0)));
                current.Add(new("$replaceRoot", new BsonDocument("newRoot", "$row")));
            }
        }
        // All protected outcomes in the last stage have already been checked
        // by the same snapshot owner before this final output bound is applied.
        current.Add(new("$limit", maximumResults + 1));
        ValidatePipeline(current);
        return new MongoCqlCommand(current.ToArray(), maximumResults + 1, membership,
            entityComposition: new MongoCqlEntityCompositionCommand(source, stages.ToArray(), plan.Options));
    }
}

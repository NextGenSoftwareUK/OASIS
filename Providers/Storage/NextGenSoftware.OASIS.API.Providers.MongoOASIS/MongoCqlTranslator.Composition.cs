using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed class MongoCqlCompositionCommand
{
    internal MongoCqlProjectionCommand Source { get; }
    internal BsonDocument[][] Stages { get; }
    internal BsonDocument[] Reduction { get; }
    internal BsonDocument[] Cardinality { get; }
    internal MongoCqlCompositionCommand(MongoCqlProjectionCommand source, BsonDocument[][] stages, BsonDocument[] reduction = null, BsonDocument[] cardinality = null)
    { Source = source; Stages = stages; Reduction = reduction; Cardinality = cardinality; }
}

internal sealed partial class MongoCqlTranslator
{
    // Enrollment requires a snapshot owner that validates Source before running
    // any stage command. Every command must share that session and deadline.
    internal MongoCqlCompositionCommand CompileComposition<T, TResult>(
        CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits) where T : IHolon
    {
        if (plan == null || limits == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != ProviderType.MongoDBOASIS
            || limits.Admit(plan.Options).IsError || plan.Query.ResultStages.Count == 0 && !plan.RequestsCardinality)
            throw new ArgumentException("Mongo composition needs one matching plan and admitted remaining work.");
        return CompileSelected(plan.Query, limits, cardinality: plan.RequestsCardinality);
    }

    internal MongoCqlCompositionCommand CompileProjectedAll<T, TResult>(
        CosmicProjectedAllPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits) where T : IHolon
    {
        if (plan == null || limits == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != ProviderType.MongoDBOASIS
            || limits.Admit(plan.Options).IsError)
            throw new ArgumentException("Mongo projected All needs one matching plan and admitted remaining work.");
        return CompileSelected(plan.Query.Projection, limits, plan.Query.Predicate);
    }

    private MongoCqlCompositionCommand CompileSelected<T, TResult>(CosmicProjectionQuery<T, TResult> query,
        CosmicProjectionCompositionExecutionLimits limits, CosmicQueryPredicate terminal = null, bool cardinality = false) where T : IHolon
    {
        if (query.Source.GraphOptions != null) throw new NotSupportedException("Composed graph sources require separate native enrollment.");
        var source = CompileSource(query.Source, out var traverses);
        var fetch = query.Source.Limit.HasValue ? Math.Min(query.Source.Limit.Value, limits.MaximumInputRows + 1) : limits.MaximumInputRows + 1;
        var page = SelectedWindow(query.Source, fetch);
        var proof = new BsonDocument
        {
            { "_id", 0 }, { "id", "$HolonId" }, { "visible", Op("$ifNull", "$IsPublic", Literal(false)) },
            { "creator", Op("$ifNull", "$CreatedByAvatarId", Literal(Guid.Empty.ToString("D"))) }
        };
        var values = new BsonArray(); var ordering = new BsonArray(); var sourceSort = new BsonDocument();
        for (var index = 0; index < query.Columns.Count; index++)
        {
            values.Add(ProjectionCell(query.Columns[index].Path, "composedValue" + index));
            proof.Add("k" + index, OrderingKey(query.Columns[index].Path, "composedComparable" + index));
        }
        for (var index = 0; index < query.Source.Orderings.Count; index++)
        {
            var item = query.Source.Orderings[index]; var name = "o" + index; var direction = item.Descending ? -1 : 1;
            ordering.Add(ProjectionCell(item.Path, "composedOrdering" + index));
            proof.Add(name, OrderingKey(item.Path, "composedSourceSort" + index));
            sourceSort.Add(name + ".rank", direction); sourceSort.Add(name + ".value", direction);
        }
        sourceSort.Add("id", 1); proof.Add("values", values); proof.Add("ordering", ordering);
        page.Add(new("$project", proof));
        page.Add(new("$setWindowFields", new BsonDocument
        {
            { "sortBy", sourceSort },
            { "output", new BsonDocument { { "rank", CompositionRank() }, { "nativeCount", CompositionCount() } } }
        }));
        page.Add(new("$sort", (BsonDocument)sourceSort.DeepClone()));
        if (traverses) FinishDescendantEnvelope(source, page); else source.AddRange(page);
        ValidatePipeline(source);
        var membership = query.Source.Predicates.Any(HasRelationships);
        var command = new MongoCqlProjectionCommand(source.ToArray(), membership, true, traverses, fetch == 0);
        var current = source.Select(stage => (BsonDocument)stage.DeepClone()).ToList();
        if (traverses)
        {
            // Full scope errors are validated by the snapshot owner first.
            current.Add(new("$match", new BsonDocument("error", 0)));
            current.Add(new("$replaceRoot", new BsonDocument("newRoot", "$row")));
        }
        var stages = new List<BsonDocument[]>();
        foreach (var stage in query.ResultStages)
        {
            if (stage is CosmicSelectedFilterStage filter)
                current.Add(new("$match", new BsonDocument("$expr", Selected(filter.Predicate))));
            else if (stage is CosmicSelectedOrderStage sorted)
            {
                // Stable least-significant-first ordering avoids duplicate BSON
                // keys and Mongo's compound-sort field limit. Every pass retains
                // the previous rank as its deterministic tie, entirely natively.
                foreach (var item in sorted.Orderings.Reverse())
                {
                    var sort = new BsonDocument { { "k" + item.ColumnIndex + ".value", item.Descending ? -1 : 1 }, { "rank", 1 } };
                    current.Add(new("$setWindowFields", new BsonDocument
                    {
                        { "sortBy", sort }, { "output", new BsonDocument("nextRank", CompositionRank()) }
                    }));
                    current.Add(new("$set", new BsonDocument("rank", "$nextRank")));
                    current.Add(new("$unset", "nextRank"));
                }
            }
            else if (stage is CosmicSelectedReverseStage)
            {
                current.Add(new("$setWindowFields", new BsonDocument
                {
                    { "sortBy", new BsonDocument("rank", -1) },
                    { "output", new BsonDocument("nextRank", CompositionRank()) }
                }));
                current.Add(new("$set", new BsonDocument("rank", "$nextRank")));
                current.Add(new("$unset", "nextRank"));
            }
            else if (stage is CosmicSelectedWindowStage window)
            {
                // A window reads the previous stage's deterministic rank.
                current.Add(new("$sort", new BsonDocument("rank", 1)));
                if (window.Kind == CosmicSelectedWindowKind.Take)
                    current.Add(window.Count == 0 ? new("$match", new BsonDocument("$expr", Literal(false))) : new("$limit", window.Count));
                else if (window.Count != 0) current.Add(new("$skip", window.Count));
            }
            else throw new NotSupportedException("Unknown composed result stage.");
            current.Add(new("$setWindowFields", new BsonDocument
            {
                { "sortBy", new BsonDocument("rank", 1) }, { "output", new BsonDocument("stageCount", CompositionCount()) }
            }));
            current.Add(new("$sort", new BsonDocument("rank", 1)));
            var output = current.Select(step => (BsonDocument)step.DeepClone()).ToList();
            output.Add(new("$project", new BsonDocument { { "_id", 0 }, { "id", 1 }, { "rank", 1 }, { "stageCount", 1 } }));
            ValidatePipeline(output); stages.Add(output.ToArray());
        }
        BsonDocument[] reduction = null;
        if (terminal != null)
        {
            var pipeline = current.Select(step => (BsonDocument)step.DeepClone()).ToList();
            pipeline.Add(new("$group", new BsonDocument
            {
                { "_id", BsonNull.Value }, { "selectedCount", new BsonDocument("$sum", 1) },
                { "counterexamples", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray { Selected(terminal), 0, 1 })) }
            }));
            ValidatePipeline(pipeline); reduction = pipeline.ToArray();
        }
        BsonDocument[] countPipeline = null;
        if (cardinality)
        {
            var count = current.Select(step => (BsonDocument)step.DeepClone()).ToList();
            count.Add(new("$facet", new BsonDocument("counts", new BsonArray { new BsonDocument("$count", "value") })));
            count.Add(new("$project", new BsonDocument
            {
                { "_id", 0 }, { "selectedCount", new BsonDocument("$ifNull", new BsonArray
                    { new BsonDocument("$arrayElemAt", new BsonArray { "$counts.value", 0 }), 0L }) }
            }));
            ValidatePipeline(count); countPipeline = count.ToArray();
        }
        return new MongoCqlCompositionCommand(command, stages.ToArray(), reduction, countPipeline);

        BsonValue Selected(CosmicQueryPredicate predicate) => SelectedFilterPredicate(predicate, path =>
        {
            var index = !path.IsMetadata && path.Segments.Count == 1
                ? query.Columns.ToList().FindIndex(column => column.Alias == path.Segments[0] && column.Path.ValueType == path.ValueType) : -1;
            if (index < 0) throw new NotSupportedException("Selected predicate must bind an exact selected scalar.");
            return Op("$ifNull", "$k" + index + ".value", Literal(BsonNull.Value));
        });
    }
    // $documentNumber accepts only one sort field. A cumulative documents
    // window counts exact positions while retaining native compound tie keys.
    private static BsonDocument CompositionRank() => new()
    {
        { "$sum", 1 },
        { "window", new BsonDocument("documents", new BsonArray { "unbounded", "current" }) }
    };
    private static BsonDocument CompositionCount() => new()
    {
        { "$count", new BsonDocument() },
        { "window", new BsonDocument("documents", new BsonArray { "unbounded", "unbounded" }) }
    };
}

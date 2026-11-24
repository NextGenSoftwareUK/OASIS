using System;
using System.Linq;
using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Compiler
{
    public static CosmicProjectionQuery<Holon, CosmicProjectionRecord> CompileComposition(
        CqlCompositionV1Request request, Guid actor, ProviderType? provider)
        =>CompileComposition(request,actor,provider,null);

    public static CosmicProjectionQuery<Holon, CosmicProjectionRecord> CompileComposition(
        CqlCompositionV1Request request, Guid actor, ProviderType? provider,CosmicQueryReadAccess access)
    {
        if (request?.Source == null || request.Source.Provider != null || request.Source.Graph != null
            || request.Stages == null || request.Stages.Length is < 1 or > 16)
            throw new ArgumentException("Composition requires one source without nested provider/graph overrides and1–16 ordered stages.");
        return ApplyCompositionStages(CompileProjection(request.Source, actor, provider,access),request.Stages);
    }

    private static CosmicProjectionQuery<Holon,CosmicProjectionRecord> ApplyCompositionStages(
        CosmicProjectionQuery<Holon,CosmicProjectionRecord> query,CqlCompositionV1Stage[] stages)
    {
        if(query==null || stages==null || stages.Length is <1 or >16)
            throw new ArgumentException("Selected composition requires one captured projection and1–16 ordered stages.");
        var filterCount = 0; var orderCount = 0;
        foreach (var stage in stages)
        {
            if (stage == null) throw new ArgumentException("Composition stages cannot be null.");
            if (stage.Operation == "Where")
            {
                var filters = stage.Filters ?? Array.Empty<CqlGroupedV1Filter>();
                var any = stage.AnyOf ?? Array.Empty<CqlGroupedV1Filter>();
                filterCount = checked(filterCount + filters.Length + any.Length);
                if (stage.Order != null || stage.Count != null || stage.Columns != null || filters.Length + any.Length == 0 || filterCount > 32)
                    throw new ArgumentException("Where requires bounded selected comparisons only; at most32 across all stages.");
                var row = Expression.Parameter(typeof(CosmicProjectionRecord), "row");
                Expression Term(CqlGroupedV1Filter filter)
                {
                    if (filter == null) throw new ArgumentException("A selected comparison cannot be null.");
                    var column = VisibleColumn(filter.Alias);
                    return CompileSelectedFilter(column.ValueType, filter.Alias, filter.Operator, filter.Value, row).Body;
                }
                Expression predicate = null;
                foreach (var filter in filters) predicate = predicate == null ? Term(filter) : Expression.AndAlso(predicate, Term(filter));
                if (any.Length != 0)
                {
                    Expression alternatives = null;
                    foreach (var filter in any) alternatives = alternatives == null ? Term(filter) : Expression.OrElse(alternatives, Term(filter));
                    predicate = predicate == null ? alternatives : Expression.AndAlso(predicate, alternatives);
                }
                query = RequiredComposition(query.Where(Expression.Lambda<Func<CosmicProjectionRecord, bool>>(predicate, row)));
            }
            else if (stage.Operation == "OrderBy")
            {
                if (stage.Filters != null || stage.AnyOf != null || stage.Count != null || stage.Columns != null || stage.Order == null
                    || stage.Order.Length is < 1 or > 8 || (orderCount += stage.Order.Length) > 32)
                    throw new ArgumentException("OrderBy requires1–8 selected terms only; at most32 across all stages.");
                for (var index = 0; index < stage.Order.Length; index++)
                {
                    var term = stage.Order[index] ?? throw new ArgumentException("Ordering terms cannot be null.");
                    var column = VisibleColumn(term.Alias);
                    query = BindSelectedOrdering(column.ValueType, term.Alias, term.Descending, index > 0,
                        new CompositionOrderingConsumer(query));
                }
            }
            else if (stage.Operation == "Distinct")
            {
                if (stage.Filters != null || stage.AnyOf != null || stage.Order != null
                    || stage.Count != null || stage.Columns != null)
                    throw new ArgumentException("Distinct accepts no fields and compares exactly the currently visible record.");
                query = RequiredComposition(query.Distinct());
            }
            else if(stage.Operation=="Reverse")
            {
                if(stage.Filters!=null || stage.AnyOf!=null || stage.Order!=null || stage.Count!=null || stage.Columns!=null)
                    throw new ArgumentException("Reverse accepts no additional fields.");
                query=RequiredComposition(query.Reverse());
            }
            else if (stage.Operation is "Skip" or "Take")
            {
                if (stage.Filters != null || stage.AnyOf != null || stage.Order != null || stage.Columns != null || !stage.Count.HasValue
                    || stage.Count < 0 || stage.Count > (stage.Operation == "Take" ? 100 : 10000))
                    throw new ArgumentException("Skip/Take require only a bounded Count; Take0–100 and Skip0–10000.");
                query = RequiredComposition(stage.Operation == "Skip" ? query.Skip(stage.Count.Value) : query.Take(stage.Count.Value));
            }
            else if(stage.Operation=="Select")
            {
                if(stage.Filters!=null || stage.AnyOf!=null || stage.Order!=null || stage.Count!=null
                    || stage.Columns==null || stage.Columns.Length is <1 or >32)
                    throw new ArgumentException("Select requires one to32 explicit output bindings only.");
                if(stage.Columns.Any(column=>column==null || string.IsNullOrEmpty(column.Alias) || column.Alias.Length>128
                    || string.IsNullOrEmpty(column.SourceAlias) || column.SourceAlias.Length>128))
                    throw new ArgumentException("Selected output bindings require bounded nonempty aliases.");
                query=RequiredComposition(query.SelectOutputColumns(stage.Columns.Select(column=>
                    new CosmicProjectionOutputBinding(column.Alias,column.SourceAlias)).ToArray()));
            }
            else throw new ArgumentException("Unknown composition stage operation.");
        }
        return query;

        CosmicProjectionColumn VisibleColumn(string alias)
        {
            var index=query.OutputAliases.IndexOf(alias);
            if(index<0)throw new ArgumentException("A result stage must name an exact currently visible output alias.");
            return query.Columns[query.OutputColumnIndices[index]];
        }
    }

    private static CosmicProjectionQuery<Holon, CosmicProjectionRecord> RequiredComposition(
        OASISResult<CosmicProjectionQuery<Holon, CosmicProjectionRecord>> result)
        => result.IsError ? throw new ArgumentException(result.Message) : result.Result;

    private sealed class CompositionOrderingConsumer : ISelectedOrderingConsumer<CosmicProjectionQuery<Holon, CosmicProjectionRecord>>
    {
        private readonly CosmicProjectionQuery<Holon, CosmicProjectionRecord> query;
        internal CompositionOrderingConsumer(CosmicProjectionQuery<Holon, CosmicProjectionRecord> query) { this.query = query; }
        public CosmicProjectionQuery<Holon, CosmicProjectionRecord> Bind<TValue>(
            Expression<Func<CosmicProjectionRecord, TValue>> selector, bool descending, bool append)
            => RequiredComposition(append ? (descending ? query.ThenByDescending(selector) : query.ThenBy(selector))
                : (descending ? query.OrderByDescending(selector) : query.OrderBy(selector)));
    }
}

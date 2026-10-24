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
    public static CosmicGroupedSelection<Holon, CosmicProjectionRecord, CosmicProjectionRecord> CompileGrouped(
        CqlGroupedV1Request request, Guid actor, ProviderType? provider)
    {
        if (request == null || request.Keys == null || request.Keys.Length is < 1 or > 8
            || request.ResultOrder == null || request.ResultOrder.Length > 32
            || request.Columns == null || request.Columns.Length is < 1 or > 32 || request.MaximumGroups is < 1 or > 100)
            throw new ArgumentException("Grouping requires one to8 keys, one to32 columns and one to100 output groups.");
        var source = CompileSource(actor, provider, request.Filters, request.AnyOf, request.AnyChildren,
            request.AnyDescendants, request.Graph, request.OrderBy, request.Descending, request.Skip, request.Take);
        var options = CosmicGroupingOptions.Create(request.MaximumGroups, 1024 * 1024).Result;
        var groupingResult = source.GroupByColumns(request.Keys.Select(ProjectionColumn).ToArray(), options);
        if (groupingResult.IsError) throw new ArgumentException(groupingResult.Message);
        var grouping = groupingResult.Result;
        var bindings = request.Columns.Select(column =>
        {
            if (column == null) throw new ArgumentException("Grouped columns cannot be null.");
            // Reuse the public alias grammar even for columns with no selector.
            ValidateSelectionAlias(column.Alias);
            if (column.KeyAlias != null)
            {
                if (column.Operation != null || column.Selection != null) throw new ArgumentException("A key column cannot also aggregate.");
                var index = grouping.Key.Columns.ToList().FindIndex(key => key.Alias == column.KeyAlias);
                return RequiredBinding(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Key(grouping, column.Alias, index));
            }
            if (column.Operation is "Count" or "LongCount")
            {
                if (column.Selection != null) throw new ArgumentException("Grouped counts do not select paths.");
                return column.Operation == "Count"
                    ? RequiredBinding(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Aggregate(grouping, column.Alias, source.Count().Result))
                    : RequiredBinding(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Aggregate(grouping, column.Alias, source.LongCount().Result));
            }
            var operation = column.Operation switch
            {
                "Min" => CosmicScalarOperation.Min, "Max" => CosmicScalarOperation.Max,
                "Sum" => CosmicScalarOperation.Sum, "Average" => CosmicScalarOperation.Average,
                _ => throw new ArgumentException("Select an explicit key or supported grouped aggregate.")
            };
            var selected = column.Selection ?? throw new ArgumentException("This aggregate requires a selection.");
            return CompileSelection(new CqlProjectionV1Column { Alias = column.Alias, Field = selected.Field,
                MetadataPath = selected.MetadataPath, ValueType = selected.ValueType, Nullable = selected.Nullable },
                new GroupedSelectionConsumer(grouping, column.Alias, operation));
        }).ToArray();
        var result = grouping.SelectColumns(bindings);
        if (result.IsError) throw new ArgumentException(result.Message);
        var ordered = result.Result;
        foreach (var order in request.ResultOrder)
        {
            if (order == null) throw new ArgumentException("Result ordering cannot contain null entries.");
            var column = ordered.Columns.SingleOrDefault(candidate => candidate.Alias == order.Alias)
                ?? throw new ArgumentException("Result ordering must name an exact selected output alias.");
            ordered = BindGroupedOrdering(ordered, column.ValueType, column.Alias, order.Descending);
        }
        return ordered;
    }

    private static CosmicGroupedSelection<Holon, CosmicProjectionRecord, CosmicProjectionRecord> BindGroupedOrdering(
        CosmicGroupedSelection<Holon, CosmicProjectionRecord, CosmicProjectionRecord> query,
        Type type, string alias, bool descending)
    {
        // Bind the exact selected CLR type. This constructs an expression for the
        // shared Core owner; it never compiles or executes a selector delegate.
        if (type == typeof(string)) return Bind<string>();
        if (type == typeof(bool)) return Bind<bool>();
        if (type == typeof(bool?)) return Bind<bool?>();
        if (type == typeof(int)) return Bind<int>();
        if (type == typeof(int?)) return Bind<int?>();
        if (type == typeof(long)) return Bind<long>();
        if (type == typeof(long?)) return Bind<long?>();
        if (type == typeof(Guid)) return Bind<Guid>();
        if (type == typeof(Guid?)) return Bind<Guid?>();
        if (type == typeof(DateTime)) return Bind<DateTime>();
        if (type == typeof(DateTime?)) return Bind<DateTime?>();
        if (type == typeof(decimal)) return Bind<decimal>();
        if (type == typeof(decimal?)) return Bind<decimal?>();
        if (type == typeof(byte)) return Bind<byte>();
        if (type == typeof(byte?)) return Bind<byte?>();
        if (type == typeof(sbyte)) return Bind<sbyte>();
        if (type == typeof(sbyte?)) return Bind<sbyte?>();
        if (type == typeof(short)) return Bind<short>();
        if (type == typeof(short?)) return Bind<short?>();
        if (type == typeof(ushort)) return Bind<ushort>();
        if (type == typeof(ushort?)) return Bind<ushort?>();
        if (type == typeof(uint)) return Bind<uint>();
        if (type == typeof(uint?)) return Bind<uint?>();
        if (type == typeof(HolonType)) return Bind<HolonType>();
        if (type == typeof(HolonType?)) return Bind<HolonType?>();
        if (type == typeof(ProviderType)) return Bind<ProviderType>();
        if (type == typeof(ProviderType?)) return Bind<ProviderType?>();
        throw new ArgumentException("This grouped output domain cannot be ordered by HTTP v1.");

        CosmicGroupedSelection<Holon, CosmicProjectionRecord, CosmicProjectionRecord> Bind<TValue>()
        {
            var row = Expression.Parameter(typeof(CosmicProjectionRecord), "row");
            var cell = Expression.Call(row, typeof(CosmicProjectionRecord).GetMethod("get_Item"), Expression.Constant(alias));
            var selector = Expression.Lambda<Func<CosmicProjectionRecord, TValue>>(Expression.Convert(cell, typeof(TValue)), row);
            var result = query.Orderings.Count == 0
                ? (descending ? query.OrderByDescending(selector) : query.OrderBy(selector))
                : (descending ? query.ThenByDescending(selector) : query.ThenBy(selector));
            if (result.IsError) throw new ArgumentException(result.Message);
            return result.Result;
        }
    }

    private static CosmicGroupedBinding<Holon, CosmicProjectionRecord> RequiredBinding(
        OASISResult<CosmicGroupedBinding<Holon, CosmicProjectionRecord>> result)
    {
        if (result.IsError) throw new ArgumentException(result.Message);
        return result.Result;
    }

    private sealed class GroupedSelectionConsumer : ISelectionConsumer<CosmicGroupedBinding<Holon, CosmicProjectionRecord>>,
        IScalarConsumer<CosmicGroupedBinding<Holon, CosmicProjectionRecord>>
    {
        private readonly CosmicGroupedQuery<Holon, CosmicProjectionRecord> grouping;
        private readonly string alias;
        private readonly CosmicScalarOperation operation;
        internal GroupedSelectionConsumer(CosmicGroupedQuery<Holon, CosmicProjectionRecord> grouping, string alias, CosmicScalarOperation operation)
        { this.grouping = grouping; this.alias = alias; this.operation = operation; }
        public CosmicGroupedBinding<Holon, CosmicProjectionRecord> Bind<TValue>(string unused, Expression<Func<Holon, TValue>> selector)
            => BindScalar(grouping.Source, operation, selector, this);
        public CosmicGroupedBinding<Holon, CosmicProjectionRecord> Consume<TResult>(OASISResult<CosmicScalarQuery<Holon, TResult>> intent)
        {
            if (intent.IsError) throw new ArgumentException(intent.Message);
            return RequiredBinding(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Aggregate(grouping, alias, intent.Result));
        }
    }
}

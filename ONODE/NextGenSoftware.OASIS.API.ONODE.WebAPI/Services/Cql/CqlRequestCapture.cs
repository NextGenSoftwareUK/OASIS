using System;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Detach caller-owned wire descriptions before callbacks or awaits. The compiler still
/// owns semantic admission; these larger capture ceilings only bound snapshot allocation.</summary>
internal static class CqlRequestCapture
{
    internal static CqlFederationV1Request CaptureAndValidateFederation(CqlFederationV1Request value)
    {
        var request = Capture(value);
        if (request.Providers == null || request.ProviderPriority == null
            || request.Providers.Length > 8 || request.ProviderPriority.Length > 8
            || request.Take is < 1 or > 100 || request.Skip is < 0 or > 1000
            || request.MaximumOutputBytes is < 1 or > CqlV1Endpoint.MaximumResponseBytes
            || request.MaximumDurationMilliseconds is < 1 or > 30000
            || request.MaximumRowsPerProvider < 1 || request.MaximumRowsPerProvider > (request.EnableContinuation ? 10000 : 100)
            || request.MaximumInputRows < 1 || request.MaximumInputRows > (request.EnableContinuation ? 10000 : 800)
            || request.MaximumProviderAttempts < 1 || request.MaximumProviderAttempts > (request.EnableContinuation ? 64 : 8)
            || request.Cursor != null && (!request.EnableContinuation || request.Cursor.Length is < 1 or > 4096))
            throw new ArgumentException("Invalid bounded federation request.");
        return request;
    }

    internal static CqlFederationV1Request Capture(CqlFederationV1Request value)
    {
        if (value == null) throw new ArgumentException("A federation request is required.");
        return new CqlFederationV1Request
        {
            Scope = value.Scope, Providers = Array(value.Providers, 8, item => item),
            ProviderPriority = Array(value.ProviderPriority, 8, item => item),
            Source = Capture(value.Source), IdentityMode = value.IdentityMode,
            Skip = value.Skip, Take = value.Take, MaximumInputRows = value.MaximumInputRows,
            MaximumRowsPerProvider = value.MaximumRowsPerProvider, MaximumInputBytes = value.MaximumInputBytes,
            MaximumOutputBytes = value.MaximumOutputBytes, MaximumWork = value.MaximumWork,
            MaximumProviderAttempts = value.MaximumProviderAttempts, MaximumDurationMilliseconds = value.MaximumDurationMilliseconds,
            EnableContinuation = value.EnableContinuation, Cursor = value.Cursor
        };
    }

    internal static CqlV1Request Capture(CqlV1Request value)
    {
        if (value == null) throw new ArgumentException("A source request is required.");
        if (value.MaximumDurationMilliseconds is < 1 or > 30000)
            throw new ArgumentException("Source execution duration is outside its bounded domain.");
        var filterCount = 0;
        CqlV1Filter Filter(CqlV1Filter filter)
        {
            if (filter == null || ++filterCount > 64) throw new ArgumentException("Filter capture is incomplete or oversized.");
            var scalar = filter.Value;
            void BoundScalar(JsonElement item, bool member)
            {
                if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                    || member && item.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                    || item.ValueKind == JsonValueKind.String && item.GetString()!.Length > 512
                    || item.ValueKind == JsonValueKind.Number && item.GetRawText().Length > 64)
                    throw new ArgumentException("Filter capture requires bounded scalar literals.");
            }
            // Capture freezes the same bounded membership accepted by the compiler.
            // It does not expand it into point reads or interpret literal types.
            if (filter.Operator == CqlV1Operator.In)
            {
                if (scalar.ValueKind != JsonValueKind.Array || scalar.GetArrayLength() is < 1 or > 100)
                    throw new ArgumentException("Membership capture requires one to100 literals.");
                foreach (var item in scalar.EnumerateArray()) BoundScalar(item, true);
            }
            else BoundScalar(scalar, false);
            return new CqlV1Filter
            {
                Field = filter.Field, MetadataPath = filter.MetadataPath == null ? null : Array(filter.MetadataPath, 8, item => item),
                Scalar = filter.Scalar, Operator = filter.Operator,
                Value = scalar.ValueKind == JsonValueKind.Undefined ? default : scalar.Clone()
            };
        }
        var graph = value.Graph;
        return new CqlV1Request
        {
            Provider = value.Provider, OrderBy = value.OrderBy, Descending = value.Descending,
            Skip = value.Skip, Take = value.Take, IncludeMetadata = value.IncludeMetadata,
            MaximumDurationMilliseconds = value.MaximumDurationMilliseconds,
            Filters = Array(value.Filters, 64, Filter), AnyOf = Array(value.AnyOf, 64, Filter),
            AnyChildren = Array(value.AnyChildren, 8, group => group == null
                ? throw new ArgumentException("Missing child group.")
                : new CqlV1ChildExists { Filters = Array(group.Filters, 64, Filter), AnyOf = Array(group.AnyOf, 64, Filter) }),
            AnyDescendants = Array(value.AnyDescendants, 8, group => group == null
                ? throw new ArgumentException("Missing descendant group.")
                : new CqlV1DescendantExists { MaximumDepth = group.MaximumDepth, MaximumNodes = group.MaximumNodes,
                    Filters = Array(group.Filters, 64, Filter), AnyOf = Array(group.AnyOf, 64, Filter) }),
            Graph = graph == null ? null : new CqlV1Graph { Recursive = graph.Recursive, MaxChildDepth = graph.MaxChildDepth,
                ChildType = graph.ChildType, MaximumNodes = graph.MaximumNodes,
                MaximumPayloadBytes = graph.MaximumPayloadBytes, Consistency = graph.Consistency }
        };
    }

    // Scalar windows deliberately retain null: omitted Take means the complete
    // eligible source, whereas zero means an empty source window.
    internal static CqlScalarV1Request Capture(CqlScalarV1Request value)
    {
        if (value == null) throw new ArgumentException("A scalar request is required.");
        var operation = value.Operation;
        var take = value.Take;
        var selection = value.Selection;
        var copiedSelection = selection == null ? null : new CqlScalarV1Selection
        {
            Field = selection.Field, ValueType = selection.ValueType, Nullable = selection.Nullable,
            MetadataPath = selection.MetadataPath == null ? null : Array(selection.MetadataPath, 8, item => item)
        };
        var source = Capture(new CqlV1Request
        {
            Provider = value.Provider, Filters = value.Filters, AnyOf = value.AnyOf,
            AnyChildren = value.AnyChildren, AnyDescendants = value.AnyDescendants,
            Graph = value.Graph, OrderBy = value.OrderBy, Descending = value.Descending,
            Skip = value.Skip, Take = take ?? 0
        });
        return new CqlScalarV1Request
        {
            Operation = operation, Selection = copiedSelection, Provider = source.Provider,
            Filters = source.Filters, AnyOf = source.AnyOf, AnyChildren = source.AnyChildren,
            AnyDescendants = source.AnyDescendants, Graph = source.Graph,
            OrderBy = source.OrderBy, Descending = source.Descending, Skip = source.Skip, Take = take
        };
    }

    internal static CqlProjectionV1Column[] CaptureColumns(CqlProjectionV1Column[] columns)
    {
        if(columns==null)return null;
        if(columns.Length==0)throw new ArgumentException("Selected indexed intent requires at least one column.");
        return Array(columns,32,column=>column==null?throw new ArgumentException("Missing selected column."):
            new CqlProjectionV1Column{Alias=column.Alias,Field=column.Field,ValueType=column.ValueType,Nullable=column.Nullable,
                MetadataPath=column.MetadataPath==null?null:Array(column.MetadataPath,8,item=>item)});
    }

    private static TOutput[] Array<TInput, TOutput>(TInput[] values, int maximum, Func<TInput, TOutput> copy)
    {
        if (values == null || values.Length > maximum) throw new ArgumentException("Request collection capture is incomplete or oversized.");
        // Capture references once, then copy every nested mutable descriptor.
        return ((TInput[])values.Clone()).Select(copy).ToArray();
    }
}

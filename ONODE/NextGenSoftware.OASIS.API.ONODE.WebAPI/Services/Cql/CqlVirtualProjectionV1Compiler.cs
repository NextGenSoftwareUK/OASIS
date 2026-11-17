using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Compiler
{
    internal static CqlVirtualProjection CompileVirtualProjection(CqlProjectionV1Request request)
    {
        if (request == null || request.OrderBy != "Id" || request.SelectedOrder != null ||
            request.Skip is < 0 or >= CqlVirtualProjection.MaximumRows * 2 ||
            request.Take is < 1 or > CqlVirtualProjection.MaximumRows || request.Graph != null ||
            request.AnyOf is not { Length: 0 } || request.AnyChildren is not { Length: 0 } ||
            request.AnyDescendants is not { Length: 0 } ||
            request.Filters is not { Length: 1 } || request.Columns is not { Length: >= 2 and <= 32 })
            throw new ArgumentException("Mixed projection requires native equality, Id ordering and a bounded result.");

        var filter = request.Filters[0];
        if (filter == null || filter.Field != null || filter.MetadataPath is not { Length: 1 } ||
            filter.Operator != CqlV1Operator.Equal)
            throw new ArgumentException("Mixed projection requires equality on one scalar metadata field.");
        RequireVirtualField(filter.MetadataPath[0]);

        // These are the exact domains supported by both the CQL metadata predicate
        // and the first native virtual reader.
        var matchType = filter.Scalar switch
        {
            CqlV1Scalar.String => typeof(string),
            CqlV1Scalar.Boolean => typeof(bool),
            CqlV1Scalar.Integer => typeof(long),
            _ => throw new ArgumentException("This virtual source supports String, Boolean and Int64 equality.")
        };

        if (request.Columns[0] is not { Field: "Id", MetadataPath: null })
            throw new ArgumentException("Select Id first so both representations can share one logical identity.");
        var columns = request.Columns.Skip(1).Select(column =>
        {
            if (column == null || column.Field != null || column.MetadataPath is not { Length: 1 })
                throw new ArgumentException("Virtual projections require a direct mapped metadata field.");
            RequireVirtualField(column.MetadataPath[0]);
            var type = column.ValueType switch
            {
                "String" => typeof(string),
                "Boolean" => typeof(bool),
                "Int32" => typeof(int),
                "Int64" => typeof(long),
                _ => throw new ArgumentException("This virtual source supports String, Boolean, Int32 and Int64 projections.")
            };
            return new CqlVirtualColumn(column.MetadataPath[0], type, type == typeof(string) || column.Nullable == true);
        }).ToArray();
        return new CqlVirtualProjection(filter.MetadataPath[0], Literal(filter.Value, matchType).Value, columns);
    }

    private static void RequireVirtualField(string field)
    {
        if (string.IsNullOrEmpty(field) || field.Length > 64 || field.Any(char.IsControl) ||
            field.StartsWith("oasis.native.", StringComparison.OrdinalIgnoreCase) ||
            field.StartsWith("oasis.virtual.", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A direct non-reserved mapped metadata name is required.");
    }
}

internal sealed record CqlVirtualColumn(string Field, Type Type, bool Nullable);

internal sealed record CqlVirtualProjection(string MatchField, object MatchValue, CqlVirtualColumn[] Columns)
{
    // A full branch at this limit cannot prove absence of further rows.
    internal const int MaximumRows = 100;
    internal string[] ProjectedFields => Columns.Select(column => column.Field).Distinct(StringComparer.Ordinal).ToArray();

    internal object[] Select(VirtualHolonRead row)
    {
        var values = new object[Columns.Length + 1];
        values[0] = row.Id;
        for (var index = 0; index < Columns.Length; index++)
        {
            var column = Columns[index];
            row.MetaData.TryGetValue(column.Field, out var value);
            if (value == null ? !column.Nullable : value.GetType() != column.Type)
                throw new ArgumentException("The source value does not match the selected CQL domain.");
            values[index + 1] = value;
        }
        return values;
    }
}

internal interface ICqlVirtualMappingReader
{
    Task<OASISResult<IReadOnlyList<VirtualHolonRead>>> QueryVirtualMappingAsync(
        Guid mappingId, IAvatar avatar, CqlVirtualProjection projection, CancellationToken cancellationToken);
}

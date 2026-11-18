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
            request.Columns is not { Length: >= 2 and <= 32 })
            throw new ArgumentException("Mixed projection requires supported native predicates, Id ordering and a bounded result.");

        var matches = CompileVirtualMatches(request.Filters);

        if (request.Columns[0] is not { Field: "Id", MetadataPath: null })
            throw new ArgumentException("Select Id first so both representations can share one logical identity.");

        if (request.Columns[0].Function != null || request.Columns[0].ValueType != null || request.Columns[0].Nullable.HasValue)
            throw new ArgumentException("Logical identity retains its canonical Guid domain.");
        ValidateSelectionAlias(request.Columns[0].Alias);
        var columns = request.Columns.Skip(1).Select(column =>
        {
            if (column == null || column.Field != null || column.MetadataPath is not { Length: 1 })
                throw new ArgumentException("Virtual projections require a direct mapped metadata field.");
            RequireVirtualField(column.MetadataPath[0]);
            ValidateSelectionAlias(column.Alias);
            var type = column.ValueType switch
            {
                "String" => typeof(string),
                "Boolean" => typeof(bool),
                "Int32" => typeof(int),
                "Int64" => typeof(long),
                _ => throw new ArgumentException("This virtual source supports String, Boolean, Int32 and Int64 projections.")
            };
            var function = column.Function switch
            {
                null => NativeRecordFunction.None,
                CqlV1Function.Utf16Length when type == typeof(string) => NativeRecordFunction.Utf16Length,
                _ => throw new ArgumentException("Mapped LENGTH requires a declared String input.")
            };
            // The DTO declares its input domain; function output is independently Int32.
            var nullable = function == NativeRecordFunction.None && type == typeof(string) || column.Nullable == true;
            if (function == NativeRecordFunction.None && type == typeof(string) && column.Nullable == false)
                throw new ArgumentException("String projection retains its nullable source contract.");
            return new NativeProjectionColumn(column.MetadataPath[0], column.Alias, type, nullable, function);
        }).ToArray();
        NativeProjectionColumn.Capture(columns);
        if (columns.Any(column => column.Alias == request.Columns[0].Alias))
            throw new ArgumentException("Result aliases must be unique, including logical identity.");
        return new CqlVirtualProjection(matches, columns);

    }

    internal static NativeRecordFilter[] CompileVirtualMatches(CqlV1Filter[] filters)
    {
        if (filters is not { Length: >= 1 and <= 8 })
            throw new ArgumentException("Mixed projection supports one to eight typed metadata filters.");
        return filters.Select(filter =>
        {
            if (filter == null || filter.Field != null || filter.MetadataPath is not { Length: 1 })
                throw new ArgumentException("Mixed projection requires a direct mapped scalar metadata field.");
            RequireVirtualField(filter.MetadataPath[0]);
            if (filter.Operator is CqlV1Operator.Contains or CqlV1Operator.StartsWith or CqlV1Operator.EndsWith
                or CqlV1Operator.NotContains or CqlV1Operator.NotStartsWith or CqlV1Operator.NotEndsWith)
            {
                if (filter.Function != null || filter.Scalar != CqlV1Scalar.String)
                    throw new ArgumentException("Virtual string matches require one declared string input.");
                var method = filter.Operator switch
                {
                    CqlV1Operator.Contains or CqlV1Operator.NotContains => NativeRecordFunction.Contains,
                    CqlV1Operator.StartsWith or CqlV1Operator.NotStartsWith => NativeRecordFunction.StartsWith,
                    _ => NativeRecordFunction.EndsWith
                };
                var comparison = filter.Operator is CqlV1Operator.NotContains or CqlV1Operator.NotStartsWith or CqlV1Operator.NotEndsWith
                    ? NextGenSoftware.OASIS.API.Core.Queries.CosmicQueryComparisonOperator.NotEqual
                    : NextGenSoftware.OASIS.API.Core.Queries.CosmicQueryComparisonOperator.Equal;
                return new NativeRecordFilter(filter.MetadataPath[0], comparison,
                    Literal(filter.Value, typeof(string)).Value, method);
            }
            var operation = filter.Operator switch
            {
                CqlV1Operator.Equal => NextGenSoftware.OASIS.API.Core.Queries.CosmicQueryComparisonOperator.Equal,
                CqlV1Operator.NotEqual => NextGenSoftware.OASIS.API.Core.Queries.CosmicQueryComparisonOperator.NotEqual,
                CqlV1Operator.GreaterThan => NextGenSoftware.OASIS.API.Core.Queries.CosmicQueryComparisonOperator.GreaterThan,
                CqlV1Operator.GreaterThanOrEqual => NextGenSoftware.OASIS.API.Core.Queries.CosmicQueryComparisonOperator.GreaterThanOrEqual,
                CqlV1Operator.LessThan => NextGenSoftware.OASIS.API.Core.Queries.CosmicQueryComparisonOperator.LessThan,
                CqlV1Operator.LessThanOrEqual => NextGenSoftware.OASIS.API.Core.Queries.CosmicQueryComparisonOperator.LessThanOrEqual,
                _ => throw new ArgumentException("Use an admitted scalar comparison with this virtual mapping.")
            };
            if (filter.Function != null)
            {
                if (filter.Function != CqlV1Function.Utf16Length || filter.Scalar != CqlV1Scalar.String)
                    throw new ArgumentException("Virtual LENGTH requires a string input.");
                return new NativeRecordFilter(filter.MetadataPath[0], operation,
                    Literal(filter.Value, typeof(long)).Value, NativeRecordFunction.Utf16Length);
            }
            var matchType = filter.Scalar switch
            {
                CqlV1Scalar.String => typeof(string),
                CqlV1Scalar.Boolean => typeof(bool),
                CqlV1Scalar.Integer => typeof(long),
                _ => throw new ArgumentException("This virtual source supports String, Boolean and Int64 scalar predicates.")
            };
            return new NativeRecordFilter(filter.MetadataPath[0], operation, Literal(filter.Value, matchType).Value);
        }).ToArray();
    }

    internal static bool IsVirtualField(string field) =>
        !string.IsNullOrWhiteSpace(field) && field.Length <= 64 && !field.Any(char.IsControl) &&
        !field.StartsWith("oasis.native.", StringComparison.OrdinalIgnoreCase) &&
        !field.StartsWith("oasis.virtual.", StringComparison.OrdinalIgnoreCase);

    private static void RequireVirtualField(string field)
    {
        if (!IsVirtualField(field))
            throw new ArgumentException("A direct non-reserved mapped metadata name is required.");
    }
}


internal sealed record CqlVirtualProjection(NativeRecordFilter[] Matches, NativeProjectionColumn[] Columns)
{
    // A full branch at this limit cannot prove absence of further rows.
    internal const int MaximumRows = 100;
    internal string[] ProjectedFields => Columns.Select(column => column.Field).Distinct(StringComparer.Ordinal).ToArray();

    internal object[] Select(VirtualHolonProjectionRead row)
    {
        if (row == null || row.Id == Guid.Empty || row.Columns.Count != Columns.Length || row.Cells.Count != Columns.Length)
            throw new ArgumentException("The mapped result shape differs from its admitted projection.");
        var values = new object[Columns.Length + 1];
        values[0] = row.Id;
        for (var index = 0; index < Columns.Length; index++)
        {
            var column = Columns[index]; var observed = row.Columns[index]; var cell = row.Cells[index];
            if (column.Field != observed.Field || column.Alias != observed.Alias ||
                column.InputType != observed.InputType || column.ValueType != observed.ValueType ||
                column.Function != observed.Function || column.Nullable != observed.Nullable ||
                (cell.State == NativeProjectionValueState.Present ? cell.Value.GetType() != column.ValueType : !column.Nullable))
                throw new ArgumentException("The mapped result schema differs from its admitted projection.");
            values[index + 1] = cell.Value;
        }
        return values;
    }
}

internal interface ICqlVirtualMappingReader
{
    Task<OASISResult<IReadOnlyList<VirtualHolonProjectionRead>>> QueryVirtualMappingAsync(
        Guid mappingId, IAvatar avatar, CqlVirtualProjection projection, CancellationToken cancellationToken);
}

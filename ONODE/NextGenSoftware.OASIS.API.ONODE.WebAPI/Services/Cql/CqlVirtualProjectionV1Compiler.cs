using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Compiler
{
    internal static CqlVirtualProjection CompileVirtualProjection(CqlProjectionV1Request request)
    {
        if (request == null || request.Where != null || (request.SelectedOrder == null ? request.OrderBy != "Id"
                : request.OrderBy != "None" || request.Descending) ||
            request.Skip is < 0 or >= CqlVirtualProjection.MaximumRows * 2 ||
            request.Take is < 1 or > CqlVirtualProjection.MaximumRows || request.Graph != null ||
            request.AnyOf is not { Length: 0 } || request.AnyChildren is not { Length: 0 } ||
            request.AnyDescendants is not { Length: 0 } ||
            request.Columns is not { Length: >= 1 and <= 32 })
            throw new ArgumentException("Mixed projection requires supported native predicates, selected ordering and a bounded result.");

        if ((request.ResultOperation == CqlProjectionV1ResultOperation.Group) != (request.Grouping != null))
            throw new ArgumentException("Only mixed grouping accepts a grouping description.");
        if (!Enum.IsDefined(typeof(CqlProjectionV1ResultOperation), request.ResultOperation) ||
            request.ResultOperation != CqlProjectionV1ResultOperation.Rows &&
                (request.ResultOperation != CqlProjectionV1ResultOperation.Group &&
                    request.Columns.Length != (request.ResultOperation == CqlProjectionV1ResultOperation.LongCount ? 1 : 2) ||
                 request.OrderBy != "Id" || request.Descending || request.SelectedOrder != null ||
                 request.Skip != 0 || request.Take != 1))
            throw new ArgumentException("Mixed aggregates require canonical Id, an admitted value and one unpaged scalar result.");

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
                "Guid" => typeof(Guid),
                "DateTime" => typeof(DateTime),
                "Int32" => typeof(int),
                "Int64" => typeof(long),
                "Double" => typeof(double),
                _ => throw new ArgumentException("This virtual source supports String, Boolean, Guid, UTC DateTime, Int32, Int64 and finite Double projections.")
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
        if (request.ResultOperation is CqlProjectionV1ResultOperation.Min or CqlProjectionV1ResultOperation.Max
            or CqlProjectionV1ResultOperation.Sum or CqlProjectionV1ResultOperation.Average)
            if (columns.Length != 1 || columns[0].ValueType != typeof(int) &&
                columns[0].ValueType != typeof(long) && columns[0].ValueType != typeof(double) &&
                !(columns[0].ValueType == typeof(string) && request.ResultOperation is
                    CqlProjectionV1ResultOperation.Min or CqlProjectionV1ResultOperation.Max))
                throw new ArgumentException("Mapped aggregation requires numeric output, or String for Min/Max.");
        if (columns.Any(column => column.Alias == request.Columns[0].Alias))
            throw new ArgumentException("Result aliases must be unique, including logical identity.");
        var orderings = CosmicMixedProjectionOrdering.Capture(
            request.SelectedOrder == null
                ? new[] { new CosmicMixedProjectionOrdering(request.Columns[0].Alias, request.Descending) }
                : request.SelectedOrder.Select(term => term == null ? null
                    : new CosmicMixedProjectionOrdering(term.Alias, term.Descending)).ToArray(),
            request.Columns.Select(column => column.Alias).ToArray());
        return new CqlVirtualProjection(matches, columns, orderings);

    }

    internal static NativeRecordFilter[] CompileVirtualMatches(CqlV1Filter[] filters)
    {
        if (filters is not { Length: <= 8 })
            throw new ArgumentException("Mixed projection supports zero to eight typed metadata filters within its bounded source.");
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
                CqlV1Scalar.Guid => typeof(Guid),
                CqlV1Scalar.DateTime => typeof(DateTime),
                CqlV1Scalar.Integer => typeof(long),
                CqlV1Scalar.Int32 => typeof(int),
                CqlV1Scalar.Double => typeof(double),
                _ => throw new ArgumentException("This virtual source supports String, Boolean, Guid, UTC DateTime, Int32, Int64 and finite Double scalar predicates.")
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


internal sealed record CqlVirtualProjection(NativeRecordFilter[] Matches, NativeProjectionColumn[] Columns,
    IReadOnlyList<CosmicMixedProjectionOrdering> Orderings)
{
    // A full branch at this limit cannot prove absence of further rows.
    internal const int MaximumRows = CosmicMixedProjectionResults.MaximumBranchRows;
    internal string[] ProjectedFields => Columns.Select(column => column.Field).Distinct(StringComparer.Ordinal).ToArray();

    internal object[] Select(VirtualHolonProjectionRead row)
        => CosmicMixedProjectionResults.CaptureVirtualRow(Columns, row);
}

internal interface ICqlVirtualMappingReader
{
    Task<OASISResult<IReadOnlyList<VirtualHolonProjectionRead>>> QueryVirtualMappingAsync(
        Guid mappingId, IAvatar avatar, CqlVirtualProjection projection, CancellationToken cancellationToken);
}

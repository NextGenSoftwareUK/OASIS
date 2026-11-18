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
    public static CosmicProjectionQuery<Holon, CosmicProjectionRecord> CompileProjection(
        CqlProjectionV1Request request, Guid actor, ProviderType? provider)
        =>CompileProjection(request,actor,provider,null);

    public static CosmicProjectionQuery<Holon, CosmicProjectionRecord> CompileProjection(
        CqlProjectionV1Request request, Guid actor, ProviderType? provider,CosmicQueryReadAccess access)
    {
        if (request?.VirtualMappingId.HasValue == true || request?.Grouping != null || request?.ResultOperation != CqlProjectionV1ResultOperation.Rows)
            throw new ArgumentException("A virtual mapping requires the direct mixed projection route.");
        return CompileProjection(request, actor, provider, access, null);
    }

    internal static CosmicProjectionQuery<Holon, CosmicProjectionRecord> CompileProjection(
        CqlProjectionV1Request request, Guid actor, ProviderType? provider, CosmicQueryReadAccess access, int? branchLimit)
    {
        if (request == null || request.Take is < 0 or > 100 || request.Columns == null
            || request.Columns.Length is < 1 or > CosmicProjectionQuery<Holon, CosmicProjectionRecord>.MaximumColumns)
            throw new ArgumentException("Projection requires one to32 columns and Take0–100.");
        var order = branchLimit.HasValue ? null : request.SelectedOrder;
        if (order != null && (order.Length is < 1 or > CosmicQuery<Holon>.MaximumOrderings
            || request.OrderBy != "None" || request.Descending || request.Skip is < 0 or > 1000))
            throw new ArgumentException("SelectedOrder requires one to eight aliases, OrderBy None and Skip 0–1000.");

        var source = CompileSource(actor, provider, request.Filters, request.AnyOf, request.AnyChildren,
            request.AnyDescendants, request.Graph, branchLimit.HasValue ? "Id" : order == null ? request.OrderBy : "Id",
            branchLimit == null && request.Descending,
            order == null && branchLimit == null ? request.Skip : 0, order == null ? branchLimit ?? request.Take : null, access,
            includeCloudReadProfile: true);
        var columns = request.Columns.Select(ProjectionColumn).ToArray();
        if (order != null && order.Any(term => request.Columns.Any(column =>
            column.Alias == term?.Alias && column.Function.HasValue)))
        {
            if (branchLimit.HasValue)
                throw new ArgumentException("Computed ordering requires the stored composition route.");
            var captured = source.SelectColumns(columns);
            if (captured.IsError) throw new ArgumentException(captured.Message);
            var composed = captured.Result;
            var seenComputed = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < order.Length; index++)
            {
                var term = order[index];
                if (term?.Alias == null || !seenComputed.Add(term.Alias))
                    throw new ArgumentException("Selected ordering aliases must be non-null and unique.");
                var column = composed.Columns.SingleOrDefault(item => item.Alias == term.Alias)
                    ?? throw new ArgumentException("Selected ordering must name an exact selected alias.");
                composed = BindSelectedOrdering(column.ValueType, column.Alias, term.Descending, index > 0,
                    new CompositionOrderingConsumer(composed));
            }
            // Preserve complete source participation; apply paging only after the computed sort.
            // The composition executor rejects an input-budget overflow, rather than sorting a prefix.
            return RequiredComposition(composed.Skip(request.Skip).Take(request.Take));
        }
        if (order != null)
        {
            // Bind selected aliases to source fields before any paging or projection is applied.
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < order.Length; i++)
            {
                var term = order[i];
                if (term?.Alias == null || !seen.Add(term.Alias))
                    throw new ArgumentException("Selected ordering aliases must be non-null and unique.");
                var column = request.Columns.FirstOrDefault(item => item.Alias == term.Alias)
                    ?? throw new ArgumentException("Selected ordering must name an exact selected alias.");
                source = CompileSelection(column, new SourceOrderingConsumer(source, term.Descending, i > 0));
            }
            var page = source.Skip(request.Skip).Take(request.Take);
            if (page.IsError) throw new ArgumentException(page.Message);
            source = page.Result;
        }
        var selected = source.SelectColumns(columns);
        if (selected.IsError) throw new ArgumentException(selected.Message);
        return selected.Result;
    }

    internal static CosmicProjectionQuery<Holon,CosmicProjectionRecord> CompileIndexedProjection(
        CosmicQuery<Holon> source,CqlProjectionV1Column[] columns)
    {
        if(columns==null||columns.Length is <1 or >32)throw new ArgumentException("Select one to32 indexed columns.");
        var selected=source.SelectColumns(columns.Select(ProjectionColumn).ToArray());
        if(selected.IsError)throw new ArgumentException(selected.Message);
        return selected.Result;
    }

    internal interface ISelectionConsumer<TResult>
    {
        TResult Bind<TValue>(string alias, Expression<Func<Holon, TValue>> selector);
    }

    private sealed class SourceOrderingConsumer(CosmicQuery<Holon> source, bool descending, bool append)
        : ISelectionConsumer<CosmicQuery<Holon>>
    {
        public CosmicQuery<Holon> Bind<TValue>(string alias, Expression<Func<Holon, TValue>> selector)
        {
            var ordered = append
                ? (descending ? source.ThenByDescending(selector) : source.ThenBy(selector))
                : (descending ? source.OrderByDescending(selector) : source.OrderBy(selector));
            if (ordered.IsError) throw new ArgumentException(ordered.Message);
            return ordered.Result;
        }
    }

    private sealed class LengthSelectionConsumer<TResult>(ISelectionConsumer<TResult> consumer, bool nullable)
        : ISelectionConsumer<TResult>
    {
        public TResult Bind<TValue>(string alias, Expression<Func<Holon, TValue>> selector)
        {
            if (typeof(TValue) != typeof(string))
                throw new ArgumentException("Selected LENGTH requires an exact string input.");
            Expression length = Expression.Property(selector.Body, nameof(string.Length));
            if (nullable)
                return consumer.Bind(alias, Expression.Lambda<Func<Holon, int?>>(
                    Expression.Convert(length, typeof(int?)), selector.Parameters));
            return consumer.Bind(alias, Expression.Lambda<Func<Holon, int>>(length, selector.Parameters));
        }
    }

    private sealed class ProjectionSelectionConsumer : ISelectionConsumer<CosmicProjectionSelection<Holon>>
    {
        public CosmicProjectionSelection<Holon> Bind<TValue>(string alias, Expression<Func<Holon, TValue>> selector)
            => Required(CosmicProjectionSelection<Holon>.Create(alias, selector));
    }

    private static CosmicProjectionSelection<Holon> ProjectionColumn(CqlProjectionV1Column column)
        => CompileSelection(column, new ProjectionSelectionConsumer());

    // HTTP selection shapes bind one bounded typed expression. No delegate is compiled
    // or invoked; the Core selector compiler remains the path authority.
    internal static TResult CompileSelection<TResult>(CqlProjectionV1Column column, ISelectionConsumer<TResult> consumer)
    {
        if (column == null) throw new ArgumentException("A column is required.");
        ValidateSelectionAlias(column.Alias);
        var alias = column.Alias;
        if (column.Function.HasValue)
        {
            if (column.Function != CqlV1Function.Utf16Length)
                throw new ArgumentException("Unsupported selected function.");
            var input = new CqlProjectionV1Column
            {
                Alias = column.Alias, Field = column.Field, MetadataPath = column.MetadataPath,
                ValueType = column.ValueType, Nullable = column.MetadataPath == null ? null : true
            };
            return CompileSelection(input, new LengthSelectionConsumer<TResult>(consumer, column.Nullable == true));
        }

        if (column.MetadataPath == null)
        {
            if (column.ValueType != null || column.Nullable.HasValue)
                throw new ArgumentException("Canonical column types are inferred and cannot be overridden.");
            return column.Field switch
            {
                "Id" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.Id)),
                "Name" => consumer.Bind(alias, (Expression<Func<Holon, string>>)(h => h.Name)),
                "Description" => consumer.Bind(alias, (Expression<Func<Holon, string>>)(h => h.Description)),
                "Version" => consumer.Bind(alias, (Expression<Func<Holon, int>>)(h => h.Version)),
                "HolonType" => consumer.Bind(alias, (Expression<Func<Holon, HolonType>>)(h => h.HolonType)),
                "IsActive" => consumer.Bind(alias, (Expression<Func<Holon, bool>>)(h => h.IsActive)),
                "IsPublic" => consumer.Bind(alias, (Expression<Func<Holon, bool>>)(h => h.IsPublic)),
                "IsDeleted" => consumer.Bind(alias, (Expression<Func<Holon, bool>>)(h => h.IsDeleted)),
                "ParentHolonId" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.ParentHolonId)),
                "VersionId" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.VersionId)),
                "PreviousVersionId" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.PreviousVersionId)),
                "CreatedByAvatarId" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.CreatedByAvatarId)),
                "ModifiedByAvatarId" => consumer.Bind(alias, (Expression<Func<Holon, Guid>>)(h => h.ModifiedByAvatarId)),
                _ => throw new ArgumentException("Unsupported canonical projection field.")
            };
        }
        if (column.Field != null || column.MetadataPath.Length is < 1 or > 4
            || column.MetadataPath.Any(segment => string.IsNullOrEmpty(segment) || segment.Length > 64 || segment.Any(char.IsControl)))
            throw new ArgumentException("Select exactly one field or a metadata path of one to4 bounded segments.");
        var path = (string[])column.MetadataPath.Clone();
        if (column.ValueType == "String")
        {
            if (column.Nullable == false) throw new ArgumentException("String projection retains the nullable string contract.");
            return MetadataSelection<string, TResult>(alias, path, consumer);
        }
        var nullable = column.Nullable == true;
        return column.ValueType switch
        {
            "Boolean" => MetadataSelection<bool, TResult>(alias, path, nullable, consumer),
            "Int32" => MetadataSelection<int, TResult>(alias, path, nullable, consumer),
            "Int64" => MetadataSelection<long, TResult>(alias, path, nullable, consumer),
            "UInt64" => MetadataSelection<ulong, TResult>(alias, path, nullable, consumer),
            "Single" => MetadataSelection<float, TResult>(alias, path, nullable, consumer),
            "Double" => MetadataSelection<double, TResult>(alias, path, nullable, consumer),
            "Decimal" => MetadataSelection<decimal, TResult>(alias, path, nullable, consumer),
            "Byte" => MetadataSelection<byte, TResult>(alias, path, nullable, consumer),
            "SByte" => MetadataSelection<sbyte, TResult>(alias, path, nullable, consumer),
            "Int16" => MetadataSelection<short, TResult>(alias, path, nullable, consumer),
            "UInt16" => MetadataSelection<ushort, TResult>(alias, path, nullable, consumer),
            "UInt32" => MetadataSelection<uint, TResult>(alias, path, nullable, consumer),
            "Guid" => MetadataSelection<Guid, TResult>(alias, path, nullable, consumer),
            "DateTime" => MetadataSelection<DateTime, TResult>(alias, path, nullable, consumer),
            "HolonType" => MetadataSelection<HolonType, TResult>(alias, path, nullable, consumer),
            "ProviderType" => MetadataSelection<ProviderType, TResult>(alias, path, nullable, consumer),
            _ => throw new ArgumentException("Unsupported exact metadata projection type.")
        };
    }

    internal static void ValidateSelectionAlias(string alias)
    {
        if (string.IsNullOrEmpty(alias) || alias.Length > 64
            || !AsciiLetter(alias[0]) || alias.Any(c => !AsciiLetter(c) && c is not (>= '0' and <= '9') && c != '_'))
            throw new ArgumentException("Column alias requires one to64 ASCII letters/digits/underscores, starting with a letter.");
    }

    private static bool AsciiLetter(char value) => value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    private static TResult MetadataSelection<TValue, TResult>(string alias, string[] segments, bool nullable,
        ISelectionConsumer<TResult> consumer) where TValue : struct
        => nullable ? MetadataSelection<TValue?, TResult>(alias, segments, consumer)
            : MetadataSelection<TValue, TResult>(alias, segments, consumer);

    private static TResult MetadataSelection<TValue, TResult>(string alias, string[] segments, ISelectionConsumer<TResult> consumer)
    {
        var parameter = Expression.Parameter(typeof(Holon), "holon");
        var path = Expression.NewArrayInit(typeof(string), segments.Select(segment => Expression.Constant(segment)));
        var value = Expression.Call(typeof(CosmicQueryMetadata), "Value", new[] { typeof(TValue) }, parameter, path);
        return consumer.Bind(alias, Expression.Lambda<Func<Holon, TValue>>(value, parameter));
    }
    private static CosmicProjectionSelection<Holon> Required(OASISResult<CosmicProjectionSelection<Holon>> selection)
    {
        if (selection.IsError) throw new ArgumentException(selection.Message);
        return selection.Result;
    }
}
